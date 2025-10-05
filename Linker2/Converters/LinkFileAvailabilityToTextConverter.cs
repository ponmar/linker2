using Avalonia.Data.Converters;
using Linker2.Configuration;
using Linker2.Validators;
using System;

namespace Linker2.Converters;

public class LinkFileAvailabilityToTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is LinkFileAvailability enumValue)
        {
            return enumValue switch 
            {
                LinkFileAvailability.Available => "Available",
                LinkFileAvailability.NotAvailable => "Not available",
                _ => string.Empty,
            };
        }
        return string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
