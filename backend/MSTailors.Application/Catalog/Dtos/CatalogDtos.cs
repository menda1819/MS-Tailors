namespace MSTailors.Application.Catalog.Dtos;

// ---- Brands ---------------------------------------------------------------

public sealed record BrandListItemDto(
    string Id,
    string Name,
    string Slug,
    string Tagline,
    string LogoUrl,
    string HeroImageUrl,
    int DisplayOrder);

public sealed record BrandDetailDto(
    string Id,
    string Name,
    string Slug,
    string Tagline,
    string Story,
    string Philosophy,
    string TargetAudience,
    string LogoUrl,
    string HeroImageUrl,
    string? SeoTitle,
    string? SeoDescription,
    long ProductCount);

// ---- Categories -----------------------------------------------------------

public sealed record CategoryDto(
    string Id,
    string Name,
    string Slug,
    string? ParentCategoryId,
    string ImageUrl,
    int DisplayOrder,
    IReadOnlyList<CategoryDto> Children);

// ---- Products -------------------------------------------------------------

public sealed record ProductImageDto(string Url, string Alt, int Order);

public sealed record SpecificationDto(string Label, string Value);

/// <summary>
/// A sellable variant. <see cref="Price"/> is resolved server-side — the
/// variant override, else the product sale price, else base — so the client
/// never computes money.
/// </summary>
public sealed record ProductVariantDto(
    string Sku,
    string Size,
    string Color,
    string ColorHex,
    int StockQuantity,
    bool InStock,
    decimal Price,
    string? ImageUrl);

/// <summary>Card shape for grids. Deliberately smaller than the detail DTO.</summary>
public sealed record ProductListItemDto(
    string Id,
    string Name,
    string Slug,
    string BrandId,
    string ShortDescription,
    decimal BasePrice,
    decimal? SalePrice,
    decimal EffectivePrice,
    string Currency,
    bool IsOnSale,
    bool IsFeatured,
    bool IsNewArrival,
    bool InStock,
    int TotalStock,
    ProductImageDto? PrimaryImage,
    IReadOnlyList<string> AvailableSizes,
    IReadOnlyList<string> AvailableColors);

public sealed record ProductDetailDto(
    string Id,
    string Name,
    string Slug,
    string BrandId,
    IReadOnlyList<string> CategoryIds,
    string Description,
    string ShortDescription,
    string Fabric,
    string CareInstructions,
    IReadOnlyList<SpecificationDto> Specifications,
    IReadOnlyList<ProductImageDto> Images,
    decimal BasePrice,
    decimal? SalePrice,
    decimal EffectivePrice,
    string Currency,
    bool IsOnSale,
    IReadOnlyList<ProductVariantDto> Variants,
    string? SizeGuideId,
    IReadOnlyList<string> Tags,
    bool IsFeatured,
    bool IsNewArrival,
    bool InStock,
    int TotalStock,
    string? SeoTitle,
    string? SeoDescription);
