using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace SlideExtractor.WPF.Helpers;

public class DragDropHelper
{
	public static readonly DependencyProperty IsVideoDropTargetProperty =
		DependencyProperty.RegisterAttached(
			"IsVideoDropTarget",
			typeof(bool),
			typeof(DragDropHelper),
			new PropertyMetadata(false, OnIsVideoDropTargetChanged));

	public static void SetIsVideoDropTarget(UIElement element, bool value) => element.SetValue(IsVideoDropTargetProperty, value);
	public static bool GetIsVideoDropTarget(UIElement element) => (bool)element.GetValue(IsVideoDropTargetProperty);

	public event Action<string>? FileDropped;

	private static void OnIsVideoDropTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is not Control control) return;
		if ((bool)e.NewValue)
		{
			control.AllowDrop = true;
			control.PreviewDragEnter += ControlOnPreviewDragEnter;
			control.PreviewDragOver += ControlOnPreviewDragEnter;
			control.PreviewDrop += ControlOnPreviewDrop;
		}
		else
		{
			control.AllowDrop = false;
			control.PreviewDragEnter -= ControlOnPreviewDragEnter;
			control.PreviewDragOver -= ControlOnPreviewDragEnter;
			control.PreviewDrop -= ControlOnPreviewDrop;
		}
	}

	private static void ControlOnPreviewDragEnter(object sender, DragEventArgs e)
	{
		e.Effects = HasValidVideo(e) ? DragDropEffects.Copy : DragDropEffects.None;
		e.Handled = true;
	}

	private static void ControlOnPreviewDrop(object sender, DragEventArgs e)
	{
		if (sender is not FrameworkElement element) return;
		if (!HasValidVideo(e)) return;

		var helper = FindHelper(element);
		var files = (string[])e.Data.GetData(DataFormats.FileDrop);
		helper?.FileDropped?.Invoke(files[0]);
		e.Handled = true;
	}

	private static bool HasValidVideo(DragEventArgs e)
	{
		if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return false;
		var paths = (string[])e.Data.GetData(DataFormats.FileDrop);
		return paths.Length > 0 && File.Exists(paths[0]) && Path.GetExtension(paths[0]).Equals(".mp4", StringComparison.OrdinalIgnoreCase);
	}

	private static DragDropHelper? FindHelper(FrameworkElement element) =>
		element.Tag as DragDropHelper ?? element.DataContext as DragDropHelper;
}
