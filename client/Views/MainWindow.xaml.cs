using client.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace client.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var vm = App.ServiceProvider.GetRequiredService<MainWindowViewModel>();
            DataContext = vm;
        }
    }
}