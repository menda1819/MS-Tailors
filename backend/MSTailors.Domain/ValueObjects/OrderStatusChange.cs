using MongoDB.Bson.Serialization.Attributes;
using MSTailors.Domain.Enums;

namespace MSTailors.Domain.ValueObjects;

public class OrderStatusChange
{
    [BsonElement("status")]
    [BsonRepresentation(MongoDB.Bson.BsonType.String)]
    public OrderStatus Status { get; set; }

    [BsonElement("changedAt")]
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Admin user id, or "system" for automated transitions.</summary>
    [BsonElement("changedBy")]
    public string ChangedBy { get; set; } = "system";

    [BsonElement("note")]
    [BsonIgnoreIfNull]
    public string? Note { get; set; }
}
