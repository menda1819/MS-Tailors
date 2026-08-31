using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MSTailors.Domain.ValueObjects;

/// <summary>
/// A line on an order. Product name, size and colour are snapshots so the order
/// still renders correctly after the product is renamed, repriced or deleted.
/// </summary>
public class OrderItem
{
    [BsonElement("productId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string ProductId { get; set; } = string.Empty;

    [BsonElement("brandId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string BrandId { get; set; } = string.Empty;

    [BsonElement("productNameSnapshot")]
    public string ProductNameSnapshot { get; set; } = string.Empty;

    [BsonElement("variantSku")]
    public string VariantSku { get; set; } = string.Empty;

    [BsonElement("size")]
    public string Size { get; set; } = string.Empty;

    [BsonElement("color")]
    public string Color { get; set; } = string.Empty;

    [BsonElement("quantity")]
    public int Quantity { get; set; }

    [BsonElement("unitPrice")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal UnitPrice { get; set; }

    [BsonElement("lineTotal")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal LineTotal { get; set; }
}
