using System.Windows;
using System.Windows.Media.Imaging;

namespace SMWYG.Windows
{
    public partial class ImageViewer : Window
    {
        public ImageViewer()
        {
            InitializeComponent();
        }

        public void LoadFromUrl(string url)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new System.Uri(url);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            ViewerImage.Source = bitmap;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
