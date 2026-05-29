using System.Diagnostics;

namespace WeChatPublisher.Services;

public static class Logger
{
    private static readonly string LogPath =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.log");

    static Logger()
    {
        File.AppendAllText(LogPath, $"\n=== App started {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n");
    }

    public static void Info(string msg) => Log("INFO", msg);
    public static void Warn(string msg) => Log("WARN", msg);
    public static void Error(string msg, Exception? ex = null)
    {
        Log("ERROR", msg);
        if (ex != null) Log("ERROR", ex.ToString());
    }

    private static void Log(string level, string msg)
    {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] [{level}] {msg}";
        Debug.WriteLine(line);
        try { File.AppendAllText(LogPath, line + "\n"); } catch { }
    }
}
