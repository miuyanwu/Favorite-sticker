using FavoriteSticker.Models;

namespace FavoriteSticker.Services;

public class FavoriteManager
{
    private readonly DataStore _store;

    public FavoriteManager(DataStore store)
    {
        _store = store;
    }

    public Folder CreateFolder(string name, string? parentId = null)
    {
        var folder = new Folder
        {
            Name = name,
            ParentId = parentId,
            SortOrder = GetNextSortOrder(parentId)
        };
        _store.InsertFolder(folder);
        return folder;
    }

    public void RenameFolder(string id, string newName)
    {
        var folder = _store.GetFolder(id);
        if (folder == null) return;
        folder.Name = newName;
        _store.UpdateFolder(folder);
    }

    public void MoveFolder(string id, string? newParentId)
    {
        var folder = _store.GetFolder(id);
        if (folder == null) return;

        // Prevent circular reference: don't allow moving under own descendant
        if (newParentId != null && IsDescendantOf(newParentId, id))
            return;

        folder.ParentId = newParentId;
        _store.UpdateFolder(folder);
    }

    public void DeleteFolder(string id)
    {
        _store.DeleteFolder(id);
    }

    public void MoveItemToFolder(string itemId, string? folderId)
    {
        var item = _store.GetItem(itemId);
        if (item == null) return;
        item.FolderId = folderId;
        item.IsFavorite = folderId != null;
        _store.UpdateItem(item);
    }

    public List<Folder> GetFolderTree()
    {
        return _store.GetAllFolders();
    }

    public List<Folder> GetSubFolders(string? parentId)
    {
        return string.IsNullOrEmpty(parentId)
            ? _store.GetAllFolders().Where(f => f.ParentId == null).ToList()
            : _store.GetChildFolders(parentId);
    }

    /// <summary>
    /// Build a display-ready tree. Returns a list of (Folder, depth) tuples in pre-order.
    /// </summary>
    public List<(Folder Folder, int Depth)> BuildFolderTree()
    {
        var result = new List<(Folder, int)>();
        var roots = _store.GetAllFolders().Where(f => f.ParentId == null).ToList();
        foreach (var root in roots)
            AddFolderRecursive(root, 0, result);
        return result;
    }

    private void AddFolderRecursive(Folder folder, int depth, List<(Folder, int)> result)
    {
        result.Add((folder, depth));
        var children = _store.GetChildFolders(folder.Id);
        foreach (var child in children)
            AddFolderRecursive(child, depth + 1, result);
    }

    private bool IsDescendantOf(string folderId, string targetAncestorId)
    {
        var current = _store.GetFolder(folderId);
        while (current?.ParentId != null)
        {
            if (current.ParentId == targetAncestorId)
                return true;
            current = _store.GetFolder(current.ParentId);
        }
        return false;
    }

    private int GetNextSortOrder(string? parentId)
    {
        var siblings = string.IsNullOrEmpty(parentId)
            ? _store.GetAllFolders().Where(f => f.ParentId == null)
            : _store.GetChildFolders(parentId);
        return siblings.Any() ? siblings.Max(f => f.SortOrder) + 1 : 0;
    }
}
