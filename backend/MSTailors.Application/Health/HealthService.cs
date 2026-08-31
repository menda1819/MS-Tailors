using MSTailors.Application.Common.Interfaces;

namespace MSTailors.Application.Health;

/// <summary>
/// Composes the database probe with build metadata into the health payload.
/// The controller stays thin; this is where the "healthy vs degraded" call is made.
/// </summary>
public sealed class HealthService : IHealthService
{
    private readonly IDatabaseHealthProbe _databaseHealthProbe;
    private readonly IApplicationInfo _applicationInfo;

    public HealthService(IDatabaseHealthProbe databaseHealthProbe, IApplicationInfo applicationInfo)
    {
        _databaseHealthProbe = databaseHealthProbe;
        _applicationInfo = applicationInfo;
    }

    public async Task<HealthResponse> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        var database = await _databaseHealthProbe.CheckAsync(cancellationToken);

        return new HealthResponse(
            Status: database.IsConnected ? "Healthy" : "Degraded",
            Application: _applicationInfo.Name,
            Version: _applicationInfo.Version,
            Environment: _applicationInfo.Environment,
            TimestampUtc: DateTime.UtcNow,
            Database: new DatabaseStatusResponse(
                Status: database.IsConnected ? "Connected" : "Disconnected",
                Name: database.DatabaseName,
                LatencyMs: database.LatencyMs,
                Error: database.Error));
    }
}
