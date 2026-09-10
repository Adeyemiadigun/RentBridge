using System.Collections.Generic;

namespace RentBridge.Application.Common;

public sealed class PagedResult<T>
{
    public int Page { get; }
    public int PageSize { get; }
    public int TotalCount { get; }
    public int TotalPages { get; }
    public IReadOnlyList<T> Items { get; }

    public PagedResult(int page, int pageSize, int totalCount, IReadOnlyList<T> items)
    {
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
        TotalPages = pageSize > 0 ? (int)System.Math.Ceiling(totalCount / (double)pageSize) : 0;
        Items = items ?? System.Array.Empty<T>();
    }
}