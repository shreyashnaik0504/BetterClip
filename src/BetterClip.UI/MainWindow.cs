#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using Microsoft.Win32;

namespace BetterClip.UI
{
public class ClipboardRecord
{
public string Id { get; set; } = Guid.NewGuid().ToString();
public string Type { get; set; } = "Text"; // Text, Images, Links, Files, Video
public bool IsCode { get; set; } = false;
public bool IsScreenshot { get; set; } = false;
public string Hash { get; set; } = string.Empty;
public string Title { get; set; } = string.Empty;
public string Detail { get; set; } = string.Empty;
public string FullPayload { get; set; } = string.Empty;
public bool IsPinned { get; set; } = false;
public DateTime Timestamp { get; set; } = DateTime.Now;
public BitmapImage? Preview { get; set; }
}

public class MainWindow : Window
{
// ---------------------------------------------------------------
// Settings you may want to change
// ---------------------------------------------------------------
private const uint HotkeyModifiers = 0x0002 | 0x4000; // MOD_CONTROL | MOD_NOREPEAT
private const uint HotkeyVirtualKey = 0x20; // VK_SPACE
private const string HotkeyLabel = "Ctrl + Space";

private const string PlaceholderText = "Search everything you copied...";
private const int MaxItems = 200;

private static readonly FontFamily IconFont = new("Segoe Fluent Icons, Segoe MDL2 Assets");

// Glyphs (Segoe Fluent Icons / Segoe MDL2 Assets)
private const string G_Clipboard = "\uE77F";
private const string G_Search = "\uE721";
private const string G_All = "\uE71D";
private const string G_StarEmpty = "\uE734";
private const string G_StarFull = "\uE735";
private const string G_Text = "\uE8D2";
private const string G_Image = "\uE91B";
private const string G_Link = "\uE71B";
private const string G_File = "\uE8A5";
private const string G_Video = "\uE714";
private const string G_Play = "\uE768";
private const string G_Code = "\uE943";
private const string G_More = "\uE712";
private const string G_Delete = "\uE74D";

private readonly Grid _mainContainerGrid = new();
private readonly StackPanel _cardsDeck = new();
private readonly ScrollViewer _mainScrollViewer = new();
private readonly TextBox _searchBox = new();
private readonly TextBlock _placeholder = new();
private readonly UniformGrid _chipsPanel = new();
private readonly StackPanel _emptyState = new();
private readonly TextBlock _countText = new();
private Border _searchWrapper = new();

private string _activeCategoryFilter = "All";
private string _searchFilterTerm = string.Empty;
private readonly List<ClipboardRecord> _inMemoryItems = new();
private ClipboardRecord? _firstVisible;
private IntPtr _hwnd = IntPtr.Zero;
private IntPtr _previousWindow = IntPtr.Zero;
private IntPtr _trayIconHandle = IntPtr.Zero;

private bool _isInternalClipboardWrite = false;
private string _lastCapturedPayload = string.Empty;
private bool _menuOpen = false;
private bool _reallyQuit = false;

private static Mutex? _singleInstance;
private static readonly string ImageCacheDir =
Path.Combine(Path.GetTempPath(), "BetterClip", "Images");

// ---------------------------------------------------------------
// Win32
// ---------------------------------------------------------------
private const int WM_CLIPBOARDUPDATE = 0x031D;
private const int WM_HOTKEY = 0x0312;
private const int WM_TRAY = 0x8000 + 1;
private const int WM_LBUTTONUP = 0x0202;
private const int WM_RBUTTONUP = 0x0205;
private const int HOTKEY_ID = 0x0B1C;
private const uint TRAY_ID = 1;

private const byte VK_CONTROL = 0x11;
private const byte VK_V = 0x56;
private const uint KEYEVENTF_KEYUP = 0x0002;

private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
private const int DWMWCP_ROUND = 2;

private const uint SWP_NOSIZE = 0x0001;
private const uint SWP_NOZORDER = 0x0004;
private const uint SWP_NOACTIVATE = 0x0010;

private const uint NIF_MESSAGE = 0x1, NIF_ICON = 0x2, NIF_TIP = 0x4;
private const uint NIM_ADD = 0, NIM_DELETE = 2;

private const uint MF_STRING = 0x0, MF_CHECKED = 0x8, MF_SEPARATOR = 0x800;
private const uint TPM_RIGHTBUTTON = 0x0002, TPM_RETURNCMD = 0x0100;

[StructLayout(LayoutKind.Sequential)]
private struct POINT { public int X; public int Y; }

[StructLayout(LayoutKind.Sequential)]
private struct RECT { public int Left, Top, Right, Bottom; }

[StructLayout(LayoutKind.Sequential)]
private struct MONITORINFO
{
public int cbSize;
public RECT rcMonitor;
public RECT rcWork;
public uint dwFlags;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
private struct NOTIFYICONDATA
{
public int cbSize;
public IntPtr hWnd;
public uint uID;
public uint uFlags;
public uint uCallbackMessage;
public IntPtr hIcon;
[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string szTip;
public uint dwState;
public uint dwStateMask;
[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string szInfo;
public uint uTimeoutOrVersion;
[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string szInfoTitle;
public uint dwInfoFlags;
public Guid guidItem;
public IntPtr hBalloonIcon;
}

[StructLayout(LayoutKind.Sequential)]
private struct ICONINFO
{
[MarshalAs(UnmanagedType.Bool)] public bool fIcon;
public int xHotspot;
public int yHotspot;
public IntPtr hbmMask;
public IntPtr hbmColor;
}

[DllImport("user32.dll", SetLastError = true)]
[return: MarshalAs(UnmanagedType.Bool)]
private static extern bool AddClipboardFormatListener(IntPtr hwnd);

[DllImport("user32.dll", SetLastError = true)]
[return: MarshalAs(UnmanagedType.Bool)]
private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

[DllImport("user32.dll", SetLastError = true)]
[return: MarshalAs(UnmanagedType.Bool)]
private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

[DllImport("user32.dll", SetLastError = true)]
[return: MarshalAs(UnmanagedType.Bool)]
private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

[DllImport("user32.dll")]
private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

[DllImport("user32.dll")]
private static extern IntPtr GetForegroundWindow();

[DllImport("user32.dll")]
[return: MarshalAs(UnmanagedType.Bool)]
private static extern bool SetForegroundWindow(IntPtr hWnd);

[DllImport("user32.dll", CharSet = CharSet.Unicode)]
private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

[DllImport("user32.dll")]
[return: MarshalAs(UnmanagedType.Bool)]
private static extern bool GetCursorPos(out POINT lpPoint);

[DllImport("user32.dll")]
private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

[DllImport("user32.dll")]
[return: MarshalAs(UnmanagedType.Bool)]
private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

[DllImport("user32.dll")]
[return: MarshalAs(UnmanagedType.Bool)]
private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

[DllImport("user32.dll")]
[return: MarshalAs(UnmanagedType.Bool)]
private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

[DllImport("user32.dll")]
private static extern IntPtr CreatePopupMenu();

[DllImport("user32.dll", CharSet = CharSet.Unicode)]
[return: MarshalAs(UnmanagedType.Bool)]
private static extern bool AppendMenu(IntPtr hMenu, uint uFlags, UIntPtr uIDNewItem, string? lpNewItem);

[DllImport("user32.dll")]
private static extern int TrackPopupMenu(IntPtr hMenu, uint uFlags, int x, int y, int nReserved, IntPtr hWnd, IntPtr prcRect);

[DllImport("user32.dll")]
[return: MarshalAs(UnmanagedType.Bool)]
private static extern bool DestroyMenu(IntPtr hMenu);

[DllImport("user32.dll")]
private static extern IntPtr CreateIconIndirect(ref ICONINFO piconinfo);

[DllImport("user32.dll")]
[return: MarshalAs(UnmanagedType.Bool)]
private static extern bool DestroyIcon(IntPtr hIcon);

[DllImport("user32.dll")]
private static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

[DllImport("gdi32.dll")]
private static extern IntPtr CreateBitmap(int nWidth, int nHeight, uint cPlanes, uint cBitsPerPel, byte[] lpvBits);

[DllImport("gdi32.dll")]
[return: MarshalAs(UnmanagedType.Bool)]
private static extern bool DeleteObject(IntPtr hObject);

[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
[return: MarshalAs(UnmanagedType.Bool)]
private static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA lpData);

[DllImport("dwmapi.dll")]
private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

// ---------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------
private static SolidColorBrush Hex(string hex) =>
new((Color)ColorConverter.ConvertFromString(hex));

private static string FormatAgo(DateTime t)
{
var span = DateTime.Now - t;
if (span.TotalSeconds < 60) return "Just now";
if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} min ago";
if (span.TotalHours < 24) return $"{(int)span.TotalHours} hr ago";
return $"{(int)span.TotalDays} d ago";
}

private static string FormatSize(long bytes)
{
string[] units = { "B", "KB", "MB", "GB" };
double size = bytes;
int i = 0;
while (size >= 1024 && i < units.Length - 1) { size /= 1024; i++; }
return $"{size:0.#} {units[i]}";
}

private static string TypeLabel(ClipboardRecord r) => r.Type switch
{
"Images" => r.IsScreenshot ? "Screenshot" : "Image",
"Links" => "Link",
"Files" => "File",
"Video" => "Video",
_ => r.IsCode ? "Code" : "Text"
};

private static string Preview(string text, int maxLines, int maxCharsPerLine)
{
var lines = text.Replace("\r\n", "\n").Split('\n')
.Where(l => l.Trim().Length > 0)
.Take(maxLines)
.Select(l => l.TrimEnd().Length > maxCharsPerLine
? l.TrimEnd().Substring(0, maxCharsPerLine) + "…"
: l.TrimEnd());
return string.Join("\n", lines);
}

private static string FirstLine(string text, int max)
{
var line = text.Replace("\r\n", "\n").Split('\n').FirstOrDefault(l => l.Trim().Length > 0)?.Trim() ?? "";
return line.Length > max ? line.Substring(0, max) + "…" : line;
}

// ---------------------------------------------------------------
// Window setup
// ---------------------------------------------------------------
public MainWindow()
{
// Only one copy may own the hotkey
_singleInstance = new Mutex(true, "BetterClip.SingleInstance", out bool created);
if (!created)
{
Application.Current?.Shutdown();
return;
}

try
{
Directory.CreateDirectory(ImageCacheDir);
// History lives in memory only, so leftover screenshots from a previous run are orphans
foreach (var f in Directory.GetFiles(ImageCacheDir)) { try { File.Delete(f); } catch { } }
}
catch { }

InitializeWindowStyle();
BuildInterface();

this.SourceInitialized += OnWindowSourceInitialized;
this.Closing += OnWindowClosing;
this.Closed += OnWindowClosed;
this.Deactivated += (s, e) => { if (!_menuOpen) HideWindow(); };
this.PreviewKeyDown += OnWindowPreviewKeyDown;

// The window is created once at startup, then hidden until the hotkey is pressed
this.Loaded += (s, e) =>
{
Hide();
Opacity = 1;
};
}

private void InitializeWindowStyle()
{
this.Title = "Better Clip";
this.Width = 470;
this.Height = 670;
this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
this.WindowStyle = WindowStyle.None;
this.ResizeMode = ResizeMode.NoResize;
this.ShowInTaskbar = false;
this.ShowActivated = false;
this.Topmost = true;
this.Opacity = 0; // invisible during the one startup show, then hidden
this.AllowsTransparency = false;
this.Background = Brushes.White;
this.FontFamily = new FontFamily("Segoe UI Variable, Segoe UI");
this.UseLayoutRounding = true;
this.SnapsToDevicePixels = true;

WindowChrome.SetWindowChrome(this, new WindowChrome
{
CaptionHeight = 0,
ResizeBorderThickness = new Thickness(0),
GlassFrameThickness = new Thickness(-1),
CornerRadius = new CornerRadius(0),
UseAeroCaptionButtons = false
});
}

private void OnWindowPreviewKeyDown(object sender, KeyEventArgs e)
{
if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
{
_searchBox.Focus();
_searchBox.SelectAll();
e.Handled = true;
}
else if (e.Key == Key.Escape)
{
if (_searchBox.IsKeyboardFocused && _searchBox.Text.Length > 0)
_searchBox.Clear();
else
HideWindow();
e.Handled = true;
}
else if (e.Key == Key.Enter && _searchBox.IsKeyboardFocused && _firstVisible != null)
{
_ = PasteRecordAsync(_firstVisible);
e.Handled = true;
}
}

private void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
{
// Alt+F4 just hides it; "Quit" in the tray really exits
if (!_reallyQuit)
{
e.Cancel = true;
HideWindow();
}
}

// ---------------------------------------------------------------
// Show / hide (hotkey + tray)
// ---------------------------------------------------------------
private void HideWindow()
{
if (IsVisible) Hide();
}

private void ToggleWindow()
{
if (IsVisible && IsActive) HideWindow();
else ShowPopup();
}

private static string ClassNameOf(IntPtr hwnd)
{
var sb = new StringBuilder(256);
GetClassName(hwnd, sb, sb.Capacity);
return sb.ToString();
}

private void ShowPopup()
{
// Remember where the user was so we can paste back into it
var fg = GetForegroundWindow();
if (fg == _hwnd) fg = IntPtr.Zero;
if (fg != IntPtr.Zero)
{
var cls = ClassNameOf(fg);
if (cls == "Shell_TrayWnd" || cls == "NotifyIconOverflowWindow" || cls == "TopLevelWindowForOverflowXamlIsland")
fg = IntPtr.Zero;
}
_previousWindow = fg;

_searchBox.Clear();
_activeCategoryFilter = "All";
UpdateFilterTabSelectionStyles();

CenterOnActiveMonitor();

Show();
Activate();
SetForegroundWindow(_hwnd);

RefreshCardVisuals();
_mainScrollViewer.ScrollToTop();
Keyboard.Focus(_searchBox);
}

private void CenterOnActiveMonitor()
{
if (_hwnd == IntPtr.Zero || !GetCursorPos(out var pt)) return;

var monitor = MonitorFromPoint(pt, 2 /* MONITOR_DEFAULTTONEAREST */);
var mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
if (!GetMonitorInfo(monitor, ref mi)) return;
if (!GetWindowRect(_hwnd, out var rc)) return;

int w = rc.Right - rc.Left;
int h = rc.Bottom - rc.Top;
int x = mi.rcWork.Left + (mi.rcWork.Right - mi.rcWork.Left - w) / 2;
int y = mi.rcWork.Top + (mi.rcWork.Bottom - mi.rcWork.Top - h) / 2;

SetWindowPos(_hwnd, IntPtr.Zero, x, y, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
}

// ---------------------------------------------------------------
// Tray icon
// ---------------------------------------------------------------
private NOTIFYICONDATA NewNotifyData() => new()
{
cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
hWnd = _hwnd,
uID = TRAY_ID,
szTip = string.Empty,
szInfo = string.Empty,
szInfoTitle = string.Empty
};

private IntPtr CreateTrayIconHandle()
{
    try
    {
        string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "app_logo.png");
        const int size = 32;

        if (File.Exists(logoPath))
        {
            var srcBmp = new BitmapImage();
            srcBmp.BeginInit();
            srcBmp.UriSource = new Uri(logoPath, UriKind.Absolute);
            srcBmp.DecodePixelWidth = size;
            srcBmp.DecodePixelHeight = size;
            srcBmp.CacheOption = BitmapCacheOption.OnLoad;
            srcBmp.EndInit();
            srcBmp.Freeze();

            var convertedBmp = new FormatConvertedBitmap(srcBmp, PixelFormats.Pbgra32, null, 0);
            convertedBmp.Freeze();

            var pixels = new byte[size * size * 4];
            convertedBmp.CopyPixels(pixels, size * 4, 0);

            IntPtr colorBmp = CreateBitmap(size, size, 1, 32, pixels);
            IntPtr maskBmp = CreateBitmap(size, size, 1, 1, new byte[size * size / 8]);

            var iconInfo = new ICONINFO { fIcon = true, hbmMask = maskBmp, hbmColor = colorBmp };
            IntPtr hIcon = CreateIconIndirect(ref iconInfo);

            DeleteObject(colorBmp);
            DeleteObject(maskBmp);

            if (hIcon != IntPtr.Zero) return hIcon;
        }
    }
    catch { }

    return LoadIcon(IntPtr.Zero, (IntPtr)32512); // Fallback to standard app icon
}

private void AddTrayIcon()
{
_trayIconHandle = CreateTrayIconHandle();

var nid = NewNotifyData();
nid.uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP;
nid.uCallbackMessage = WM_TRAY;
nid.hIcon = _trayIconHandle;
nid.szTip = $"Better Clip ({HotkeyLabel})";
Shell_NotifyIcon(NIM_ADD, ref nid);
}

private void RemoveTrayIcon()
{
var nid = NewNotifyData();
Shell_NotifyIcon(NIM_DELETE, ref nid);
if (_trayIconHandle != IntPtr.Zero)
{
DestroyIcon(_trayIconHandle);
_trayIconHandle = IntPtr.Zero;
}
}

private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
private const string RunValueName = "BetterClip";

private static bool IsAutostartEnabled()
{
try
{
using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
return key?.GetValue(RunValueName) != null;
}
catch { return false; }
}

private static void SetAutostart(bool enabled)
{
try
{
using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
if (key == null) return;
if (enabled && Environment.ProcessPath != null)
key.SetValue(RunValueName, $"\"{Environment.ProcessPath}\"");
else
key.DeleteValue(RunValueName, false);
}
catch { }
}

private void ShowTrayMenu()
{
GetCursorPos(out var pt);

IntPtr menu = CreatePopupMenu();
AppendMenu(menu, MF_STRING, (UIntPtr)1, $"Open Better Clip\t{HotkeyLabel}");
AppendMenu(menu, MF_STRING | (IsAutostartEnabled() ? MF_CHECKED : 0), (UIntPtr)2, "Start with Windows");
AppendMenu(menu, MF_SEPARATOR, UIntPtr.Zero, null);
AppendMenu(menu, MF_STRING, (UIntPtr)3, "Quit");

SetForegroundWindow(_hwnd);
int cmd = TrackPopupMenu(menu, TPM_RIGHTBUTTON | TPM_RETURNCMD, pt.X, pt.Y, 0, _hwnd, IntPtr.Zero);
DestroyMenu(menu);

switch (cmd)
{
case 1: ShowPopup(); break;
case 2: SetAutostart(!IsAutostartEnabled()); break;
case 3:
_reallyQuit = true;
Application.Current.Shutdown();
break;
}
}

// ---------------------------------------------------------------
// UI
// ---------------------------------------------------------------
private void BuildInterface()
{
var shell = new Border
{
Background = Brushes.White,
BorderBrush = Hex("#E5E7EB"),
BorderThickness = new Thickness(1)
};

_mainContainerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
_mainContainerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
_mainContainerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
_mainContainerGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
_mainContainerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Footer row

// ---- 1. Header ----
var header = new Grid { Height = 64, Background = Brushes.Transparent };
header.MouseLeftButtonDown += (s, e) => { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); };

header.Children.Add(new Border
{
Background = Hex("#D1D5DB"),
Width = 44,
Height = 4.5,
CornerRadius = new CornerRadius(2.5),
HorizontalAlignment = HorizontalAlignment.Center,
VerticalAlignment = VerticalAlignment.Top,
Margin = new Thickness(0, 8, 0, 0)
});

var titleRow = new DockPanel { Margin = new Thickness(22, 22, 22, 0), LastChildFill = false };

var brand = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

// New glossy logo image
var logoBorder = new Border
{
Width = 32,
Height = 32,
CornerRadius = new CornerRadius(8),
ClipToBounds = true,
Margin = new Thickness(0, 0, 10, 0)
};

string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "app_logo.png");
if (File.Exists(logoPath))
{
    var logoBitmap = new BitmapImage();
    logoBitmap.BeginInit();
    logoBitmap.UriSource = new Uri(logoPath, UriKind.Absolute);
    logoBitmap.CacheOption = BitmapCacheOption.OnLoad;
    logoBitmap.EndInit();
    logoBitmap.Freeze();

    logoBorder.Child = new Image
    {
        Source = logoBitmap,
        Stretch = Stretch.UniformToFill
    };
}
else
{
    // Fallback if image path is not yet created
    logoBorder.Background = Hex("#2563EB");
    logoBorder.Child = new TextBlock
    {
        Text = G_Clipboard,
        FontFamily = IconFont,
        FontSize = 15,
        Foreground = Brushes.White,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center
    };
}

brand.Children.Add(logoBorder);
brand.Children.Add(new TextBlock
{
Text = "Better Clip",
FontSize = 17,
FontWeight = FontWeights.SemiBold,
Foreground = Hex("#111827"),
VerticalAlignment = VerticalAlignment.Center
});
DockPanel.SetDock(brand, Dock.Left);
titleRow.Children.Add(brand);

var rightHeader = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
_countText.FontSize = 12;
_countText.Foreground = Hex("#6B7280");
_countText.VerticalAlignment = VerticalAlignment.Center;
_countText.Margin = new Thickness(0, 0, 8, 0);
rightHeader.Children.Add(_countText);

rightHeader.Children.Add(CreateIconButton(G_Delete, Hex("#6B7280"), "Clear all unpinned", _ =>
{
foreach (var r in _inMemoryItems.Where(i => !i.IsPinned).ToList())
RemoveRecord(r);
RefreshCardVisuals();
}));
DockPanel.SetDock(rightHeader, Dock.Right);
titleRow.Children.Add(rightHeader);

header.Children.Add(titleRow);
Grid.SetRow(header, 0);
_mainContainerGrid.Children.Add(header);

// ---- 2. Search ----
_searchWrapper = new Border
{
Background = Hex("#F3F4F6"),
BorderBrush = Hex("#E5E7EB"),
BorderThickness = new Thickness(1),
CornerRadius = new CornerRadius(12),
Padding = new Thickness(14, 9, 10, 9),
Margin = new Thickness(22, 4, 22, 14)
};

var searchDock = new DockPanel();

var searchIcon = new TextBlock
{
Text = G_Search,
FontFamily = IconFont,
FontSize = 14,
Foreground = Hex("#6B7280"),
VerticalAlignment = VerticalAlignment.Center,
Margin = new Thickness(0, 0, 12, 0)
};
DockPanel.SetDock(searchIcon, Dock.Left);
searchDock.Children.Add(searchIcon);

var hotkeyPill = new Border
{
Background = Hex("#E5E7EB"),
CornerRadius = new CornerRadius(6),
Padding = new Thickness(7, 3, 7, 3),
VerticalAlignment = VerticalAlignment.Center,
Child = new TextBlock
{
Text = "Ctrl + F",
FontSize = 10.5,
FontWeight = FontWeights.Medium,
Foreground = Hex("#4B5563")
}
};
DockPanel.SetDock(hotkeyPill, Dock.Right);
searchDock.Children.Add(hotkeyPill);

_searchBox.Background = Brushes.Transparent;
_searchBox.Foreground = Hex("#111827");
_searchBox.CaretBrush = Hex("#111827");
_searchBox.BorderThickness = new Thickness(0);
_searchBox.FontSize = 13.5;
_searchBox.VerticalAlignment = VerticalAlignment.Center;

_placeholder.Text = PlaceholderText;
_placeholder.FontSize = 13.5;
_placeholder.Foreground = Hex("#6B7280");
_placeholder.VerticalAlignment = VerticalAlignment.Center;
_placeholder.IsHitTestVisible = false;
_placeholder.Margin = new Thickness(2, 0, 0, 0);

_searchBox.GotKeyboardFocus += (s, e) => _searchWrapper.BorderBrush = Hex("#2563EB");
_searchBox.LostKeyboardFocus += (s, e) => _searchWrapper.BorderBrush = Hex("#E5E7EB");
_searchBox.TextChanged += (s, e) =>
{
_placeholder.Visibility = _searchBox.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
_searchFilterTerm = _searchBox.Text.Trim().ToLowerInvariant();
RefreshCardVisuals();
};

var searchHost = new Grid();
searchHost.Children.Add(_placeholder);
searchHost.Children.Add(_searchBox);
searchDock.Children.Add(searchHost);

_searchWrapper.Child = searchDock;
Grid.SetRow(_searchWrapper, 1);
_mainContainerGrid.Children.Add(_searchWrapper);

// ---- 3. Category chips ----
_chipsPanel.Columns = 7;
var chipsHost = new Border
{
Margin = new Thickness(19, 0, 19, 14),
Child = _chipsPanel
};

var tabSpecs = new (string Name, string Glyph)[]
{
("All", G_All),
("Pinned", G_StarEmpty),
("Text", G_Text),
("Images", G_Image),
("Links", G_Link),
("Files", G_File),
("Video", G_Video)
};

foreach (var (name, glyph) in tabSpecs)
{
bool selected = name == _activeCategoryFilter;
var chip = new Border
{
Background = selected ? Hex("#2563EB") : Hex("#F3F4F6"),
CornerRadius = new CornerRadius(12),
Padding = new Thickness(0, 9, 0, 9),
Margin = new Thickness(3, 0, 3, 0),
Cursor = Cursors.Hand,
Tag = name
};

var content = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
content.Children.Add(new TextBlock
{
Text = glyph,
FontFamily = IconFont,
FontSize = 16,
Foreground = selected ? Brushes.White : Hex("#374151"),
HorizontalAlignment = HorizontalAlignment.Center,
Margin = new Thickness(0, 0, 0, 4)
});
content.Children.Add(new TextBlock
{
Text = name,
FontSize = 11,
FontWeight = selected ? FontWeights.SemiBold : FontWeights.Normal,
Foreground = selected ? Brushes.White : Hex("#374151"),
HorizontalAlignment = HorizontalAlignment.Center
});
chip.Child = content;

chip.MouseEnter += (s, e) =>
{
if ((string)chip.Tag != _activeCategoryFilter) chip.Background = Hex("#E5E7EB");
};
chip.MouseLeave += (s, e) =>
{
if ((string)chip.Tag != _activeCategoryFilter) chip.Background = Hex("#F3F4F6");
};
chip.MouseLeftButtonDown += (s, e) =>
{
_activeCategoryFilter = name;
UpdateFilterTabSelectionStyles();
RefreshCardVisuals();
};

_chipsPanel.Children.Add(chip);
}

Grid.SetRow(chipsHost, 2);
_mainContainerGrid.Children.Add(chipsHost);

// ---- 4. Cards ----
_mainScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
_mainScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
_mainScrollViewer.Margin = new Thickness(22, 0, 14, 8);
_mainScrollViewer.Padding = new Thickness(0, 0, 8, 0);
_mainScrollViewer.Resources.MergedDictionaries.Add(BuildSlimScrollBarStyle());

BuildEmptyState();

_cardsDeck.Orientation = Orientation.Vertical;
_mainScrollViewer.Content = _cardsDeck;

Grid.SetRow(_mainScrollViewer, 3);
_mainContainerGrid.Children.Add(_mainScrollViewer);

// ---- 5. Footer ----
var footer = new Border
{
Height = 28,
Background = Hex("#F9FAFB"),
BorderBrush = Hex("#E5E7EB"),
BorderThickness = new Thickness(0, 1, 0, 0)
};
var creditText = new TextBlock
{
Text = "Made with ⚡ by Shreyash Naik",
FontSize = 11,
FontWeight = FontWeights.Medium,
Foreground = Hex("#9CA3AF"),
HorizontalAlignment = HorizontalAlignment.Center,
VerticalAlignment = VerticalAlignment.Center
};
footer.Child = creditText;

Grid.SetRow(footer, 4);
_mainContainerGrid.Children.Add(footer);

shell.Child = _mainContainerGrid;
this.Content = shell;
}

private static ResourceDictionary BuildSlimScrollBarStyle()
{
const string xaml = @"
<ResourceDictionary xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
<Style TargetType='ScrollBar'>
<Setter Property='Width' Value='8'/>
<Setter Property='Background' Value='Transparent'/>
<Setter Property='Template'>
<Setter.Value>
<ControlTemplate TargetType='ScrollBar'>
<Grid Background='Transparent'>
<Track x:Name='PART_Track' IsDirectionReversed='True'>
<Track.DecreaseRepeatButton>
<RepeatButton Command='ScrollBar.PageUpCommand' Opacity='0' Focusable='False'/>
</Track.DecreaseRepeatButton>
<Track.Thumb>
<Thumb>
<Thumb.Template>
<ControlTemplate TargetType='Thumb'>
<Border Background='#CBD5E1' CornerRadius='4' Margin='1,0'/>
</ControlTemplate>
</Thumb.Template>
</Thumb>
</Track.Thumb>
<Track.IncreaseRepeatButton>
<RepeatButton Command='ScrollBar.PageDownCommand' Opacity='0' Focusable='False'/>
</Track.IncreaseRepeatButton>
</Track>
</Grid>
</ControlTemplate>
</Setter.Value>
</Setter>
</Style>
</ResourceDictionary>";
return (ResourceDictionary)XamlReader.Parse(xaml);
}

private void BuildEmptyState()
{
_emptyState.HorizontalAlignment = HorizontalAlignment.Center;
_emptyState.Margin = new Thickness(0, 70, 0, 0);

_emptyState.Children.Add(new Border
{
Width = 64,
Height = 64,
CornerRadius = new CornerRadius(32),
Background = Hex("#EFF6FF"),
HorizontalAlignment = HorizontalAlignment.Center,
Child = new TextBlock
{
Text = G_Clipboard,
FontFamily = IconFont,
FontSize = 26,
Foreground = Hex("#2563EB"),
HorizontalAlignment = HorizontalAlignment.Center,
VerticalAlignment = VerticalAlignment.Center
}
});
_emptyState.Children.Add(new TextBlock
{
Text = "Nothing here yet",
FontSize = 15,
FontWeight = FontWeights.SemiBold,
Foreground = Hex("#111827"),
HorizontalAlignment = HorizontalAlignment.Center,
Margin = new Thickness(0, 16, 0, 4)
});
_emptyState.Children.Add(new TextBlock
{
Text = "Copy text, a link or a file, or take a\nscreenshot, and it will show up here",
FontSize = 12.5,
Foreground = Hex("#6B7280"),
TextAlignment = TextAlignment.Center,
HorizontalAlignment = HorizontalAlignment.Center
});
}

private void UpdateFilterTabSelectionStyles()
{
foreach (UIElement element in _chipsPanel.Children)
{
if (element is Border chip && chip.Tag is string tag)
{
bool selected = tag == _activeCategoryFilter;
chip.Background = selected ? Hex("#2563EB") : Hex("#F3F4F6");

if (chip.Child is StackPanel sp && sp.Children.Count > 1
&& sp.Children[0] is TextBlock iconTb && sp.Children[1] is TextBlock labelTb)
{
var fg = selected ? Brushes.White : (Brush)Hex("#374151");
iconTb.Foreground = fg;
labelTb.Foreground = fg;
labelTb.FontWeight = selected ? FontWeights.SemiBold : FontWeights.Normal;
}
}
}
}

private Border CreateIconButton(string glyph, Brush foreground, string tooltip, Action<Border> onClick)
{
var btn = new Border
{
Width = 30,
Height = 30,
CornerRadius = new CornerRadius(8),
Background = Brushes.Transparent,
Cursor = Cursors.Hand,
ToolTip = tooltip,
Child = new TextBlock
{
Text = glyph,
FontFamily = IconFont,
FontSize = 14,
Foreground = foreground,
HorizontalAlignment = HorizontalAlignment.Center,
VerticalAlignment = VerticalAlignment.Center
}
};
btn.MouseEnter += (s, e) => btn.Background = Hex("#E5E7EB");
btn.MouseLeave += (s, e) => btn.Background = Brushes.Transparent;
btn.MouseLeftButtonDown += (s, e) =>
{
e.Handled = true;
onClick(btn);
};
return btn;
}

// ---------------------------------------------------------------
// Cards
// ---------------------------------------------------------------
private void RefreshCardVisuals()
{
if (!IsVisible) return;

_cardsDeck.Children.Clear();
_firstVisible = null;
int rendered = 0;

var ordered = _inMemoryItems.OrderByDescending(i => i.IsPinned);

foreach (var record in ordered)
{
if (_activeCategoryFilter == "Pinned" && !record.IsPinned) continue;
if (_activeCategoryFilter != "All" && _activeCategoryFilter != "Pinned" && record.Type != _activeCategoryFilter) continue;

if (!string.IsNullOrEmpty(_searchFilterTerm))
{
bool match = record.Title.ToLowerInvariant().Contains(_searchFilterTerm)
|| record.Detail.ToLowerInvariant().Contains(_searchFilterTerm)
|| (!record.IsScreenshot && record.FullPayload.ToLowerInvariant().Contains(_searchFilterTerm));
if (!match) continue;
}

_firstVisible ??= record;
_cardsDeck.Children.Add(RenderItemCard(record));
rendered++;
}

if (rendered == 0)
_cardsDeck.Children.Add(_emptyState);

int total = _inMemoryItems.Count;
_countText.Text = total == 1 ? "1 item" : $"{total} items";
}

private UIElement RenderItemCard(ClipboardRecord record)
{
var card = new Border
{
Background = Hex("#F9FAFB"),
BorderBrush = Hex("#E5E7EB"),
BorderThickness = new Thickness(1),
CornerRadius = new CornerRadius(14),
Padding = new Thickness(12),
Margin = new Thickness(0, 0, 0, 10),
Cursor = Cursors.Hand
};
card.MouseEnter += (s, e) => { card.Background = Hex("#EFF6FF"); card.BorderBrush = Hex("#BFDBFE"); };
card.MouseLeave += (s, e) => { card.Background = Hex("#F9FAFB"); card.BorderBrush = Hex("#E5E7EB"); };

var dock = new DockPanel();

var badge = CreateTypeBadge(record);
DockPanel.SetDock(badge, Dock.Left);
dock.Children.Add(badge);

var actions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(8, 0, 0, 0) };

actions.Children.Add(CreateIconButton(
record.IsPinned ? G_StarFull : G_StarEmpty,
record.IsPinned ? Hex("#F59E0B") : Hex("#6B7280"),
record.IsPinned ? "Unpin" : "Pin",
_ =>
{
record.IsPinned = !record.IsPinned;
RefreshCardVisuals();
}));

actions.Children.Add(CreateIconButton(G_More, Hex("#6B7280"), "More", anchor =>
{
var menu = new ContextMenu { PlacementTarget = anchor, Placement = PlacementMode.Bottom };
menu.Opened += (s, e) => _menuOpen = true;
menu.Closed += (s, e) => _menuOpen = false;

var pin = new MenuItem { Header = record.IsPinned ? "Unpin" : "Pin" };
pin.Click += (s, e) => { record.IsPinned = !record.IsPinned; RefreshCardVisuals(); };

var copy = new MenuItem { Header = "Copy" };
copy.Click += async (s, e) => await CopyRecordAsync(record);

var delete = new MenuItem { Header = "Delete" };
delete.Click += (s, e) => { RemoveRecord(record); RefreshCardVisuals(); };

menu.Items.Add(pin);
menu.Items.Add(copy);
menu.Items.Add(new Separator());
menu.Items.Add(delete);
menu.IsOpen = true;
}));

DockPanel.SetDock(actions, Dock.Right);
dock.Children.Add(actions);

var info = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

info.Children.Add(new TextBlock
{
Text = record.Title,
FontSize = 14,
FontWeight = FontWeights.SemiBold,
Foreground = Hex("#111827"),
TextTrimming = TextTrimming.CharacterEllipsis
});

info.Children.Add(new TextBlock
{
Text = $"{TypeLabel(record)} · {FormatAgo(record.Timestamp)}",
FontSize = 11.5,
Foreground = Hex("#6B7280"),
Margin = new Thickness(0, 2, 0, 0)
});

if (record.Type == "Images")
{
var preview = GetPreview(record);
if (preview != null)
{
info.Children.Add(new Border
{
Height = 130,
Margin = new Thickness(0, 8, 0, 0),
CornerRadius = new CornerRadius(8),
BorderBrush = Hex("#E5E7EB"),
BorderThickness = new Thickness(1),
Background = new ImageBrush(preview) { Stretch = Stretch.UniformToFill }
});
}
}

if (record.IsCode && !string.IsNullOrWhiteSpace(record.Detail))
{
info.Children.Add(new Border
{
Background = Hex("#F3F4F6"),
BorderBrush = Hex("#E5E7EB"),
BorderThickness = new Thickness(1),
CornerRadius = new CornerRadius(8),
Padding = new Thickness(10, 8, 10, 8),
Margin = new Thickness(0, 8, 0, 0),
Child = new TextBlock
{
Text = record.Detail,
FontFamily = new FontFamily("Cascadia Code, Consolas, Courier New"),
FontSize = 11.5,
Foreground = Hex("#1F2937"),
TextTrimming = TextTrimming.CharacterEllipsis
}
});
}
else if (!string.IsNullOrWhiteSpace(record.Detail))
{
info.Children.Add(new TextBlock
{
Text = record.Detail,
FontSize = 12,
Foreground = Hex("#4B5563"),
TextTrimming = TextTrimming.CharacterEllipsis,
Margin = new Thickness(0, 4, 0, 0)
});
}

dock.Children.Add(info);
card.Child = dock;

card.MouseLeftButtonDown += async (s, e) => await PasteRecordAsync(record);

return card;
}

private UIElement CreateTypeBadge(ClipboardRecord record)
{
Border MakeBadge(string bg, string fg, string glyph)
{
return new Border
{
Background = Hex(bg),
CornerRadius = new CornerRadius(12),
Width = 46,
Height = 46,
Margin = new Thickness(0, 0, 12, 0),
VerticalAlignment = VerticalAlignment.Top,
Child = new TextBlock
{
Text = glyph,
FontFamily = IconFont,
FontSize = 20,
Foreground = Hex(fg),
HorizontalAlignment = HorizontalAlignment.Center,
VerticalAlignment = VerticalAlignment.Center
}
};
}

switch (record.Type)
{
case "Images":
return MakeBadge("#FFEDD5", "#EA580C", G_Image);

case "Video":
return MakeBadge("#FCE7F3", "#DB2777", G_Play);

case "Links":
return MakeBadge("#CCFBF1", "#0D9488", G_Link);

case "Files":
{
string ext = Path.GetExtension(record.Title).TrimStart('.').ToUpperInvariant();
if (ext.Length == 0) return MakeBadge("#E5E7EB", "#374151", G_File);
if (ext.Length > 4) ext = ext.Substring(0, 4);

string bg = ext switch
{
"PDF" => "#EF4444",
"DOC" or "DOCX" => "#2563EB",
"XLS" or "XLSX" or "CSV" => "#16A34A",
"PPT" or "PPTX" => "#EA580C",
"ZIP" or "RAR" or "7Z" => "#D97706",
_ => "#64748B"
};

return new Border
{
Background = Hex(bg),
CornerRadius = new CornerRadius(12),
Width = 46,
Height = 46,
Margin = new Thickness(0, 0, 12, 0),
VerticalAlignment = VerticalAlignment.Top,
Child = new TextBlock
{
Text = ext,
FontSize = 11.5,
FontWeight = FontWeights.Bold,
Foreground = Brushes.White,
HorizontalAlignment = HorizontalAlignment.Center,
VerticalAlignment = VerticalAlignment.Center
}
};
}

default:
return record.IsCode
? MakeBadge("#EDE9FE", "#7C3AED", G_Code)
: MakeBadge("#DBEAFE", "#2563EB", G_Text);
}
}

private static BitmapImage? GetPreview(ClipboardRecord r)
{
r.Preview ??= TryLoadThumbnail(r.FullPayload, 460);
return r.Preview;
}

private static BitmapImage? TryLoadThumbnail(string path, int decodeWidth)
{
try
{
if (!File.Exists(path)) return null;
var bmp = new BitmapImage();
bmp.BeginInit();
bmp.UriSource = new Uri(path);
bmp.DecodePixelWidth = decodeWidth;
bmp.CacheOption = BitmapCacheOption.OnLoad;
bmp.EndInit();
bmp.Freeze();
return bmp;
}
catch
{
return null;
}
}

// ---------------------------------------------------------------
// Clipboard actions
// ---------------------------------------------------------------
private void WriteRecordToClipboard(ClipboardRecord r)
{
_lastCapturedPayload = r.FullPayload;

if (r.IsScreenshot && File.Exists(r.FullPayload))
{
var bmp = new BitmapImage();
bmp.BeginInit();
bmp.UriSource = new Uri(r.FullPayload);
bmp.CacheOption = BitmapCacheOption.OnLoad;
bmp.EndInit();
bmp.Freeze();

var data = new DataObject();
data.SetImage(bmp);

using var ms = new MemoryStream();
var encoder = new PngBitmapEncoder();
encoder.Frames.Add(BitmapFrame.Create(bmp));
encoder.Save(ms);
data.SetData("PNG", ms);

Clipboard.SetDataObject(data, true);
}
else if ((r.Type == "Images" || r.Type == "Files" || r.Type == "Video") && File.Exists(r.FullPayload))
{
var list = new System.Collections.Specialized.StringCollection { r.FullPayload };
Clipboard.SetFileDropList(list);
}
else
{
Clipboard.SetDataObject(r.FullPayload, true);
}
}

private async Task CopyRecordAsync(ClipboardRecord r)
{
try
{
_isInternalClipboardWrite = true;
WriteRecordToClipboard(r);
}
catch { }
finally
{
await Task.Delay(400);
_isInternalClipboardWrite = false;
}
}

private async Task PasteRecordAsync(ClipboardRecord r)
{
try
{
_isInternalClipboardWrite = true;
WriteRecordToClipboard(r);

HideWindow();
if (_previousWindow != IntPtr.Zero)
SetForegroundWindow(_previousWindow);

await Task.Delay(150);

keybd_event(VK_CONTROL, 0, 0, 0);
keybd_event(VK_V, 0, 0, 0);
keybd_event(VK_V, 0, KEYEVENTF_KEYUP, 0);
keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, 0);
}
catch { }
finally
{
await Task.Delay(350);
_isInternalClipboardWrite = false;
}
}

// ---------------------------------------------------------------
// Win32 hooks
// ---------------------------------------------------------------
private void OnWindowSourceInitialized(object? sender, EventArgs e)
{
_hwnd = new WindowInteropHelper(this).Handle;

try
{
int darkMode = 0;
DwmSetWindowAttribute(_hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));

int corner = DWMWCP_ROUND;
DwmSetWindowAttribute(_hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref corner, sizeof(int));
}
catch (DllNotFoundException) { }
catch (EntryPointNotFoundException) { }

HwndSource.FromHwnd(_hwnd)?.AddHook(WndProc);
AddClipboardFormatListener(_hwnd);

if (!RegisterHotKey(_hwnd, HOTKEY_ID, HotkeyModifiers, HotkeyVirtualKey))
{
MessageBox.Show(
$"Better Clip couldn't register {HotkeyLabel}; another app or input method is probably using it.\n\n" +
"You can still open it from the tray icon. To use a different shortcut, change HotkeyModifiers / HotkeyVirtualKey at the top of MainWindow.",
"Better Clip", MessageBoxButton.OK, MessageBoxImage.Warning);
}

AddTrayIcon();

if (Application.Current != null)
Application.Current.SessionEnding += (s, args) => _reallyQuit = true;
}

