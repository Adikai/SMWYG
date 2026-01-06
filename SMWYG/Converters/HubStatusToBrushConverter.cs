using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SMWYG.Converters
{
    public class HubStatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s)
            {
                return s switch
                {
                    "Connected" => new SolidColorBrush(Color.FromRgb(60, 180, 75)), // green
                    "Reconnecting..." => new SolidColorBrush(Color.FromRgb(255, 165, 0)), // orange
                    _ => new SolidColorBrush(Color.FromRgb(220, 53, 69)), // red
                };
            }

            return new SolidColorBrush(Color.FromRgb(220, 53, 69));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
