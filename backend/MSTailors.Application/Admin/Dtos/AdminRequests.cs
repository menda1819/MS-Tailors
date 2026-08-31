using MSTailors.Domain.Enums;

namespace MSTailors.Application.Admin.Dtos;

public sealed class BrandRequest
{
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string Tagline { get; init; } = string.Empty;
    public string Story { get; init; } = string.Empty;
    public string Philosophy { get; init; } = string.Empty;
    public string TargetAudience { get; init; } = string.Empty;
    public string LogoUrl { get; init; } = string.Empty;
    public string HeroImageUrl { get; init; } = string.Empty;
    public bool IsActive { get; init; } = true;
    public int DisplayOrder { get; init; }
    public string? SeoTitle { get; init; }
    public string? SeoDescription { get; init; }
}

public sealed class CategoryRequest
{
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? ParentCategoryId { get; init; }
    public string ImageUrl { get; init; } = string.Empty;
    public bool IsActive { get; init; } = true;
    public int DisplayOrder { get; init; }
}

public sealed class ProductRequest
{
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string BrandId { get; init; } = string.Empty;
    public IReadOnlyList<string> CategoryIds { get; init; } = [];
    public string Description { get; init; } = string.Empty;
    public string ShortDescription { get; init; } = string.Empty;
    public string Fabric { get; init; } = string.Empty;
    public string CareInstructions { get; init; } = string.Empty;
    public IReadOnlyList<SpecificationRequest> Specifications { get; init; } = [];
    public IReadOnlyList<ProductImageRequest> Images { get; init; } = [];
    public decimal BasePrice { get; init; }
    public decimal? SalePrice { get; init; }
    public string? SizeGuideId { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];
    public bool IsFeatured { get; init; }
    public bool IsNewArrival { get; init; }
    public bool IsActive { get; init; } = true;
    public string? SeoTitle { get; init; }
    public string? SeoDescription { get; init; }
}

public sealed class SpecificationRequest
{
    public string Label { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
}

public sealed class ProductImageRequest
{
    public string Url { get; init; } = string.Empty;
    public string Alt { get; init; } = string.Empty;
    public int Order { get; init; }
}

/// <summary>
/// Variants are managed through their own endpoints rather than as part of the
/// product body, so an edit to a description can never clobber live stock.
/// </summary>
public sealed class VariantRequest
{
    public string Sku { get; init; } = string.Empty;
    public string Size { get; init; } = string.Empty;
    public string Color { get; init; } = string.Empty;
    public string ColorHex { get; init; } = string.Empty;
    public int StockQuantity { get; init; }
    public decimal? PriceOverride { get; init; }
    public string? ImageUrl { get; init; }
    public bool IsActive { get; init; } = true;
}

public sealed class ServiceRequest
{
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string ShortDescription { get; init; } = string.Empty;
    public string LongDescription { get; init; } = string.Empty;
    public string ImageUrl { get; init; } = string.Empty;
    public IReadOnlyList<string> BulletPoints { get; init; } = [];
    public int DisplayOrder { get; init; }
    public bool IsActive { get; init; } = true;
}

public sealed class GalleryItemRequest
{
    public string ImageUrl { get; init; } = string.Empty;
    public string ThumbnailUrl { get; init; } = string.Empty;
    public string Caption { get; init; } = string.Empty;
    public string AltText { get; init; } = string.Empty;
    public GalleryCategory Category { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsActive { get; init; } = true;
}

public sealed class BlogPostRequest
{
    public string Title { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string Excerpt { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string CoverImageUrl { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public IReadOnlyList<string> Tags { get; init; } = [];
    public DateTime? PublishedAt { get; init; }
    public bool IsPublished { get; init; }
}

public sealed class TestimonialRequest
{
    public string CustomerName { get; init; } = string.Empty;
    public string? CustomerTitle { get; init; }
    public string Quote { get; init; } = string.Empty;
    public int Rating { get; init; } = 5;
    public string? ImageUrl { get; init; }
    public bool IsApproved { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsActive { get; init; } = true;
}

public sealed class CouponRequest
{
    public string Code { get; init; } = string.Empty;
    public CouponType Type { get; init; }
    public decimal Value { get; init; }
    public decimal MinOrderTotal { get; init; }
    public decimal? MaxDiscount { get; init; }
    public int? UsageLimit { get; init; }
    public DateTime StartsAt { get; init; }
    public DateTime ExpiresAt { get; init; }
    public bool IsActive { get; init; } = true;
}

public sealed record CouponDto(
    string Id, string Code, CouponType Type, decimal Value, decimal MinOrderTotal,
    decimal? MaxDiscount, int? UsageLimit, int UsedCount,
    DateTime StartsAt, DateTime ExpiresAt, bool IsActive);

public sealed class BulkStockRequest
{
    public IReadOnlyList<BulkStockLine> Updates { get; init; } = [];
}

public sealed class BulkStockLine
{
    public string ProductId { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public int StockQuantity { get; init; }
}

/// <summary>
/// Partial success is reported rather than thrown: a stock-take of sixty rows
/// should not be discarded because one SKU was renamed since the page loaded.
/// </summary>
public sealed record BulkStockResultDto(int UpdatedCount, IReadOnlyList<string> Problems);
