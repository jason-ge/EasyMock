using MahApps.Metro.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace EasyMock.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = App.ServiceProvider.GetRequiredService<MainWindowViewModel>();
        }
    }
}
