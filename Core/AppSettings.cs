using System.Text.Json;
using Microsoft.Win32;

namespace Copi2Ctrl.Core;

public enum TargetControlKey
{
    LeftControl,
    RightControl
}

public class AppSettings
{
    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Copi2Ctrl");

    private static readonly string ConfigPath = Path.Combine(ConfigDir, "settings.json");
    private const string StartupRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "Copi2Ctrl";

    public bool Enabled { get; set; } = true;
    public TargetControlKey TargetKey { get; set; } = TargetControlKey.RightControl;
    public bool RunAtStartup { get; set; } = false;
    public bool LogToConsole { get; set; } = false;

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings != null)
                {
                    settings.RunAtStartup = CheckIsStartupEnabled();
                    return settings;
                }
            }
        }
        catch
        {
            // フォールバック
        }

        var defaultSettings = new AppSettings
        {
            RunAtStartup = CheckIsStartupEnabled()
        };
        return defaultSettings;
    }

    public void Save()
    {
        try
        {
            if (!Directory.Exists(ConfigDir))
            {
                Directory.CreateDirectory(ConfigDir);
            }

            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }

    public static bool CheckIsStartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, false);
            var value = key?.GetValue(AppName);
            return value != null;
        }
        catch
        {
            return false;
        }
    }

    public void SetStartup(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, true);
            if (key == null) return;

            if (enable)
            {
                var exePath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(exePath))
                {
                    key.SetValue(AppName, $"\"{exePath}\"");
                    RunAtStartup = true;
                }
            }
            else
            {
                key.DeleteValue(AppName, false);
                RunAtStartup = false;
            }
            Save();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to set startup registry: {ex.Message}");
        }
    }
}
