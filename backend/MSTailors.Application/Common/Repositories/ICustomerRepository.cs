using MSTailors.Application.Common.Repositories;
using MSTailors.Domain.Entities;

namespace MSTailors.Application.Common.Repositories;

public interface ICustomerRepository : IRepository<Customer>
{
    /// <summary>Case-insensitive: the email is normalised to lowercase on write and lookup.</summary>
    Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> IsEmailAvailableAsync(string email, string? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>Looks a customer up by refresh-token hash, for token rotation.</summary>
    Task<Customer?> GetByRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken = default);
}
