namespace EasyMock.UI
{
    public interface IWindowService
    {
        bool ConfirmCloseWithUnsavedChanges();
        bool OpenMockNodeEditWindow(ref MockNodeEditorViewModel viewModel);
        bool OpenNewMockFileWindow(ref NewMockFileViewModel viewModel);
        void OpenReplayInQAWindow(ReplayInQAViewModel viewModel);
        void ChangeTheme(string theme);
        void DisplayStartupErrors(string message);
        void Shutdown();
        void DispatcherInvoke(Action action);
    }
}