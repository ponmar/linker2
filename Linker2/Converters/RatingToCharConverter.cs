using Avalonia.Data.Converters;
using System;

namespace Linker2.Converters;

public class RatingToCharConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is not null &&
            parameter is not null)
        {
            var linkRating = (int)value;
            var buttonRating = int.Parse((string)parameter);
            if (buttonRating <= linkRating)
            {
                return RatingToTextConverter.RatingChar;
            }
        }
        return RatingToTextConverter.NoRatingChar;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
