using FluentValidation;
using Linker2.Model;

namespace Linker2.Validators;

public class DataDtoValidator : AbstractValidator<DataDto>
{
    public DataDtoValidator()
    {
        RuleForEach(x => x.Links)
            .NotNull()
            .SetValidator(new LinkDtoValidator());

        RuleFor(x => x.Settings)
            .NotNull()
            .SetValidator(new SettingsDtoValidator());

        RuleFor(x => x.Filters)
            .NotNull()
            .SetValidator(new FiltersDtoValidator());
    }
}
