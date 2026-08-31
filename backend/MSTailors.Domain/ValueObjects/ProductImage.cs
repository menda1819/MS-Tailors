using MongoDB.Bson.Serialization.Attributes;

namespace MSTailors.Domain.ValueObjects;

public class ProductImage
{
    [BsonElement("url")]
    public string Url { get; set; } = string.Empty;

    /// <summary>Required for accessibility. Empty string only for decorative images.</summary>
    [BsonElement("alt")]
    public string Alt { get; set; } = string.Empty;

    [BsonElement("order")]
    public int Order { get; set; }
}
