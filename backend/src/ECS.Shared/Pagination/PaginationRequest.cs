namespace ECS.Shared.Pagination;

/// <summary>
/// Normalized paging input. Clamps page and page size to safe bounds.
/// </summary>
public record PaginationRequest(int Page = 1, int PageSize = 20)
{
    private const int MaxPageSize = 200;

    public int Page { get; init; } = Page < 1 ? 1 : Page;
    public int PageSize { get; init; } = PageSize is < 1 or > MaxPageSize ? 20 : PageSize;

    public int Skip => (Page - 1) * PageSize;
}
