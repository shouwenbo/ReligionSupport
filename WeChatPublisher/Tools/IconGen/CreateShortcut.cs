using System.Runtime.InteropServices;
using System.Text;

class Program
{
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern bool SHCreateShortcut(string pszShortcut, string pszTarget, string pszArgs,
        string pszIconPath, int iIconIndex);

    static void Main()
    {
        var baseDir = Path.GetFullPath(Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."));
        var publish = Path.Combine(baseDir, "bin", "Release", "net8.0-windows", "publish");
        var exeName = "微讯创作发布工具.exe";
        var shortcutName = "微讯创作发布工具.lnk";

        var startMenu = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
            "Programs", "微讯创作发布工具");
        Directory.CreateDirectory(startMenu);

        var exePath = Path.Combine(publish, exeName);
        var icoPath = Path.Combine(baseDir, "Assets", "app.ico");
        var lnkPath = Path.Combine(startMenu, shortcutName);

        if (!File.Exists(exePath))
        {
            Console.WriteLine($"EXE not found: {exePath}");
            return;
        }

        // Use COM to create shortcut (proper Unicode support)
        Type t = Type.GetTypeFromProgID("WScript.Shell")!;
        dynamic ws = Activator.CreateInstance(t)!;
        var sc = ws.CreateShortcut(lnkPath);
        sc.TargetPath = exePath;
        sc.IconLocation = $"{icoPath},0";
        sc.WorkingDirectory = publish;
        sc.Description = "公众号视频号AI创作发布工具";
        sc.Save();

        Console.WriteLine($"Shortcut created:");
        Console.WriteLine($"  {lnkPath}");
        Console.WriteLine($"  Target: {exePath}");
        Console.WriteLine($"  Icon: {icoPath}");
    }
}
