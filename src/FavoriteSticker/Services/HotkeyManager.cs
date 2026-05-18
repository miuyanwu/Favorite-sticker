using FavoriteSticker.Helpers;

namespace FavoriteSticker.Services;

public class HotkeyManager : IDisposable
{
    private const int HotkeyId = 9001;
    private IntPtr _hwnd;
    private uint _modifiers;
    private uint _key;
    private bool _registered;

    public event Action? OnHotkeyPressed;

    public HotkeyManager()
    {
        _modifiers = Win32.MOD_ALT;
        _key = 0x56; // 'V'
    }

    public void Attach(IntPtr hwnd)
    {
        _hwnd = hwnd;
    }

    public void SetHotkey(string modifiers, string key)
    {
        var newMods = Win32.ParseModifiers(modifiers);
        var newKey = ParseKey(key);

        if (newMods == 0 || newKey == 0)
            throw new ArgumentException($"Invalid hotkey: {modifiers}+{key}");

        if (_registered)
            Unregister();

        _modifiers = newMods;
        _key = newKey;
        Register();
    }

    public (string modifiers, string key) GetHotkey()
    {
        return (Win32.ModifiersToString(_modifiers), Win32.KeyCodeToString(_key));
    }

    public bool IsRegistered => _registered;

    public void Register()
    {
        if (_hwnd == IntPtr.Zero) return;
        if (_registered) return;

        _registered = Win32.RegisterHotKey(_hwnd, HotkeyId, _modifiers, _key);
    }

    public void Unregister()
    {
        if (!_registered) return;
        Win32.UnregisterHotKey(_hwnd, HotkeyId);
        _registered = false;
    }

    public bool HandleHotkey(int id)
    {
        if (id != HotkeyId) return false;
        OnHotkeyPressed?.Invoke();
        return true;
    }

    private static uint ParseKey(string key)
    {
        if (key.Length == 1)
        {
            var c = char.ToUpperInvariant(key[0]);
            if (c >= 'A' && c <= 'Z') return c;
            if (c >= '0' && c <= '9') return c;
        }

        return key.ToUpperInvariant() switch
        {
            "F1" => 0x70, "F2" => 0x71, "F3" => 0x72, "F4" => 0x73,
            "F5" => 0x74, "F6" => 0x75, "F7" => 0x76, "F8" => 0x77,
            "F9" => 0x78, "F10" => 0x7A, "F11" => 0x7B, "F12" => 0x7C,
            "SPACE" => 0x20,
            "TAB" => 0x09,
            "ENTER" or "RETURN" => 0x0D,
            "ESCAPE" or "ESC" => 0x1B,
            "BACK" or "BACKSPACE" => 0x08,
            "DELETE" or "DEL" => 0x2E,
            "INSERT" or "INS" => 0x2D,
            "HOME" => 0x24, "END" => 0x23,
            "PAGEUP" or "PGUP" => 0x21,
            "PAGEDOWN" or "PGDN" => 0x22,
            "UP" => 0x26, "DOWN" => 0x28, "LEFT" => 0x25, "RIGHT" => 0x27,
            "PRINTSCREEN" or "PRTSC" => 0x2C,
            _ => 0
        };
    }

    public void Dispose() => Unregister();
}
