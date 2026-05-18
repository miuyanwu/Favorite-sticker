using System.Text.Json;
using Dapper;
using Microsoft.Data.Sqlite;
using FavoriteSticker.Models;

namespace FavoriteSticker.Services;

public class DataStore
{
    private readonly string _connectionString;
    private readonly string _configPath;
    private readonly string _dataDir;
    private readonly string _imagesDir;
    private readonly string _thumbnailsDir;
    private AppConfig _config = new();

    static DataStore()
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    public DataStore(string? customDataPath = null)
    {
        _config = LoadConfig(customDataPath);

        if (!string.IsNullOrWhiteSpace(customDataPath))
            _config.Storage.DataPath = customDataPath;

        _dataDir = _config.Storage.ResolvedDataPath;
        _imagesDir = Path.Combine(_dataDir, "images");
        _thumbnailsDir = Path.Combine(_dataDir, "thumbnails");
        _configPath = Path.Combine(_dataDir, "config.json");

        Directory.CreateDirectory(_dataDir);
        Directory.CreateDirectory(_imagesDir);
        Directory.CreateDirectory(_thumbnailsDir);

        _connectionString = $"Data Source={Path.Combine(_dataDir, "favorite-sticker.db")};Pooling=False";
        InitializeDatabase();
    }

    // ---- Config ----

    public AppConfig Config
    {
        get => _config;
        set
        {
            _config = value;
            SaveConfig();
        }
    }

    private AppConfig LoadConfig(string? customDataPath = null)
    {
        // Try data paths in priority order
        var searchPaths = new List<string>();

        if (!string.IsNullOrWhiteSpace(customDataPath))
        {
            var resolved = string.IsNullOrWhiteSpace(customDataPath)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FavoriteSticker")
                : Environment.ExpandEnvironmentVariables(customDataPath);
            searchPaths.Add(Path.Combine(resolved, "config.json"));
        }

        var defaultDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FavoriteSticker");
        searchPaths.Add(Path.Combine(defaultDataDir, "config.json"));

        foreach (var path in searchPaths.Distinct())
        {
            if (File.Exists(path))
            {
                try
                {
                    var json = File.ReadAllText(path);
                    return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                }
                catch { }
            }
        }
        return new AppConfig();
    }

