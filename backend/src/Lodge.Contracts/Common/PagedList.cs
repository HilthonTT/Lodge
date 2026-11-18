using System.Text.Json.Serialization;

namespace Lodge.Contracts.Common;

/// <summary>
/// Represents one page of a list.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public sealed record PagedList<T>
{
    [JsonConstructor]
    public PagedList(IReadOnlyList<T> items, int page, int pageSize, int totalCount)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    /// <summary>
    /// Gets the current (1-based) page.
    /// </summary>
    public int Page { get; }

    /// <summary>
    /// Gets the page size.
    /// </summary>
    public int PageSize { get; }

    /// <summary>
    /// Gets the total number of items across all pages.
    /// </summary>
    public int TotalCount { get; }

    /// <summary>
    /// Gets a value indicating whether a next page exists.
    /// </summary>
    public bool HasNextPage => Page * PageSize < TotalCount;

    /// <summary>
    /// Gets a value indicating whether a previous page exists.
    /// </summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// Gets the items of the current page.
    /// </summary>
    public IReadOnlyList<T> Items { get; }

    /// <summary>
    /// Creates a page from an in-memory sequence.
    /// </summary>
    public static PagedList<T> Create(IEnumerable<T> source, int page, int pageSize)
    {
        List<T> all = source as List<T> ?? [.. source];

        return new PagedList<T>([.. all.Skip((page - 1) * pageSize).Take(pageSize)], page, pageSize, all.Count);
    }
}
