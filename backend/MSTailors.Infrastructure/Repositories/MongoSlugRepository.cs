using MongoDB.Driver;
using MSTailors.Application.Common.Repositories;
using MSTailors.Domain.Common;

namespace MSTailors.Infrastructure.Repositories;

/// <summary>
/// Adds slug lookup for entities with a public URL. The slug field is named by
/// the caller so this works for any entity carrying one.
/// </summary>
public class MongoSlugRepository<T> : MongoRepository<T>, ISlugRepository<T> where T : Entity
{
    private readonly string _slugField;

    public MongoSlugRepository(IMongoCollection<T> collection, string slugField = "slug")
        : base(collection)
    {
        _slugField = slugField;
    }

    public virtual async Task<T?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        var filter = Filter.And(NotDeleted, Filter.Eq(_slugField, slug.Trim().ToLowerInvariant()));
        return await Collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public virtual async Task<bool> IsSlugAvailableAsync(
        string slug,
        string? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return false;
        }

        var filter = Filter.And(NotDeleted, Filter.Eq(_slugField, slug.Trim().ToLowerInvariant()));

        if (!string.IsNullOrWhiteSpace(excludeId) && IsValidObjectId(excludeId))
        {
            filter = Filter.And(filter, Filter.Ne(entity => entity.Id, excludeId));
        }

        return !await Collection.Find(filter).AnyAsync(cancellationToken);
    }
}
