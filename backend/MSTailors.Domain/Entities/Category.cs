using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MSTailors.Domain.Common;

namespace MSTailors.Domain.Entities;

public class Category : Entity
{
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("slug")]
    public string Slug { get; set; } = string.Empty;

    /// <summary>Null for a top-level category. One level of nesting is expected.</summary>
    [BsonElement("parentCategoryId")]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonIgnoreIfNull]
    public string? ParentCategoryId { get; set; }

    [BsonElement("imageUrl")]
    public string ImageUrl { get; set; } = string.Empty;

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("displayOrder")]
    public int DisplayOrder { get; set; }
}
