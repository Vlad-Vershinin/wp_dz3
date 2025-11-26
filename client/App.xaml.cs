using client.Models;
using client.Services;
using client.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using System.Reactive.Concurrency;
using System.Windows;

namespace client
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            RxApp.MainThreadScheduler = DispatcherScheduler.Current;

            var services = new ServiceCollection();
            services.AddSingleton<MainWindowViewModel>();
            services.AddTransient<IChatClientService, TcpChatClientService>();

            ServiceProvider = services.BuildServiceProvider();

            //var mainWindow = new Views.MainWindow();
            //mainWindow.Show();
        }
    }

}
