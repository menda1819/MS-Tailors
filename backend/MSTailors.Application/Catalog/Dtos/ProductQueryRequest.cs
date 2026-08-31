using MSTailors.Application.Common.Dtos;

namespace MSTailors.Application.Catalog.Dtos;

/// <summary>Public sort options for the shop listing.</summary>
public enum ProductSortOption
{
    Newest,
    PriceAsc,
    PriceDesc,
    Popular,
    BestSelling
}

/// <summary>
/// Query-string filters for GET /api/products. Brand and category accept a slug
/// or an id, because links in the UI carry slugs while the admin carries ids.
/// </summary>
public sealed class ProductQueryRequest : PageRequest
{
    public string? Brand { get; init; }
    public string? Category { get; init; }
    public string? Size { get; init; }
    public string? Color { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public bool? InStock { get; init; }
    public bool? IsNew { get; init; }
    public bool? IsFeatured { get; init; }
    /// <summary>Free-text search across name, description and tags.</summary>
    public string? Q { get; init; }
    public ProductSortOption Sort { get; init; } = ProductSortOption.Newest;
}
