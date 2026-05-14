namespace YenkoTools.Common.Cqrs.Results;

public class PagedResult<T>
{
    public IEnumerable<T> Items { get; }
    public int TotalCount { get; }
    public int PageSize { get; }
    public int PageNumber { get; }
    public int TotalPages { get; }

    public PagedResult(IEnumerable<T> items, int totalCount, int pageSize, int pageNumber)
    {
        Items = items ?? throw new ArgumentNullException(nameof(items));
        TotalCount = totalCount;
        PageSize = pageSize;
        PageNumber = pageNumber;
        TotalPages = pageSize > 0 ? (int)Math.Ceiling(totalCount / (double)pageSize) : 0;
    }

    public static PagedResult<T> Create(IEnumerable<T> items, int totalCount, int pageSize, int pageNumber) =>
        new(items, totalCount, pageSize, pageNumber);
}
