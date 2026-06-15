using ECS.Shared.Pagination;

namespace ECS.Api.Common.Models;

/// <summary>Standard pagination payload returned inside ApiResponse.Data.</summary>
public sealed class PagedResponse<T>
{
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public IReadOnlyList<T> Items { get; init; } = [];

    public static PagedResponse<T> From(PagedList<T> page) => new()
    {
        Page = page.Page,
        PageSize = page.PageSize,
        TotalCount = page.TotalCount,
        TotalPages = page.TotalPages,
        Items = page.Items
    };
}
