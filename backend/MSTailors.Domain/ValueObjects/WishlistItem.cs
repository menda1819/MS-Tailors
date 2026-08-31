using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MSTailors.Domain.ValueObjects;

public class WishlistItem
{
    [BsonElement("productId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string ProductId { get; set; } = string.Empty;

    [BsonElement("variantSku")]
    [BsonIgnoreIfNull]
    public string? VariantSku { get; set; }

    [BsonElement("addedAt")]
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
