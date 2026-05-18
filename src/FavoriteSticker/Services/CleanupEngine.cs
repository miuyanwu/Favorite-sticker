using FavoriteSticker.Models;

namespace FavoriteSticker.Services;

public class CleanupEngine
{
    private readonly DataStore _store;

    public CleanupEngine(DataStore store)
    {
        _store = store;
    }

    public bool PassesSizeGate(ClipboardItem item)
    {
        var config = _store.Config.Cleanup;
        if (item.ContentType == ContentType.Text && item.CharCount > config.MaxTextChars)
            return false;
        if (item.ContentType == ContentType.Image && item.FileSize > config.MaxImageSizeBytes)
            return false;
        return true;
    }

    public void EnforceCountCap()
    {
        var config = _store.Config.Cleanup;
        var currentCount = _store.GetHistoryCount();
        if (currentCount <= config.MaxHistoryCount)
            return;

        var excess = currentCount - config.MaxHistoryCount;
        var toRemove = _store.GetOldestNonProtectedItems(excess);
        foreach (var item in toRemove)
            _store.DeleteItem(item.Id);
    }

    public int EnforceAgeLimit()
    {
        var config = _store.Config.Cleanup;
        var cutoff = DateTime.UtcNow.AddDays(-config.MaxAgeDays);
        var items = _store.GetItemsOlderThan(cutoff);
        foreach (var item in items)
            _store.DeleteItem(item.Id);
        return items.Count;
    }

    public int CleanFiredReminders()
    {
        var cutoff = DateTime.UtcNow.AddDays(-7);
        _store.DeleteFiredRemindersOlderThan(cutoff);
        return 0; // SQLite doesn't report affected rows easily; return 0 for compat
    }

    public void DeleteSelected(IEnumerable<string> ids)
    {
        _store.DeleteItems(ids);
    }

    public void ClearNonFavorites()
    {
        _store.DeleteNonFavorites();
    }

    public void ClearFolderContents(string folderId)
    {
        var items = _store.GetItemsInFolder(folderId);
        foreach (var item in items)
            _store.DeleteItem(item.Id);
    }

    public void RunScheduledCleanup()
    {
        EnforceAgeLimit();
        EnforceCountCap();
        CleanFiredReminders();
    }
}
