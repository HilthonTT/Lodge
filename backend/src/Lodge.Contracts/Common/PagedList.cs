namespace Lodge.Contracts.Common;

/// <summary>
/// Represents the generic paged list.
/// </summary>
/// <typeparam name="T">The type of list.</typeparam>
public sealed record PagedList<T>
{
    /// <summary>
    /// Initializes a new instance of <see cref="PagedList{T}"/> record.
    /// </summary>
    /// <param name="items">The items.</param>
    /// <param name="page">The page number.</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="totalCount">The total count.</param>
    private PagedList(IEnumerable<T> items, int page, int pageSize, int totalCount)
    {
        Items = items.ToList();
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    /// <summary>
    /// Gets the current page.
    /// </summary>
    public int Page { get; }

    /// <summary>
    /// Gets the page size. The maximum page size is 100.
    /// </summary>
    public int PageSize { get; }

    /// <summary>
    /// Gets the total number of items.
    /// </summary>
    public int TotalCount { get; }

    /// <summary>
    /// Gets the flag indicating whether the next page exists.
    /// </summary>
    public bool HasNextPage => Page * PageSize > TotalCount;

    /// <summary>
    /// Gets the flag indicating whether the previous page exists.
    /// </summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// Gets the items.
    /// </summary>
    public IReadOnlyList<T> Items { get; }

    /// <summary>
    /// Creates a new instance of <see cref="PagedList{T}"/>
    /// </summary>
    /// <param name="query">The items.</param>
    /// <param name="page">The current page.</param>
    /// <param name="pageSize">The page size.</param>
    /// <returns>A new instance of the <see cref="PagedList{T}"/></returns>
    public static PagedList<T> Create(IEnumerable<T> query, int page, int pageSize)
    {
        int totalCount = query.Count();

        List<T> items = [.. query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
        ];

        return new(items, page, pageSize, totalCount);
    }
}
