using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MSTailors.Application.Auth.Dtos;
using MSTailors.Domain.Enums;
using MSTailors.Infrastructure.Persistence;
using MSTailors.Tests.Fixtures;

namespace MSTailors.Tests.Integration;

[Collection(MongoCollection.Name)]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    private readonly MongoServerFixture _server;
    private readonly string _databaseName;

    protected ApiFactory Factory { get; private set; } = null!;
    protected IMongoContext Context => Factory.Context;
    protected string? SkipReason => _server.SkipReason;

    /// <summary>Mirrors the API's serializer settings, including string enums.</summary>
    protected static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    protected IntegrationTestBase(MongoServerFixture server)
    {
        _server = server;
        _databaseName = $"mstailors_it_{GetType().Name.ToLowerInvariant()}";
    }

    protected void RequireMongo() => Assert.True(SkipReason is null, SkipReason);

    public virtual Task InitializeAsync()
    {
        if (_server.SkipReason is null)
        {
            Factory = new ApiFactory(_server.ConnectionString, _databaseName);
        }

        return Task.CompletedTask;
    }

    public virtual async Task DisposeAsync()
    {
        if (_server.SkipReason is null)
        {
            await Factory.DisposeAsync();
            await _server.DropDatabaseAsync(_databaseName);
        }
    }

    /// <summary>A client carrying a real access token for a freshly created account.</summary>
    protected async Task<HttpClient> CreateAuthenticatedClientAsync(string email, UserRole role)
    {
        const string password = "integration-test-password";
        await Factory.CreateUserAsync(email, password, role);

        var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = email, Password = password });

        response.EnsureSuccessStatusCode();

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(Json);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        return client;
    }
}
