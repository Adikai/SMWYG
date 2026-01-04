using System;
using System.Globalization;
using System.Windows.Data;

namespace SMWYG.Converters
{
    public class ChannelTypeToIconConverter : IValueConverter
    {
        private const string TextIcon = "#";
        private const string VoiceIcon = "??";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string type && type.Equals("voice", StringComparison.OrdinalIgnoreCase))
            {
                return VoiceIcon;
            }

            return TextIcon;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
