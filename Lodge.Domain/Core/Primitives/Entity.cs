using Lodge.Domain.Core.Events;

namespace Lodge.Domain.Core.Primitives;

/// <summary>
/// Represents the base class that all entities derive from.
/// </summary>
public abstract class Entity
{
    /// <summary>
    /// Gets the domain events. This collection is readonly.
    /// </summary>
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="Entity"/> class.
    /// </summary>
    /// <param name="id">The entity identifier.</param>
    protected Entity(Guid id)
    {
        Id = id;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Entity"/> class.
    /// </summary>
    /// <remarks>
    /// Required by EF Core.
    /// </remarks>
    protected Entity()
    {
    }

    /// <summary>
    /// Gets the entity identifier.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets the domain events
    /// </summary>
    public List<IDomainEvent> DomainEvents => [.. _domainEvents];

    /// <summary>
    /// Adds the specified <see cref="IDomainEvent"/> to the <see cref="Entity"/>.
    /// </summary>
    /// <param name="domainEvent">The domain event.</param>
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Clears all the domain events from the <see cref="Entity"/>.
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
