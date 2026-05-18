using Microsoft.Win32;

namespace FavoriteSticker.Services;

public class AutoStartService
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "FavoriteSticker";

    public bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(AppName) != null;
        }
    }

    public void Enable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(RunKey);
        var exePath = Environment.ProcessPath ?? System.Reflection.Assembly.GetExecutingAssembly().Location;
        key.SetValue(AppName, $"\"{exePath}\"");
    }

    public void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        if (key == null) return;
        try { key.DeleteValue(AppName, throwOnMissingValue: false); } catch { }
    }

    public void SetEnabled(bool enabled)
    {
        if (enabled) Enable(); else Disable();
    }
}
