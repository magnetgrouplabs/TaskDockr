using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TaskDockr.Models;

namespace TaskDockr.Converters
{
    public class TargetPathToIconConverter : IMultiValueConverter
    {
        private static readonly string[] ImageExts =
            { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tiff", ".webp" };

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (values.Length < 2) return DependencyProperty.UnsetValue;
                var path = values[0] as string;
                if (string.IsNullOrWhiteSpace(path)) return DependencyProperty.UnsetValue;

                var type = values[1] is ShortcutType t ? t : ShortcutType.App;

                // ── Image files: load directly as bitmap ──────────────────
                if (File.Exists(path))
                {
                    var ext = Path.GetExtension(path).ToLowerInvariant();
                    if (Array.IndexOf(ImageExts, ext) >= 0)
                    {
                        var bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.UriSource       = new Uri(path, UriKind.Absolute);
                        bmp.CacheOption     = BitmapCacheOption.OnLoad;
                        bmp.DecodePixelWidth = 32;
                        bmp.EndInit();
                        bmp.Freeze();
                        return bmp;
                    }
                }

                // ── URL: generic globe via shell icon ─────────────────────
                if (type == ShortcutType.URL)
                    return ExtractShellIcon(
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),
                                     "url.dll"), 0);

                // ── Folder ─────────────────────────────────────────────────
                if (type == ShortcutType.Folder || (!File.Exists(path) && Directory.Exists(path)))
                    return ExtractShellIcon(
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),
                                     "shell32.dll"), 3); // folder index

                // ── File / App: extract associated Windows icon ────────────
                if (File.Exists(path))
                {
                    var icon = Icon.ExtractAssociatedIcon(path);
                    if (icon != null)
                    {
                        var src = Imaging.CreateBitmapSourceFromHIcon(
                            icon.Handle, Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());
                        src.Freeze();
                        icon.Dispose();
                        return src;
                    }
                }

                return DependencyProperty.UnsetValue;
            }
            catch
            {
                return DependencyProperty.UnsetValue;
            }
        }

        private static BitmapSource? ExtractShellIcon(string dllPath, int index)
        {
            try
            {
                using var ico = Icon.ExtractAssociatedIcon(dllPath);
                if (ico == null) return null;
                var src = Imaging.CreateBitmapSourceFromHIcon(
                    ico.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                src.Freeze();
                return src;
            }
            catch { return null; }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
