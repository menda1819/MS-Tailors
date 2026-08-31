namespace MSTailors.Application.Health;

/// <summary>Response body for <c>GET /api/health</c>.</summary>
public sealed record HealthResponse(
    string Status,
    string Application,
    string Version,
    string Environment,
    DateTime TimestampUtc,
    DatabaseStatusResponse Database);

public sealed record DatabaseStatusResponse(
    string Status,
    string Name,
    long LatencyMs,
    string? Error);
