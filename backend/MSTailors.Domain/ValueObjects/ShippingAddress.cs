using MongoDB.Bson.Serialization.Attributes;

namespace MSTailors.Domain.ValueObjects;

/// <summary>
/// Address snapshot taken at order time. Separate from <see cref="Address"/>
/// because a customer editing their address book must never rewrite history on
/// an order that has already shipped.
/// </summary>
public class ShippingAddress
{
    [BsonElement("line1")]
    public string Line1 { get; set; } = string.Empty;

    [BsonElement("line2")]
    [BsonIgnoreIfNull]
    public string? Line2 { get; set; }

    [BsonElement("city")]
    public string City { get; set; } = string.Empty;

    [BsonElement("postalCode")]
    [BsonIgnoreIfNull]
    public string? PostalCode { get; set; }
}
