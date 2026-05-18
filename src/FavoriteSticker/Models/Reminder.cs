namespace FavoriteSticker.Models;

public enum ReminderType
{
    ContentReminder = 0,
    TimedPaste = 1
}

public class Reminder
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ItemId { get; set; } = string.Empty;
    public DateTime RemindAt { get; set; }
    public ReminderType ReminderType { get; set; }
    public bool IsFired { get; set; }
}
