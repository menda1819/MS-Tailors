using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MSTailors.Domain.Common;
using MSTailors.Domain.ValueObjects;

namespace MSTailors.Domain.Entities;

/// <summary>
/// A ready-made product sold by a brand through the shop.
/// </summary>
/// <remarks>
/// Stock lives only on <see cref="ProductVariant.StockQuantity"/>. There is
/// deliberately no product-level stock field to go stale.
/// </remarks>
public class Product : Entity
{
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("slug")]
    public string Slug { get; set; } = string.Empty;

    [BsonElement("brandId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string BrandId { get; set; } = string.Empty;

    [BsonElement("categoryIds")]
    [BsonRepresentation(BsonType.ObjectId)]
    public List<string> CategoryIds { get; set; } = [];

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("shortDescription")]
    public string ShortDescription { get; set; } = string.Empty;

    [BsonElement("fabric")]
    public string Fabric { get; set; } = string.Empty;

    [BsonElement("careInstructions")]
    public string CareInstructions { get; set; } = string.Empty;

    [BsonElement("specifications")]
    public List<Specification> Specifications { get; set; } = [];

    [BsonElement("images")]
    public List<ProductImage> Images { get; set; } = [];

    [BsonElement("basePrice")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal BasePrice { get; set; }

    /// <summary>Null when the product is not on sale.</summary>
    [BsonElement("salePrice")]
    [BsonRepresentation(BsonType.Decimal128)]
    [BsonIgnoreIfNull]
    public decimal? SalePrice { get; set; }

    [BsonElement("currency")]
    public string Currency { get; set; } = "LKR";

    [BsonElement("variants")]
    public List<ProductVariant> Variants { get; set; } = [];

    [BsonElement("sizeGuideId")]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonIgnoreIfNull]
    public string? SizeGuideId { get; set; }

    [BsonElement("tags")]
    public List<string> Tags { get; set; } = [];

    [BsonElement("isFeatured")]
    public bool IsFeatured { get; set; }

    [BsonElement("isNewArrival")]
    public bool IsNewArrival { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("viewCount")]
    public long ViewCount { get; set; }

    [BsonElement("seoTitle")]
    [BsonIgnoreIfNull]
    public string? SeoTitle { get; set; }

    [BsonElement("seoDescription")]
    [BsonIgnoreIfNull]
    public string? SeoDescription { get; set; }

    /// <summary>Effective price for a variant: its override, else the sale price, else base.</summary>
    [BsonIgnore]
    public decimal EffectivePrice => SalePrice ?? BasePrice;

    /// <summary>Sum of stock across variants. Derived — never stored.</summary>
    [BsonIgnore]
    public int TotalStock => Variants.Sum(variant => variant.StockQuantity);

    public decimal PriceFor(ProductVariant variant) => variant.PriceOverride ?? EffectivePrice;
}
