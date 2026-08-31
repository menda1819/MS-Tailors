using MSTailors.Domain.Common;

namespace MSTailors.Application.Common.Repositories;

/// <summary>Lookup by URL slug, shared by every entity that has a public page.</summary>
public interface ISlugRepository<T> : IRepository<T> where T : Entity
{
    Task<T?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>True when the slug is free. <paramref name="excludeId"/> lets an edit keep its own slug.</summary>
    Task<bool> IsSlugAvailableAsync(string slug, string? excludeId = null, CancellationToken cancellationToken = default);
}
