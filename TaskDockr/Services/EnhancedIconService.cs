using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media;
using TaskDockr.Utils;

namespace TaskDockr.Services
{
    public class EnhancedIconService : IIconService
    {
        private readonly IIconService _baseIconService;
        private readonly IconBackgroundProcessor _backgroundProcessor;
        private readonly ConcurrentDictionary<string, Task<ImageSource>> _loadingTasks;

        public EnhancedIconService(IconService baseIconService)
        {
            _baseIconService     = baseIconService;
            _backgroundProcessor = new IconBackgroundProcessor();
            _loadingTasks        = new ConcurrentDictionary<string, Task<ImageSource>>();
        }

        public async Task<ImageSource> LoadIconAsync(string iconPath, int size = 32)
        {
            if (string.IsNullOrEmpty(iconPath))
                return await _baseIconService.GetDefaultIconAsync(IconType.Error, size);

            var cacheKey = $"{iconPath}_{size}";
            if (_loadingTasks.TryGetValue(cacheKey, out var loadingTask))
                return await loadingTask;

            var task = _backgroundProcessor.QueueIconProcessAsync(cacheKey,
                async _ => await _baseIconService.LoadIconAsync(iconPath, size));
            _loadingTasks[cacheKey] = task;
            try { return await task; }
            finally { _loadingTasks.TryRemove(cacheKey, out _); }
        }

        public Task<ImageSource> ExtractIconFromExecutableAsync(string executablePath, int size = 32)
            => _backgroundProcessor.QueueIconProcessAsync($"extract_{executablePath}_{size}",
                async _ => await _baseIconService.ExtractIconFromExecutableAsync(executablePath, size));

        public Task<ImageSource> LoadIconFromUrlAsync(string url, int size = 32)
            => _backgroundProcessor.QueueIconProcessAsync($"url_{url}_{size}",
                async _ => await _baseIconService.LoadIconFromUrlAsync(url, size));

        public Task<ImageSource> GetDefaultIconAsync(IconType iconType, int size = 32)
            => _baseIconService.GetDefaultIconAsync(iconType, size);

        public Task<bool> CacheIconAsync(string key, ImageSource icon)
            => _baseIconService.CacheIconAsync(key, icon);

        public Task<ImageSource?> GetCachedIconAsync(string key)
            => _baseIconService.GetCachedIconAsync(key);

        public Task<bool> ClearIconCacheAsync()     => _baseIconService.ClearIconCacheAsync();
        public Task<bool> RemoveCachedIconAsync(string key) => _baseIconService.RemoveCachedIconAsync(key);
        public Task<bool> IsValidIconSourceAsync(string source) => _baseIconService.IsValidIconSourceAsync(source);
        public Task<string> ResolveIconPathAsync(string source, IconSourceType sourceType)
            => _baseIconService.ResolveIconPathAsync(source, sourceType);
        public Task<ImageSource> ScaleIconAsync(ImageSource icon, int newSize)
            => _baseIconService.ScaleIconAsync(icon, newSize);
        public Task<List<ImageSource>> ExtractAllIconsFromExecutableAsync(string executablePath)
            => _baseIconService.ExtractAllIconsFromExecutableAsync(executablePath);
        public Task<int> GetIconCountInExecutableAsync(string executablePath)
            => _baseIconService.GetIconCountInExecutableAsync(executablePath);
        public int GetCacheSize()    => _baseIconService.GetCacheSize();
        public int GetCacheHitRate() => _baseIconService.GetCacheHitRate();
    }
}
