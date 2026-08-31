using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MSTailors.Domain.Common;
using MSTailors.Domain.Enums;

namespace MSTailors.Domain.Entities;

public class GalleryItem : Entity
{
    [BsonElement("imageUrl")]
    public string ImageUrl { get; set; } = string.Empty;

    [BsonElement("thumbnailUrl")]
    public string ThumbnailUrl { get; set; } = string.Empty;

    [BsonElement("caption")]
    public string Caption { get; set; } = string.Empty;

    /// <summary>Required for accessibility — never left empty for content images.</summary>
    [BsonElement("altText")]
    public string AltText { get; set; } = string.Empty;

    [BsonElement("category")]
    [BsonRepresentation(BsonType.String)]
    public GalleryCategory Category { get; set; }

    [BsonElement("displayOrder")]
    public int DisplayOrder { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;
}
