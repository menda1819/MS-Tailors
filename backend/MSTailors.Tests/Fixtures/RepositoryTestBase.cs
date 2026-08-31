using MSTailors.Infrastructure.Persistence;

namespace MSTailors.Tests.Fixtures;

/// <summary>
/// Base for repository tests. Each test class gets its own database so classes
/// running in sequence cannot see each other's documents.
/// </summary>
[Collection(MongoCollection.Name)]
public abstract class RepositoryTestBase : IAsyncLifetime
{
    private readonly MongoServerFixture _server;
    private readonly string _databaseName;

    protected IMongoContext Context { get; private set; } = null!;

    /// <summary>The replica-set client, for code that needs sessions/transactions.</summary>
    protected MongoDB.Driver.IMongoClient MongoClient => _server.Client;

    /// <summary>Connection string of the test server, for integration hosts.</summary>
    protected string ConnectionString => _server.ConnectionString;

    /// <summary>Database this test class owns.</summary>
    protected string DatabaseName => _databaseName;

    protected RepositoryTestBase(MongoServerFixture server)
    {
        _server = server;
        _databaseName = $"mstailors_test_{GetType().Name.ToLowerInvariant()}";
    }

    /// <summary>Null when mongod is available; a reason string when it is not.</summary>
    protected string? SkipReason => _server.SkipReason;

    /// <summary>
    /// Fails the test with a clear message when mongod is missing, rather than
    /// throwing a NullReferenceException from deep inside the driver.
    /// </summary>
    protected void RequireMongo()
    {
        Assert.True(SkipReason is null, SkipReason);
    }

    public virtual async Task InitializeAsync()
    {
        if (_server.SkipReason is not null)
        {
            return;
        }

        Context = _server.CreateContext(_databaseName);

        // Indexes are part of what is under test: unique constraints only exist
        // if the initializer actually created them.
        var initializer = new MongoIndexInitializer(
            Context, Microsoft.Extensions.Logging.Abstractions.NullLogger<MongoIndexInitializer>.Instance);

        await initializer.CreateIndexesAsync();
    }

    public virtual async Task DisposeAsync()
    {
        if (_server.SkipReason is null)
        {
            await _server.DropDatabaseAsync(_databaseName);
        }
    }
}
