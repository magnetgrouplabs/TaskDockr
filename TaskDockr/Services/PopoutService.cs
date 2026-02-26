using TaskDockr.Models;
using TaskDockr.Views;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;

namespace TaskDockr.Services
{
    public interface IPopoutService
    {
        void ShowPopout();
        void ShowPopoutForGroup(Group group);
        void ShowPopoutForGroupAtPosition(Group group, int taskbarIconX, int taskbarIconY, int taskbarIconWidth = 0, int taskbarIconHeight = 0);
        void HidePopout();
        bool IsPopoutVisible { get; }
        void TogglePopout();
        void UpdatePopoutPosition();
        void RefreshPopoutData();
    }

    public enum TaskbarPosition
    {
        Bottom,
        Top,
        Left,
        Right
    }

    public class PopoutService : IPopoutService
    {
        private PopoutWindow? _popoutWindow;
        private bool _isPopoutVisible;

        public bool IsPopoutVisible => _isPopoutVisible;

        /// <summary>Shows the general (all-shortcuts) popout.</summary>
        public void ShowPopout() => OpenPopout(group: null, taskbarIconX: 0, taskbarIconY: 0, taskbarIconWidth: 0, taskbarIconHeight: 0, useIconPosition: false);

        /// <summary>Shows the popout pre-filtered to a specific group's shortcuts.</summary>
        public void ShowPopoutForGroup(Group group) => OpenPopout(group, taskbarIconX: 0, taskbarIconY: 0, taskbarIconWidth: 0, taskbarIconHeight: 0, useIconPosition: false);

        /// <summary>Shows the popout for a specific group positioned near the taskbar icon.</summary>
        public void ShowPopoutForGroupAtPosition(Group group, int taskbarIconX, int taskbarIconY, int taskbarIconWidth = 0, int taskbarIconHeight = 0)
        {
            OpenPopout(group, taskbarIconX, taskbarIconY, taskbarIconWidth, taskbarIconHeight, useIconPosition: true);
        }

        public void HidePopout()
        {
            if (_popoutWindow != null)
            {
                _popoutWindow.Close();
                _popoutWindow    = null;
                _isPopoutVisible = false;
            }
        }

        public void TogglePopout()
        {
            if (_isPopoutVisible) HidePopout();
            else ShowPopout();
        }

        public void UpdatePopoutPosition()
        {
            if (_popoutWindow == null) return;
            PositionWindow(_popoutWindow, 0, 0, 0, 0, useIconPosition: false, shortcutCount: 0);
        }

        public void RefreshPopoutData() => _popoutWindow?.RefreshData();

        // ── private ────────────────────────────────────────────────────────

        private static TaskbarPosition GetTaskbarPosition()
        {
            var workArea = SystemParameters.WorkArea;
            var screenWidth = SystemParameters.FullPrimaryScreenWidth;

            if (workArea.Top > 0) return TaskbarPosition.Top;
            if (workArea.Left > 0) return TaskbarPosition.Left;
            if (workArea.Right < screenWidth) return TaskbarPosition.Right;
            return TaskbarPosition.Bottom;
        }

    private void OpenPopout(Group? group, int taskbarIconX, int taskbarIconY, int taskbarIconWidth, int taskbarIconHeight, bool useIconPosition)
        {
            LogPosition($"OpenPopout called: group={group?.Name ?? "null"}, iconX={taskbarIconX}, iconY={taskbarIconY}, useIcon={useIconPosition}");

            // If already open for a different group, close first
            if (_isPopoutVisible && _popoutWindow != null)
            {
                // If re-clicking the same group's button just close the popout (toggle)
                if (_popoutWindow.GroupId == group?.Id)
                {
                    HidePopout();
                    return;
                }
                HidePopout();
            }

            try
            {
                _popoutWindow = group != null
                    ? new PopoutWindow(group)
                    : new PopoutWindow();

                _popoutWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                _popoutWindow.Closed += OnPopoutClosed;

                // Load fresh shortcut count from config (the passed-in group may be stale)
                int shortcutCount = GetFreshShortcutCount(group);

                // Set position before Show() for initial placement
                PositionWindow(_popoutWindow, taskbarIconX, taskbarIconY, taskbarIconWidth, taskbarIconHeight, useIconPosition, shortcutCount);
                _popoutWindow.Show();

                // Re-apply position after Show() — WPF can override Left/Top during Show()
                PositionWindow(_popoutWindow, taskbarIconX, taskbarIconY, taskbarIconWidth, taskbarIconHeight, useIconPosition, shortcutCount);
                _isPopoutVisible = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to open popout: {ex.Message}");
                _isPopoutVisible = false;
            }
        }

