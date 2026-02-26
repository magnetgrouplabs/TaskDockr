using System.Collections.Generic;

namespace TaskDockr.Services
{
    public class IconCatalogEntry
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string UnicodeCodePoint { get; set; } = string.Empty;
        public string FontFamily { get; set; } = string.Empty;
        public string SearchTerms { get; set; } = string.Empty;
    }

    public interface IIconCatalogService
    {
        IReadOnlyList<IconCatalogEntry> GetAllIcons();
        IReadOnlyList<IconCatalogEntry> SearchIcons(string query, string? category = null);
        IReadOnlyList<string> GetCategories();
    }
}
