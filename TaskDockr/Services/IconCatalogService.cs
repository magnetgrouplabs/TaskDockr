using System;
using System.Collections.Generic;
using System.Linq;
using FontAwesome6;
using FontAwesome6.Fonts.Extensions;

namespace TaskDockr.Services
{
    public class IconCatalogService : IIconCatalogService
    {
        private readonly List<IconCatalogEntry> _allIcons;
        private readonly List<string> _categories;

        public IconCatalogService()
        {
            _allIcons = new List<IconCatalogEntry>();
            var seen = new HashSet<string>();

            foreach (EFontAwesomeIcon icon in Enum.GetValues(typeof(EFontAwesomeIcon)))
            {
                if (icon == EFontAwesomeIcon.None)
                    continue;

                var name = icon.ToString();
                // Enum values are like Solid_Heart, Regular_Heart, Brands_Github
                var underscoreIndex = name.IndexOf('_');
                if (underscoreIndex < 0) continue;

                var style = name.Substring(0, underscoreIndex);
                var iconName = name.Substring(underscoreIndex + 1);

                // Convert PascalCase to readable: "ArrowRight" -> "Arrow Right"
                var displayName = System.Text.RegularExpressions.Regex.Replace(iconName, "(?<!^)([A-Z])", " $1");

                string unicode;
                try
                {
                    unicode = icon.GetUnicode();
                    if (string.IsNullOrEmpty(unicode)) continue;
                }
                catch
                {
                    continue;
                }

                // Use enum name as unique key to avoid duplicates
                if (!seen.Add(name)) continue;

                _allIcons.Add(new IconCatalogEntry
                {
                    Name = displayName,
                    Category = style,
                    UnicodeCodePoint = name, // Store enum name for serialization
                    FontFamily = style,
                    SearchTerms = displayName.ToLowerInvariant()
                });
            }

            _categories = _allIcons.Select(i => i.Category).Distinct().OrderBy(c => c).ToList();
        }

        public IReadOnlyList<IconCatalogEntry> GetAllIcons() => _allIcons;

        public IReadOnlyList<IconCatalogEntry> SearchIcons(string query, string? category = null)
        {
            var results = (IEnumerable<IconCatalogEntry>)_allIcons;

            if (!string.IsNullOrEmpty(category) && category != "All")
            {
                results = results.Where(i => i.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(query))
            {
                var lowerQuery = query.ToLowerInvariant();
                results = results.Where(i => i.SearchTerms.Contains(lowerQuery));
            }

            return results.ToList();
        }

        public IReadOnlyList<string> GetCategories() => _categories;
    }
}
