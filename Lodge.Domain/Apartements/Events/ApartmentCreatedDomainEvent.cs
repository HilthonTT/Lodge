using Lodge.Domain.Core.Events;

namespace Lodge.Domain.Apartements.Events;

/// <summary>
/// Represents the event that is raised when an apartment is created.
/// </summary>
/// <param name="ApartmentId">The created apartment's id.</param>
public sealed record ApartmentCreatedDomainEvent(Guid ApartmentId) : IDomainEvent;
