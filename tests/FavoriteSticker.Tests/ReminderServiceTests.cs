using FavoriteSticker.Models;
using FavoriteSticker.Services;
using Xunit;

namespace FavoriteSticker.Tests;

public class ReminderServiceTests : IDisposable
{
    private readonly string _testDir;
    private readonly DataStore _store;
    private readonly ReminderService _service;

    public ReminderServiceTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"ReminderTests_{Guid.NewGuid():N}");
        _store = new DataStore(_testDir);
        _service = new ReminderService(_store);
    }

    [Fact]
    public void CreateReminder_StoresInDatabase()
    {
        var item = new ClipboardItem { TextContent = "remind me" };
        _store.InsertItem(item);

        var remindAt = DateTime.UtcNow.AddHours(1);
        var reminder = _service.CreateReminder(item.Id, remindAt, ReminderType.ContentReminder);

        Assert.NotNull(reminder.Id);
        Assert.Equal(item.Id, reminder.ItemId);
        Assert.Equal(ReminderType.ContentReminder, reminder.ReminderType);
        Assert.False(reminder.IsFired);
    }

    [Fact]
    public void GetAllReminders_ReturnsAll()
    {
        var item = new ClipboardItem { TextContent = "test" };
        _store.InsertItem(item);

        _service.CreateReminder(item.Id, DateTime.UtcNow.AddHours(1), ReminderType.ContentReminder);
        _service.CreateReminder(item.Id, DateTime.UtcNow.AddHours(2), ReminderType.TimedPaste);

        var all = _service.GetAllReminders();
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public void CancelReminder_RemovesFromDatabase()
    {
        var item = new ClipboardItem { TextContent = "test" };
        _store.InsertItem(item);

        var reminder = _service.CreateReminder(item.Id, DateTime.UtcNow.AddHours(1), ReminderType.ContentReminder);
        _service.CancelReminder(reminder.Id);

        Assert.Empty(_service.GetAllReminders());
    }

    public void Dispose()
    {
        _service.Dispose();
        try { Directory.Delete(_testDir, recursive: true); } catch { }
    }
}
