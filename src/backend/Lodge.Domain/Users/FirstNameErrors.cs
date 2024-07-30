﻿using Lodge.Domain.Core.Primitives;

namespace Lodge.Domain.Users;

/// <summary>
/// Represents the first name's domain errors.
/// </summary>
public static class FirstNameErrors
{
    public static readonly Error Empty = Error.Problem(
        "FirstName.Empty", "First name is empty");

    public static readonly Error TooLong = Error.Problem(
        "FirstName.TooLong", "First name exceeds the 128 characters limit");
}