private void OnWindowClosed(object? sender, EventArgs e)
{
if (_hwnd != IntPtr.Zero)
{
UnregisterHotKey(_hwnd, HOTKEY_ID);
RemoveClipboardFormatListener(_hwnd);
RemoveTrayIcon();
}
try { _singleInstance?.ReleaseMutex(); } catch { }
}

private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
{
switch (msg)
{
case WM_HOTKEY when wParam.ToInt32() == HOTKEY_ID:
ToggleWindow();
handled = true;
break;

case WM_TRAY:
{
int mouseMsg = (int)(lParam.ToInt64() & 0xFFFF);
if (mouseMsg == WM_LBUTTONUP) ToggleWindow();
else if (mouseMsg == WM_RBUTTONUP) ShowTrayMenu();
handled = true;
break;
}

case WM_CLIPBOARDUPDATE:
if (!_isInternalClipboardWrite)
_ = CaptureSystemClipboardAsync();
handled = true;
break;
}
return IntPtr.Zero;
}

// ---------------------------------------------------------------
// Capture
// ---------------------------------------------------------------
private void RemoveRecord(ClipboardRecord record)
{
_inMemoryItems.Remove(record);
if (record.IsScreenshot)
{
try { File.Delete(record.FullPayload); } catch { }
}
}

