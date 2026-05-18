using System.Windows;
using FavoriteSticker.Models;

namespace FavoriteSticker.Services;

public class ReminderService : IDisposable
{
    private readonly DataStore _store;
    private System.Timers.Timer? _timer;
    private bool _disposed;

    public ReminderService(DataStore store)
    {
        _store = store;
    }

    public void Start()
    {
        _timer = new System.Timers.Timer(60_000); // Poll every 60 seconds
        _timer.Elapsed += (_, _) => CheckReminders();
        _timer.AutoReset = true;
        _timer.Start();
    }

    public void Stop()
    {
        _timer?.Stop();
        _timer?.Dispose();
        _timer = null;
    }

    public Reminder CreateReminder(string itemId, DateTime remindAt, ReminderType type)
    {
        var reminder = new Reminder
        {
            ItemId = itemId,
            RemindAt = remindAt,
            ReminderType = type
        };
        _store.InsertReminder(reminder);
        return reminder;
    }

    public void CancelReminder(string reminderId)
    {
        _store.DeleteReminder(reminderId);
    }

    public List<Reminder> GetAllReminders()
    {
        return _store.GetAllReminders();
    }

    private void CheckReminders()
    {
        var pending = _store.GetPendingReminders();
        foreach (var reminder in pending)
        {
            FireReminder(reminder);
            reminder.IsFired = true;
            _store.UpdateReminder(reminder);
        }
    }

    private void FireReminder(Reminder reminder)
    {
        var item = _store.GetItem(reminder.ItemId);
        if (item == null) return;

        var preview = item.ContentType == ContentType.Text
            ? Truncate(item.TextContent ?? "", 100)
            : "[Image]";

        if (reminder.ReminderType == ReminderType.TimedPaste)
        {
            // Push content to clipboard on UI thread
            Application.Current?.Dispatcher.Invoke(() =>
            {
                try
                {
                    if (item.ContentType == ContentType.Text && item.TextContent != null)
                        Clipboard.SetText(item.TextContent);
                    else if (item.ContentType == ContentType.Image && item.ImagePath != null
                             && File.Exists(item.ImagePath))
                    {
                        var bitmap = new System.Windows.Media.Imaging.BitmapImage(
                            new Uri(item.ImagePath));
                        Clipboard.SetImage(bitmap);
                    }
                }
                catch { }
            });
        }

        ShowToast(
            reminder.ReminderType == ReminderType.TimedPaste ? "Timed Paste" : "Content Reminder",
            preview);
    }

    private static void ShowToast(string title, string message)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            // Use a simple approach: fire a Windows notification via PowerShell
            // In production, use Microsoft.Toolkit.Uwp.Notifications or a WPF Toast library
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -Command \"& {{" +
                        $"$t = [Windows.UI.Notifications.ToastNotificationManager, Windows.UI, ContentType = WindowsRuntime];" +
                        $"$x = '<toast><visual><binding template=\\\"ToastGeneric\\\">" +
                        $"<text>{EscapeXml(title)}</text>" +
                        $"<text>{EscapeXml(message)}</text>" +
                        $"</binding></visual></toast>';" +
                        $"$xml = [Windows.Data.Xml.Dom.XmlDocument, Windows.Data.Xml.Dom.XmlDocument, ContentType = WindowsRuntime]::new();" +
                        $"$xml.LoadXml($x);" +
                        $"$n = [Windows.UI.Notifications.ToastNotification, Windows.UI, ContentType = WindowsRuntime]::new($xml);" +
                        $"$id = 'FavoriteSticker';" +
                        $"$notifier = [Windows.UI.Notifications.ToastNotificationManager, Windows.UI, ContentType = WindowsRuntime]::CreateToastNotifier($id);" +
                        $"$notifier.Show($n);" +
                        $"}}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                System.Diagnostics.Process.Start(psi);
            }
            catch { /* Toast not available */ }
        });
    }

    private static string EscapeXml(string s) =>
        System.Security.SecurityElement.Escape(s);

    private static string Truncate(string text, int maxLen) =>
        text.Length <= maxLen ? text : text[..maxLen] + "...";

    public void Dispose()
    {
        if (!_disposed)
        {
            Stop();
            _disposed = true;
        }
    }
}
