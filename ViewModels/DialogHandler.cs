using Microsoft.Win32;

namespace MovieEditor.ViewModels;

internal class DialogHandler
{
    private readonly OpenFileDialog _fileDialog = new();
    private readonly OpenFolderDialog _folderDialog = new();

    public DialogHandler()
    {
        // 設定値
        _fileDialog.Multiselect = true;
        _fileDialog.Filter = "MP4|*.mp4|AVI|*.avi|AGM|*.agm|Windows Media Video|*.wmv|MOV|*.MOV|すべてのファイル|*.*";
        _folderDialog.Multiselect = false;
    }

    public string[]? GetFilesFromDialog()
    {
        bool? isSuccess = _fileDialog.ShowDialog();

        if(true == isSuccess)
        {
            return _fileDialog.FileNames;
        }
        else
        {
            return null;
        }
    }

    public string? GetDirectoryFromDialog()
    {
        bool? isSuccess = _folderDialog.ShowDialog();

        if(true == isSuccess)
        {
            return _folderDialog.FolderName;
        }
        else
        {
            return null;
        }
    }
}