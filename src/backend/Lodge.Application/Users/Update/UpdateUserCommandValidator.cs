﻿using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Users.Update;

/// <summary>
/// Represents the <see cref="UpdateUserCommand"/> validator.
/// </summary>
internal sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateUserCommandValidator"/> class.
    /// </summary>
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithError(ValidationErrors.UpdateUser.UserIdIsRequired);

        RuleFor(x => x.FirstName).NotEmpty().WithError(ValidationErrors.UpdateUser.FirstNameIsRequired);

        RuleFor(x => x.LastName).NotEmpty().WithError(ValidationErrors.UpdateUser.LastNameIsRequired);
    }
}
