using Microsoft.Win32;

namespace MovieEditor.ViewModels;

internal class DialogHandler
{
    private readonly OpenFolderDialog _folderDialog = new();

    public DialogHandler()
    {
        // 設定値
        _folderDialog.Multiselect = false;
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