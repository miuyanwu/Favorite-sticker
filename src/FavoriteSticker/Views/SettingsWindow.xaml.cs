using System.Windows;
using System.Windows.Input;
using FavoriteSticker.Services;
using FavoriteSticker.ViewModels;

namespace FavoriteSticker.Views;

public partial class SettingsWindow : Window
{
    private readonly MainViewModel _mainViewModel;
    private readonly HotkeyManager _hotkeyManager;
    private readonly DataStore _store;
    private bool _isRecordingHotkey;
    private uint _recordedModifiers;
    private uint _recordedKey;

    public SettingsWindow(MainViewModel mainViewModel, HotkeyManager hotkeyManager)
    {
        InitializeComponent();
        _mainViewModel = mainViewModel;
        _hotkeyManager = hotkeyManager;
        _store = new DataStore();
        LoadSettings();
    }

    private void LoadSettings()
    {
        var hk = _hotkeyManager.GetHotkey();
        CurrentHotkeyText.Text = $"{hk.modifiers}+{hk.key}";

        MaxHistoryBox.Text = _store.Config.Cleanup.MaxHistoryCount.ToString();
        MaxAgeBox.Text = _store.Config.Cleanup.MaxAgeDays.ToString();
        MaxTextCharsBox.Text = _store.Config.Cleanup.MaxTextChars.ToString();
        MaxImageSizeBox.Text = (_store.Config.Cleanup.MaxImageSizeBytes / (1024.0 * 1024.0)).ToString("F1");
        DedupCheckBox.IsChecked = _store.Config.Cleanup.DedupEnabled;
        AutoStartCheckBox.IsChecked = _store.Config.AutoStart.Enabled;
        DataPathBox.Text = _store.Config.Storage.DataPath;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        // Save cleanup config
        if (int.TryParse(MaxHistoryBox.Text, out var maxHist))
            _store.Config.Cleanup.MaxHistoryCount = maxHist;
        if (int.TryParse(MaxAgeBox.Text, out var maxAge))
            _store.Config.Cleanup.MaxAgeDays = maxAge;
        if (int.TryParse(MaxTextCharsBox.Text, out var maxChars))
            _store.Config.Cleanup.MaxTextChars = maxChars;
        if (double.TryParse(MaxImageSizeBox.Text, out var maxImgMB))
            _store.Config.Cleanup.MaxImageSizeBytes = (long)(maxImgMB * 1024 * 1024);
        _store.Config.Cleanup.DedupEnabled = DedupCheckBox.IsChecked == true;

        // Save auto-start
        _store.Config.AutoStart.Enabled = AutoStartCheckBox.IsChecked == true;
        new AutoStartService().SetEnabled(_store.Config.AutoStart.Enabled);

        // Save storage path
        _store.Config.Storage.DataPath = DataPathBox.Text.Trim();

        // Save hotkey config
        var hk = _hotkeyManager.GetHotkey();
        _store.Config.Hotkey.Modifiers = hk.modifiers;
        _store.Config.Hotkey.Key = hk.key;

        // Persist all config
        _store.Config = _store.Config;

        _mainViewModel.RefreshItems();

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void ChangeHotkey_Click(object sender, RoutedEventArgs e)
    {
        if (_isRecordingHotkey)
        {
            ChangeHotkeyBtn.Content = "Change";
            _isRecordingHotkey = false;
            KeyDown -= RecordKeyDown;
            return;
        }

        _isRecordingHotkey = true;
        ChangeHotkeyBtn.Content = "Press keys...";
        KeyDown += RecordKeyDown;
    }

    private void RecordKeyDown(object sender, KeyEventArgs e)
    {
        var modifiers = Keyboard.Modifiers;
        _recordedModifiers = 0;
        if ((modifiers & ModifierKeys.Alt) != 0) _recordedModifiers |= Helpers.Win32.MOD_ALT;
        if ((modifiers & ModifierKeys.Control) != 0) _recordedModifiers |= Helpers.Win32.MOD_CONTROL;
        if ((modifiers & ModifierKeys.Shift) != 0) _recordedModifiers |= Helpers.Win32.MOD_SHIFT;
        if ((modifiers & ModifierKeys.Windows) != 0) _recordedModifiers |= Helpers.Win32.MOD_WIN;

        _recordedKey = (uint)KeyInterop.VirtualKeyFromKey(e.Key == Key.System ? e.SystemKey : e.Key);

        if (_recordedModifiers != 0 && _recordedKey != 0)
        {
            _isRecordingHotkey = false;
            KeyDown -= RecordKeyDown;
            ChangeHotkeyBtn.Content = "Change";

            try
            {
                var modsStr = Helpers.Win32.ModifiersToString(_recordedModifiers);
                var keyStr = Helpers.Win32.KeyCodeToString(_recordedKey);
                _hotkeyManager.SetHotkey(modsStr, keyStr);
                CurrentHotkeyText.Text = $"{modsStr}+{keyStr}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Invalid hotkey: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        e.Handled = true;
    }

    private void BrowseDataPath_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Please type or paste the full folder path in the text box.\n\n" +
            "Default: %LocalAppData%\\FavoriteSticker",
            "Browse Folder", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