private void AddRecord(ClipboardRecord record)
{
var existing = _inMemoryItems.FirstOrDefault(i => i.FullPayload == record.FullPayload && i.Type == record.Type);
if (existing != null)
{
record.IsPinned = existing.IsPinned;
_inMemoryItems.Remove(existing);
}

_inMemoryItems.Insert(0, record);

while (_inMemoryItems.Count > MaxItems)
{
var victim = _inMemoryItems.LastOrDefault(i => !i.IsPinned);
if (victim == null) break;
RemoveRecord(victim);
}

RefreshCardVisuals();
}

private static async Task<T?> ClipboardRetryAsync<T>(Func<T> action)
{
for (int i = 0; i < 8; i++)
{
try { return action(); }
catch (ExternalException) { await Task.Delay(40); }
}
return default;
}

private async Task CaptureSystemClipboardAsync()
{
try
{
int kind = await ClipboardRetryAsync(() =>
{
if (Clipboard.ContainsText()) return 1;
if (Clipboard.ContainsFileDropList()) return 2;
if (Clipboard.ContainsImage()) return 3;
return 0;
});

switch (kind)
{
case 1: await CaptureTextAsync(); break;
case 2: await CaptureFilesAsync(); break;
case 3: await CaptureImageAsync(); break;
}
}
catch { }
}

