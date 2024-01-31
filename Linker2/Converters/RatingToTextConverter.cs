using Avalonia.Data.Converters;
using Linker2.Validators;
using System;

namespace Linker2.Converters;

public class RatingToTextConverter : IValueConverter
{
    public const char RatingChar = '\x2605';
    public const char NoRatingChar = '\x2606';

    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is not null)
        {
            var ratingValue = (int)value;
            return new string(RatingChar, ratingValue) + new string(NoRatingChar, LinkDtoValidator.MaxLinkRating - ratingValue);
        }
        return string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
