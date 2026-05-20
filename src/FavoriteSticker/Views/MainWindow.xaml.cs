using System.Windows;
using System.Windows.Controls;
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

    public void Initialize()
    {
        var hwnd = new WindowInteropHelper(this).EnsureHandle();

        _clipboardMonitor.Attach(hwnd);

        _hotkeyManager.Attach(hwnd);
        _hotkeyManager.OnHotkeyPressed += ToggleVisibility;

        var source = HwndSource.FromHwnd(hwnd);
        source?.AddHook(WndProc);

        _trayIcon.Attach(hwnd);
        _trayIcon.Show();
        _trayIcon.OnDoubleClick += ShowWindow;
        _trayIcon.OnShowRequested += ShowWindow;
        _trayIcon.OnSettingsRequested += ShowSettings;
        _trayIcon.OnExitRequested += ExitApplication;

        Show();
        Hide();
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == Win32.WM_CLIPBOARDUPDATE)
            _clipboardMonitor.HandleClipboardUpdate();
        else if (msg == Win32.WM_HOTKEY)
            _hotkeyManager.HandleHotkey(wParam.ToInt32());

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
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd != IntPtr.Zero)
        {
            Win32.ShowWindow(hwnd, Win32.SW_RESTORE);
            Win32.SetForegroundWindow(hwnd);
        }
        SearchBox.Focus();
        SearchBox.SelectAll();
    }

    // ---- Title bar ----
    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        else
            DragMove();
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
    private void Item_Click(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is Button) return;

        if (sender is FrameworkElement el && el.Tag is ClipboardItem item)
            _viewModel.CopyItemCommand.Execute(item);
    }

    private void Item_MouseEnter(object sender, MouseEventArgs e)
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

    private void Item_MouseLeave(object sender, MouseEventArgs e)
    {
        HoverPopup.IsOpen = false;
    }

    private void StarButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement el && el.Tag is ClipboardItem item)
            _viewModel.ToggleStarCommand.Execute(item);
    }

    // ---- Context menu (more actions) ----
    private void MoreActions_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement el && el.Tag is ClipboardItem item)
            ShowItemContextMenu(item, el);
    }

    private void Item_RightClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement el && el.Tag is ClipboardItem item)
        {
            ShowItemContextMenu(item, el);
            e.Handled = true;
        }
    }

    private void ShowItemContextMenu(ClipboardItem item, FrameworkElement placementTarget)
    {
        var menu = new ContextMenu();

        var deleteItem = new MenuItem { Header = "Delete" };
        deleteItem.Click += (_, _) => _viewModel.DeleteItemCommand.Execute(item);
        menu.Items.Add(deleteItem);

        menu.Items.Add(new Separator());

        // Move to folder submenu
        var moveMenu = new MenuItem { Header = "Move to Folder" };
        foreach (var node in _viewModel.FolderTree)
        {
            var folder = node.Folder;
            var folderItem = new MenuItem { Header = folder.Name };
            folderItem.Click += (_, _) =>
            {
                _viewModel.MoveToFolderCommand.Execute((item.Id, folder.Id));
            };
            moveMenu.Items.Add(folderItem);
        }
        if (!_viewModel.FolderTree.Any())
            moveMenu.Items.Add(new MenuItem { Header = "(no folders)", IsEnabled = false });
        menu.Items.Add(moveMenu);

        menu.Items.Add(new Separator());

        var starItem = new MenuItem { Header = item.IsStarred ? "Unstar" : "Star" };
        starItem.Click += (_, _) => _viewModel.ToggleStarCommand.Execute(item);
        menu.Items.Add(starItem);

        menu.PlacementTarget = placementTarget;
        menu.IsOpen = true;
    }

    // ---- Sidebar buttons ----
    private void HomeButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.SelectedFolderId = null;
    }

    private void AddFolder_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.CreateFolderCommand.Execute(null);
    }

    // ---- Filter buttons ----
    private void StarredFilter_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ShowStarredOnlyCommand.Execute(null);
        StarredFilterBtn.Foreground = _viewModel.ShowOnlyStarred
            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gold)
            : null;
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
