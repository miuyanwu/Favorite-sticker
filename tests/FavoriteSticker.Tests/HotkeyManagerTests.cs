using FavoriteSticker.Helpers;
using FavoriteSticker.Services;
using Xunit;

namespace FavoriteSticker.Tests;

public class HotkeyManagerTests
{
    [Fact]
    public void DefaultHotkey_IsAltV()
    {
        var manager = new HotkeyManager();
        var hk = manager.GetHotkey();
        Assert.Equal("Alt", hk.modifiers);
        Assert.Equal("V", hk.key);
    }

    [Fact]
    public void SetHotkey_UpdatesValues()
    {
        var manager = new HotkeyManager();
        manager.SetHotkey("Ctrl+Shift", "K");
        var hk = manager.GetHotkey();
        Assert.Equal("Ctrl+Shift", hk.modifiers);
        Assert.Equal("K", hk.key);
    }

    [Fact]
    public void ParseModifiers_HandlesAllCombinations()
    {
        Assert.Equal(Win32.MOD_ALT, Win32.ParseModifiers("Alt"));
        Assert.Equal(Win32.MOD_CONTROL, Win32.ParseModifiers("Ctrl"));
        Assert.Equal(Win32.MOD_SHIFT, Win32.ParseModifiers("Shift"));
        Assert.Equal(Win32.MOD_WIN, Win32.ParseModifiers("Win"));
        Assert.Equal(Win32.MOD_ALT | Win32.MOD_CONTROL, Win32.ParseModifiers("Alt+Ctrl"));
        Assert.Equal(Win32.MOD_CONTROL | Win32.MOD_SHIFT, Win32.ParseModifiers("Control+Shift"));
    }

    [Fact]
    public void ModifiersToString_RoundTrips()
    {
        var mods = Win32.MOD_ALT | Win32.MOD_CONTROL;
        var str = Win32.ModifiersToString(mods);
        Assert.Contains("Ctrl", str);
        Assert.Contains("Alt", str);
    }

    [Fact]
    public void SetHotkey_ThrowsOnEmptyModifiers()
    {
        var manager = new HotkeyManager();
        Assert.Throws<ArgumentException>(() => manager.SetHotkey("", "V"));
    }

    [Fact]
    public void IsRegistered_InitiallyFalse()
    {
        var manager = new HotkeyManager();
        Assert.False(manager.IsRegistered);
    }
}
