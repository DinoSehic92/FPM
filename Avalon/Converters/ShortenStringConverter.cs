using System;
using System.Diagnostics;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Avalon.Converters
{
    public class ShortenStringConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            string input = (string)value;

            return input[0].ToString();
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return "none";
        }
    }
}