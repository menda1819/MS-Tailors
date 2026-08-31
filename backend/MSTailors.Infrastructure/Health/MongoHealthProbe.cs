using System.Diagnostics;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MSTailors.Application.Common.Interfaces;
using MSTailors.Application.Common.Models;
using MSTailors.Infrastructure.Persistence;

namespace MSTailors.Infrastructure.Health;

/// <summary>
/// Runs the MongoDB <c>{ ping: 1 }</c> admin command and reports round-trip latency.
/// </summary>
public sealed class MongoHealthProbe : IDatabaseHealthProbe
{
    private readonly IMongoContext _context;
    private readonly ILogger<MongoHealthProbe> _logger;

    public MongoHealthProbe(IMongoContext context, ILogger<MongoHealthProbe> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DatabaseHealth> CheckAsync(CancellationToken cancellationToken = default)
    {
        var databaseName = _context.Database.DatabaseNamespace.DatabaseName;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _context.Database.RunCommandAsync<BsonDocument>(
                new BsonDocument("ping", 1), cancellationToken: cancellationToken);

            stopwatch.Stop();
            return new DatabaseHealth(true, databaseName, stopwatch.ElapsedMilliseconds, null);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "MongoDB ping failed for database {DatabaseName}", databaseName);

            // Surface the exception type and message only. A driver exception can
            // carry the full connection string, which must never leave the server.
            return new DatabaseHealth(false, databaseName, stopwatch.ElapsedMilliseconds, ex.GetType().Name);
        }
    }
}