        /// <summary>
        /// Positions the popup window relative to the taskbar icon.
        /// taskbarIconX/Y are in WPF DIPs (already DPI-converted by TaskbarService).
        /// Window height adjusts based on shortcut count.
        /// </summary>
        private static void PositionWindow(PopoutWindow win, int taskbarIconX, int taskbarIconY, int taskbarIconWidth, int taskbarIconHeight, bool useIconPosition, int shortcutCount)
        {
            const double windowWidth = 310;
            const double margin = 8;
            const double edgeSpacing = 4;

            // Dynamic height based on shortcut count
            double windowHeight = CalculateWindowHeight(shortcutCount);

            var workArea = SystemParameters.WorkArea;
            double left, top;

            if (useIconPosition && taskbarIconX != 0 && taskbarIconY != 0)
            {
                var taskbarPos = GetTaskbarPosition();

                switch (taskbarPos)
                {
                    case TaskbarPosition.Bottom:
                        left = taskbarIconX - (windowWidth / 2);
                        top = taskbarIconY - windowHeight - edgeSpacing;
                        break;
                    case TaskbarPosition.Top:
                        left = taskbarIconX - (windowWidth / 2);
                        top = taskbarIconY + edgeSpacing;
                        break;
                    case TaskbarPosition.Left:
                        left = taskbarIconX + edgeSpacing;
                        top = taskbarIconY - (windowHeight / 2);
                        break;
                    case TaskbarPosition.Right:
                        left = taskbarIconX - windowWidth - edgeSpacing;
                        top = taskbarIconY - (windowHeight / 2);
                        break;
                    default:
                        left = taskbarIconX - (windowWidth / 2);
                        top = taskbarIconY - windowHeight - edgeSpacing;
                        break;
                }

                // Clamp horizontal position to screen bounds
                if (left < workArea.Left + margin)
                    left = workArea.Left + margin;
                else if (left + windowWidth > workArea.Right - margin)
                    left = workArea.Right - windowWidth - margin;
            }
            else
            {
                // Fallback: center in work area
                left = workArea.Left + (workArea.Width - windowWidth) / 2;
                top = workArea.Top + (workArea.Height - windowHeight) / 2;
            }

            // Clamp vertical position to screen bounds
            top = Math.Max(workArea.Top + margin, Math.Min(top, workArea.Bottom - windowHeight - margin));

            win.Left = left;
            win.Top = top;
            win.Width = windowWidth;
            win.Height = windowHeight;

            Debug.WriteLine($"[PopoutService] Popup at ({left}, {top}) size {windowWidth}x{windowHeight}");
            LogPosition($"Input: iconX={taskbarIconX}, iconY={taskbarIconY}, iconW={taskbarIconWidth}, iconH={taskbarIconHeight}, useIcon={useIconPosition} | Final: left={left:F1}, top={top:F1}, w={windowWidth}, h={windowHeight} | WorkArea: {workArea}");
        }

        /// <summary>
        /// Calculates the popup window height based on the number of shortcuts.
        /// Layout: header (36) + shortcut grid + padding. No search bar or footer.
        /// Each shortcut tile is 86px tall (78 + 8 margin), 98px wide (90 + 8 margin).
        /// 3 tiles per row in 310px wide window.
        /// </summary>
        private static double CalculateWindowHeight(int shortcutCount)
        {
            const double headerHeight = 36;
            const double gridPadding = 16;   // WrapPanel margins + border padding
            const double tileHeight = 86;    // 78 height + 4 margin top + 4 margin bottom
            const int tilesPerRow = 3;
            const double minContentHeight = 86;
            const double maxWindowHeight = 500;

            int rows = shortcutCount > 0
                ? (int)Math.Ceiling((double)shortcutCount / tilesPerRow)
                : 0;

            double contentHeight = Math.Max(rows * tileHeight, minContentHeight);
            double totalHeight = headerHeight + contentHeight + gridPadding;

            return Math.Min(totalHeight, maxWindowHeight);
        }

        /// <summary>
        /// Gets the current shortcut count for a group from the config service.
        /// The group object passed from TaskbarService may be stale (captured at creation time).
        /// </summary>
        private static int GetFreshShortcutCount(Group? group)
        {
            if (group == null) return 0;
            try
            {
                var configService = App.GetService<IConfigurationService>();
                var config = configService.LoadConfigAsync().GetAwaiter().GetResult();
                var freshGroup = config.Groups?.FirstOrDefault(g => g.Id == group.Id);
                return freshGroup?.Shortcuts?.Count ?? 0;
            }
            catch
            {
                return group.Shortcuts?.Count ?? 0;
            }
        }

        private static void LogPosition(string message)
        {
            try
            {
                var logPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "TaskDockr", "position_debug.log");
                File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss.fff}] {message}\n");
            }
            catch { }
        }

        private void OnPopoutClosed(object? sender, EventArgs e)
        {
            _popoutWindow    = null;
            _isPopoutVisible = false;
        }
    }
}
