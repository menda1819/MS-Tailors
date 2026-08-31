using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MSTailors.Domain.Common;
using MSTailors.Domain.Enums;
using MSTailors.Domain.ValueObjects;

namespace MSTailors.Domain.Entities;

/// <summary>
/// A registered account. Admins live here too, separated by <see cref="Role"/>.
/// Guest orders carry no customer at all.
/// </summary>
public class Customer : Entity
{
    /// <summary>Stored lowercased. Unique.</summary>
    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("passwordHash")]
    public string PasswordHash { get; set; } = string.Empty;

    [BsonElement("fullName")]
    public string FullName { get; set; } = string.Empty;

    [BsonElement("phone")]
    public string Phone { get; set; } = string.Empty;

    [BsonElement("addresses")]
    public List<Address> Addresses { get; set; } = [];

    [BsonElement("wishlist")]
    public List<WishlistItem> Wishlist { get; set; } = [];

    [BsonElement("emailVerified")]
    public bool EmailVerified { get; set; }

    [BsonElement("role")]
    [BsonRepresentation(BsonType.String)]
    public UserRole Role { get; set; } = UserRole.Customer;

    /// <summary>Hashed refresh tokens. Expired entries are pruned on rotation.</summary>
    [BsonElement("refreshTokens")]
    public List<RefreshTokenEntry> RefreshTokens { get; set; } = [];
}
