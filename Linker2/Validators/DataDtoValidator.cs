using FluentValidation;
using Linker2.Model;
using System.IO.Abstractions;

namespace Linker2.Validators;

public class DataDtoValidator : AbstractValidator<DataDto>
{
    public DataDtoValidator(IFileSystem fileSystem)
    {
        RuleForEach(x => x.Links)
            .NotNull()
            .SetValidator(new LinkDtoValidator());

        RuleFor(x => x.Settings)
            .NotNull()
            .SetValidator(new SettingsDtoValidator(fileSystem));

        RuleFor(x => x.Filters)
            .NotNull()
            .SetValidator(new FiltersDtoValidator());
    }
}
