using FavoriteSticker.Models;

namespace FavoriteSticker.Services;

public class StarManager
{
    private readonly DataStore _store;

    public StarManager(DataStore store)
    {
        _store = store;
    }

    public void ToggleStar(string itemId)
    {
        var item = _store.GetItem(itemId);
        if (item == null) return;
        item.IsStarred = !item.IsStarred;
        _store.UpdateItem(item);
    }

    public void Star(string itemId)
    {
        var item = _store.GetItem(itemId);
        if (item == null) return;
        item.IsStarred = true;
        _store.UpdateItem(item);
    }

    public void Unstar(string itemId)
    {
        var item = _store.GetItem(itemId);
        if (item == null) return;
        item.IsStarred = false;
        _store.UpdateItem(item);
    }

    public List<ClipboardItem> GetStarredItems()
    {
        return _store.GetStarredItems();
    }

    public bool IsStarred(string itemId)
    {
        return _store.GetItem(itemId)?.IsStarred ?? false;
    }
}
