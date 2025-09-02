using Microsoft.Win32;

public class FileDialogService : IFileDialogService
{
    public string? OpenFile(string filter)
    {
        var dlg = new OpenFileDialog { Filter = filter };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }

    public string SaveFile(string filter, string defaultExt)
    {
        SaveFileDialog saveFileDialog = new SaveFileDialog
        {
            Filter = filter,
            DefaultExt = defaultExt
        };

        return saveFileDialog.ShowDialog() == true ? saveFileDialog.FileName : null;
    }
}