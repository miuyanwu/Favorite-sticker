using System.Text.Json.Serialization;

namespace FavoriteSticker.Models;

public class AppConfig
{
    public HotkeyConfig Hotkey { get; set; } = new();
    public CleanupConfig Cleanup { get; set; } = new();
    public AutoStartConfig AutoStart { get; set; } = new();
    public StorageConfig Storage { get; set; } = new();
}

public class HotkeyConfig
{
    public string Modifiers { get; set; } = "Alt";
    public string Key { get; set; } = "V";
}

public class CleanupConfig
{
    public int MaxHistoryCount { get; set; } = 200;
    public int MaxAgeDays { get; set; } = 30;
    public int MaxTextChars { get; set; } = 10000;
    public long MaxImageSizeBytes { get; set; } = 5 * 1024 * 1024; // 5 MB
    public bool DedupEnabled { get; set; } = true;
}

public class AutoStartConfig
{
    public bool Enabled { get; set; } = true;
}

public class StorageConfig
{
    public string DataPath { get; set; } = string.Empty;

    [JsonIgnore]
    public string ResolvedDataPath =>
        string.IsNullOrWhiteSpace(DataPath)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FavoriteSticker")
            : Environment.ExpandEnvironmentVariables(DataPath);
}
