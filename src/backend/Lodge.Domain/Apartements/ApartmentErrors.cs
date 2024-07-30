using Lodge.Domain.Core.Primitives;

namespace Lodge.Domain.Apartements;

/// <summary>
/// Represents the apartment's domain errors.
/// </summary>
public static class ApartmentErrors
{
    public static Error NotFound(Guid id) => Error.NotFound(
        "Apartment.NotFound", 
        $"The apartment with the Id = '{id}' was not found");
}
