using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MSTailors.Domain.ValueObjects;

/// <summary>
/// A sellable unit of a product. Stock lives here and nowhere else — a product
/// has no stock of its own, only the sum of its variants.
/// </summary>
public class ProductVariant
{
    /// <summary>Unique across every product, not just within one.</summary>
    [BsonElement("sku")]
    public string Sku { get; set; } = string.Empty;

    [BsonElement("size")]
    public string Size { get; set; } = string.Empty;

    [BsonElement("color")]
    public string Color { get; set; } = string.Empty;

    [BsonElement("colorHex")]
    public string ColorHex { get; set; } = string.Empty;

    [BsonElement("stockQuantity")]
    public int StockQuantity { get; set; }

    /// <summary>Overrides the product's base/sale price for this variant only. Null = inherit.</summary>
    [BsonElement("priceOverride")]
    [BsonRepresentation(BsonType.Decimal128)]
    [BsonIgnoreIfNull]
    public decimal? PriceOverride { get; set; }

    [BsonElement("imageUrl")]
    [BsonIgnoreIfNull]
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Lets a size or colour be retired without deleting it, so historical
    /// orders referencing the SKU still resolve.
    /// </summary>
    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonIgnore]
    public bool InStock => IsActive && StockQuantity > 0;
}
