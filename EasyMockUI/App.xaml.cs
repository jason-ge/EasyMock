using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace EasyMock.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; }
        public static IConfiguration Config { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            var services = new ServiceCollection();

            services.AddHttpClient();
            services.AddSingleton<MainWindowViewModel>();
            services.AddTransient<IFileDialogService, FileDialogService>();
            services.AddTransient<IDialogService, DialogService>();

            ServiceProvider = services.BuildServiceProvider();
            Config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
            base.OnStartup(e);

            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }
}
