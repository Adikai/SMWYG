using System;
using System.Globalization;
using System.Windows.Data;

namespace SMWYG.Converters
{
    public class StringToInitialsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string str || string.IsNullOrWhiteSpace(str))
                return "?";

            var parts = str.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return "?";

            var initials = parts.Length == 1
                ? parts[0][0].ToString().ToUpper()
                : parts[0][0].ToString().ToUpper() + parts[^1][0].ToString().ToUpper();

            return initials;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}