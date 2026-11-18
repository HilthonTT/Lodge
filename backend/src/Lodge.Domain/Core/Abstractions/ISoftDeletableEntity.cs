namespace Lodge.Domain.Core.Abstractions;

/// <summary>
/// Represents the marker interface for soft-deletable entities.
/// </summary>
public interface ISoftDeletableEntity
{
    /// <summary>
    /// Gets the date and time in UTC format the entity was deleted on.
    /// </summary>
    public DateTime? DeletedOnUtc { get; set; }

    /// <summary>
    /// Gets a value indicating whether the entity has been deleted.
    /// </summary>
    public bool Deleted { get; set; }
}
