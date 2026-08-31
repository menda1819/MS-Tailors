using MSTailors.Application.Common.Pagination;
using MSTailors.Application.Common.Repositories;
using MSTailors.Domain.Entities;

namespace MSTailors.Application.Catalog;

public interface IProductRepository : ISlugRepository<Product>
{
    /// <summary>Filtered, sorted, paged shop listing.</summary>
    Task<PagedResult<Product>> SearchAsync(ProductQuery query, CancellationToken cancellationToken = default);

    Task<Product?> GetByVariantSkuAsync(string sku, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Product>> GetByBrandAsync(string brandId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Product>> GetByIdsAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default);

    Task<bool> IsSkuAvailableAsync(string sku, string? excludeProductId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrements variant stock atomically, and only if enough is on hand.
    /// Returns false when stock is insufficient — the guard against overselling
    /// under concurrent checkouts.
    /// </summary>
    Task<bool> TryDecrementStockAsync(string productId, string sku, int quantity, CancellationToken cancellationToken = default);

    /// <summary>Returns stock after a cancellation or refund.</summary>
    Task<bool> IncrementStockAsync(string productId, string sku, int quantity, CancellationToken cancellationToken = default);

    /// <summary>Fire-and-forget view counter. Does not touch UpdatedAt.</summary>
    Task IncrementViewCountAsync(string productId, CancellationToken cancellationToken = default);
}
