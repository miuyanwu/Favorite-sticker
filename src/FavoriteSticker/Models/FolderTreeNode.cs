namespace FavoriteSticker.Models;

public class FolderTreeNode
{
    public Folder Folder { get; set; } = null!;
    public int Depth { get; set; }
    public string Indent => new string(' ', Depth * 4);
}
