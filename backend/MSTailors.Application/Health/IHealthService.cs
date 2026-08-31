namespace MSTailors.Application.Health;

public interface IHealthService
{
    Task<HealthResponse> GetHealthAsync(CancellationToken cancellationToken = default);
}
