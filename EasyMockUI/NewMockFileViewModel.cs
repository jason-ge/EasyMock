using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace EasyMock.UI
{
    internal class NewMockFileViewModel: INotifyPropertyChanged
    {
        public ICommand OkCommand { get; }
        private string _mockFileName;
        public string MockFileName
        {
            get { return _mockFileName; }
            set
            {
                _mockFileName = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MockFileName)));
            }
        }

        public NewMockFileViewModel()
        {
            MockFileName = "";
            OkCommand = new RelayCommand<Window>(OnOk);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnOk(Window? window)
        {
            if (string.IsNullOrWhiteSpace(MockFileName))
            {
                MessageBox.Show("Mock file name cannot be empty.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            else if (string.IsNullOrEmpty(Path.GetExtension(MockFileName)))
            {
                MockFileName += ".xml";
            }
            else if (!string.Equals(Path.GetExtension(MockFileName), ".xml", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Mock file name must have .xml extension.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (window != null)
            {
                window.DialogResult = true;
                window.Close();
            }
        }

    }
}
