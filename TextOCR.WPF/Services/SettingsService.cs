namespace TextOCR.WPF.Services;

public interface ISettingsService
{
    T GetValue<T>(string key, T defaultValue);
    void SetValue<T>(string key, T value);
    void Save();
}

public class SettingsService : ISettingsService
{
    public SettingsService()
    {
        // 确保 Settings 已初始化
        try
        {
            _ = Properties.Settings.Default;
        }
        catch
        {
            // Ignore initialization errors
        }
    }

    public T GetValue<T>(string key, T defaultValue)
    {
        try
        {
            if (Properties.Settings.Default == null)
                return defaultValue;

            var value = Properties.Settings.Default[key];
            if (value == null)
                return defaultValue;

            return (T)value;
        }
        catch
        {
            return defaultValue;
        }
    }

    public void SetValue<T>(string key, T value)
    {
        try
        {
            if (Properties.Settings.Default != null)
            {
                Properties.Settings.Default[key] = value;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to set setting {key}: {ex.Message}");
        }
    }

    public void Save()
    {
        try
        {
            Properties.Settings.Default?.Save();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }
}
