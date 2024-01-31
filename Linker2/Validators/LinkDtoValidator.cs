using FluentValidation;
using Linker2.Model;
using System;

namespace Linker2.Validators;

public class LinkDtoValidator : AbstractValidator<LinkDto>
{
    public const int MinLinkRating = 1;
    public const int MaxLinkRating = 5;

    public LinkDtoValidator()
    {
        RuleFor(x => x.Url)
            .NotNull();
        When(x => x.Url is not null, () =>
        {
            RuleFor(x => x.Url)
                .Must(IsUrl);
        });

        When(x => x.ThumbnailUrl is not null, () =>
        {
            RuleFor(x => x.ThumbnailUrl!)
                .Must(IsUrl);
        });

        When(x => x.Rating is not null, () =>
        {
            RuleFor(x => x.Rating).InclusiveBetween(MinLinkRating, MaxLinkRating);
        });

        RuleFor(x => x.Tags)
            .NotNull();
        RuleForEach(x => x.Tags)
            .NotNull()
            .NotEmpty();

        RuleFor(x => x.OpenCounter).GreaterThanOrEqualTo(0);
    }

    public static bool IsUrl(string url)
    {
        try
        {
            _ = new Uri(url);
            return true;
        }
        catch (UriFormatException)
        {
            return false;
        }
    }
}
