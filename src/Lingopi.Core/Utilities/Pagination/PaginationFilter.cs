namespace Lingopi.Core.Utilities.Pagination;

public record PaginationFilter
{
    private int _page;
    private int _pageSize;

    private void SetPage(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
        _page = value;
    }

    private void SetPageSize(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
        _pageSize = value;
    }

    public int Page { get => _page; set => SetPage(value); }
    public int PageSize { get => _pageSize; set => SetPageSize(value); }
    public bool HasPagination { get; set; } = true;
}
