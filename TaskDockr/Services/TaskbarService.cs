using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FontAwesome6;
using FontAwesome6.Fonts.Extensions;
using TaskDockr.Models;
using TaskDockr.Utils;

namespace TaskDockr.Services
{
    /// <summary>
    /// Creates one minimal, always-minimized WPF Window per Group so that each group
    /// appears as a button on the Windows taskbar. Clicking the button shows the
    /// group-specific shortcuts popout and immediately re-minimizes the window.
    /// </summary>
    public class TaskbarService : ITaskbarService
    {
        private readonly IPopoutService _popoutService;
        private readonly IGroupService _groupService;

        // Maps groupId → the hidden taskbar window for that group
        private readonly Dictionary<string, Window> _groupWindows = new();

        // Cursor position captured at the exact moment of WM_ACTIVATE
        private int _clickX, _clickY;
        private DateTime _clickCapturedAt = DateTime.MinValue;

        // Tracks when we last showed a popup to prevent re-triggering from
        // window re-activation when the popup closes and returns focus
        private DateTime _lastPopoutShowTime = DateTime.MinValue;

        public event EventHandler<TaskbarIconClickEventArgs>? IconClicked;

        public TaskbarService(IPopoutService popoutService, IGroupService groupService)
        {
            _popoutService = popoutService;
            _groupService = groupService;

            // Subscribe to group lifecycle events to avoid circular dependency
            _groupService.GroupCreated += OnGroupCreated;
            _groupService.GroupUpdated += OnGroupUpdated;
            _groupService.GroupDeleted += OnGroupDeleted;
        }

        private async void OnGroupCreated(object? sender, GroupEventArgs e)
        {
            if (e.Group != null)
            {
                Debug.WriteLine($"[TaskbarService] GroupCreated event received for '{e.Group.Name}'");
                await CreateGroupIconAsync(e.Group);
            }
        }

        private async void OnGroupUpdated(object? sender, GroupEventArgs e)
        {
            if (e.Group != null)
            {
                Debug.WriteLine($"[TaskbarService] GroupUpdated event received for '{e.Group.Name}'");
                await UpdateGroupIconAsync(e.Group);
            }
        }

        private async void OnGroupDeleted(object? sender, GroupEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.GroupId))
            {
                Debug.WriteLine($"[TaskbarService] GroupDeleted event received for ID '{e.GroupId}'");
                await RemoveGroupIconAsync(e.GroupId);
            }
        }

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task CleanupAsync()
        {
            // Unsubscribe from events to prevent memory leaks
            _groupService.GroupCreated -= OnGroupCreated;
            _groupService.GroupUpdated -= OnGroupUpdated;
            _groupService.GroupDeleted -= OnGroupDeleted;

            foreach (var win in _groupWindows.Values)
            {
                try { win.Close(); } catch { }
            }
            _groupWindows.Clear();
            await Task.CompletedTask;
        }

