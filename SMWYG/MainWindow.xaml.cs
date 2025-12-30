using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace SMWYG
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel viewModel)  // Injected directly!
        {
            InitializeComponent();
            DataContext = viewModel;

            Loaded += async (s, e) =>
            {
                await viewModel.LoadServersAsync();
            };
        }
    }
}