using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using SlideExtractor.WPF.Helpers;
using SlideExtractor.WPF.Models;
using SlideExtractor.WPF.Services;
using SlideExtractor.WPF.ViewModels;
using SlideExtractor.WPF.Views;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace SlideExtractor.WPF;

public partial class App : Application
{
	private IHost? _host;

	static App()
	{
		var bootstrapLogPath = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
			"SlideExtractor.WPF",
			"logs");

		Directory.CreateDirectory(bootstrapLogPath);

		Log.Logger = new LoggerConfiguration()
			.MinimumLevel.Debug()
			.WriteTo.Debug()
			.WriteTo.File(Path.Combine(bootstrapLogPath, "bootstrap-.log"), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
			.CreateLogger();
	}

	protected override void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);

		try
		{
			SettingsHelper.InitializeDefaults();
			_host = BuildHost(e.Args);
			_host.Start();

			var mainWindow = _host.Services.GetRequiredService<MainWindow>();
			MainWindow = mainWindow;
			ShutdownMode = ShutdownMode.OnMainWindowClose;
			mainWindow.Show();

			AppDomain.CurrentDomain.UnhandledException += (_, args) =>
				Log.Fatal(args.ExceptionObject as Exception, "Unhandled exception");
			TaskScheduler.UnobservedTaskException += (_, args) =>
			{
				Log.Error(args.Exception, "Unobserved task exception");
				args.SetObserved();
			};

			Log.Information("应用启动完成。");
		}
		catch (Exception ex)
		{
			Log.Fatal(ex, "应用启动失败");
			MessageBox.Show($"应用启动失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
			Current.Shutdown(-1);
		}
	}

	private static IHost BuildHost(string[] args) =>
		Host.CreateDefaultBuilder(args)
			.UseSerilog((_, configuration) =>
			{
				var logPath = Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
					"SlideExtractor.WPF",
					"logs",
					"app-.log");

				configuration
					.MinimumLevel.Debug()
					.MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
					.Enrich.FromLogContext()
					.WriteTo.Debug()
					.WriteTo.File(logPath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30);
			})
			.ConfigureServices((_, services) =>
			{
				services.AddSingleton<VideoMetadataStore>();
				services.AddSingleton<SettingsHelper>();
				services.AddSingleton<ILoggingService, LoggingService>();
				services.AddSingleton<IFrameExtractionService, FrameExtractionService>();
				services.AddSingleton<ISlideDeduplicationService, SlideDeduplicationService>();
				services.AddSingleton<IOcrService, OcrService>();
				services.AddSingleton<IPresentationService, PresentationService>();
				services.AddSingleton<FileDialogHelper>();
				services.AddSingleton<DragDropHelper>();
				services.AddSingleton<SlideSelectionViewModel>();
				services.AddSingleton<SettingsViewModel>();
				services.AddSingleton<MainViewModel>();
				services.AddSingleton<MainWindow>();
			})
			.Build();

	protected override async void OnExit(ExitEventArgs e)
	{
		SettingsHelper.Save();

		if (_host is not null)
		{
			await _host.StopAsync();
			_host.Dispose();
		}

		Log.CloseAndFlush();
		base.OnExit(e);
	}
}