public async Task<bool> CreateGroupIconAsync(Group group)
    {
        Debug.WriteLine($"[TaskbarService] CreateGroupIconAsync called for group: '{group?.Name ?? "null"}' (ID: {group?.Id ?? "null"})");

        if (group == null || string.IsNullOrEmpty(group.Id))
        {
            Debug.WriteLine("[TaskbarService] CreateGroupIconAsync failed: group is null or has no ID");
            return false;
        }

        // Don't create a duplicate
        if (_groupWindows.ContainsKey(group.Id))
        {
            Debug.WriteLine($"[TaskbarService] Window already exists for group: '{group.Name}'");
            return true;
        }

        try
        {
            if (Application.Current == null)
            {
                Debug.WriteLine("[TaskbarService] ERROR: Application.Current is null!");
                return false;
            }

            if (Application.Current.Dispatcher == null)
            {
                Debug.WriteLine("[TaskbarService] ERROR: Application.Current.Dispatcher is null!");
                return false;
            }

            Debug.WriteLine($"[TaskbarService] Dispatching window creation for '{group.Name}' on thread {Environment.CurrentManagedThreadId}");
            
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Debug.WriteLine($"[TaskbarService] Dispatcher.InvokeAsync executing for '{group.Name}' on thread {Environment.CurrentManagedThreadId}");
                CreateTaskbarWindowForGroup(group);
            });
            
            Debug.WriteLine($"[TaskbarService] Window creation completed for '{group.Name}'");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TaskbarService] CreateGroupIconAsync EXCEPTION for '{group.Name}': {ex}");
            return false;
        }
    }

        public Task<bool> UpdateGroupIconAsync(Group group)
        {
            if (group == null) return Task.FromResult(false);

            LogToFile($"UpdateGroupIconAsync for '{group.Name}': IconGlyph='{group.IconGlyph}', IconColor='{group.IconColor}'");

            if (_groupWindows.TryGetValue(group.Id, out var win))
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    win.Title = group.Name;
                    TrySetWindowIcon(win, group);
                });
                return Task.FromResult(true);
            }

            LogToFile($"UpdateGroupIconAsync: no window for '{group.Name}', creating new");
            // Not yet created — create it now
            return CreateGroupIconAsync(group);
        }

        public Task<bool> RemoveGroupIconAsync(string groupId)
        {
            if (string.IsNullOrEmpty(groupId)) return Task.FromResult(false);

            if (_groupWindows.TryGetValue(groupId, out var win))
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    try { win.Close(); } catch { }
                });
                _groupWindows.Remove(groupId);
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public Task ShowGroupMenuAsync(string groupId, int x, int y)
        {
            // Handled inline via StateChanged; this overload is unused
            return Task.CompletedTask;
        }

        // ── private helpers ─────────────────────────────────────────────────

        private void CreateTaskbarWindowForGroup(Group group)
        {
            var win = new Window
            {
                Title = group.Name,
                WindowStyle = WindowStyle.SingleBorderWindow,
                ShowInTaskbar = true,
                ShowActivated = false,
                Width = 200,
                Height = 100,
                Left = 0,
                Top = 0,
                ResizeMode = ResizeMode.NoResize,
            };

            // Set the icon: use group's custom icon or fall back to the app icon
            TrySetWindowIcon(win, group);

            // CRITICAL: set AppUserModelID BEFORE Show() so Windows 11 registers
            // this window as a separate taskbar button rather than grouping it
            // with the main TaskDockr window.
            var helper = new WindowInteropHelper(win);
            helper.EnsureHandle();
            TaskbarHelper.SetWindowAppUserModelId(helper.Handle, $"TaskDockr.Group.{group.Id}");

            // Install WndProc hook to capture cursor position at WM_ACTIVATE time
            var hwndSource = HwndSource.FromHwnd(helper.Handle);
            hwndSource?.AddHook(WndProcHook);

            LogToFile($"[POSITION] Setting up events for '{group.Name}'");

            var capturedGroup = group;
            var capturedWin = win;

            // Use StateChanged to detect taskbar button clicks (Minimized → Normal)
            // Activated doesn't fire reliably on Windows 11 for minimized proxy windows
            win.StateChanged += (s, e) =>
            {
                LogToFile($"[POSITION] StateChanged: WindowState={capturedWin.WindowState}");

                // Only act when the window is being restored (user clicked taskbar button)
                if (capturedWin.WindowState != WindowState.Minimized)
                {
                    // Capture cursor IMMEDIATELY — still on taskbar button at this point
                    GetCursorPos(out POINT immediatePos);

                    LogToFile($"[POSITION] StateChanged restore: cursor=({immediatePos.X},{immediatePos.Y})");

                    // Guard: when the popup closes it returns focus to the taskbar window,
                    // which fires StateChanged again. Skip if we just showed a popup.
                    var elapsed = DateTime.UtcNow - _lastPopoutShowTime;
                    if (elapsed.TotalMilliseconds < 600)
                    {
                        Debug.WriteLine($"[TaskbarService.StateChanged] Skipping re-activation (elapsed={elapsed.TotalMilliseconds}ms)");
                        capturedWin.WindowState = WindowState.Minimized;
                        return;
                    }

                    // Taskbar button was clicked - show popup
                    int posX, posY, width, height;

                    // Use cursor position — prefer WndProc capture if fresh, else use immediate
                    GetTaskbarButtonPosition(immediatePos.X, immediatePos.Y, out posX, out posY, out width, out height);

                    Debug.WriteLine($"[TaskbarService.StateChanged] Button position: ({posX}, {posY}) size: {width}x{height}");

                    _lastPopoutShowTime = DateTime.UtcNow;

                    // Use position for popup positioning
                    _popoutService.ShowPopoutForGroupAtPosition(capturedGroup, posX, posY, width, height);

                    // Keep window minimized to maintain taskbar button behavior
                    capturedWin.WindowState = WindowState.Minimized;
                }
            };

            win.Closed += (s, e) =>
            {
                _groupWindows.Remove(capturedGroup.Id);
            };

            win.Show();
            win.WindowState = WindowState.Minimized;

_groupWindows[group.Id] = win;
            Debug.WriteLine($"[TaskbarService] Taskbar button created for '{group.Name}'");
            LogPositionDebug($"Taskbar window created for '{group.Name}', WindowState={win.WindowState}, ShowInTaskbar={win.ShowInTaskbar}");
        }

        private static void TrySetWindowIcon(Window win, Group group)
        {
            try
            {
                Debug.WriteLine($"[TaskbarService] TrySetWindowIcon for '{group.Name}': IconGlyph='{group.IconGlyph}', IconColor='{group.IconColor}', IconPath='{group.IconPath}'");
                LogToFile($"TrySetWindowIcon for '{group.Name}': IconGlyph='{group.IconGlyph}', IconColor='{group.IconColor}', IconPath='{group.IconPath}'");

                // Priority 1: Font Awesome glyph
                if (!string.IsNullOrEmpty(group.IconGlyph) &&
                    Enum.TryParse<EFontAwesomeIcon>(group.IconGlyph, out var faIcon))
                {
                    LogToFile($"Parsed FA icon: {faIcon}");
                    var iconSource = RenderGlyphToIcon(faIcon, group.IconColor);
                    if (iconSource != null)
                    {
                        LogToFile($"FA icon rendered successfully ({iconSource.PixelWidth}x{iconSource.PixelHeight}), setting window icon");
                        win.Icon = iconSource;
                        return;
                    }
                    LogToFile($"RenderGlyphToIcon returned null!");
                }

                // Priority 2: group's custom icon file
                if (!string.IsNullOrEmpty(group.IconPath) && File.Exists(group.IconPath))
                {
                    win.Icon = BitmapFrame.Create(
                        new Uri(group.IconPath, UriKind.Absolute),
                        BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                    return;
                }

                // Priority 3: app's own icon
                var appIconPath = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "Assets", "AppIcon.ico");
                if (File.Exists(appIconPath))
                {
                    win.Icon = BitmapFrame.Create(
                        new Uri(appIconPath, UriKind.Absolute),
                        BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TaskbarService] TrySetWindowIcon error: {ex.Message}");
            }
        }

        /// <summary>
        /// Renders a Font Awesome glyph to a 32x32 BitmapSource suitable for a window icon.
        /// </summary>
        private static BitmapSource? RenderGlyphToIcon(EFontAwesomeIcon icon, string? colorHex)
        {
            try
            {
                Brush brush = Brushes.White;
                if (!string.IsNullOrEmpty(colorHex))
                {
                    try
                    {
                        var color = (Color)ColorConverter.ConvertFromString(colorHex);
                        brush = new SolidColorBrush(color);
                        brush.Freeze();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[TaskbarService] RenderGlyphToIcon color parse error for '{colorHex}': {ex.Message}");
                    }
                }

                const double emSize = 24;
                const int bitmapSize = 32;

                // Try CreateImageSource first (handles font loading internally)
                try
                {
                    var imageSource = icon.CreateImageSource(brush, null, null, null, null, emSize);
                    if (imageSource != null)
                    {
                        LogToFile($"CreateImageSource succeeded for {icon}");
                        var visual = new DrawingVisual();
                        using (var ctx = visual.RenderOpen())
                        {
                            ctx.DrawImage(imageSource, new Rect(0, 0, bitmapSize, bitmapSize));
                        }
                        var rtb = new RenderTargetBitmap(bitmapSize, bitmapSize, 96, 96, PixelFormats.Pbgra32);
                        rtb.Render(visual);
                        rtb.Freeze();
                        return rtb;
                    }
                    LogToFile($"CreateImageSource returned null for {icon}");
                }
                catch (Exception ex)
                {
                    LogToFile($"CreateImageSource failed for {icon}: {ex.Message}");
                }

                // Fallback: render using GetUnicode + GetFontFamily directly
                try
                {
                    var unicode = icon.GetUnicode();
                    var fontFamily = icon.GetFontFamily();
                    LogToFile($"Fallback render: unicode='{unicode}', fontFamily={fontFamily?.Source ?? "null"}");

                    if (!string.IsNullOrEmpty(unicode) && fontFamily != null)
                    {
                        var typeface = new Typeface(fontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
                        var formattedText = new FormattedText(
                            unicode, System.Globalization.CultureInfo.InvariantCulture,
                            FlowDirection.LeftToRight, typeface, emSize, brush,
                            VisualTreeHelper.GetDpi(Application.Current.MainWindow).PixelsPerDip);

                        // Center the glyph in the bitmap
                        double offsetX = (bitmapSize - formattedText.Width) / 2;
                        double offsetY = (bitmapSize - formattedText.Height) / 2;

                        var visual = new DrawingVisual();
                        using (var ctx = visual.RenderOpen())
                        {
                            ctx.DrawText(formattedText, new System.Windows.Point(offsetX, offsetY));
                        }
                        var rtb = new RenderTargetBitmap(bitmapSize, bitmapSize, 96, 96, PixelFormats.Pbgra32);
                        rtb.Render(visual);
                        rtb.Freeze();
                        LogToFile($"Fallback render succeeded");
                        return rtb;
                    }
                }
                catch (Exception ex)
                {
                    LogToFile($"Fallback render failed: {ex.Message}");
                }

                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TaskbarService] RenderGlyphToIcon error: {ex.Message}");
                return null;
            }
        }

        private const int WM_ACTIVATE = 0x0006;
        private const int WA_ACTIVE = 1;
        private const int WA_CLICKACTIVE = 2;

        /// <summary>
        /// Win32 message hook that intercepts WM_ACTIVATE at the exact moment the taskbar
        /// button is clicked. At this point the cursor is still physically on the button,
        /// so GetCursorPos gives us the correct position (unlike GetMessagePos which returns
        /// stale data by the time WPF's Activated event fires).
        /// </summary>
        private IntPtr WndProcHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_ACTIVATE)
            {
                int activateState = wParam.ToInt32() & 0xFFFF;
                if (activateState == WA_ACTIVE || activateState == WA_CLICKACTIVE)
                {
                    if (GetCursorPos(out POINT pt))
                    {
                        _clickX = pt.X;
                        _clickY = pt.Y;
                        _clickCapturedAt = DateTime.UtcNow;
                        Debug.WriteLine($"[TaskbarService.WndProc] WM_ACTIVATE captured cursor at ({pt.X}, {pt.Y})");
                    }
                }
            }
            return IntPtr.Zero;
        }

        /// <summary>
        /// Gets the taskbar button position using the cursor location captured by WndProc or the
        /// Activated handler's immediate capture. Returns coordinates in WPF DIPs.
        /// </summary>
        private void GetTaskbarButtonPosition(int immediateX, int immediateY, out int x, out int y, out int width, out int height)
        {
            var dpiScale = GetDpiScale();

            width = 44;
            height = 48;

            int rawX, rawY;

            // Use WndProc-captured position if fresh (< 300ms), otherwise use the
            // immediate cursor position captured at the top of the Activated handler
            var age = (DateTime.UtcNow - _clickCapturedAt).TotalMilliseconds;
            if (age < 300)
            {
                rawX = _clickX;
                rawY = _clickY;
                Debug.WriteLine($"[TaskbarService] Using WndProc position ({rawX},{rawY}), age={age:F0}ms");
            }
            else
            {
                rawX = immediateX;
                rawY = immediateY;
                Debug.WriteLine($"[TaskbarService] Using immediate position ({rawX},{rawY}), WndProc age={age:F0}ms");
            }

            // Convert physical pixels to WPF DIPs
            x = (int)(rawX / dpiScale);
            y = (int)(rawY / dpiScale);

            // Snap to the exact taskbar edge for precise popup alignment
            IntPtr taskbarHwnd = FindWindow("Shell_TrayWnd", null);
            if (taskbarHwnd != IntPtr.Zero && GetWindowRect(taskbarHwnd, out RECT taskbarRect))
            {
                switch (GetTaskbarPosition())
                {
                    case TaskbarPosition.Bottom:
                        y = (int)(taskbarRect.Top / dpiScale);
                        break;
                    case TaskbarPosition.Top:
                        y = (int)(taskbarRect.Bottom / dpiScale);
                        break;
                    case TaskbarPosition.Left:
                        x = (int)(taskbarRect.Right / dpiScale);
                        break;
                    case TaskbarPosition.Right:
                        x = (int)(taskbarRect.Left / dpiScale);
                        break;
                }
            }

            Debug.WriteLine($"[TaskbarService] Click at ({rawX},{rawY})px -> ({x},{y})dip, dpiScale={dpiScale}");
            LogPositionDebug($"Raw cursor: ({rawX},{rawY})px | DPI scale: {dpiScale} | DIPs: ({x},{y}) | Source: {(age < 300 ? "WndProc" : "Immediate")} | WndProc age: {age:F0}ms");
        }

        /// <summary>
        /// Gets the DPI scale factor for the primary screen using Win32 API.
        /// Returns physical pixels per DIP (e.g. 1.25 for 125%, 1.5 for 150%).
        /// </summary>
        private static double GetDpiScale()
        {
            try
            {
                // GetDpiForSystem returns the system DPI (e.g. 96, 120, 144, 168, 192)
                // 96 DPI = 100% scaling, so scale = dpi / 96.0
                uint dpi = GetDpiForSystem();
                if (dpi > 0)
                    return dpi / 96.0;
            }
            catch { }

            return 1.0;
        }

        /// <summary>
        /// Determines taskbar position based on work area.
        /// </summary>
        private static TaskbarPosition GetTaskbarPosition()
        {
            var workArea = SystemParameters.WorkArea;
            var screenHeight = SystemParameters.FullPrimaryScreenHeight;
            var screenWidth = SystemParameters.FullPrimaryScreenWidth;

            if (workArea.Top > 0) return TaskbarPosition.Top;
            if (workArea.Left > 0) return TaskbarPosition.Left;
            if (workArea.Right < screenWidth) return TaskbarPosition.Right;
            return TaskbarPosition.Bottom;
        }

        private static void LogPositionDebug(string message)
        {
            try
            {
                var logPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "TaskDockr", "position_debug.log");
                File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss.fff}] [TaskbarService] {message}\n");
            }
            catch { }
        }

        private static void LogToFile(string message)
        {
            try
            {
                var logPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "TaskDockr", "icon_debug.log");
                File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss.fff}] {message}\n");
            }
            catch { }
        }

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern uint GetDpiForSystem();

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }
    }
}
