using System;
using System.IO;
using System.Windows.Forms;

namespace PlaylistMaker.WPF.Services;

public sealed class DialogService : IDialogService
{
    public string? PickFolder(string? initialPath)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "请选择要扫描的媒体文件夹",
            ShowNewFolderButton = true,
            SelectedPath = Directory.Exists(initialPath)
                ? initialPath
                : Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)
        };

        return dialog.ShowDialog() == DialogResult.OK ? dialog.SelectedPath : null;
    }
}
