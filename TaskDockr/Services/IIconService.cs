using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media;

namespace TaskDockr.Services
{
    public interface IIconService
    {
        Task<ImageSource> LoadIconAsync(string iconPath, int size = 32);
        Task<ImageSource> ExtractIconFromExecutableAsync(string executablePath, int size = 32);
        Task<ImageSource> LoadIconFromUrlAsync(string url, int size = 32);
        Task<ImageSource> GetDefaultIconAsync(IconType iconType, int size = 32);

        Task<bool> CacheIconAsync(string key, ImageSource icon);
        Task<ImageSource?> GetCachedIconAsync(string key);
        Task<bool> ClearIconCacheAsync();
        Task<bool> RemoveCachedIconAsync(string key);

        Task<bool> IsValidIconSourceAsync(string source);
        Task<string> ResolveIconPathAsync(string source, IconSourceType sourceType);
        Task<ImageSource> ScaleIconAsync(ImageSource icon, int newSize);

        Task<List<ImageSource>> ExtractAllIconsFromExecutableAsync(string executablePath);
        Task<int> GetIconCountInExecutableAsync(string executablePath);

        int GetCacheSize();
        int GetCacheHitRate();
    }

    public enum IconType
    {
        DefaultGroup,
        DefaultApp,
        DefaultFile,
        DefaultUrl,
        Error
    }

    public enum IconSourceType
    {
        FilePath,
        Url,
        Executable,
        EmbeddedResource,
        Default
    }
}
