using MongoDB.Bson.Serialization.Attributes;
using MSTailors.Domain.Common;

namespace MSTailors.Domain.Entities;

/// <summary>
/// A tailoring service offered by MS Tailors. These pages drive inquiries, not
/// checkout, so no price field exists here by design.
/// </summary>
public class Service : Entity
{
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("slug")]
    public string Slug { get; set; } = string.Empty;

    [BsonElement("category")]
    public string Category { get; set; } = string.Empty;

    [BsonElement("shortDescription")]
    public string ShortDescription { get; set; } = string.Empty;

    [BsonElement("longDescription")]
    public string LongDescription { get; set; } = string.Empty;

    [BsonElement("imageUrl")]
    public string ImageUrl { get; set; } = string.Empty;

    [BsonElement("bulletPoints")]
    public List<string> BulletPoints { get; set; } = [];

    [BsonElement("displayOrder")]
    public int DisplayOrder { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;
}
