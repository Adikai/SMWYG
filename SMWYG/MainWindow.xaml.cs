using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Collections.Specialized;

namespace SMWYG
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _vm;

        public MainWindow(MainViewModel viewModel)  // Injected directly!
        {
            InitializeComponent();
            DataContext = viewModel;
            _vm = viewModel;

            Loaded += async (s, e) =>
            {
                await viewModel.LoadServersAsync();

                if (viewModel.Messages is System.Collections.ObjectModel.ObservableCollection<SMWYG.Models.Message> coll)
                {
                    coll.CollectionChanged += Messages_CollectionChanged;
                }
            };
        }

        private void Messages_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                // Wait for layout to update then scroll
                Dispatcher.InvokeAsync(() =>
                {
                    MessagesScrollViewer.ScrollToEnd();
                }, System.Windows.Threading.DispatcherPriority.Background);
            }
        }
    }
}