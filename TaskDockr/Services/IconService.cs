using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TaskDockr.Services
{
    public class IconService : IIconService
    {
        private readonly IConfigurationService _configService;
        private readonly ConcurrentDictionary<string, CachedIcon> _iconCache;
        private readonly HttpClient _httpClient;
        private readonly List<string> _supportedImageExtensions;
        private readonly List<string> _executableExtensions;
        private int _cacheHits;
        private int _cacheMisses;

        public IconService(IConfigurationService configService)
        {
            _configService = configService;
            _iconCache = new ConcurrentDictionary<string, CachedIcon>();
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            _supportedImageExtensions = new List<string> { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".ico", ".svg" };
            _executableExtensions = new List<string> { ".exe", ".dll", ".scr", ".cpl", ".ocx" };
        }

        public async Task<ImageSource> LoadIconAsync(string iconPath, int size = 32)
        {
            if (string.IsNullOrEmpty(iconPath))
                return await GetDefaultIconAsync(IconType.Error, size);

            var cacheKey = GenerateCacheKey(iconPath, size);
            if (_iconCache.TryGetValue(cacheKey, out var cachedIcon) && !IsCacheExpired(cachedIcon))
            { _cacheHits++; return cachedIcon.Icon; }
            _cacheMisses++;

            try
            {
                ImageSource? icon = null;
                if (File.Exists(iconPath))
                {
                    var extension = Path.GetExtension(iconPath).ToLowerInvariant();
                    if (_executableExtensions.Contains(extension))
                        icon = await ExtractIconFromExecutableAsync(iconPath, size);
                    else if (_supportedImageExtensions.Contains(extension))
                        icon = await LoadImageFileAsync(iconPath, size);
                }
                else if (Uri.TryCreate(iconPath, UriKind.Absolute, out var uri))
                {
                    if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                        icon = await LoadIconFromUrlAsync(iconPath, size);
                }
                icon ??= await GetDefaultIconAsync(IconType.Error, size);
                await CacheIconAsync(cacheKey, icon);
                return icon;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to load icon: " + ex.Message);
                return await GetDefaultIconAsync(IconType.Error, size);
            }
        }

        public Task<ImageSource> ExtractIconFromExecutableAsync(string executablePath, int size = 32)
        {
            try
            {
                if (!string.IsNullOrEmpty(executablePath) && File.Exists(executablePath))
                {
                    var icon = System.Drawing.Icon.ExtractAssociatedIcon(executablePath);
                    if (icon != null)
                    {
                        var bitmap = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                            icon.Handle,
                            System.Windows.Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());
                        icon.Dispose();
                        bitmap.Freeze();
                        return Task.FromResult<ImageSource>(bitmap);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Icon extraction failed: " + ex.Message);
            }
            return GetDefaultIconAsync(IconType.DefaultApp, size);
        }

        public async Task<ImageSource> LoadIconFromUrlAsync(string url, int size = 32)
        {
            if (string.IsNullOrEmpty(url))
                return await GetDefaultIconAsync(IconType.Error, size);
            try
            {
                var bytes = await _httpClient.GetByteArrayAsync(url);
                using var ms = new MemoryStream(bytes);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = ms;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to load icon from URL: " + ex.Message);
                return await GetDefaultIconAsync(IconType.DefaultUrl, size);
            }
        }

        public Task<ImageSource> GetDefaultIconAsync(IconType iconType, int size = 32)
        {
            var cacheKey = "default_" + iconType + "_" + size;
            if (_iconCache.TryGetValue(cacheKey, out var cachedIcon))
                return Task.FromResult(cachedIcon.Icon);

            // Return a solid-color DrawingImage as a placeholder
            var color = iconType switch
            {
                IconType.DefaultGroup => Colors.CornflowerBlue,
                IconType.DefaultApp   => Colors.SlateBlue,
                IconType.DefaultFile  => Colors.SteelBlue,
                IconType.DefaultUrl   => Colors.MediumSeaGreen,
                _                     => Colors.Gray
            };
            var drawing = new GeometryDrawing(new SolidColorBrush(color), null,
                new RectangleGeometry(new System.Windows.Rect(0, 0, size, size)));
            var drawingImage = new DrawingImage(drawing);
            drawingImage.Freeze();
            _iconCache[cacheKey] = new CachedIcon { Icon = drawingImage, LastAccessed = DateTime.Now, Size = 10 };
            return Task.FromResult<ImageSource>(drawingImage);
        }

        public Task<bool> CacheIconAsync(string key, ImageSource icon)
        {
            try
            {
                var cachedIcon = new CachedIcon { Icon = icon, LastAccessed = DateTime.Now, Size = 10 };
                _iconCache.AddOrUpdate(key, cachedIcon, (k, v) => cachedIcon);
                CleanupCache();
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to cache icon: " + ex.Message);
                return Task.FromResult(false);
            }
        }

        public Task<ImageSource?> GetCachedIconAsync(string key)
        {
            if (_iconCache.TryGetValue(key, out var cachedIcon))
            { cachedIcon.LastAccessed = DateTime.Now; _cacheHits++; return Task.FromResult<ImageSource?>(cachedIcon.Icon); }
            _cacheMisses++;
            return Task.FromResult<ImageSource?>(null);
        }

        public Task<bool> ClearIconCacheAsync()
        {
            try { _iconCache.Clear(); _cacheHits = 0; _cacheMisses = 0; return Task.FromResult(true); }
            catch (Exception ex) { Debug.WriteLine("Failed to clear icon cache: " + ex.Message); return Task.FromResult(false); }
        }

        public Task<bool> RemoveCachedIconAsync(string key) => Task.FromResult(_iconCache.TryRemove(key, out _));

        public Task<bool> IsValidIconSourceAsync(string source)
        {
            if (string.IsNullOrEmpty(source)) return Task.FromResult(false);
            if (File.Exists(source))
            {
                var extension = Path.GetExtension(source).ToLowerInvariant();
                return Task.FromResult(_supportedImageExtensions.Contains(extension) || _executableExtensions.Contains(extension));
            }
            if (Uri.TryCreate(source, UriKind.Absolute, out var uri))
                return Task.FromResult(uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
            return Task.FromResult(false);
        }

        public Task<string> ResolveIconPathAsync(string source, IconSourceType sourceType)
        {
            if (string.IsNullOrEmpty(source)) return Task.FromResult(string.Empty);
            return Task.FromResult(sourceType switch
            {
                IconSourceType.FilePath => File.Exists(source) ? source : string.Empty,
                IconSourceType.Url => Uri.TryCreate(source, UriKind.Absolute, out var uri) &&
                    (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) ? source : string.Empty,
                IconSourceType.Executable => File.Exists(source) && _executableExtensions.Contains(
                    Path.GetExtension(source).ToLowerInvariant()) ? source : string.Empty,
                _ => string.Empty
            });
        }

        public Task<ImageSource> ScaleIconAsync(ImageSource icon, int newSize) => Task.FromResult(icon);

        public Task<List<ImageSource>> ExtractAllIconsFromExecutableAsync(string executablePath)
            => Task.FromResult(new List<ImageSource>());

        public Task<int> GetIconCountInExecutableAsync(string executablePath)
            => Task.FromResult(0);

        public int GetCacheSize() => _iconCache.Count;

        public int GetCacheHitRate()
        {
            var totalRequests = _cacheHits + _cacheMisses;
            return totalRequests > 0 ? (int)((double)_cacheHits / totalRequests * 100) : 0;
        }

        private Task<ImageSource> LoadImageFileAsync(string filePath, int size)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            return Task.FromResult<ImageSource>(bitmap);
        }

        private string GenerateCacheKey(string source, int size) => source + "_" + size;

        private bool IsCacheExpired(CachedIcon cachedIcon)
            => (DateTime.Now - cachedIcon.LastAccessed).TotalHours > 24;

        private void CleanupCache()
        {
            const int maxCacheSize = 100;
            if (_iconCache.Count <= maxCacheSize) return;
            var itemsToRemove = _iconCache.OrderBy(kvp => kvp.Value.LastAccessed)
                .Take(_iconCache.Count - maxCacheSize / 2).ToList();
            foreach (var item in itemsToRemove) _iconCache.TryRemove(item.Key, out _);
        }
    }

    internal class CachedIcon
    {
        public ImageSource Icon { get; set; } = null!;
        public DateTime LastAccessed { get; set; }
        public long Size { get; set; }
    }
}
