using ControlzEx.Theming;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Threading;

namespace EasyMock.UI
{
    internal class WindowService : IWindowService
    {
        private readonly IServiceProvider _provider;

        public WindowService(IServiceProvider provider)
        {
            _provider = provider;
        }

        public bool ConfirmCloseWithUnsavedChanges()
        {
            var result = MessageBox.Show(
                "You have unsaved changes. Do you really want to exit?",
                "Unsaved Changes",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            return result == MessageBoxResult.Yes;
        }

        public bool OpenMockNodeEditWindow(ref MockNodeEditorViewModel viewModel)
        {
            var window = _provider.GetRequiredService<MockNodeEditorWindow>();
            window.DataContext = viewModel;
            return window.ShowDialog() ?? false;
        }

        public bool OpenNewMockFileWindow(ref NewMockFileViewModel viewModel)
        {
            var window = _provider.GetRequiredService<NewMockFileWindow>();
            window.DataContext = viewModel;
            return window.ShowDialog() ?? false;
        }

        public bool OpenReplayInQAWindow(ReplayInQAViewModel viewModel)
        {
            var window = _provider.GetRequiredService<MockNodeEditorWindow>();
            window.DataContext = viewModel;
            return window.ShowDialog() ?? false;
        }

        public void ChangeTheme(string theme)
        {
            ThemeManager.Current.ChangeTheme(Application.Current, theme);
        }

        public void DisplayStartupErrors(string message)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                MessageBox.Show(Application.Current.MainWindow, message, "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }, DispatcherPriority.ApplicationIdle);
        }

        public void Shutdown()
        {
            Application.Current.Shutdown();
        }

        public void DispatcherInvoke(Action action)
        {
            action();
        }
    }
}