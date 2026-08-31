using MSTailors.Application.Catalog.Dtos;
using MSTailors.Domain.Entities;
using MSTailors.Domain.ValueObjects;

namespace MSTailors.Application.Common.Mapping;

/// <summary>
/// Entity to DTO projection. Hand-written rather than reflection-mapped: these
/// are the API's public contract, and an accidental field leak here is a data
/// disclosure, not a cosmetic bug.
/// </summary>
public static class CatalogMappings
{
    public static BrandListItemDto ToListItem(this Brand brand) => new(
        brand.Id, brand.Name, brand.Slug, brand.Tagline,
        brand.LogoUrl, brand.HeroImageUrl, brand.DisplayOrder);

    public static BrandDetailDto ToDetail(this Brand brand, long productCount) => new(
        brand.Id, brand.Name, brand.Slug, brand.Tagline, brand.Story, brand.Philosophy,
        brand.TargetAudience, brand.LogoUrl, brand.HeroImageUrl,
        brand.SeoTitle, brand.SeoDescription, productCount);

    public static CategoryDto ToDto(this Category category, IReadOnlyList<CategoryDto>? children = null) => new(
        category.Id, category.Name, category.Slug, category.ParentCategoryId,
        category.ImageUrl, category.DisplayOrder, children ?? []);

    public static ProductImageDto ToDto(this ProductImage image) => new(image.Url, image.Alt, image.Order);

    public static SpecificationDto ToDto(this Specification specification) =>
        new(specification.Label, specification.Value);

    public static ProductVariantDto ToDto(this ProductVariant variant, Product product) => new(
        variant.Sku, variant.Size, variant.Color, variant.ColorHex,
        variant.StockQuantity, variant.InStock, product.PriceFor(variant), variant.ImageUrl);

    public static ProductListItemDto ToListItem(this Product product) => new(
        product.Id,
        product.Name,
        product.Slug,
        product.BrandId,
        product.ShortDescription,
        product.BasePrice,
        product.SalePrice,
        product.EffectivePrice,
        product.Currency,
        product.SalePrice is not null,
        product.IsFeatured,
        product.IsNewArrival,
        product.Variants.Any(variant => variant.InStock),
        product.TotalStock,
        product.Images.OrderBy(image => image.Order).FirstOrDefault()?.ToDto(),
        // Only surface options a customer could actually buy.
        [.. product.Variants.Where(v => v.IsActive).Select(v => v.Size).Distinct()],
        [.. product.Variants.Where(v => v.IsActive).Select(v => v.Color).Distinct()]);

    public static ProductDetailDto ToDetail(this Product product) => new(
        product.Id,
        product.Name,
        product.Slug,
        product.BrandId,
        product.CategoryIds,
        product.Description,
        product.ShortDescription,
        product.Fabric,
        product.CareInstructions,
        [.. product.Specifications.Select(specification => specification.ToDto())],
        [.. product.Images.OrderBy(image => image.Order).Select(image => image.ToDto())],
        product.BasePrice,
        product.SalePrice,
        product.EffectivePrice,
        product.Currency,
        product.SalePrice is not null,
        [.. product.Variants.Where(variant => variant.IsActive).Select(variant => variant.ToDto(product))],
        product.SizeGuideId,
        product.Tags,
        product.IsFeatured,
        product.IsNewArrival,
        product.Variants.Any(variant => variant.InStock),
        product.TotalStock,
        product.SeoTitle,
        product.SeoDescription);
}
