using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MSTailors.Domain.ValueObjects;

public class CartItem
{
    [BsonElement("productId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string ProductId { get; set; } = string.Empty;

    [BsonElement("variantSku")]
    public string VariantSku { get; set; } = string.Empty;

    [BsonElement("quantity")]
    public int Quantity { get; set; }

    /// <summary>
    /// Price when the item was added, for showing "price changed since you added
    /// this". It is a display hint only — checkout re-reads the real price from
    /// the product and never trusts this value.
    /// </summary>
    [BsonElement("unitPriceSnapshot")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal UnitPriceSnapshot { get; set; }
}
