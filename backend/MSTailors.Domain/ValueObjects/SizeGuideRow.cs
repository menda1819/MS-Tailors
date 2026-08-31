using MongoDB.Bson.Serialization.Attributes;

namespace MSTailors.Domain.ValueObjects;

public class SizeGuideRow
{
    [BsonElement("label")]
    public string Label { get; set; } = string.Empty;

    /// <summary>Cell values, positionally aligned with SizeGuide.Columns.</summary>
    [BsonElement("values")]
    public List<string> Values { get; set; } = [];
}
