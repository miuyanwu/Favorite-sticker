using FavoriteSticker.Models;

namespace FavoriteSticker.Services;

public class SearchService
{
    private readonly DataStore _store;

    public SearchService(DataStore store)
    {
        _store = store;
    }

    /// <summary>
    /// Search clipboard items by text content or folder name.
    /// </summary>
    public List<ClipboardItem> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return _store.GetHistory();

        return _store.SearchItems(query.Trim());
    }

    /// <summary>
    /// Search only within starred items.
    /// </summary>
    public List<ClipboardItem> SearchStarred(string? query = null)
    {
        var starred = _store.GetStarredItems();
        if (string.IsNullOrWhiteSpace(query))
            return starred;

        var q = query.Trim().ToLowerInvariant();
        return starred.Where(item =>
            (item.TextContent?.ToLowerInvariant().Contains(q) ?? false)
        ).ToList();
    }

    /// <summary>
    /// Get full history with starred items pinned at the top.
    /// </summary>
    public List<ClipboardItem> GetHistoryWithPinned()
    {
        return _store.GetHistory();
    }

    /// <summary>
    /// Get items in a specific folder with starred items pinned at the top.
    /// </summary>
    public List<ClipboardItem> GetFolderItemsWithPinned(string folderId)
    {
        return _store.GetItemsByFolder(folderId);
    }
}
