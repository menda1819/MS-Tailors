using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MSTailors.Domain.Common;
using MSTailors.Domain.Enums;

namespace MSTailors.Domain.Entities;

public class Inquiry : Entity
{
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("phone")]
    public string Phone { get; set; } = string.Empty;

    [BsonElement("serviceNeeded")]
    [BsonRepresentation(BsonType.String)]
    public ServiceNeeded ServiceNeeded { get; set; }

    [BsonElement("message")]
    public string Message { get; set; } = string.Empty;

    [BsonElement("preferredDate")]
    [BsonIgnoreIfNull]
    public DateTime? PreferredDate { get; set; }

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public InquiryStatus Status { get; set; } = InquiryStatus.New;
}
