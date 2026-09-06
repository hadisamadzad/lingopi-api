namespace Lingopi.Core.Utilities.Pagination;

public record PaginatedList<T>
{
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public required IList<T> Results { get; init; }
}
