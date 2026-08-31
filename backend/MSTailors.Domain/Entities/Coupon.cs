using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MSTailors.Domain.Common;
using MSTailors.Domain.Enums;

namespace MSTailors.Domain.Entities;

public class Coupon : Entity
{
    /// <summary>Stored uppercased. Unique.</summary>
    [BsonElement("code")]
    public string Code { get; set; } = string.Empty;

    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)]
    public CouponType Type { get; set; }

    /// <summary>Percent (0–100) for Percentage, LKR amount for FixedAmount.</summary>
    [BsonElement("value")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Value { get; set; }

    [BsonElement("minOrderTotal")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal MinOrderTotal { get; set; }

    /// <summary>Caps a percentage discount. Null means uncapped.</summary>
    [BsonElement("maxDiscount")]
    [BsonRepresentation(BsonType.Decimal128)]
    [BsonIgnoreIfNull]
    public decimal? MaxDiscount { get; set; }

    /// <summary>Null means unlimited redemptions.</summary>
    [BsonElement("usageLimit")]
    [BsonIgnoreIfNull]
    public int? UsageLimit { get; set; }

    [BsonElement("usedCount")]
    public int UsedCount { get; set; }

    [BsonElement("startsAt")]
    public DateTime StartsAt { get; set; }

    [BsonElement("expiresAt")]
    public DateTime ExpiresAt { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonIgnore]
    public bool HasRemainingUses => UsageLimit is null || UsedCount < UsageLimit;

    public bool IsRedeemableAt(DateTime utcNow)
        => IsActive && !IsDeleted && HasRemainingUses && utcNow >= StartsAt && utcNow < ExpiresAt;
}
