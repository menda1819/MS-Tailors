using MongoDB.Bson.Serialization.Attributes;
using MSTailors.Domain.Common;

namespace MSTailors.Domain.Entities;

public class Testimonial : Entity
{
    [BsonElement("customerName")]
    public string CustomerName { get; set; } = string.Empty;

    [BsonElement("customerTitle")]
    [BsonIgnoreIfNull]
    public string? CustomerTitle { get; set; }

    [BsonElement("quote")]
    public string Quote { get; set; } = string.Empty;

    [BsonElement("rating")]
    public int Rating { get; set; } = 5;

    [BsonElement("imageUrl")]
    [BsonIgnoreIfNull]
    public string? ImageUrl { get; set; }

    [BsonElement("isApproved")]
    public bool IsApproved { get; set; }

    [BsonElement("displayOrder")]
    public int DisplayOrder { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;
}
