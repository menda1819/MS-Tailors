namespace MSTailors.Application.Common.Dtos;

/// <summary>The wire shape for every paginated list endpoint.</summary>
public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    long TotalCount,
    int TotalPages,
    bool HasPrevious,
    bool HasNext)
{
    public static PagedResponse<T> From(Pagination.PagedResult<T> result) => new(
        result.Items, result.Page, result.PageSize, result.TotalCount,
        result.TotalPages, result.HasPrevious, result.HasNext);

    public static PagedResponse<TOut> From<TSource, TOut>(
        Pagination.PagedResult<TSource> result,
        Func<TSource, TOut> map) => new(
        [.. result.Items.Select(map)], result.Page, result.PageSize, result.TotalCount,
        result.TotalPages, result.HasPrevious, result.HasNext);
}