    private void SaveConfig()
    {
        var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_configPath, json);
    }

    // ---- Database ----

    private void InitializeDatabase()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        // Ensure DELETE journal mode for cross-connection visibility
        conn.Execute("PRAGMA journal_mode=DELETE");

        conn.Execute(@"
            CREATE TABLE IF NOT EXISTS clipboard_items (
                id TEXT PRIMARY KEY,
                content_type INTEGER NOT NULL,
                text_content TEXT,
                image_path TEXT,
                thumbnail_path TEXT,
                char_count INTEGER NOT NULL DEFAULT 0,
                file_size INTEGER NOT NULL DEFAULT 0,
                copy_count INTEGER NOT NULL DEFAULT 1,
                content_hash TEXT NOT NULL DEFAULT '',
                is_favorite INTEGER NOT NULL DEFAULT 0,
                is_starred INTEGER NOT NULL DEFAULT 0,
                folder_id TEXT,
                created_at TEXT NOT NULL,
                last_copied_at TEXT NOT NULL,
                FOREIGN KEY (folder_id) REFERENCES folders(id) ON DELETE SET NULL
            );

            CREATE TABLE IF NOT EXISTS folders (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                parent_id TEXT,
                sort_order INTEGER NOT NULL DEFAULT 0,
                FOREIGN KEY (parent_id) REFERENCES folders(id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS reminders (
                id TEXT PRIMARY KEY,
                item_id TEXT NOT NULL,
                remind_at TEXT NOT NULL,
                reminder_type INTEGER NOT NULL DEFAULT 0,
                is_fired INTEGER NOT NULL DEFAULT 0,
                FOREIGN KEY (item_id) REFERENCES clipboard_items(id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS idx_items_folder ON clipboard_items(folder_id);
            CREATE INDEX IF NOT EXISTS idx_items_hash ON clipboard_items(content_hash);
            CREATE INDEX IF NOT EXISTS idx_items_created ON clipboard_items(created_at);
            CREATE INDEX IF NOT EXISTS idx_items_starred ON clipboard_items(is_starred);
            CREATE INDEX IF NOT EXISTS idx_folders_parent ON folders(parent_id);
            CREATE INDEX IF NOT EXISTS idx_reminders_fired ON reminders(is_fired, remind_at);
        ");
    }

    public SqliteConnection OpenConnection()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }

    // ---- Clipboard Items CRUD ----

    public void InsertItem(ClipboardItem item)
    {
        using var conn = OpenConnection();
        conn.Execute(@"
            INSERT INTO clipboard_items (id, content_type, text_content, image_path, thumbnail_path,
                char_count, file_size, copy_count, content_hash, is_favorite, is_starred,
                folder_id, created_at, last_copied_at)
            VALUES (@Id, @ContentType, @TextContent, @ImagePath, @ThumbnailPath,
                @CharCount, @FileSize, @CopyCount, @ContentHash, @IsFavorite, @IsStarred,
                @FolderId, @CreatedAt, @LastCopiedAt)", item);
    }

    public void UpdateItem(ClipboardItem item)
    {
        using var conn = OpenConnection();
        conn.Execute(@"
            UPDATE clipboard_items SET
                content_type = @ContentType,
                text_content = @TextContent,
                image_path = @ImagePath,
                thumbnail_path = @ThumbnailPath,
                char_count = @CharCount,
                file_size = @FileSize,
                copy_count = @CopyCount,
                content_hash = @ContentHash,
                is_favorite = @IsFavorite,
                is_starred = @IsStarred,
                folder_id = @FolderId,
                last_copied_at = @LastCopiedAt
            WHERE id = @Id", item);
    }

    public ClipboardItem? GetItem(string id)
    {
        using var conn = OpenConnection();
        return conn.QuerySingleOrDefault<ClipboardItem>(
            "SELECT * FROM clipboard_items WHERE id = @id", new { id });
    }

    public List<ClipboardItem> GetHistory(int limit = 200)
    {
        using var conn = OpenConnection();
        return conn.Query<ClipboardItem>(@"
            SELECT * FROM clipboard_items
            ORDER BY is_starred DESC, last_copied_at DESC
            LIMIT @limit", new { limit }).ToList();
    }

    public List<ClipboardItem> GetStarredItems()
    {
        using var conn = OpenConnection();
        return conn.Query<ClipboardItem>(@"
            SELECT * FROM clipboard_items WHERE is_starred = 1
            ORDER BY last_copied_at DESC").ToList();
    }

    public List<ClipboardItem> GetItemsByFolder(string folderId)
    {
        using var conn = OpenConnection();
        return conn.Query<ClipboardItem>(@"
            SELECT * FROM clipboard_items WHERE folder_id = @folderId
            ORDER BY is_starred DESC, last_copied_at DESC", new { folderId }).ToList();
    }

    public List<ClipboardItem> SearchItems(string query)
    {
        using var conn = OpenConnection();
        var like = $"%{query}%";
        return conn.Query<ClipboardItem>(@"
            SELECT ci.* FROM clipboard_items ci
            LEFT JOIN folders f ON ci.folder_id = f.id
            WHERE (ci.text_content LIKE @like OR f.name LIKE @like)
            ORDER BY ci.is_starred DESC, ci.last_copied_at DESC
            LIMIT 500", new { like }).ToList();
    }

    public ClipboardItem? GetLatestItemByHash(string hash)
    {
        using var conn = OpenConnection();
        return conn.QuerySingleOrDefault<ClipboardItem>(@"
            SELECT * FROM clipboard_items
            WHERE content_hash = @hash
            ORDER BY last_copied_at DESC
            LIMIT 1", new { hash });
    }

    public void DeleteItem(string id)
    {
        using var conn = OpenConnection();
        var item = GetItem(id);
        if (item != null)
        {
            DeleteItemFiles(item);
            conn.Execute("DELETE FROM clipboard_items WHERE id = @id", new { id });
        }
    }

    public void DeleteItems(IEnumerable<string> ids)
    {
        foreach (var id in ids)
            DeleteItem(id);
    }

    public void DeleteNonFavorites()
    {
        using var conn = OpenConnection();
        var items = conn.Query<ClipboardItem>(
            "SELECT * FROM clipboard_items WHERE is_favorite = 0 AND is_starred = 0").ToList();
        foreach (var item in items)
            DeleteItemFiles(item);
        conn.Execute("DELETE FROM clipboard_items WHERE is_favorite = 0 AND is_starred = 0");
    }

    public int GetHistoryCount()
    {
        using var conn = OpenConnection();
        return conn.ExecuteScalar<int>("SELECT COUNT(*) FROM clipboard_items");
    }

    public List<ClipboardItem> GetOldestNonProtectedItems(int count)
    {
        using var conn = OpenConnection();
        return conn.Query<ClipboardItem>(@"
            SELECT * FROM clipboard_items
            WHERE is_favorite = 0 AND is_starred = 0
            ORDER BY last_copied_at ASC
            LIMIT @count", new { count }).ToList();
    }

    public List<ClipboardItem> GetItemsOlderThan(DateTime cutoff)
    {
        using var conn = OpenConnection();
        return conn.Query<ClipboardItem>(@"
            SELECT * FROM clipboard_items
            WHERE is_favorite = 0 AND is_starred = 0
            AND created_at < @cutoff", new { cutoff = cutoff.ToString("O") }).ToList();
    }

    public List<ClipboardItem> GetItemsInFolder(string folderId)
    {
        using var conn = OpenConnection();
        return conn.Query<ClipboardItem>(@"
            SELECT * FROM clipboard_items WHERE folder_id = @folderId", new { folderId }).ToList();
    }

    // ---- Folders CRUD ----

    public void InsertFolder(Folder folder)
    {
        using var conn = OpenConnection();
        conn.Execute(@"
            INSERT INTO folders (id, name, parent_id, sort_order)
            VALUES (@Id, @Name, @ParentId, @SortOrder)", folder);
    }

    public void UpdateFolder(Folder folder)
    {
        using var conn = OpenConnection();
        conn.Execute(@"
            UPDATE folders SET name = @Name, parent_id = @ParentId, sort_order = @SortOrder
            WHERE id = @Id", folder);
    }

    public void DeleteFolder(string id)
    {
        using var conn = OpenConnection();
        conn.Execute("UPDATE clipboard_items SET folder_id = NULL WHERE folder_id = @id", new { id });
        conn.Execute("DELETE FROM folders WHERE id = @id", new { id });
    }

    public List<Folder> GetAllFolders()
    {
        using var conn = OpenConnection();
        return conn.Query<Folder>("SELECT * FROM folders ORDER BY sort_order, name").ToList();
    }

    public Folder? GetFolder(string id)
    {
        using var conn = OpenConnection();
        return conn.QuerySingleOrDefault<Folder>(
            "SELECT * FROM folders WHERE id = @id", new { id });
    }

    public List<Folder> GetChildFolders(string parentId)
    {
        using var conn = OpenConnection();
        return conn.Query<Folder>(@"
            SELECT * FROM folders WHERE parent_id = @parentId
            ORDER BY sort_order, name", new { parentId }).ToList();
    }

    // ---- Reminders CRUD ----

    public void InsertReminder(Reminder reminder)
    {
        using var conn = OpenConnection();
        conn.Execute(@"
            INSERT INTO reminders (id, item_id, remind_at, reminder_type, is_fired)
            VALUES (@Id, @ItemId, @RemindAt, @ReminderType, @IsFired)", reminder);
    }

    public void UpdateReminder(Reminder reminder)
    {
        using var conn = OpenConnection();
        conn.Execute(@"
            UPDATE reminders SET
                remind_at = @RemindAt,
                reminder_type = @ReminderType,
                is_fired = @IsFired
            WHERE id = @Id", reminder);
    }

    public void DeleteReminder(string id)
    {
        using var conn = OpenConnection();
        conn.Execute("DELETE FROM reminders WHERE id = @Id", new { Id = id });
    }

    public List<Reminder> GetPendingReminders()
    {
        using var conn = OpenConnection();
        return conn.Query<Reminder>(@"
            SELECT * FROM reminders WHERE is_fired = 0 AND remind_at <= @now
            ORDER BY remind_at", new { now = DateTime.UtcNow.ToString("O") }).ToList();
    }

    public List<Reminder> GetAllReminders()
    {
        using var conn = OpenConnection();
        return conn.Query<Reminder>(@"
            SELECT * FROM reminders ORDER BY remind_at").ToList();
    }

    public void DeleteFiredRemindersOlderThan(DateTime cutoff)
    {
        using var conn = OpenConnection();
        conn.Execute(@"
            DELETE FROM reminders WHERE is_fired = 1 AND remind_at < @cutoff",
            new { cutoff = cutoff.ToString("O") });
    }

    // ---- Path Helpers ----

    public string GetImagePath(string fileName) => Path.Combine(_imagesDir, fileName);
    public string GetThumbnailPath(string fileName) => Path.Combine(_thumbnailsDir, fileName);
    public string ImagesDir => _imagesDir;
    public string ThumbnailsDir => _thumbnailsDir;

    private void DeleteItemFiles(ClipboardItem item)
    {
        try
        {
            if (!string.IsNullOrEmpty(item.ImagePath) && File.Exists(item.ImagePath))
                File.Delete(item.ImagePath);
            if (!string.IsNullOrEmpty(item.ThumbnailPath) && File.Exists(item.ThumbnailPath))
                File.Delete(item.ThumbnailPath);
        }
        catch { }
    }
}
