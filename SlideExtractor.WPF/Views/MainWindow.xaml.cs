using SlideExtractor.WPF.Helpers;
using SlideExtractor.WPF.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace SlideExtractor.WPF.Views;

public partial class MainWindow : Window
{
	private MainViewModel _viewModel;
	private SlideSelectionViewModel _slidesVm; // �Ƴ� readonly
	private SettingsViewModel _settingsVm;     // �Ƴ� readonly
	private readonly SettingsHelper _settingsHelper;

	public MainWindow(
		MainViewModel viewModel,
		SlideSelectionViewModel slideSelectionViewModel,
		SettingsViewModel settingsViewModel,
		SettingsHelper settingsHelper,
		DragDropHelper dragDropHelper)
	{
		InitializeComponent();
		DataContext = viewModel;
		_viewModel = viewModel;
		_slidesVm = slideSelectionViewModel;
		_settingsVm = settingsViewModel;
		_settingsHelper = settingsHelper;

		SlideSelectionHost.DataContext = _slidesVm;
		SettingsHost.DataContext = _settingsVm;

		_slidesVm.Attach(_viewModel.Slides);

		dragDropHelper.FileDropped += path => _viewModel.SelectedVideoPath = path;
		Tag = dragDropHelper;

		AddHandler(Keyboard.KeyDownEvent, new KeyEventHandler(OnKeyDown));
	}

	private void OnKeyDown(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.O && Keyboard.Modifiers == ModifierKeys.Control)
		{
			_viewModel.BrowseCommand.Execute(null);
		}
		else if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
		{
			_viewModel.ExportImagesCommand.Execute(null);
		}
		else if (e.Key == Key.A && Keyboard.Modifiers == ModifierKeys.Control)
		{
			_slidesVm.SelectAllCommand.Execute(null);
		}
		else if (e.Key == Key.D && Keyboard.Modifiers == ModifierKeys.Control)
		{
			_slidesVm.DeselectAllCommand.Execute(null);
		}
		else if (e.Key == Key.F5)
		{
			_viewModel.StartExtractionCommand.Execute(null);
		}
	}

	protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
	{
		base.OnClosing(e);
		Properties.Settings.Default.WindowWidth = Width;
		Properties.Settings.Default.WindowHeight = Height;
		Properties.Settings.Default.WindowLeft = Left;
		Properties.Settings.Default.WindowTop = Top;
		_settingsHelper.RegisterRecentFile(_viewModel.SelectedVideoPath);
	}

	public static readonly DependencyProperty SlideSelectionHostProperty =
		DependencyProperty.Register(nameof(SlideSelectionHostProperty), typeof(SlideSelectionViewModel), typeof(MainWindow), new PropertyMetadata(null, OnSlideSelectionHostChanged));

	public static readonly DependencyProperty SettingsHostProperty =
		DependencyProperty.Register(nameof(SettingsHostProperty), typeof(SettingsViewModel), typeof(MainWindow), new PropertyMetadata(null, OnSettingsHostChanged));

	private static void OnSlideSelectionHostChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		var window = d as MainWindow;
		if (window != null)
		{
			window._slidesVm = e.NewValue as SlideSelectionViewModel;
			window.SlideSelectionHost.DataContext = window._slidesVm;
		}
	}

	private static void OnSettingsHostChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		var window = d as MainWindow;
		if (window != null)
		{
			window._settingsVm = e.NewValue as SettingsViewModel;
			window.SettingsHost.DataContext = window._settingsVm;
		}
	}

	public MainWindow()
	{
		SlideSelectionHost.DataContext = _slidesVm;
		SettingsHost.DataContext = _settingsVm;
	}
}
