using CustomSelectFileDlg;
using Microsoft.Win32;
using System;
using System.Threading.Tasks;

namespace SlideExtractor.WPF.Helpers;

public class FileDialogHelper
{
	public Task<string?> PickVideoAsync()
	{
		return Task.Run(() =>
		{
			var dialog = CreateCustomDialog();
			if (dialog is null)
			{
				var fallback = new OpenFileDialog
				{
					Filter = "MP4 文件 (*.mp4)|*.mp4",
					Title = "选择本地 MP4 文件",
					CheckFileExists = true,
					Multiselect = false
				};
				return fallback.ShowDialog() == true ? fallback.FileName : null;
			}

			dynamic dynamicDialog = dialog;
			dynamicDialog.Filter = "MP4 文件 (*.mp4)|*.mp4";
			dynamicDialog.Title = "选择本地 MP4 文件";
			dynamicDialog.CheckFileExists = true;
			dynamicDialog.MultiSelect = false;

			var result = dynamicDialog.ShowDialog();
			return result == true ? (string)dynamicDialog.FileName : null;
		});
	}

	private static object? CreateCustomDialog()
	{
		var type = Type.GetType("CustomSelectFileDlg.SelectFileDialog, CustomSelectFileDlg");
		return type == null ? null : Activator.CreateInstance(type);
	}
}
