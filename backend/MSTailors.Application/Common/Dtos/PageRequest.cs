using System.ComponentModel.DataAnnotations;

namespace MSTailors.Application.Common.Dtos;

/// <summary>
/// Paging query parameters shared by list endpoints. PageSize is clamped in the
/// repository layer too, so a hand-crafted query string cannot ask the database
/// for an unbounded result set.
/// </summary>
public class PageRequest
{
    private const int MaxPageSize = 100;

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, MaxPageSize)]
    public int PageSize { get; init; } = 24;
}
