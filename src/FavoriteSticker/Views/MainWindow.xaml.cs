using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using FavoriteSticker.Helpers;
using FavoriteSticker.Models;
using FavoriteSticker.Services;
using FavoriteSticker.ViewModels;

namespace FavoriteSticker.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly HotkeyManager _hotkeyManager;
    private readonly ClipboardMonitor _clipboardMonitor;
    private readonly TrayIcon _trayIcon;
    private bool _isClosingToTray;

    public MainWindow(MainViewModel viewModel, HotkeyManager hotkeyManager, ClipboardMonitor clipboardMonitor)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _hotkeyManager = hotkeyManager;
        _clipboardMonitor = clipboardMonitor;
        _trayIcon = new TrayIcon();
        DataContext = _viewModel;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        _clipboardMonitor.Attach(hwnd);
        _hotkeyManager.Attach(hwnd);
        _hotkeyManager.Register();
        _hotkeyManager.OnHotkeyPressed += ToggleVisibility;

        var source = HwndSource.FromHwnd(hwnd);
        source?.AddHook(WndProc);

        _trayIcon.Attach(hwnd);
        _trayIcon.Show();
        _trayIcon.OnDoubleClick += ShowWindow;
        _trayIcon.OnShowRequested += ShowWindow;
        _trayIcon.OnSettingsRequested += ShowSettings;
        _trayIcon.OnExitRequested += ExitApplication;

        Hide(); // Start hidden, shown via hotkey
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == Win32.WM_CLIPBOARDUPDATE)
        {
            _clipboardMonitor.HandleClipboardUpdate();
        }
        else if (msg == Win32.WM_HOTKEY)
        {
            var id = wParam.ToInt32();
            _hotkeyManager.HandleHotkey(id);
        }

        // Let TrayIcon handle its message
        _trayIcon.HandleMessage(msg, wParam, lParam);

        return IntPtr.Zero;
    }

    private void ToggleVisibility()
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            if (IsVisible && !_isClosingToTray)
                Hide();
            else
                ShowWindow();
        });
    }

    private void ShowWindow()
    {
        _isClosingToTray = false;
        Show();
        Activate();
        SearchBox.Focus();
        SearchBox.SelectAll();
    }

    // ---- Title bar drag ----
    private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }
        else
        {
            DragMove();
        }
    }

    // ---- Top-right buttons ----
    private void PinButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.IsTopmost = !_viewModel.IsTopmost;

        var hwnd = new WindowInteropHelper(this).Handle;
        if (_viewModel.IsTopmost)
            Win32.SetWindowPos(hwnd, Win32.HWND_TOPMOST, 0, 0, 0, 0,
                Win32.SWP_NOMOVE | Win32.SWP_NOSIZE);
        else
            Win32.SetWindowPos(hwnd, Win32.HWND_NOTOPMOST, 0, 0, 0, 0,
                Win32.SWP_NOMOVE | Win32.SWP_NOSIZE);

        PinButton.Foreground = _viewModel.IsTopmost
            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gold)
            : null;
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e) => ShowSettings();

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        _isClosingToTray = true;
        Hide();
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!_isClosingToTray)
        {
            _trayIcon?.Dispose();
            _clipboardMonitor.Detach();
            _hotkeyManager.Unregister();
        }
    }

    // ---- Item interactions ----
    private void Item_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement el && el.Tag is ClipboardItem item)
        {
            _viewModel.CopyItemCommand.Execute(item);
        }
    }

    private void Item_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is FrameworkElement el && el.Tag is ClipboardItem item)
        {
            if (item.ContentType == ContentType.Text)
            {
                var text = item.TextContent ?? "";
                HoverPreviewText.Text = text.Length > 200 ? text[..200] + "..." : text;
                HoverPreviewText.Visibility = Visibility.Visible;
                HoverPreviewImage.Visibility = Visibility.Collapsed;
            }
            else
            {
                HoverPreviewText.Visibility = Visibility.Collapsed;
                HoverPreviewImage.Visibility = Visibility.Visible;
                try
                {
                    var path = item.ThumbnailPath ?? item.ImagePath;
                    if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
                        HoverPreviewImage.Source = new BitmapImage(new Uri(path));
                }
                catch { }
            }
            HoverPopup.IsOpen = true;
        }
    }

    private void Item_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        HoverPopup.IsOpen = false;
    }

    private void StarButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement el && el.Tag is ClipboardItem item)
        {
            _viewModel.ToggleStarCommand.Execute(item);
        }
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement el && el.Tag is ClipboardItem item)
        {
            _viewModel.DeleteItemCommand.Execute(item);
        }
    }

    // ---- Toolbar buttons ----
    private void StarredFilter_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ShowStarredOnlyCommand.Execute(null);
        StarredFilterBtn.Foreground = _viewModel.ShowOnlyStarred
            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gold)
            : null;
    }

    private void AddFolder_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.CreateFolderCommand.Execute(null);
    }

    private void ClearNonFavorites_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ClearNonFavoritesCommand.Execute(null);
    }

    private void ShowSettings()
    {
        var settingsWindow = new SettingsWindow(_viewModel, _hotkeyManager);
        settingsWindow.Owner = this;
        settingsWindow.ShowDialog();
    }

    private void ExitApplication()
    {
        _isClosingToTray = false;
        _trayIcon?.Dispose();
        _clipboardMonitor.Detach();
        _hotkeyManager.Unregister();
        System.Windows.Application.Current.Shutdown();
    }
}
