using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Apartments.GetById;

internal sealed class GetApartmentByIdQueryValidator : AbstractValidator<GetApartmentByIdQuery>
{
    public GetApartmentByIdQueryValidator()
    {
        RuleFor(x => x.ApartmentId)
            .NotEmpty()
            .WithError(ValidationErrors.Apartments.ApartmentIdIsRequired);
    }
}
