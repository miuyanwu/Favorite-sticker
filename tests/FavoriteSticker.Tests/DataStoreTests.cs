using FavoriteSticker.Models;
using FavoriteSticker.Services;
using Xunit;

namespace FavoriteSticker.Tests;

public class DataStoreTests : IDisposable
{
    private readonly string _testDir;
    private readonly DataStore _store;

    public DataStoreTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"FavoriteStickerTests_{Guid.NewGuid():N}");
        _store = new DataStore(_testDir);
    }

    [Fact]
    public void InsertAndGetItem_RoundTripsCorrectly()
    {
        var item = new ClipboardItem
        {
            ContentType = ContentType.Text,
            TextContent = "Hello, world!",
            ContentHash = "abc123"
        };
        _store.InsertItem(item);

        var retrieved = _store.GetItem(item.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("Hello, world!", retrieved!.TextContent);
        Assert.Equal(ContentType.Text, retrieved.ContentType);
    }

    [Fact]
    public void GetHistory_ReturnsItemsOrderedByLastCopiedAtDesc()
    {
        var old = new ClipboardItem { TextContent = "old", LastCopiedAt = DateTime.UtcNow.AddHours(-1) };
        var mid = new ClipboardItem { TextContent = "mid", LastCopiedAt = DateTime.UtcNow.AddMinutes(-30) };
        var recent = new ClipboardItem { TextContent = "recent", LastCopiedAt = DateTime.UtcNow };
        _store.InsertItem(old);
        _store.InsertItem(mid);
        _store.InsertItem(recent);

        var history = _store.GetHistory();
        Assert.Equal(3, history.Count);
        Assert.True(history[0].LastCopiedAt >= history[1].LastCopiedAt);
        Assert.True(history[1].LastCopiedAt >= history[2].LastCopiedAt);
    }

    [Fact]
    public void StarredItems_AreSortedFirst()
    {
        var starred = new ClipboardItem { TextContent = "starred", IsStarred = true, LastCopiedAt = DateTime.UtcNow.AddHours(-2) };
        var normal = new ClipboardItem { TextContent = "normal", IsStarred = false, LastCopiedAt = DateTime.UtcNow };
        _store.InsertItem(starred);
        _store.InsertItem(normal);

        var history = _store.GetHistory();
        Assert.True(history[0].IsStarred);
    }

    [Fact]
    public void DeleteItem_RemovesFromDatabase()
    {
        var item = new ClipboardItem { TextContent = "to delete" };
        _store.InsertItem(item);

        _store.DeleteItem(item.Id);
        Assert.Null(_store.GetItem(item.Id));
    }

    [Fact]
    public void DeleteNonFavorites_OnlyRemovesNonFavoritedNonStarred()
    {
        var fav = new ClipboardItem { TextContent = "fav", IsFavorite = true };
        var starred = new ClipboardItem { TextContent = "star", IsStarred = true };
        var normal = new ClipboardItem { TextContent = "normal" };
        _store.InsertItem(fav);
        _store.InsertItem(starred);
        _store.InsertItem(normal);

        _store.DeleteNonFavorites();

        Assert.NotNull(_store.GetItem(fav.Id));
        Assert.NotNull(_store.GetItem(starred.Id));
        Assert.Null(_store.GetItem(normal.Id));
    }

    [Fact]
    public void SearchItems_FindsTextContent()
    {
        _store.InsertItem(new ClipboardItem { TextContent = "apple banana" });
        _store.InsertItem(new ClipboardItem { TextContent = "cherry dog" });
        _store.InsertItem(new ClipboardItem { TextContent = "elephant" });

        var results = _store.SearchItems("banana");
        Assert.Single(results);
        Assert.Contains("apple", results[0].TextContent);
    }

    [Fact]
    public void SearchItems_FindsByFolderName()
    {
        var folder = new Folder { Name = "Work" };
        _store.InsertFolder(folder);
        _store.InsertItem(new ClipboardItem { TextContent = "task 1", FolderId = folder.Id });
        _store.InsertItem(new ClipboardItem { TextContent = "other" });

        var results = _store.SearchItems("Work");
        Assert.Single(results);
    }

    [Fact]
    public void GetLatestItemByHash_ReturnsMostRecent()
    {
        var old = new ClipboardItem { TextContent = "dup", ContentHash = "same", LastCopiedAt = DateTime.UtcNow.AddHours(-1) };
        var recent = new ClipboardItem { TextContent = "dup again", ContentHash = "same", LastCopiedAt = DateTime.UtcNow };
        _store.InsertItem(old);
        _store.InsertItem(recent);

        var latest = _store.GetLatestItemByHash("same");
        Assert.NotNull(latest);
        Assert.Equal("dup again", latest!.TextContent);
    }

    [Fact]
    public void FolderCrud_Works()
    {
        var folder = new Folder { Name = "Test Folder" };
        _store.InsertFolder(folder);

        var retrieved = _store.GetFolder(folder.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("Test Folder", retrieved!.Name);

        retrieved.Name = "Renamed";
        _store.UpdateFolder(retrieved);
        Assert.Equal("Renamed", _store.GetFolder(folder.Id)!.Name);

        _store.DeleteFolder(folder.Id);
        Assert.Null(_store.GetFolder(folder.Id));
    }

    [Fact]
    public void FolderTree_HierarchyWorks()
    {
        var root = new Folder { Name = "Root" };
        _store.InsertFolder(root);
        var child = new Folder { Name = "Child", ParentId = root.Id };
        _store.InsertFolder(child);

        var children = _store.GetChildFolders(root.Id);
        Assert.Single(children);
        Assert.Equal("Child", children[0].Name);
    }

    [Fact]
    public void CreateAndCancelReminder_Works()
    {
        var item = new ClipboardItem { TextContent = "test item" };
        _store.InsertItem(item);

        var reminder = new Reminder
        {
            ItemId = item.Id,
            RemindAt = DateTime.UtcNow.AddHours(1),
            ReminderType = ReminderType.ContentReminder
        };
        _store.InsertReminder(reminder);

        Assert.Single(_store.GetAllReminders());

        // Raw verify: count should be 1
        using (var conn1 = _store.OpenConnection())
        {
            using var cmd = conn1.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM reminders";
            var count = (long)cmd.ExecuteScalar()!;
            Assert.Equal(1L, count);
        }

        _store.DeleteReminder(reminder.Id);

        // Verify via Dapper (fresh connection)
        var freshList = _store.GetAllReminders();
        Assert.Empty(freshList);
    }

    [Fact]
    public void Config_RoundTrips()
    {
        _store.Config.Cleanup.MaxHistoryCount = 99;
        _store.Config.Hotkey.Key = "C";
        _store.Config = _store.Config; // Force save

        // Re-load
        var store2 = new DataStore(_testDir);
        Assert.Equal(99, store2.Config.Cleanup.MaxHistoryCount);
        Assert.Equal("C", store2.Config.Hotkey.Key);
    }

    [Fact]
    public void GetOldestNonProtectedItems_ReturnsCorrectItems()
    {
        var old = new ClipboardItem { TextContent = "old", LastCopiedAt = DateTime.UtcNow.AddHours(-5) };
        var newer = new ClipboardItem { TextContent = "newer", LastCopiedAt = DateTime.UtcNow };
        var fav = new ClipboardItem { TextContent = "fav", IsFavorite = true, LastCopiedAt = DateTime.UtcNow.AddHours(-10) };
        _store.InsertItem(old);
        _store.InsertItem(newer);
        _store.InsertItem(fav);

        var result = _store.GetOldestNonProtectedItems(2);
        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(result, item => item.IsFavorite);
        Assert.Equal("old", result[0].TextContent);
    }

    public void Dispose()
    {
        try { Directory.Delete(_testDir, recursive: true); } catch { }
    }
}
