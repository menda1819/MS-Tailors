using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MSTailors.Domain.Common;
using MSTailors.Domain.ValueObjects;

namespace MSTailors.Domain.Entities;

/// <summary>
/// A shopping cart, owned by either a signed-in customer or an anonymous
/// browser id. Exactly one of the two is set.
/// </summary>
public class Cart : Entity
{
    [BsonElement("customerId")]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonIgnoreIfNull]
    public string? CustomerId { get; set; }

    /// <summary>Opaque id from a first-party cookie, for carts before sign-in.</summary>
    [BsonElement("anonymousId")]
    [BsonIgnoreIfNull]
    public string? AnonymousId { get; set; }

    [BsonElement("items")]
    public List<CartItem> Items { get; set; } = [];

    [BsonElement("couponCode")]
    [BsonIgnoreIfNull]
    public string? CouponCode { get; set; }

    /// <summary>Reaped by a TTL index. Extended on every write.</summary>
    [BsonElement("expiresAt")]
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(30);
}
