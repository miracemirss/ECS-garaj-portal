namespace ECS.Shared.Pagination;

/// <summary>
/// Paged list query with optional full-text search and sorting. Page/PageSize are
/// clamped to safe bounds (same rules as <see cref="PaginationRequest"/>).
/// </summary>
public record PagedQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string? SortBy = null,
    bool SortDescending = false)
{
    private const int MaxPageSize = 200;

    public int Page { get; init; } = Page < 1 ? 1 : Page;
    public int PageSize { get; init; } = PageSize is < 1 or > MaxPageSize ? 20 : PageSize;

    public int Skip => (Page - 1) * PageSize;
}
