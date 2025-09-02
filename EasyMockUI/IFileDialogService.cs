public interface IFileDialogService
{
    string? OpenFile(string filter);
    string? SaveFile(string filter, string defaultExt);
}