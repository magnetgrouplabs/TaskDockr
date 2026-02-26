using TaskDockr.Models;
using System;
using System.Windows;

namespace TaskDockr.Services
{
    public interface IWindowManagerService
    {
        void ApplyWindowSettings(Window window, WindowSettings settings);
        WindowSettings GetWindowSettings(Window window);
        void CenterWindow(Window window);
        void EnsureWindowIsVisible(Window window);
    }

    public class WindowManagerService : IWindowManagerService
    {
        public void ApplyWindowSettings(Window window, WindowSettings settings)
        {
            if (window == null) return;

            if (settings.Width > 0 && settings.Height > 0)
            {
                window.Width  = Math.Max(settings.Width,  400);
                window.Height = Math.Max(settings.Height, 300);
            }

            if (settings.Left >= 0 && settings.Top >= 0)
            {
                var workArea = SystemParameters.WorkArea;
                window.Left = Math.Min(settings.Left, workArea.Width  - window.Width);
                window.Top  = Math.Min(settings.Top,  workArea.Height - window.Height);
                window.Left = Math.Max(window.Left, 0);
                window.Top  = Math.Max(window.Top,  0);
            }

            if (settings.IsMaximized)
                window.WindowState = WindowState.Maximized;
            else
                window.WindowState = WindowState.Normal;

            EnsureWindowIsVisible(window);
        }

        public WindowSettings GetWindowSettings(Window window)
        {
            if (window == null) return new WindowSettings();

            // Use RestoreBounds when maximized/minimized so we don't save garbage coordinates
            var bounds = window.WindowState == WindowState.Normal
                ? new System.Windows.Rect(window.Left, window.Top, window.Width, window.Height)
                : window.RestoreBounds;

            return new WindowSettings
            {
                Left        = double.IsNaN(bounds.Left)   || double.IsInfinity(bounds.Left)   ? -1 : bounds.Left,
                Top         = double.IsNaN(bounds.Top)    || double.IsInfinity(bounds.Top)    ? -1 : bounds.Top,
                Width       = double.IsNaN(bounds.Width)  || bounds.Width  <= 0 ? 1000 : bounds.Width,
                Height      = double.IsNaN(bounds.Height) || bounds.Height <= 0 ? 650  : bounds.Height,
                IsMaximized = window.WindowState == WindowState.Maximized,
                IsMinimized = false  // never restore to minimized
            };
        }

        public void CenterWindow(Window window)
        {
            if (window == null) return;
            var workArea = SystemParameters.WorkArea;
            window.Left = (workArea.Width  - window.Width)  / 2 + workArea.Left;
            window.Top  = (workArea.Height - window.Height) / 2 + workArea.Top;
        }

        public void EnsureWindowIsVisible(Window window)
        {
            if (window == null) return;
            var workArea = SystemParameters.WorkArea;

            bool outOfBounds =
                window.Left < workArea.Left ||
                window.Top  < workArea.Top  ||
                window.Left + window.Width  > workArea.Right ||
                window.Top  + window.Height > workArea.Bottom;

            if (outOfBounds)
                CenterWindow(window);
        }
    }
}
