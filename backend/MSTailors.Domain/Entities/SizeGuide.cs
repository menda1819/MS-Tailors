using MongoDB.Bson.Serialization.Attributes;
using MSTailors.Domain.Common;
using MSTailors.Domain.ValueObjects;

namespace MSTailors.Domain.Entities;

/// <summary>
/// A measurement chart a product can point at via Product.SizeGuideId.
/// </summary>
public class SizeGuide : Entity
{
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("slug")]
    public string Slug { get; set; } = string.Empty;

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>Column headers, e.g. Size, Chest, Waist, Sleeve.</summary>
    [BsonElement("columns")]
    public List<string> Columns { get; set; } = [];

    /// <summary>One row per size; each cell aligns with <see cref="Columns"/> by position.</summary>
    [BsonElement("rows")]
    public List<SizeGuideRow> Rows { get; set; } = [];

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;
}
