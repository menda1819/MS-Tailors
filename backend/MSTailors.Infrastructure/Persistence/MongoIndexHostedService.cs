using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MSTailors.Infrastructure.Persistence;

/// <summary>
/// Ensures indexes exist when the application starts.
/// </summary>
/// <remarks>
/// Failure handling is deliberately environment-dependent. In Development a
/// failure throws, so a broken index definition is noticed immediately. In
/// Production it is logged as critical and startup continues: taking the whole
/// site down because Atlas was briefly unreachable at boot trades a large
/// outage for a small one, and the condition is loud in the logs either way.
/// </remarks>
public sealed class MongoIndexHostedService : IHostedService
{
    private readonly MongoIndexInitializer _initializer;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<MongoIndexHostedService> _logger;

    public MongoIndexHostedService(
        MongoIndexInitializer initializer,
        IHostEnvironment environment,
        ILogger<MongoIndexHostedService> logger)
    {
        _initializer = initializer;
        _environment = environment;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _initializer.CreateIndexesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to create MongoDB indexes");

            if (_environment.IsDevelopment())
            {
                throw;
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
