namespace MSTailors.Application.Catalog;

public enum ProductSort
{
    Newest,
    PriceAscending,
    PriceDescending,
    NameAscending,
    MostViewed
}

/// <summary>
/// Filters for a shop listing. Every property is optional; an empty query means
/// "all active products".
/// </summary>
public sealed record ProductQuery
{
    public string? BrandId { get; init; }
    public string? CategoryId { get; init; }
    /// <summary>Free-text search across name, description and tags.</summary>
    public string? SearchTerm { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
    public bool? IsFeatured { get; init; }
    public bool? IsNewArrival { get; init; }
    /// <summary>When true, only products with at least one variant in stock.</summary>
    public bool InStockOnly { get; init; }
    public ProductSort Sort { get; init; } = ProductSort.Newest;
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 24;
}
