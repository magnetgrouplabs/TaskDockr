using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Windows.Input;
using System.Windows.Threading;
using TaskDockr.Services;
using TaskDockr.Utils;

namespace TaskDockr.ViewModels
{
    public class IconPickerViewModel : ObservableObject
    {
        private readonly IIconCatalogService _catalogService;
        private readonly DispatcherTimer _debounceTimer;

        public ObservableCollection<IconCatalogEntry> DisplayedIcons { get; } = new();
        public ObservableCollection<string> Categories { get; } = new();
        public ObservableCollection<string> PresetColors { get; } = new()
        {
            "#FFFFFF", "#FF4444", "#FF8C00", "#FFD700",
            "#44BB44", "#4488FF", "#8844CC", "#FF69B4"
        };

        private string _searchQuery = string.Empty;
        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (SetProperty(ref _searchQuery, value))
                {
                    _debounceTimer.Stop();
                    _debounceTimer.Start();
                }
            }
        }

        private string _selectedCategory = "All";
        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                {
                    _isUseMyOwn = value == "Use My Own";
                    OnPropertyChanged(nameof(IsUseMyOwn));
                    OnPropertyChanged(nameof(IsIconGrid));
                    if (!_isUseMyOwn)
                        FilterIcons();
                }
            }
        }

        private IconCatalogEntry? _selectedIcon;
        public IconCatalogEntry? SelectedIcon
        {
            get => _selectedIcon;
            set
            {
                if (SetProperty(ref _selectedIcon, value))
                    OnPropertyChanged(nameof(HasSelection));
            }
        }

        private string _selectedColor = "#FFFFFF";
        public string SelectedColor
        {
            get => _selectedColor;
            set => SetProperty(ref _selectedColor, value);
        }

        private bool _isUseMyOwn;
        public bool IsUseMyOwn => _isUseMyOwn;
        public bool IsIconGrid => !_isUseMyOwn;

        private string _customIconPath = string.Empty;
        public string CustomIconPath
        {
            get => _customIconPath;
            set
            {
                if (SetProperty(ref _customIconPath, value))
                    OnPropertyChanged(nameof(HasSelection));
            }
        }

        public bool HasSelection => SelectedIcon != null || (!string.IsNullOrEmpty(CustomIconPath) && IsUseMyOwn);

        public Action<bool>? CloseAction { get; set; }

        // Results passed back to the caller
        public string ResultIconGlyph { get; private set; } = string.Empty;
        public string ResultIconColor { get; private set; } = string.Empty;
        public string ResultIconPath { get; private set; } = string.Empty;

        public ICommand SelectCategoryCommand { get; }
        public ICommand SelectIconCommand { get; }
        public ICommand SelectColorCommand { get; }
        public ICommand ConfirmCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand BrowseFileCommand { get; }

        public IconPickerViewModel(IIconCatalogService catalogService)
        {
            _catalogService = catalogService;

            _debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            _debounceTimer.Tick += (s, e) =>
            {
                _debounceTimer.Stop();
                FilterIcons();
            };

            SelectCategoryCommand = new RelayCommand<string>(cat => SelectedCategory = cat ?? "All");
            SelectIconCommand = new RelayCommand<IconCatalogEntry>(icon => SelectedIcon = icon);
            SelectColorCommand = new RelayCommand<string>(color =>
            {
                if (!string.IsNullOrEmpty(color))
                    SelectedColor = color;
            });
            ConfirmCommand = new RelayCommand(Confirm, () => HasSelection);
            CancelCommand = new RelayCommand(() => CloseAction?.Invoke(false));
            BrowseFileCommand = new RelayCommand(BrowseFile);

            // Load categories
            Categories.Add("All");
            foreach (var cat in _catalogService.GetCategories())
                Categories.Add(cat);
            Categories.Add("Use My Own");

            // Load initial icons
            FilterIcons();
        }

        private void FilterIcons()
        {
            var category = _selectedCategory == "All" ? null : _selectedCategory;
            var results = _catalogService.SearchIcons(_searchQuery, category);

            DisplayedIcons.Clear();
            // Limit to 500 for performance in the UI
            foreach (var icon in results.Take(500))
                DisplayedIcons.Add(icon);
        }

        private void Confirm()
        {
            if (IsUseMyOwn && !string.IsNullOrEmpty(CustomIconPath))
            {
                ResultIconPath = CustomIconPath;
                ResultIconGlyph = string.Empty;
                ResultIconColor = string.Empty;
            }
            else if (SelectedIcon != null)
            {
                ResultIconGlyph = SelectedIcon.UnicodeCodePoint; // enum name e.g. "Solid_Heart"
                ResultIconColor = SelectedColor;
                ResultIconPath = string.Empty;
            }

            CloseAction?.Invoke(true);
        }

        private void BrowseFile()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select Icon",
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.ico|All Files|*.*"
            };
            if (dlg.ShowDialog() == true)
                CustomIconPath = dlg.FileName;
        }
    }
}
