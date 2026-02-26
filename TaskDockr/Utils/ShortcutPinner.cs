using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FontAwesome6;
using FontAwesome6.Fonts.Extensions;

namespace TaskDockr.Utils
{
    public static class ShortcutPinner
    {
        /// <summary>
        /// Creates a .lnk on the Desktop for the given group and returns the path.
        /// </summary>
        // Icons are stored here permanently so .lnk files can always find them
        private static readonly string IconCacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TaskDockr", "icons");

        public static string CreateDesktopShortcut(
            string groupId, string groupName, string? groupIconPath,
            string? iconGlyph = null, string? iconColor = null)
        {
            var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName;
            var lnkName = SanitizeName(groupName) + ".lnk";
            var lnkPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop), lnkName);

            // Resolve the icon source — must be a path that exists permanently
            string iconSrc = exePath; // default: use exe's own icon

            // Priority 1: Font Awesome glyph → render to ICO
            if (!string.IsNullOrEmpty(iconGlyph) &&
                Enum.TryParse<EFontAwesomeIcon>(iconGlyph, out var faIcon))
            {
                var rendered = RenderGlyphToIco(faIcon, iconColor, groupId);
                if (rendered != null) iconSrc = rendered;
            }
            // Priority 2: file-based icon
            else if (!string.IsNullOrEmpty(groupIconPath) && File.Exists(groupIconPath))
            {
                var ext = Path.GetExtension(groupIconPath).ToLowerInvariant();
                if (ext == ".ico")
                {
                    iconSrc = groupIconPath;
                }
                else if (ext is ".png" or ".jpg" or ".jpeg" or ".bmp")
                {
                    // Convert to ICO and save permanently so the .lnk path stays valid
                    var converted = ConvertImageToIco(groupIconPath, groupId);
                    if (converted != null) iconSrc = converted;
                }
            }

