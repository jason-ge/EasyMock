using MahApps.Metro.Controls;

namespace EasyMock.UI
{
    public partial class MockNodeEditorWindow : MetroWindow
    {
        public MockNodeEditorWindow()
        {
            InitializeComponent();
            DataContext = new MockNodeEditorViewModel();
        }
    }
}
