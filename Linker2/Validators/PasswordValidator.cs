using FluentValidation;
using System.Security;

namespace Linker2.Validators;

public class PasswordValidator : AbstractValidator<SecureString>
{
    public PasswordValidator()
    {
        RuleFor(x => x.Length).
            NotNull().
            NotEmpty().WithMessage("Password too short");
    }
}
