using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using FavoriteSticker.Helpers;
using FavoriteSticker.Models;

namespace FavoriteSticker.Services;

public class ClipboardMonitor : IDisposable
{
    private readonly DataStore _store;
    private readonly CleanupEngine _cleanup;
    private IntPtr _hwnd;
    private string? _lastHash;
    private bool _disposed;

    public event Action<ClipboardItem>? OnItemCaptured;

    public ClipboardMonitor(DataStore store, CleanupEngine cleanup)
    {
        _store = store;
        _cleanup = cleanup;
    }

    public void Attach(IntPtr hwnd)
    {
        _hwnd = hwnd;
        Win32.AddClipboardFormatListener(hwnd);
    }

    public void Detach()
    {
        if (_hwnd != IntPtr.Zero)
        {
            Win32.RemoveClipboardFormatListener(_hwnd);
            _hwnd = IntPtr.Zero;
        }
    }

    public void HandleClipboardUpdate()
    {
        try
        {
            // Must open clipboard on UI thread via WPF Dispatcher pattern.
            // This is called from the WndProc on the UI thread.
            if (!Clipboard.ContainsText() && !Clipboard.ContainsImage())
                return;

            ClipboardItem? item = null;

            if (Clipboard.ContainsImage())
            {
                item = CaptureImage();
            }
            else if (Clipboard.ContainsText())
            {
                item = CaptureText();
            }

            if (item == null)
                return;

            // Size gate check
            if (!_cleanup.PassesSizeGate(item))
                return;

            // Dedup check
            if (_store.Config.Cleanup.DedupEnabled && item.ContentHash == _lastHash)
            {
                var existing = _store.GetLatestItemByHash(item.ContentHash);
                if (existing != null)
                {
                    existing.CopyCount++;
                    existing.LastCopiedAt = DateTime.UtcNow;
                    _store.UpdateItem(existing);
                    _lastHash = item.ContentHash;
                    OnItemCaptured?.Invoke(existing);
                    return;
                }
            }

            _store.InsertItem(item);
            _lastHash = item.ContentHash;

            // FIFO cap
            _cleanup.EnforceCountCap();

            OnItemCaptured?.Invoke(item);
        }
        catch
        {
            // Clipboard access can fail if another app has it open.
            // Just skip this update.
        }
    }

    private ClipboardItem CaptureText()
    {
        var text = Clipboard.GetText();
        var hash = ComputeHash(text);

        return new ClipboardItem
        {
            ContentType = ContentType.Text,
            TextContent = text,
            CharCount = text.Length,
            ContentHash = hash
        };
    }

    private ClipboardItem? CaptureImage()
    {
        var bitmap = Clipboard.GetImage();
        if (bitmap == null) return null;

        var id = Guid.NewGuid().ToString();
        var imageFileName = $"{id}.png";
        var thumbFileName = $"{id}_thumb.png";
        var imagePath = _store.GetImagePath(imageFileName);
        var thumbPath = _store.GetThumbnailPath(thumbFileName);

        // Save original
        SavePng(bitmap, imagePath);

        // Generate thumbnail (120x120 max)
        var thumb = CreateThumbnail(bitmap, 120);
        SavePng(thumb, thumbPath);

        var fileInfo = new FileInfo(imagePath);
        var hash = ComputeHash(File.ReadAllBytes(imagePath));

        return new ClipboardItem
        {
            ContentType = ContentType.Image,
            ImagePath = imagePath,
            ThumbnailPath = thumbPath,
            FileSize = fileInfo.Length,
            ContentHash = hash
        };
    }

    private static BitmapSource CreateThumbnail(BitmapSource source, int maxSize)
    {
        var scaleX = (double)maxSize / source.PixelWidth;
        var scaleY = (double)maxSize / source.PixelHeight;
        var scale = Math.Min(Math.Min(scaleX, scaleY), 1.0);

        if (scale >= 1.0) return source;

        return new TransformedBitmap(source, new System.Windows.Media.ScaleTransform(scale, scale));
    }

    private static void SavePng(BitmapSource bitmap, string path)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path);
        encoder.Save(stream);
    }

    private static string ComputeHash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        return ComputeHash(bytes);
    }

    private static string ComputeHash(byte[] bytes)
    {
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Detach();
            _disposed = true;
        }
    }
}
