using MSTailors.Application.Common.Models;

namespace MSTailors.Application.Common.Interfaces;

/// <summary>
/// Pings the database. Implemented in Infrastructure so the Application layer
/// never references MongoDB.Driver.
/// </summary>
public interface IDatabaseHealthProbe
{
    Task<DatabaseHealth> CheckAsync(CancellationToken cancellationToken = default);
}
