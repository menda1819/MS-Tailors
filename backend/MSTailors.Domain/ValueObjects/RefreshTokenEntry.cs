using MongoDB.Bson.Serialization.Attributes;

namespace MSTailors.Domain.ValueObjects;

/// <summary>
/// An issued refresh token. <see cref="Token"/> holds a SHA-256 hash, never the
/// raw value — a leaked database dump must not yield usable tokens.
/// </summary>
public class RefreshTokenEntry
{
    [BsonElement("token")]
    public string Token { get; set; } = string.Empty;

    [BsonElement("expiresAt")]
    public DateTime ExpiresAt { get; set; }

    [BsonIgnore]
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
}
