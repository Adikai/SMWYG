using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;

namespace SMWYG.Windows
{
    public partial class UploadProgressWindow : Window, INotifyPropertyChanged
    {
        public CancellationTokenSource Cts { get; } = new CancellationTokenSource();

        private double uploadProgress;
        public double UploadProgress
        {
            get => uploadProgress;
            set { uploadProgress = value; OnPropertyChanged(); }
        }

        private bool isUploading;
        public bool IsUploading
        {
            get => isUploading;
            set { isUploading = value; OnPropertyChanged(); }
        }

        public UploadProgressWindow()
        {
            InitializeComponent();
            DataContext = this;
            UploadProgress = 0;
            IsUploading = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Cts.Cancel();
            // Notify main VM via dispatcher to show toast notification
            Application.Current?.Dispatcher.Invoke(async () =>
            {
                try
                {
                    var vm = App.Services.GetService(typeof(SMWYG.MainViewModel)) as SMWYG.MainViewModel;
                    if (vm != null)
                    {
                        await vm.NotifyAsync("Upload cancelled.", false);
                    }
                }
                catch
                {
                    // swallow
                }
            });
            Close();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
