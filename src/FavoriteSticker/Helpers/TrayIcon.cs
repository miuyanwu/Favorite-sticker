using System.Runtime.InteropServices;

namespace FavoriteSticker.Helpers;

public class TrayIcon : IDisposable
{
    private const int WM_TRAYICON = 0x8001;
    private const int WM_RBUTTONUP = 0x0205;
    private const int WM_LBUTTONDBLCLK = 0x0203;

    private IntPtr _hwnd;
    private NotifyIconData _data;
    private bool _visible;

    public event Action? OnDoubleClick;
    public event Action? OnShowRequested;
    public event Action? OnSettingsRequested;
    public event Action? OnExitRequested;

    public void Attach(IntPtr hwnd)
    {
        _hwnd = hwnd;
    }

    public void Show(string tooltip = "Favorite Sticker")
    {
        if (_visible) return;

        _data = new NotifyIconData
        {
            cbSize = Marshal.SizeOf<NotifyIconData>(),
            hWnd = _hwnd,
            uID = 1,
            uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
            uCallbackMessage = WM_TRAYICON,
            szTip = tooltip
        };

        // Create a simple icon using WPF rendering
        _data.hIcon = CreateSimpleIcon();

        Shell_NotifyIcon(NIM_ADD, ref _data);
        _visible = true;
    }

    public void Hide()
    {
        if (!_visible) return;
        Shell_NotifyIcon(NIM_DELETE, ref _data);
        _visible = false;
    }

    public IntPtr HandleMessage(int msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg != WM_TRAYICON) return IntPtr.Zero;

        var l = lParam.ToInt32();
        if (l == WM_LBUTTONDBLCLK)
            OnDoubleClick?.Invoke();
        else if (l == WM_RBUTTONUP)
            ShowContextMenu();

        return IntPtr.Zero;
    }

    private void ShowContextMenu()
    {
        var menu = CreatePopupMenu();
        AppendMenu(menu, MF_STRING, 1, "Show (Alt+V)");
        AppendMenu(menu, MF_STRING, 2, "Settings");
        AppendMenu(menu, MF_SEPARATOR, 0, "");
        AppendMenu(menu, MF_STRING, 3, "Exit");

        // Get cursor position
        GetCursorPos(out var pt);

        // Required: set foreground window so menu dismissal works correctly
        SetForegroundWindow(_hwnd);

        var cmd = TrackPopupMenu(menu, TPM_RETURNCMD | TPM_RIGHTBUTTON,
            pt.X, pt.Y, 0, _hwnd, IntPtr.Zero);

        DestroyMenu(menu);

        switch (cmd)
        {
            case 1: OnShowRequested?.Invoke(); break;
            case 2: OnSettingsRequested?.Invoke(); break;
            case 3: OnExitRequested?.Invoke(); break;
        }
    }

    private static IntPtr CreateSimpleIcon()
    {
        using var bmp = new System.Drawing.Bitmap(16, 16);
        using var g = System.Drawing.Graphics.FromImage(bmp);
        g.Clear(System.Drawing.Color.DodgerBlue);
        return bmp.GetHicon();
    }

    public void Dispose()
    {
        if (_visible) Hide();
        if (_data.hIcon != IntPtr.Zero)
            DestroyIcon(_data.hIcon);
    }

    // ---- Win32 P/Invoke ----

    private const int NIM_ADD = 0;
    private const int NIM_DELETE = 2;
    private const int NIF_MESSAGE = 1;
    private const int NIF_ICON = 2;
    private const int NIF_TIP = 4;
    private const uint MF_STRING = 0;
    private const uint MF_SEPARATOR = 0x800;
    private const uint TPM_RETURNCMD = 0x100;
    private const uint TPM_RIGHTBUTTON = 2;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct NotifyIconData
    {
        public int cbSize;
        public IntPtr hWnd;
        public int uID;
        public int uFlags;
        public int uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
    }

    [DllImport("shell32.dll")]
    private static extern bool Shell_NotifyIcon(int dwMessage, ref NotifyIconData lpData);

    [DllImport("user32.dll")]
    private static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll")]
    private static extern bool AppendMenu(IntPtr hMenu, uint uFlags, uint uIDNewItem, string lpNewItem);

    [DllImport("user32.dll")]
    private static extern uint TrackPopupMenu(IntPtr hMenu, uint uFlags, int x, int y,
        int nReserved, IntPtr hWnd, IntPtr prcRect);

    [DllImport("user32.dll")]
    private static extern bool DestroyMenu(IntPtr hMenu);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }
}