            var link = (IShellLink)new ShellLinkCoClass();
            link.SetPath(exePath);
            link.SetArguments($"--group {groupId}");
            link.SetDescription($"TaskDockr — {groupName}");
            link.SetWorkingDirectory(Path.GetDirectoryName(exePath)!);
            link.SetIconLocation(iconSrc, 0);
            ((IPersistFile)link).Save(lnkPath, false);
            Marshal.ReleaseComObject(link);
            return lnkPath;
        }

        /// <summary>
        /// Converts a PNG/JPG/BMP to a proper ICO file by embedding the PNG bytes directly
        /// inside the ICO container. This preserves exact colors (no R/B channel swap).
        /// </summary>
        private static string? ConvertImageToIco(string imagePath, string groupId)
        {
            try
            {
                Directory.CreateDirectory(IconCacheDir);
                var icoPath = Path.Combine(IconCacheDir, $"{groupId}.ico");

                // Get raw PNG bytes (colors are preserved — no GDI conversion)
                using var bmp     = new Bitmap(imagePath);
                using var resized = new Bitmap(bmp, new System.Drawing.Size(256, 256));
                using var pngMs   = new MemoryStream();
                resized.Save(pngMs, System.Drawing.Imaging.ImageFormat.Png);
                var png = pngMs.ToArray();

                // Write an ICO file that wraps the PNG directly (Vista+ format)
                // Header: 6 bytes  |  Directory entry: 16 bytes  |  PNG data
                using var fs = File.Create(icoPath);
                using var w  = new BinaryWriter(fs);

                // ICONDIR
                w.Write((ushort)0);          // reserved
                w.Write((ushort)1);          // type = 1 (ICO)
                w.Write((ushort)1);          // count = 1 image

                // ICONDIRENTRY
                w.Write((byte)0);            // width:  0 means 256
                w.Write((byte)0);            // height: 0 means 256
                w.Write((byte)0);            // color count
                w.Write((byte)0);            // reserved
                w.Write((ushort)1);          // planes
                w.Write((ushort)32);         // bits per pixel
                w.Write((uint)png.Length);   // size of image data
                w.Write((uint)22);           // offset of image data (6 + 16 = 22)

                // PNG image data (exact bytes, colors intact)
                w.Write(png);

                return icoPath;
            }
            catch { return null; }
        }

        /// <summary>
        /// Renders a Font Awesome glyph to a permanent ICO file for use in .lnk shortcuts.
        /// </summary>
        private static string? RenderGlyphToIco(EFontAwesomeIcon icon, string? colorHex, string groupId)
        {
            try
            {
                Directory.CreateDirectory(IconCacheDir);
                var icoPath = Path.Combine(IconCacheDir, $"{groupId}.ico");

                System.Windows.Media.Brush brush = System.Windows.Media.Brushes.White;
                if (!string.IsNullOrEmpty(colorHex))
                {
                    try
                    {
                        var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorHex);
                        brush = new SolidColorBrush(color);
                        ((SolidColorBrush)brush).Freeze();
                    }
                    catch { }
                }

                const int bitmapSize = 256;
                const double emSize = 192;

                // Render the glyph using FormattedText
                var unicode = icon.GetUnicode();
                var fontFamily = icon.GetFontFamily();
                if (string.IsNullOrEmpty(unicode) || fontFamily == null) return null;

                var typeface = new Typeface(fontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
                var formattedText = new FormattedText(
                    unicode, System.Globalization.CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight, typeface, emSize, brush, 1.0);

                double offsetX = (bitmapSize - formattedText.Width) / 2;
                double offsetY = (bitmapSize - formattedText.Height) / 2;

                var visual = new DrawingVisual();
                using (var ctx = visual.RenderOpen())
                {
                    ctx.DrawText(formattedText, new System.Windows.Point(offsetX, offsetY));
                }

                var rtb = new RenderTargetBitmap(bitmapSize, bitmapSize, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(visual);

                // Encode as PNG
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(rtb));
                byte[] png;
                using (var ms = new MemoryStream())
                {
                    encoder.Save(ms);
                    png = ms.ToArray();
                }

                // Write ICO container wrapping the PNG
                using var fs = File.Create(icoPath);
                using var w = new BinaryWriter(fs);
                w.Write((ushort)0);          // reserved
                w.Write((ushort)1);          // type = ICO
                w.Write((ushort)1);          // 1 image
                w.Write((byte)0);            // width 256
                w.Write((byte)0);            // height 256
                w.Write((byte)0);            // color count
                w.Write((byte)0);            // reserved
                w.Write((ushort)1);          // planes
                w.Write((ushort)32);         // bpp
                w.Write((uint)png.Length);    // image data size
                w.Write((uint)22);           // offset (6 + 16)
                w.Write(png);

                return icoPath;
            }
            catch { return null; }
        }

        private static string SanitizeName(string name)
            => string.Concat(name.Split(Path.GetInvalidFileNameChars()));

        [ComImport, Guid("00021401-0000-0000-C000-000000000046")]
        class ShellLinkCoClass { }

        [ComImport, Guid("000214F9-0000-0000-C000-000000000046"),
         InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IShellLink
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder f, int c, IntPtr p, int g);
            void GetIDList(out IntPtr p); void SetIDList(IntPtr p);
            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder n, int c);
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string n);
            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder d, int c);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string d);
            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder a, int c);
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string a);
            void GetHotkey(out short h); void SetHotkey(short h);
            void GetShowCmd(out int s); void SetShowCmd(int s);
            void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder p, int c, out int i);
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string p, int i);
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string p, int r);
            void Resolve(IntPtr h, int f);
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string p);
        }

        [ComImport, Guid("0000010B-0000-0000-C000-000000000046"),
         InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IPersistFile
        {
            void GetClassID(out Guid g); [PreserveSig] int IsDirty();
            void Load([MarshalAs(UnmanagedType.LPWStr)] string f, int m);
            void Save([MarshalAs(UnmanagedType.LPWStr)] string f, [MarshalAs(UnmanagedType.Bool)] bool r);
            void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string f);
            void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string f);
        }
    }
}
