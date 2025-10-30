using MahApps.Metro.Controls;

namespace EasyMock.UI
{
    /// <summary>
    /// Interaction logic for NewMockFileWindow.xaml
    /// </summary>
    public partial class NewMockFileWindow : MetroWindow
    {
        public NewMockFileWindow()
        {
            InitializeComponent();
            DataContext = new NewMockFileViewModel();
        }
    }
}
