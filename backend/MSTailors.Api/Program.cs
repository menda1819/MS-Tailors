using System.Reflection;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MSTailors.Api.Filters;
using MSTailors.Infrastructure.Auth;
using MSTailors.Api.Configuration;
using MSTailors.Api.Middleware;
using MSTailors.Application;
using MSTailors.Application.Common.Interfaces;
using MSTailors.Infrastructure;
using MSTailors.Api.Controllers;
using MSTailors.Domain.Enums;
using MSTailors.Infrastructure.Seeding;
using Serilog;

// Bootstrap logger: captures failures that happen before the host is built.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "MSTailors.Api"));

    // ---- Configuration -----------------------------------------------------
    builder.Services
        .AddOptions<CorsSettings>()
        .Bind(builder.Configuration.GetSection(CorsSettings.SectionName));

    // ---- Layers ------------------------------------------------------------
    builder.Services.AddSingleton<IApplicationInfo, ApplicationInfo>();
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // ---- CORS --------------------------------------------------------------
    var corsSettings = builder.Configuration
        .GetSection(CorsSettings.SectionName)
        .Get<CorsSettings>() ?? new CorsSettings();

    builder.Services.AddCors(options =>
    {
        options.AddPolicy(CorsSettings.PolicyName, policy =>
        {
            if (corsSettings.AllowedOrigins.Length == 0)
            {
                // No wildcard fallback. An unconfigured origin list means no
                // cross-origin access, which fails loudly instead of silently
                // opening the API to every site.
                return;
            }

            policy.WithOrigins(corsSettings.AllowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });

    // ---- Authentication and authorization ----------------------------------
    var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
                      ?? new JwtSettings();

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(string.IsNullOrEmpty(jwtSettings.Key)
                        ? new string('0', 32)
                        : jwtSettings.Key)),
                // No grace period on expiry. The default five minutes silently
                // extends every token's life.
                ClockSkew = TimeSpan.Zero
            };
        });

    builder.Services
        .AddAuthorizationBuilder()
        .AddPolicy(AuthorizationPolicies.AdminOnly, policy =>
            policy.RequireAuthenticatedUser().RequireRole(nameof(UserRole.Admin)));

    // ---- Rate limiting -----------------------------------------------------
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.OnRejected = async (context, cancellationToken) =>
        {
            context.HttpContext.Response.ContentType = "application/problem+json";

            await context.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Too many requests",
                Detail = "You have made too many requests. Please wait a moment and try again."
            }, cancellationToken);
        };

        // Partitioned by client IP. Behind a proxy this needs forwarded headers
        // configured, or every caller shares one bucket.
        options.AddPolicy(RateLimitPolicies.Auth, context =>
            RateLimitPartition.GetFixedWindowLimiter(
                PartitionKey(context),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));

        options.AddPolicy(RateLimitPolicies.Inquiries, context =>
            RateLimitPartition.GetFixedWindowLimiter(
                PartitionKey(context),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(10),
                    QueueLimit = 0
                }));

        options.AddPolicy(RateLimitPolicies.Checkout, context =>
            RateLimitPartition.GetFixedWindowLimiter(
                PartitionKey(context),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 15,
                    Window = TimeSpan.FromMinutes(5),
                    QueueLimit = 0
                }));

        static string PartitionKey(HttpContext context) =>
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    });

    // ---- MVC + Swagger -----------------------------------------------------
    builder.Services.AddControllers(options =>
    {
        // Global, so validation cannot be forgotten on a new endpoint.
        options.Filters.Add<ValidationFilter>();
    })
    .AddJsonOptions(options =>
    {
        // Enums travel as readable strings in both directions ("CashOnDelivery",
        // not 0). Numeric ordinals are still accepted on input, but a JSON API
        // whose contract depends on declaration order is a trap for clients.
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "MS Tailors API",
            Version = "v1",
            Description = "Tailoring services and multi-brand shop platform."
        });

        options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Description = "Paste the access token returned by /api/auth/login."
        });

        options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });

        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath);
        }
    });

    var app = builder.Build();

    // ---- Local replica-set bootstrap ---------------------------------------
    // `dotnet run --project MSTailors.Api -- --init-replset` initiates a
    // single-node replica set on a local mongod started with --replSet.
    // Checkout uses transactions, which a standalone mongod cannot serve.
    // Atlas is already a replica set and needs none of this.
    if (args.Contains("--init-replset", StringComparer.OrdinalIgnoreCase))
    {
        using var replicaScope = app.Services.CreateScope();

        var settings = replicaScope.ServiceProvider
            .GetRequiredService<IOptions<MSTailors.Infrastructure.Configuration.MongoDbSettings>>().Value;

        var initialised = await replicaScope.ServiceProvider
            .GetRequiredService<MSTailors.Infrastructure.Persistence.ReplicaSetInitializer>()
            .InitiateAsync(settings.ConnectionString);

        return initialised ? 0 : 1;
    }

    // ---- Seeding -----------------------------------------------------------
    // `dotnet run --project MSTailors.Api -- --seed [--force]` seeds and exits
    // without ever starting the web server. Seeding is never part of normal
    // startup, so a restart in production cannot rewrite live data.
    if (args.Contains("--seed", StringComparer.OrdinalIgnoreCase))
    {
        var force = args.Contains("--force", StringComparer.OrdinalIgnoreCase);

        using var seedScope = app.Services.CreateScope();

        // Indexes first: seeding into a collection without its unique indexes
        // would let duplicate slugs and SKUs land silently.
        await seedScope.ServiceProvider
            .GetRequiredService<MSTailors.Infrastructure.Persistence.MongoIndexInitializer>()
            .CreateIndexesAsync();

        try
        {
            await seedScope.ServiceProvider
                .GetRequiredService<DatabaseSeeder>()
                .SeedAsync(force);
        }
        catch (OptionsValidationException ex)
        {
            // Almost always a missing Seed__AdminPassword. Exit non-zero so a
            // script or CI step treats it as the failure it is.
            Log.Fatal("Seed configuration is invalid: {Errors}", string.Join(" | ", ex.Failures));
            return 1;
        }

        Log.Information("Seeding finished, exiting without starting the web host");
        return 0;
    }

    // ---- Pipeline ----------------------------------------------------------
    // Exception handling is first so it wraps everything downstream.
    app.UseExceptionHandling();

    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "MS Tailors API v1");
            options.DocumentTitle = "MS Tailors API";
        });
    }
    else
    {
        app.UseHsts();
        app.UseHttpsRedirection();
    }

    app.UseCors(CorsSettings.PolicyName);

    app.UseRateLimiter();

    // Order matters: authentication populates the principal, authorization
    // then decides. Reversing these silently allows every [Authorize] endpoint.
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    var startupInfo = app.Services.GetRequiredService<IApplicationInfo>();
    Log.Information(
        "Starting {Application} v{Version} in {Environment}",
        startupInfo.Name, startupInfo.Version, startupInfo.Environment);

    app.Run();

    return 0;
}
catch (OptionsValidationException ex)
{
    // Most common cause: MongoDb__ConnectionString was never set in user secrets.
    Log.Fatal("Configuration is invalid: {Errors}", string.Join(" | ", ex.Failures));
    throw;
}
catch (Exception ex)
{
    Log.Fatal(ex, "MSTailors.Api terminated unexpectedly during startup");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>Exposed so integration tests can use WebApplicationFactory&lt;Program&gt;.</summary>
public partial class Program;
