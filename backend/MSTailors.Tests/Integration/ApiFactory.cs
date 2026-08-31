using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MSTailors.Domain.Entities;
using MSTailors.Domain.Enums;
using MSTailors.Infrastructure.Persistence;
using MSTailors.Tests.Fixtures;

namespace MSTailors.Tests.Integration;

/// <summary>
/// Boots the real API against the test mongod. Nothing is stubbed: the same
/// controllers, filters, authentication and authorization run as in production.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    private readonly string _databaseName;

    public const string JwtKey = "integration-test-signing-key-not-a-real-secret";

    public ApiFactory(string connectionString, string databaseName)
    {
        _connectionString = connectionString;
        _databaseName = databaseName;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.UseSetting("MongoDb:ConnectionString", _connectionString);
        builder.UseSetting("MongoDb:DatabaseName", _databaseName);
        builder.UseSetting("Jwt:Key", JwtKey);
        builder.UseSetting("Jwt:Issuer", "https://localhost:5001");
        builder.UseSetting("Jwt:Audience", "mstailors-web");
        builder.UseSetting("Jwt:AccessTokenMinutes", "15");
        builder.UseSetting("Jwt:RefreshTokenDays", "7");
        builder.UseSetting("Cors:AllowedOrigins:0", "http://localhost:3000");

        builder.ConfigureServices(services =>
        {
            // The index initializer runs as a hosted service. Keeping it means
            // the unique indexes the tests rely on actually exist.
            _ = services;
        });
    }

    public IMongoContext Context => Services.GetRequiredService<IMongoContext>();

    /// <summary>Creates an account directly in the database, bypassing registration.</summary>
    public async Task<Customer> CreateUserAsync(string email, string password, UserRole role)
    {
        using var scope = Services.CreateScope();

        var hasher = scope.ServiceProvider
            .GetRequiredService<MSTailors.Application.Auth.IPasswordHashingService>();
        var context = scope.ServiceProvider.GetRequiredService<IMongoContext>();

        var customer = new Customer
        {
            Email = email.ToLowerInvariant(),
            FullName = role == UserRole.Admin ? "Test Admin" : "Test Customer",
            Phone = "0771234567",
            Role = role,
            EmailVerified = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        customer.PasswordHash = hasher.Hash(customer, password);

        await context.Customers.InsertOneAsync(customer);
        return customer;
    }
}
