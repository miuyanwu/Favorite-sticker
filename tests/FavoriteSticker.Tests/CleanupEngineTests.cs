using FavoriteSticker.Models;
using FavoriteSticker.Services;
using Xunit;

namespace FavoriteSticker.Tests;

public class CleanupEngineTests : IDisposable
{
    private readonly string _testDir;
    private readonly DataStore _store;
    private readonly CleanupEngine _engine;

    public CleanupEngineTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"CleanupTests_{Guid.NewGuid():N}");
        _store = new DataStore(_testDir);
        _engine = new CleanupEngine(_store);
    }

    [Fact]
    public void PassesSizeGate_RejectsOversizedText()
    {
        _store.Config.Cleanup.MaxTextChars = 100;
        var bigText = new ClipboardItem { ContentType = ContentType.Text, CharCount = 500 };
        Assert.False(_engine.PassesSizeGate(bigText));
    }

    [Fact]
    public void PassesSizeGate_AcceptsNormalText()
    {
        _store.Config.Cleanup.MaxTextChars = 100;
        var normal = new ClipboardItem { ContentType = ContentType.Text, CharCount = 50 };
        Assert.True(_engine.PassesSizeGate(normal));
    }

    [Fact]
    public void PassesSizeGate_RejectsOversizedImage()
    {
        _store.Config.Cleanup.MaxImageSizeBytes = 1024;
        var bigImg = new ClipboardItem { ContentType = ContentType.Image, FileSize = 5000 };
        Assert.False(_engine.PassesSizeGate(bigImg));
    }

    [Fact]
    public void PassesSizeGate_AcceptsNormalImage()
    {
        _store.Config.Cleanup.MaxImageSizeBytes = 1024;
        var smallImg = new ClipboardItem { ContentType = ContentType.Image, FileSize = 512 };
        Assert.True(_engine.PassesSizeGate(smallImg));
    }

    [Fact]
    public void EnforceCountCap_RemovesOldestNonProtected()
    {
        _store.Config.Cleanup.MaxHistoryCount = 3;
        for (int i = 0; i < 5; i++)
            _store.InsertItem(new ClipboardItem { TextContent = $"item {i}", LastCopiedAt = DateTime.UtcNow.AddMinutes(-i) });

        _engine.EnforceCountCap();
        Assert.True(_store.GetHistoryCount() <= 3);
    }

    [Fact]
    public void EnforceCountCap_ProtectsFavorites()
    {
        _store.Config.Cleanup.MaxHistoryCount = 2;
        var fav = new ClipboardItem { TextContent = "favorite", IsFavorite = true, LastCopiedAt = DateTime.UtcNow.AddHours(-5) };
        var star = new ClipboardItem { TextContent = "starred", IsStarred = true, LastCopiedAt = DateTime.UtcNow.AddHours(-4) };
        var n1 = new ClipboardItem { TextContent = "normal1", LastCopiedAt = DateTime.UtcNow.AddHours(-3) };
        var n2 = new ClipboardItem { TextContent = "normal2", LastCopiedAt = DateTime.UtcNow.AddHours(-2) };
        _store.InsertItem(fav);
        _store.InsertItem(star);
        _store.InsertItem(n1);
        _store.InsertItem(n2);

        _engine.EnforceCountCap();

        Assert.NotNull(_store.GetItem(fav.Id));
        Assert.NotNull(_store.GetItem(star.Id));
        Assert.Null(_store.GetItem(n1.Id));
        Assert.Null(_store.GetItem(n2.Id));
    }

    [Fact]
    public void EnforceAgeLimit_RemovesOldItems()
    {
        _store.Config.Cleanup.MaxAgeDays = 1;
        var old = new ClipboardItem { TextContent = "old", CreatedAt = DateTime.UtcNow.AddDays(-5) };
        var recent = new ClipboardItem { TextContent = "recent", CreatedAt = DateTime.UtcNow };
        _store.InsertItem(old);
        _store.InsertItem(recent);

        var removed = _engine.EnforceAgeLimit();
        Assert.True(removed >= 1);
        Assert.Null(_store.GetItem(old.Id));
        Assert.NotNull(_store.GetItem(recent.Id));
    }

    [Fact]
    public void ClearNonFavorites_RemovesOnlyUnprotected()
    {
        _store.InsertItem(new ClipboardItem { TextContent = "keep", IsFavorite = true });
        _store.InsertItem(new ClipboardItem { TextContent = "remove" });
        _store.InsertItem(new ClipboardItem { TextContent = "keep2", IsStarred = true });

        _engine.ClearNonFavorites();

        var items = _store.GetHistory();
        Assert.Equal(2, items.Count);
        Assert.All(items, item => Assert.True(item.IsFavorite || item.IsStarred));
    }

    [Fact]
    public void DeleteSelected_RemovesSpecificItems()
    {
        var a = new ClipboardItem { TextContent = "a" };
        var b = new ClipboardItem { TextContent = "b" };
        _store.InsertItem(a);
        _store.InsertItem(b);

        _engine.DeleteSelected(new[] { a.Id });
        Assert.Null(_store.GetItem(a.Id));
        Assert.NotNull(_store.GetItem(b.Id));
    }

    [Fact]
    public void ClearFolderContents_RemovesItemsInFolder()
    {
        var folder = new Folder { Name = "Test" };
        _store.InsertFolder(folder);
        var inFolder = new ClipboardItem { TextContent = "in", FolderId = folder.Id };
        var outside = new ClipboardItem { TextContent = "out" };
        _store.InsertItem(inFolder);
        _store.InsertItem(outside);

        _engine.ClearFolderContents(folder.Id);

        Assert.Null(_store.GetItem(inFolder.Id));
        Assert.NotNull(_store.GetItem(outside.Id));
    }

    public void Dispose()
    {
        try { Directory.Delete(_testDir, recursive: true); } catch { }
    }
}
