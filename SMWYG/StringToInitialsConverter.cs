using System;
using System.Globalization;
using System.Windows.Data;

namespace SMWYG
{
    public class StringToInitialsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string name && !string.IsNullOrWhiteSpace(name))
            {
                var words = name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length == 1)
                {
                    return words[0].Length >= 2 ? words[0].Substring(0, 2).ToUpperInvariant() : words[0].ToUpperInvariant();
                }
                else
                {
                    return string.Concat(words[0][0], words[words.Length - 1][0]).ToUpperInvariant();
                }
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}