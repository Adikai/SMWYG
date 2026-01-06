using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace SMWYG.Converters
{
    public class RelativePathToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string rawPath || string.IsNullOrWhiteSpace(rawPath))
            {
                return DependencyProperty.UnsetValue;
            }

            var trimmed = rawPath.Trim();

            if (TryCreateBitmap(trimmed, out var bitmap))
            {
                return bitmap;
            }

            var apiBase = App.Configuration?["ApiBaseUrl"]
                          ?? App.Configuration?["AppSettings:ApiBaseUrl"];
            if (!string.IsNullOrWhiteSpace(apiBase)
                && Uri.TryCreate(apiBase, UriKind.Absolute, out var baseUri))
            {
                try
                {
                    var combinedUri = new Uri(baseUri, trimmed.TrimStart('/'));
                    if (TryCreateBitmap(combinedUri.AbsoluteUri, out bitmap))
                    {
                        return bitmap;
                    }
                }
                catch
                {
                    // ignore and continue to default
                }
            }

            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private static bool TryCreateBitmap(string source, out BitmapImage bitmap)
        {
            bitmap = null!;

            try
            {
                if (Uri.TryCreate(source, UriKind.Absolute, out var uri))
                {
                    if (uri.IsFile && !File.Exists(uri.LocalPath))
                    {
                        return false;
                    }

                    bitmap = LoadBitmap(uri);
                    return true;
                }

                var localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, source.TrimStart('\\', '/'));
                if (!File.Exists(localPath))
                {
                    return false;
                }

                bitmap = LoadBitmap(new Uri(localPath, UriKind.Absolute));
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static BitmapImage LoadBitmap(Uri uri)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = uri;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
    }
}
