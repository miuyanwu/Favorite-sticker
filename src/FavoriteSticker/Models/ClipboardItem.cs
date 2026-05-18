namespace FavoriteSticker.Models;

public enum ContentType
{
    Text = 0,
    Image = 1
}

public class ClipboardItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public ContentType ContentType { get; set; }
    public string? TextContent { get; set; }
    public string? ImagePath { get; set; }
    public string? ThumbnailPath { get; set; }
    public int CharCount { get; set; }
    public long FileSize { get; set; }
    public int CopyCount { get; set; } = 1;
    public string ContentHash { get; set; } = string.Empty;
    public bool IsFavorite { get; set; }
    public bool IsStarred { get; set; }
    public string? FolderId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastCopiedAt { get; set; } = DateTime.UtcNow;
}
