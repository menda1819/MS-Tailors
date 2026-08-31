using System.Linq.Expressions;
using MongoDB.Driver;
using MSTailors.Application.Common.Pagination;
using MSTailors.Application.Common.Repositories;
using MSTailors.Domain.Common;

namespace MSTailors.Infrastructure.Repositories;

/// <summary>
/// Generic MongoDB repository. Every read path is soft-delete aware.
/// </summary>
public class MongoRepository<T> : IRepository<T> where T : Entity
{
    protected readonly IMongoCollection<T> Collection;

    protected static FilterDefinitionBuilder<T> Filter => Builders<T>.Filter;
    protected static UpdateDefinitionBuilder<T> Update => Builders<T>.Update;
    protected static SortDefinitionBuilder<T> Sort => Builders<T>.Sort;

    /// <summary>Excludes soft-deleted documents. Every query starts from here.</summary>
    protected static FilterDefinition<T> NotDeleted => Filter.Eq(entity => entity.IsDeleted, false);

    public MongoRepository(IMongoCollection<T> collection)
    {
        Collection = collection;
    }

    protected static FilterDefinition<T> Live(Expression<Func<T, bool>>? predicate)
        => predicate is null ? NotDeleted : Filter.And(NotDeleted, Filter.Where(predicate));

    protected static FilterDefinition<T> ById(string id)
        => Filter.And(NotDeleted, Filter.Eq(entity => entity.Id, id));

    public virtual async Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!IsValidObjectId(id))
        {
            // A malformed id is a miss, not a 500. Mongo would throw on the cast.
            return null;
        }

        return await Collection.Find(ById(id)).FirstOrDefaultAsync(cancellationToken);
    }

    public virtual async Task<T?> FindOneAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
        => await Collection.Find(Live(predicate)).FirstOrDefaultAsync(cancellationToken);

    public virtual async Task<IReadOnlyList<T>> ListAsync(
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
        => await Collection.Find(Live(predicate)).ToListAsync(cancellationToken);

    public virtual async Task<PagedResult<T>> PageAsync(
        int page,
        int pageSize,
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        var (safePage, safePageSize) = NormalisePaging(page, pageSize);
        var filter = Live(predicate);

        var total = await Collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        var items = await Collection
            .Find(filter)
            .SortByDescending(entity => entity.CreatedAt)
            .Skip((safePage - 1) * safePageSize)
            .Limit(safePageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>(items, safePage, safePageSize, total);
    }

    public virtual async Task<long> CountAsync(
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
        => await Collection.CountDocumentsAsync(Live(predicate), cancellationToken: cancellationToken);

    public virtual async Task<bool> ExistsAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
        => await Collection.Find(Live(predicate)).AnyAsync(cancellationToken);

    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        entity.CreatedAt = now;
        entity.UpdatedAt = now;
        entity.IsDeleted = false;

        await Collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
        return entity;
    }

    public virtual async Task<IReadOnlyList<T>> AddManyAsync(
        IEnumerable<T> entities,
        CancellationToken cancellationToken = default)
    {
        var list = entities.ToList();
        if (list.Count == 0)
        {
            return list;
        }

        var now = DateTime.UtcNow;
        foreach (var entity in list)
        {
            entity.CreatedAt = now;
            entity.UpdatedAt = now;
            entity.IsDeleted = false;
        }

        await Collection.InsertManyAsync(list, cancellationToken: cancellationToken);
        return list;
    }

    public virtual async Task<bool> UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        if (!IsValidObjectId(entity.Id))
        {
            return false;
        }

        entity.UpdatedAt = DateTime.UtcNow;

        var result = await Collection.ReplaceOneAsync(
            ById(entity.Id), entity, new ReplaceOptions { IsUpsert = false }, cancellationToken);

        return result.MatchedCount > 0;
    }

    public virtual async Task<bool> SoftDeleteAsync(string id, CancellationToken cancellationToken = default)
        => await SetDeletedAsync(id, true, cancellationToken);

    public virtual async Task<bool> RestoreAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!IsValidObjectId(id))
        {
            return false;
        }

        // Restore must look past the NotDeleted filter, or it could never find its target.
        var result = await Collection.UpdateOneAsync(
            Filter.Eq(entity => entity.Id, id),
            Update.Set(entity => entity.IsDeleted, false).Set(entity => entity.UpdatedAt, DateTime.UtcNow),
            cancellationToken: cancellationToken);

        return result.ModifiedCount > 0;
    }

    public virtual async Task<bool> HardDeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!IsValidObjectId(id))
        {
            return false;
        }

        var result = await Collection.DeleteOneAsync(
            Filter.Eq(entity => entity.Id, id), cancellationToken);

        return result.DeletedCount > 0;
    }

    private async Task<bool> SetDeletedAsync(string id, bool isDeleted, CancellationToken cancellationToken)
    {
        if (!IsValidObjectId(id))
        {
            return false;
        }

        var result = await Collection.UpdateOneAsync(
            ById(id),
            Update.Set(entity => entity.IsDeleted, isDeleted).Set(entity => entity.UpdatedAt, DateTime.UtcNow),
            cancellationToken: cancellationToken);

        return result.ModifiedCount > 0;
    }

    protected static bool IsValidObjectId(string? id)
        => !string.IsNullOrWhiteSpace(id) && MongoDB.Bson.ObjectId.TryParse(id, out _);

    protected static (int Page, int PageSize) NormalisePaging(int page, int pageSize)
        => (Math.Max(page, 1), Math.Clamp(pageSize, 1, 100));
}
