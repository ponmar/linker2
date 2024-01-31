using FluentValidation.Results;
using System;

namespace Linker2.Validators;

public class ValidationException : Exception
{
    public ValidationResult Result { get; }

    public ValidationException(ValidationResult result)
    {
        Result = result;
    }
}
