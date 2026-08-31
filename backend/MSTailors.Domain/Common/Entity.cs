using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MSTailors.Domain.Common;

/// <summary>
/// Base document. Every collection carries an ObjectId id, audit timestamps and
/// a soft-delete flag.
/// </summary>
public abstract class Entity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Soft delete. Nothing is removed from the database by the application;
    /// every read path must filter on this.
    /// </summary>
    [BsonElement("isDeleted")]
    public bool IsDeleted { get; set; }
}
