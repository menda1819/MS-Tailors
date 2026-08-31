using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;

namespace MSTailors.Infrastructure.Persistence;

/// <summary>
/// Global BSON conventions.
/// </summary>
/// <remarks>
/// From Phase 2 the entities carry explicit <c>[BsonElement]</c> attributes, so
/// field naming is declared per property rather than inferred. These conventions
/// remain as a safety net for anything left unattributed, and to pin the two
/// behaviours that matter for schema evolution: unknown fields are ignored, and
/// enums are stored as strings rather than brittle ordinals.
/// </remarks>
public static class MongoConventions
{
    private static bool _registered;
    private static readonly object Gate = new();

    public static void Register()
    {
        lock (Gate)
        {
            if (_registered)
            {
                return;
            }

            var pack = new ConventionPack
            {
                new CamelCaseElementNameConvention(),
                // Adding a field must not break older readers.
                new IgnoreExtraElementsConvention(true),
                new EnumRepresentationConvention(BsonType.String)
            };

            ConventionRegistry.Register("MSTailorsConventions", pack, _ => true);

            _registered = true;
        }
    }
}
