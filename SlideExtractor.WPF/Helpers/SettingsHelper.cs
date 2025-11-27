using SlideExtractor.WPF.Properties;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;

namespace SlideExtractor.WPF.Helpers;

public class SettingsHelper
{
	private static readonly object Locker = new();

	public static void InitializeDefaults()
	{
		lock (Locker)
		{
			if (Settings.Default.RecentFiles == null)
			{
				Settings.Default.RecentFiles = new StringCollection();
			}

			if (Settings.Default.IsFirstRun)
			{
				Settings.Default.OutputDirectory = Environment.ExpandEnvironmentVariables(Settings.Default.OutputDirectory);
				Settings.Default.IsFirstRun = false;
				Settings.Default.Save();
			}
		}
	}

	public static void Save()
	{
		lock (Locker)
		{
			Settings.Default.Save();
		}
	}

	public void RegisterRecentFile(string filePath)
	{
		lock (Locker)
		{
			if (Settings.Default.RecentFiles == null)
			{
				Settings.Default.RecentFiles = new StringCollection();
			}

			var normalized = Path.GetFullPath(filePath);
			var existing = Settings.Default.RecentFiles.Cast<string>().ToList();
			existing.Remove(normalized);
			existing.Insert(0, normalized);
			while (existing.Count > 10)
			{
				existing.RemoveAt(existing.Count - 1);
			}

			Settings.Default.RecentFiles.Clear();
			foreach (var item in existing)
			{
				Settings.Default.RecentFiles.Add(item);
			}

			Settings.Default.LastVideoPath = normalized;
			Settings.Default.Save();
		}
	}
}
