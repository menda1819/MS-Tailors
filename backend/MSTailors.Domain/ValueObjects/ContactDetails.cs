using MongoDB.Bson.Serialization.Attributes;

namespace MSTailors.Domain.ValueObjects;

public class ContactDetails
{
    [BsonElement("fullName")]
    public string FullName { get; set; } = string.Empty;

    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("phone")]
    public string Phone { get; set; } = string.Empty;
}
