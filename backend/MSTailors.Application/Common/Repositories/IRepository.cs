using System.Linq.Expressions;
using MSTailors.Application.Common.Pagination;
using MSTailors.Domain.Common;

namespace MSTailors.Application.Common.Repositories;

/// <summary>
/// Storage operations shared by every collection.
/// </summary>
/// <remarks>
/// Deliberately expressed with <see cref="Expression"/> rather than driver
/// types, so Application stays free of MongoDB.Driver.
///
/// Soft delete is the default everywhere: unless a method is explicitly named
/// <c>...IncludingDeleted</c>, documents with <c>isDeleted = true</c> are
/// invisible. Callers cannot forget the filter because they never write it.
/// </remarks>
public interface IRepository<T> where T : Entity
{
    Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<T?> FindOneAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> ListAsync(
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    Task<PagedResult<T>> PageAsync(
        int page,
        int pageSize,
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    Task<long> CountAsync(
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>Inserts and stamps CreatedAt/UpdatedAt. Returns the entity with its generated id.</summary>
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> AddManyAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    /// <summary>Full document replace. Stamps UpdatedAt. Returns false if no live document matched.</summary>
    Task<bool> UpdateAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>Sets isDeleted. The document stays in the collection.</summary>
    Task<bool> SoftDeleteAsync(string id, CancellationToken cancellationToken = default);

    Task<bool> RestoreAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Permanent removal. Intended for test teardown and admin data repair only —
    /// never call this from a request path.
    /// </summary>
    Task<bool> HardDeleteAsync(string id, CancellationToken cancellationToken = default);
}
