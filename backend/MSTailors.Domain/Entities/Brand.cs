using MongoDB.Bson.Serialization.Attributes;
using MSTailors.Domain.Common;

namespace MSTailors.Domain.Entities;

/// <summary>
/// An independent clothing brand selling through the MS Tailors platform.
/// Created at runtime by an admin — never hardcoded anywhere in the system.
/// </summary>
public class Brand : Entity
{
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("slug")]
    public string Slug { get; set; } = string.Empty;

    [BsonElement("tagline")]
    public string Tagline { get; set; } = string.Empty;

    [BsonElement("story")]
    public string Story { get; set; } = string.Empty;

    [BsonElement("philosophy")]
    public string Philosophy { get; set; } = string.Empty;

    [BsonElement("targetAudience")]
    public string TargetAudience { get; set; } = string.Empty;

    [BsonElement("logoUrl")]
    public string LogoUrl { get; set; } = string.Empty;

    [BsonElement("heroImageUrl")]
    public string HeroImageUrl { get; set; } = string.Empty;

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("displayOrder")]
    public int DisplayOrder { get; set; }

    [BsonElement("seoTitle")]
    [BsonIgnoreIfNull]
    public string? SeoTitle { get; set; }

    [BsonElement("seoDescription")]
    [BsonIgnoreIfNull]
    public string? SeoDescription { get; set; }
}
