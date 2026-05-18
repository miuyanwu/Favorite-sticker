namespace FavoriteSticker.Models;

public class Folder
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string? ParentId { get; set; }
    public int SortOrder { get; set; }
}
