using System.Reflection;
using Microsoft.Extensions.Options;
using MSTailors.Api.Configuration;
using MSTailors.Api.Middleware;
using MSTailors.Application;
using MSTailors.Application.Common.Interfaces;
using MSTailors.Infrastructure;
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

    // ---- MVC + Swagger -----------------------------------------------------
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "MS Tailors API",
            Version = "v1",
            Description = "Tailoring services and multi-brand shop platform."
        });

        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath);
        }
    });

    var app = builder.Build();

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
