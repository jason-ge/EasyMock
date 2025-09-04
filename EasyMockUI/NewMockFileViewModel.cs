using EasyMockLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace EasyMock.UI
{
    internal class NewMockFileViewModel
    {
        public ICommand OkCommand { get; }
        public string MockFileName { get; set; }

        public NewMockFileViewModel()
        {
            MockFileName = "";
            OkCommand = new RelayCommand<object>(OnOk);
        }

        private void OnOk(object? windowObj)
        {
            if (windowObj is Window window)
            {
                window.DialogResult = true;
                window.Close();
            }
        }

    }
}
