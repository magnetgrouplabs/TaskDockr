using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using FontAwesome6;
using FontAwesome6.Fonts.Extensions;
using TaskDockr.Models;
using TaskDockr.Services;
using TaskDockr.Utils;

namespace TaskDockr.ViewModels
{
    public class PopoutViewModel : ObservableObject
    {
        /// <summary>null = show all shortcuts; non-null = show only this group's shortcuts</summary>
        public Group? Group { get; }

        public string Title => Group?.Name ?? "TaskDockr";

        /// <summary>Unicode character for the group's Font Awesome icon.</summary>
        public string GroupIconUnicode { get; }

        /// <summary>FontFamily for the group's Font Awesome icon.</summary>
        public FontFamily? GroupIconFontFamily { get; }

        /// <summary>Brush for the group icon background, derived from GroupIconColor.</summary>
        public Brush GroupIconColorBrush { get; }

        private ObservableCollection<Shortcut> _shortcuts = new();
        public ObservableCollection<Shortcut> Shortcuts
        {
            get => _shortcuts;
            set => SetProperty(ref _shortcuts, value);
        }

        private ObservableCollection<Shortcut> _filteredShortcuts = new();
        public ObservableCollection<Shortcut> FilteredShortcuts
        {
            get => _filteredShortcuts;
            set => SetProperty(ref _filteredShortcuts, value);
        }

        private readonly IConfigurationService _configService;
        private readonly IShortcutService _shortcutService;

        public ICommand LaunchShortcutCommand  { get; }
        public ICommand DeleteShortcutCommand  { get; }
        public ICommand EditShortcutCommand    { get; }

        /// <summary>Set by PopoutWindow to suppress close-on-deactivate while a dialog is open.</summary>
        public Action? SuppressClose { get; set; }
        public Action? RestoreClose  { get; set; }

        /// <summary>General popout — all shortcuts from all groups.</summary>
        public PopoutViewModel() : this(group: null) { }

        /// <summary>Group-specific popout — only the given group's shortcuts.</summary>
        public PopoutViewModel(Group? group)
        {
            Group = group;
            _configService   = App.GetService<IConfigurationService>();
            _shortcutService = App.GetService<IShortcutService>();

            // Initialize group icon properties for header display
            string unicode = "";
            FontFamily? fontFamily = null;
            Brush colorBrush = new SolidColorBrush(Color.FromRgb(0x60, 0x60, 0x60));

            if (group != null && !string.IsNullOrEmpty(group.IconGlyph) &&
                Enum.TryParse<EFontAwesomeIcon>(group.IconGlyph, out var faIcon))
            {
                try { unicode = faIcon.GetUnicode() ?? ""; } catch { }
                try { fontFamily = faIcon.GetFontFamily(); } catch { }
            }

            if (group != null && !string.IsNullOrEmpty(group.IconColor))
            {
                try
                {
                    var color = (Color)ColorConverter.ConvertFromString(group.IconColor);
                    colorBrush = new SolidColorBrush(color);
                    colorBrush.Freeze();
                }
                catch { }
            }

            GroupIconUnicode = unicode;
            GroupIconFontFamily = fontFamily;
            GroupIconColorBrush = colorBrush;

            LaunchShortcutCommand = new RelayCommand<Shortcut>(LaunchShortcut);
            DeleteShortcutCommand = new RelayCommand<Shortcut>(DeleteShortcut);
            EditShortcutCommand   = new RelayCommand<Shortcut>(EditShortcut);

            LoadShortcutsAsync();
        }

        private async void LoadShortcutsAsync()
        {
            var config = await _configService.LoadConfigAsync();
            var groups = config.Groups ?? new List<Group>();

            var source = Group != null
                ? groups.Where(g => g.Id == Group.Id)
                        .SelectMany(g => g.Shortcuts ?? Enumerable.Empty<Shortcut>())
                : groups.SelectMany(g => g.Shortcuts ?? Enumerable.Empty<Shortcut>());

            Shortcuts         = new ObservableCollection<Shortcut>(source);
            FilteredShortcuts = new ObservableCollection<Shortcut>(source);
        }

        public void RefreshData()  => LoadShortcutsAsync();

        private async void LaunchShortcut(Shortcut? shortcut)
        {
            if (shortcut != null)
                await _shortcutService.LaunchShortcutAsync(shortcut);
        }

        private async void DeleteShortcut(Shortcut? shortcut)
        {
            if (shortcut == null || Group == null) return;
            await _shortcutService.DeleteShortcutAsync(Group.Id, shortcut.Id);
            Shortcuts.Remove(shortcut);
            FilteredShortcuts.Remove(shortcut);
        }

        private void EditShortcut(Shortcut? shortcut)
        {
            if (shortcut == null || Group == null) return;
            SuppressClose?.Invoke();
            try
            {
                var form = new Views.ShortcutEditForm(Group, shortcut);
                form.ShowDialog();
                // Reload so the popup shows updated data
                LoadShortcutsAsync();
            }
            finally
            {
                RestoreClose?.Invoke();
            }
        }
    }
}
