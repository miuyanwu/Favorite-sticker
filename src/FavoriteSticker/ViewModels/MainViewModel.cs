using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using FavoriteSticker.Models;
using FavoriteSticker.Services;

namespace FavoriteSticker.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly DataStore _store;
    private readonly ClipboardMonitor _clipboardMonitor;
    private readonly CleanupEngine _cleanup;
    private readonly FavoriteManager _favoriteManager;
    private readonly StarManager _starManager;
    private readonly SearchService _searchService;
    private readonly HotkeyManager _hotkeyManager;

    private string _searchText = string.Empty;
    private bool _isTopmost;
    private bool _showOnlyStarred;
    private string? _selectedFolderId;
    private string _hoverPreview = string.Empty;
    private bool _isHoverPreviewVisible;

    public MainViewModel(
        DataStore store,
        ClipboardMonitor clipboardMonitor,
        CleanupEngine cleanup,
        FavoriteManager favoriteManager,
        StarManager starManager,
        SearchService searchService,
        HotkeyManager hotkeyManager)
    {
        _store = store;
        _clipboardMonitor = clipboardMonitor;
        _cleanup = cleanup;
        _favoriteManager = favoriteManager;
        _starManager = starManager;
        _searchService = searchService;
        _hotkeyManager = hotkeyManager;

        _clipboardMonitor.OnItemCaptured += _ => RefreshItems();

        RefreshFolders();
        RefreshItems();
    }

    // ---- Observable Properties ----

    public ObservableCollection<ClipboardItem> Items { get; } = new();
    public ObservableCollection<(Folder Folder, int Depth)> FolderTree { get; } = new();

    public string SearchText
    {
        get => _searchText;
        set { _searchText = value; OnPropertyChanged(); RefreshItems(); }
    }

    public bool IsTopmost
    {
        get => _isTopmost;
        set { _isTopmost = value; OnPropertyChanged(); }
    }

    public bool ShowOnlyStarred
    {
        get => _showOnlyStarred;
        set { _showOnlyStarred = value; OnPropertyChanged(); RefreshItems(); }
    }

    public string? SelectedFolderId
    {
        get => _selectedFolderId;
        set { _selectedFolderId = value; OnPropertyChanged(); RefreshItems(); }
    }

    public string HoverPreview
    {
        get => _hoverPreview;
        set { _hoverPreview = value; OnPropertyChanged(); }
    }

    public bool IsHoverPreviewVisible
    {
        get => _isHoverPreviewVisible;
        set { _isHoverPreviewVisible = value; OnPropertyChanged(); }
    }

    // ---- Commands ----

    public ICommand CopyItemCommand => new RelayCommand(OnCopyItem);
    public ICommand ToggleStarCommand => new RelayCommand(OnToggleStar);
    public ICommand ToggleTopmostCommand => new RelayCommand(OnToggleTopmost);
    public ICommand DeleteItemCommand => new RelayCommand(OnDeleteItem);
    public ICommand ClearNonFavoritesCommand => new RelayCommand(OnClearNonFavorites);
    public ICommand CreateFolderCommand => new RelayCommand(OnCreateFolder);
    public ICommand DeleteFolderCommand => new RelayCommand(OnDeleteFolder);
    public ICommand MoveToFolderCommand => new RelayCommand(OnMoveToFolder);
    public ICommand OpenSettingsCommand => new RelayCommand(OnOpenSettings);
    public ICommand ExitCommand => new RelayCommand(OnExit);
    public ICommand ShowStarredOnlyCommand => new RelayCommand(_ => ShowOnlyStarred = !ShowOnlyStarred);
    public ICommand SelectAllFolderCommand => new RelayCommand(_ => { SelectedFolderId = null; ShowOnlyStarred = false; });

    // ---- Item Hover Preview ----

    public void OnMouseEnter(ClipboardItem item)
    {
        if (item.ContentType == ContentType.Text)
        {
            var text = item.TextContent ?? "";
            HoverPreview = text.Length > 200 ? text[..200] + "..." : text;
        }
        else
        {
            HoverPreview = item.ThumbnailPath ?? item.ImagePath ?? "";
        }
        IsHoverPreviewVisible = true;
    }

    public void OnMouseLeave()
    {
        IsHoverPreviewVisible = false;
    }

    // ---- Private Handlers ----

    private void OnCopyItem(object? param)
    {
        if (param is not ClipboardItem item) return;
        try
        {
            if (item.ContentType == ContentType.Text && item.TextContent != null)
                Clipboard.SetText(item.TextContent);
            else if (item.ContentType == ContentType.Image && item.ImagePath != null
                     && File.Exists(item.ImagePath))
            {
                var bitmap = new System.Windows.Media.Imaging.BitmapImage(new Uri(item.ImagePath));
                Clipboard.SetImage(bitmap);
            }
        }
        catch { }
    }

    private void OnToggleStar(object? param)
    {
        if (param is not ClipboardItem item) return;
        _starManager.ToggleStar(item.Id);
        RefreshItems();
    }

    private void OnToggleTopmost(object? _)
    {
        IsTopmost = !IsTopmost;
    }

    private void OnDeleteItem(object? param)
    {
        if (param is not ClipboardItem item) return;
        _store.DeleteItem(item.Id);
        RefreshItems();
    }

    private void OnClearNonFavorites(object? _)
    {
        var result = MessageBox.Show(
            "Delete all non-favorited items? This cannot be undone.",
            "Clear History", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result == MessageBoxResult.Yes)
        {
            _cleanup.ClearNonFavorites();
            RefreshItems();
        }
    }

    private void OnCreateFolder(object? _)
    {
        var dialog = new Views.InputDialog("New Folder", "Folder name:");
        if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.Result))
        {
            _favoriteManager.CreateFolder(dialog.Result.Trim(), SelectedFolderId);
            RefreshFolders();
        }
    }

    private void OnDeleteFolder(object? param)
    {
        if (param is not Folder folder) return;
        var result = MessageBox.Show(
            $"Delete folder '{folder.Name}' and unlink its contents?",
            "Delete Folder", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result == MessageBoxResult.Yes)
        {
            _favoriteManager.DeleteFolder(folder.Id);
            if (SelectedFolderId == folder.Id)
                SelectedFolderId = null;
            RefreshFolders();
            RefreshItems();
        }
    }

    private void OnMoveToFolder(object? param)
    {
        // param is a tuple or we parse from command parameter
        // For simplicity, handled via drag-drop in code-behind
    }

    private void OnOpenSettings(object? _)
    {
        var settingsWindow = new Views.SettingsWindow(this, _hotkeyManager);
        settingsWindow.Owner = Application.Current.MainWindow;
        settingsWindow.ShowDialog();
    }

    private void OnExit(object? _)
    {
        Application.Current.Shutdown();
    }

    // ---- Refresh ----

    public void RefreshItems()
    {
        List<ClipboardItem> results;

        if (ShowOnlyStarred)
        {
            results = _searchService.SearchStarred(SearchText);
        }
        else if (!string.IsNullOrWhiteSpace(SearchText))
        {
            results = _searchService.Search(SearchText);
        }
        else if (!string.IsNullOrEmpty(SelectedFolderId))
        {
            results = _searchService.GetFolderItemsWithPinned(SelectedFolderId);
        }
        else
        {
            results = _searchService.GetHistoryWithPinned();
        }

        Items.Clear();
        foreach (var item in results)
            Items.Add(item);
    }

    public void RefreshFolders()
    {
        var tree = _favoriteManager.BuildFolderTree();
        FolderTree.Clear();
        foreach (var entry in tree)
            FolderTree.Add(entry);
    }

    // ---- INotifyPropertyChanged ----

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

/// <summary>
/// Simple ICommand implementation for ViewModel commands.
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
    public void Execute(object? parameter) => _execute(parameter);

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}
