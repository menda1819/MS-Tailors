using MSTailors.Application.Common.Repositories;
using MSTailors.Domain.Entities;

namespace MSTailors.Application.Carts;

public interface ICouponRepository : IRepository<Coupon>
{
    /// <summary>Case-insensitive: codes are normalised to uppercase on write and lookup.</summary>
    Task<Coupon?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>Atomically increments usedCount, but only while the limit allows it.</summary>
    Task<bool> TryConsumeAsync(string couponId, CancellationToken cancellationToken = default);
}
