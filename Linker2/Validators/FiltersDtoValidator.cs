using FluentValidation;
using Linker2.Model;
using System.Collections.Generic;

namespace Linker2.Validators;

public class FiltersDtoValidator : AbstractValidator<FiltersDto>
{
    private static readonly List<string?> ratingValues = [null, Constants.NotRatedFilterText, "1", "2", "3", "4", "5"];

    public FiltersDtoValidator()
    {
        When(x => x.Text is not null, () =>
        {
            RuleFor(x => x.Text)
                .NotEmpty();
        });

        RuleFor(x => x.Rating).Must(x => ratingValues.Contains(x));

        When(x => x.Site is not null, () =>
        {
            RuleFor(x => x.Site)
                .NotEmpty();
        });

        RuleForEach(x => x.Tags)
            .NotNull()
            .NotEmpty();
    }
}
