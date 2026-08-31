using MongoDB.Bson.Serialization.Attributes;
using MSTailors.Domain.Common;

namespace MSTailors.Domain.Entities;

public class BlogPost : Entity
{
    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("slug")]
    public string Slug { get; set; } = string.Empty;

    [BsonElement("excerpt")]
    public string Excerpt { get; set; } = string.Empty;

    [BsonElement("content")]
    public string Content { get; set; } = string.Empty;

    [BsonElement("coverImageUrl")]
    public string CoverImageUrl { get; set; } = string.Empty;

    [BsonElement("author")]
    public string Author { get; set; } = string.Empty;

    [BsonElement("tags")]
    public List<string> Tags { get; set; } = [];

    [BsonElement("publishedAt")]
    [BsonIgnoreIfNull]
    public DateTime? PublishedAt { get; set; }

    [BsonElement("isPublished")]
    public bool IsPublished { get; set; }
}
