﻿namespace Lodge.Domain.Core.Primitives;

public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    Problem = 2,
    NotFound = 3,
    Conflict = 4,
    Permission = 5
}
