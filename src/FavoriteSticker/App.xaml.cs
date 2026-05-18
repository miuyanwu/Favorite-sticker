using System.Windows;
using FavoriteSticker.Services;
using FavoriteSticker.ViewModels;
using FavoriteSticker.Views;

namespace FavoriteSticker;

public partial class App : Application
{
    private DataStore? _store;
    private CleanupEngine? _cleanup;
    private ClipboardMonitor? _clipboardMonitor;
    private HotkeyManager? _hotkeyManager;
    private AutoStartService? _autoStart;
    private ReminderService? _reminderService;
    private MainWindow? _mainWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Initialize services
        _store = new DataStore();
        _cleanup = new CleanupEngine(_store);
        _clipboardMonitor = new ClipboardMonitor(_store, _cleanup);
        _hotkeyManager = new HotkeyManager();
        _autoStart = new AutoStartService();
        _reminderService = new ReminderService(_store);

        // Apply auto-start setting
        if (_store.Config.AutoStart.Enabled)
            _autoStart.Enable();
        else
            _autoStart.Disable();

        // Create the main window
        var favoriteManager = new FavoriteManager(_store);
        var starManager = new StarManager(_store);
        var searchService = new SearchService(_store);

        var viewModel = new MainViewModel(
            _store, _clipboardMonitor, _cleanup,
            favoriteManager, starManager, searchService,
            _hotkeyManager);

        _mainWindow = new MainWindow(viewModel, _hotkeyManager, _clipboardMonitor);
        _mainWindow.Closed += (_, _) => Shutdown();

        // Initialize all HWND-dependent services and start hotkey/clipboard listening
        _mainWindow.Initialize();

        // Apply hotkey from config
        var cfg = _store.Config.Hotkey;
        _hotkeyManager.SetHotkey(cfg.Modifiers, cfg.Key);
        _hotkeyManager.Register();

        // Start reminder polling
        _reminderService.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _clipboardMonitor?.Dispose();
        _hotkeyManager?.Dispose();
        _reminderService?.Dispose();
        base.OnExit(e);
    }
}
