using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MSTailors.Infrastructure.Persistence;

/// <summary>
/// Initiates a single-node replica set on a local mongod.
/// </summary>
/// <remarks>
/// Checkout uses a MongoDB transaction, and transactions require a replica set.
/// Atlas is one already; a local mongod started with --replSet is a member of a
/// set that does not exist yet until replSetInitiate is issued. The shell is not
/// bundled with the server download, so this does it through the driver.
///
/// Development convenience only — never run against a production cluster.
/// </remarks>
public sealed class ReplicaSetInitializer
{
    private readonly ILogger<ReplicaSetInitializer> _logger;

    public ReplicaSetInitializer(ILogger<ReplicaSetInitializer> logger) => _logger = logger;

    public async Task<bool> InitiateAsync(
        string connectionString, string replicaSetName = "rs0", CancellationToken cancellationToken = default)
    {
        // A direct connection is required: without a set, there is no primary
        // for the driver to discover and normal server selection would hang.
        var builder = new MongoUrlBuilder(connectionString)
        {
            ReplicaSetName = null,
            DirectConnection = true,
            ServerSelectionTimeout = TimeSpan.FromSeconds(10)
        };

        var client = new MongoClient(builder.ToMongoUrl().ToString());
        var admin = client.GetDatabase("admin");

        var host = builder.Servers.First();

        var config = new BsonDocument
        {
            { "_id", replicaSetName },
            { "members", new BsonArray { new BsonDocument { { "_id", 0 }, { "host", $"{host.Host}:{host.Port}" } } } }
        };

        try
        {
            await admin.RunCommandAsync<BsonDocument>(
                new BsonDocument("replSetInitiate", config), cancellationToken: cancellationToken);

            _logger.LogInformation("Replica set '{Name}' initiated on {Host}", replicaSetName, host);
        }
        catch (MongoCommandException ex) when (ex.CodeName == "AlreadyInitialized")
        {
            _logger.LogInformation("Replica set '{Name}' was already initialised", replicaSetName);
        }

        return await WaitForPrimaryAsync(client, cancellationToken);
    }

    private async Task<bool> WaitForPrimaryAsync(IMongoClient client, CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.AddSeconds(60);

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var hello = await client.GetDatabase("admin")
                    .RunCommandAsync<BsonDocument>(new BsonDocument("hello", 1), cancellationToken: cancellationToken);

                if (hello.GetValue("isWritablePrimary", false).ToBoolean())
                {
                    _logger.LogInformation("Node is primary; transactions are available");
                    return true;
                }
            }
            catch (MongoException)
            {
                // Election in progress — keep waiting.
            }

            await Task.Delay(250, cancellationToken);
        }

        _logger.LogError("The node did not become primary within 60 seconds");
        return false;
    }
}
