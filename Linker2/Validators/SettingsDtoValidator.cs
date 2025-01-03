using FluentValidation;
using Linker2.Model;
using System.IO.Abstractions;

namespace Linker2.Validators;

public class SettingsDtoValidator : AbstractValidator<SettingsDto>
{
    public const string UrlReplaceString = "%URL%";

    public SettingsDtoValidator()
    {
        RuleFor(x => x.OpenLinkCommand)
            .NotNull();

        RuleFor(x => x.OpenLinkArguments)
            .NotNull()
            .Must(x => x!.Contains(UrlReplaceString));

        RuleFor(x => x.LockAfterSeconds).GreaterThan(10);

        RuleFor(x => x.ThumbnailImageIds).NotNull();
    }
}