private async Task CaptureTextAsync()
{
string? text = await ClipboardRetryAsync(() => Clipboard.GetText());
if (string.IsNullOrWhiteSpace(text) || text == _lastCapturedPayload) return;
_lastCapturedPayload = text;

string trimmed = text.Trim();

bool isLink = !trimmed.Contains(' ') && !trimmed.Contains('\n')
&& Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
&& (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

if (isLink)
{
AddRecord(new ClipboardRecord
{
Type = "Links",
Title = trimmed.Length > 60 ? trimmed.Substring(0, 60) + "…" : trimmed,
Detail = new Uri(trimmed).Host,
FullPayload = text
});
return;
}

bool looksLikeCode = text.Contains('{') || text.Contains(';') || text.Contains("=>")
|| text.Contains("class ") || text.Contains("def ") || text.Contains("</");

if (looksLikeCode)
{
AddRecord(new ClipboardRecord
{
Type = "Text",
IsCode = true,
Title = "Code snippet",
Detail = Preview(text, 4, 90),
FullPayload = text
});
}
else
{
string title = FirstLine(text, 60);
string rest = Preview(text, 2, 110);
string detail = rest.Replace("\n", " ").Trim() == title.TrimEnd('…') || rest.Length <= title.Length
? string.Empty
: rest.Replace("\n", " ");

AddRecord(new ClipboardRecord
{
Type = "Text",
Title = title,
Detail = detail,
FullPayload = text
});
}
}

private async Task CaptureFilesAsync()
{
var files = await ClipboardRetryAsync(() => Clipboard.GetFileDropList());
if (files == null || files.Count == 0) return;

string filePath = files[0]!;
if (filePath == _lastCapturedPayload) return;
_lastCapturedPayload = filePath;

string ext = Path.GetExtension(filePath).ToLowerInvariant();
string fileName = Path.GetFileName(filePath);

string category = "Files";
if (ext is ".png" or ".jpg" or ".jpeg" or ".webp" or ".gif" or ".bmp")
category = "Images";
else if (ext is ".mp4" or ".mkv" or ".mov" or ".avi" or ".webm")
category = "Video";

string detail = string.Empty;
try { detail = FormatSize(new FileInfo(filePath).Length); } catch { }
if (files.Count > 1) detail += (detail.Length > 0 ? " · " : "") + $"+{files.Count - 1} more";

AddRecord(new ClipboardRecord
{
Type = category,
Title = fileName,
Detail = detail,
FullPayload = filePath
});
}

private async Task CaptureImageAsync()
{
var source = await ClipboardRetryAsync(() => Clipboard.GetImage());
if (source == null) return;

var opaque = new FormatConvertedBitmap(source, PixelFormats.Bgr32, null, 0);
opaque.Freeze();

string hash = await Task.Run(() =>
{
int stride = opaque.PixelWidth * 4;
var pixels = new byte[stride * opaque.PixelHeight];
opaque.CopyPixels(pixels, stride, 0);
return Convert.ToHexString(SHA256.HashData(pixels));
});

var dup = _inMemoryItems.FirstOrDefault(i => i.Hash == hash);
if (dup != null)
{
_inMemoryItems.Remove(dup);
dup.Timestamp = DateTime.Now;
_inMemoryItems.Insert(0, dup);
RefreshCardVisuals();
return;
}

string path = Path.Combine(ImageCacheDir, $"{Guid.NewGuid():N}.png");
long length = await Task.Run(() =>
{
Directory.CreateDirectory(ImageCacheDir);
var encoder = new PngBitmapEncoder();
encoder.Frames.Add(BitmapFrame.Create(opaque));
using (var fs = File.Create(path))
encoder.Save(fs);
return new FileInfo(path).Length;
});

AddRecord(new ClipboardRecord
{
Type = "Images",
IsScreenshot = true,
Hash = hash,
Title = "Screenshot",
Detail = $"{opaque.PixelWidth} × {opaque.PixelHeight} · {FormatSize(length)}",
FullPayload = path
});
}
}
}