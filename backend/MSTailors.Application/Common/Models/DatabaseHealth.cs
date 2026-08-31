namespace MSTailors.Application.Common.Models;

/// <summary>Outcome of a single database connectivity probe.</summary>
public sealed record DatabaseHealth(
    bool IsConnected,
    string DatabaseName,
    long LatencyMs,
    string? Error);
