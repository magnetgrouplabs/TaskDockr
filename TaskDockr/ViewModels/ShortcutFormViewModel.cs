using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Win32;
using TaskDockr.Models;
using TaskDockr.Services;
using TaskDockr.Utils;

namespace TaskDockr.ViewModels
{
    public class ShortcutFormViewModel : ObservableObject
    {
        private readonly IShortcutService _shortcutService;
        private readonly IErrorHandlingService _errorHandlingService;
        private readonly Group _group;

        // Internal Shortcut object written to on Save
        private Shortcut _shortcut = new();
        public Shortcut Shortcut => _shortcut;

        private bool _isEditMode;
        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        // Two-way bound properties that trigger validation
        private string _shortcutName = string.Empty;
        public string ShortcutName
        {
            get => _shortcutName;
            set
            {
                if (SetProperty(ref _shortcutName, value))
                {
                    _shortcut.Name = value;
                    ValidateForm();
                }
            }
        }

        private string _targetPath = string.Empty;
        public string TargetPath
        {
            get => _targetPath;
            set
            {
                if (SetProperty(ref _targetPath, value))
                {
                    _shortcut.TargetPath = value;
                    AutoDetectType();
                    ValidateForm();
                }
            }
        }

        private string _arguments = string.Empty;
        public string Arguments
        {
            get => _arguments;
            set
            {
                if (SetProperty(ref _arguments, value))
                {
                    _shortcut.Arguments = value;
                    ValidateForm();
                }
            }
        }

        private string _iconPath = string.Empty;
        public string IconPath
        {
            get => _iconPath;
            set
            {
                if (SetProperty(ref _iconPath, value))
                {
                    _shortcut.IconPath = value;
                    ValidateForm();
                }
            }
        }

        private ShortcutType _selectedType;
        public ShortcutType SelectedType
        {
            get => _selectedType;
            set
            {
                if (SetProperty(ref _selectedType, value))
                {
                    _shortcut.Type = value;
                    ValidateForm();
                }
            }
        }

        private bool _isValid;
        public bool IsValid
        {
            get => _isValid;
            set => SetProperty(ref _isValid, value);
        }

        private string _validationError = string.Empty;
        public string ValidationError
        {
            get => _validationError;
            set => SetProperty(ref _validationError, value);
        }

        // Available shortcut types for the ComboBox
        public List<ShortcutTypeItem> ShortcutTypes { get; } = new()
        {
            new ShortcutTypeItem(ShortcutType.App, "Application"),
            new ShortcutTypeItem(ShortcutType.File, "File"),
            new ShortcutTypeItem(ShortcutType.URL, "URL"),
            new ShortcutTypeItem(ShortcutType.Folder, "Folder")
        };

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand BrowseTargetCommand { get; }
        public ICommand BrowseIconCommand { get; }

        public Action<bool>? CloseAction { get; set; }

        public ShortcutFormViewModel(
            IShortcutService shortcutService,
            IErrorHandlingService errorHandlingService,
            Group group,
            Shortcut? shortcut = null)
        {
            _shortcutService = shortcutService;
            _errorHandlingService = errorHandlingService;
            _group = group;

            if (shortcut != null)
            {
                _shortcut = shortcut;
                ShortcutName = shortcut.Name ?? string.Empty;
                TargetPath = shortcut.TargetPath ?? string.Empty;
                Arguments = shortcut.Arguments ?? string.Empty;
                IconPath = shortcut.IconPath ?? string.Empty;
                SelectedType = shortcut.Type;
                IsEditMode = true;
            }
            else
            {
                _shortcut = new Shortcut
                {
                    Name = string.Empty,
                    TargetPath = string.Empty,
                    Arguments = string.Empty,
                    IconPath = string.Empty,
                    Type = ShortcutType.App
                };
                SelectedType = ShortcutType.App;
            }

            SaveCommand = new RelayCommand(async () => await SaveAsync());
            CancelCommand = new RelayCommand(() => CloseAction?.Invoke(false));
            BrowseTargetCommand = new RelayCommand(BrowseTarget);
            BrowseIconCommand = new RelayCommand(BrowseIcon);
        }

        private void AutoDetectType()
        {
            if (string.IsNullOrWhiteSpace(TargetPath))
                return;

            // Auto-detect based on target path
            if (Uri.TryCreate(TargetPath, UriKind.Absolute, out var uriResult))
            {
                if (uriResult.Scheme == Uri.UriSchemeHttp ||
                    uriResult.Scheme == Uri.UriSchemeHttps ||
                    uriResult.Scheme == Uri.UriSchemeFtp ||
                    uriResult.Scheme == Uri.UriSchemeMailto)
                {
                    SelectedType = ShortcutType.URL;
                    return;
                }
            }

            // Check if it's a URL-like string without scheme
            if (TargetPath.Contains("://") ||
                TargetPath.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ||
                (!Path.HasExtension(TargetPath) && TargetPath.Contains(".")))
            {
                SelectedType = ShortcutType.URL;
                return;
            }

            // Check if it's a file
            if (File.Exists(TargetPath) || Path.HasExtension(TargetPath))
            {
                var ext = Path.GetExtension(TargetPath).ToLowerInvariant();
                if (ext == ".exe" || ext == ".com" || ext == ".bat" || ext == ".cmd" || ext == ".msi" || ext == ".ps1")
                {
                    SelectedType = ShortcutType.App;
                }
                else
                {
                    SelectedType = ShortcutType.File;
                }
                return;
            }

            // Check if it's a folder
            if (Directory.Exists(TargetPath))
            {
                SelectedType = ShortcutType.Folder;
                return;
            }
        }

        private async Task SaveAsync()
        {
            if (!IsValid) return;

            _shortcut.Name = ShortcutName.Trim();
            _shortcut.TargetPath = TargetPath.Trim();
            _shortcut.Arguments = Arguments?.Trim() ?? string.Empty;
            _shortcut.IconPath = IconPath?.Trim() ?? string.Empty;
            _shortcut.Type = SelectedType;

            try
            {
                if (IsEditMode)
                {
                    var updated = await _shortcutService.UpdateShortcutAsync(_shortcut);
                    if (updated)
                    {
                        CloseAction?.Invoke(true);
                    }
                    else
                    {
                        ValidationError = "Failed to update shortcut. Please check the details.";
                    }
                }
                else
                {
                    var result = await _shortcutService.CreateShortcutAsync(
                        _group.Id,
                        _shortcut.Name,
                        _shortcut.TargetPath,
                        _shortcut.Type,
                        _shortcut.Arguments,
                        _shortcut.IconPath);

                    if (result != null)
                    {
                        CloseAction?.Invoke(true);
                    }
                    else
                    {
                        ValidationError = "Failed to create shortcut. Please check the details.";
                    }
                }
            }
            catch (Exception ex)
            {
                ValidationError = $"Error: {ex.Message}";
            }
        }

        private void BrowseTarget()
        {
            switch (SelectedType)
            {
                case ShortcutType.App:
                    BrowseForApplication();
                    break;
                case ShortcutType.File:
                    BrowseForFile();
                    break;
                case ShortcutType.Folder:
                    BrowseForFolder();
                    break;
                case ShortcutType.URL:
                    // URLs don't typically need browsing
                    break;
            }
        }

        private void BrowseForApplication()
        {
            var dlg = new OpenFileDialog
            {
                Title = "Select Application",
                Filter = "Executable Files|*.exe;*.com;*.bat;*.cmd;*.msi;*.ps1|All Files|*.*",
                CheckFileExists = true,
                CheckPathExists = true
            };

            if (dlg.ShowDialog() == true)
            {
                TargetPath = dlg.FileName;
                // Auto-detect type after setting
                AutoDetectType();
            }
        }

        private void BrowseForFile()
        {
            var dlg = new OpenFileDialog
            {
                Title = "Select File",
                Filter = "All Files|*.*",
                CheckFileExists = true,
                CheckPathExists = true
            };

            if (dlg.ShowDialog() == true)
            {
                TargetPath = dlg.FileName;
            }
        }

        private void BrowseForFolder()
        {
            // Use OpenFileDialog to select a folder by using a file pattern that won't match any files
            // This is a workaround since WPF doesn't have a native folder browser dialog
            var dlg = new OpenFileDialog
            {
                Title = "Select Folder",
                FileName = "Folder Selection",
                Filter = "Folders|*.thisisnotarealextension",
                CheckFileExists = false,
                CheckPathExists = true
            };

            if (dlg.ShowDialog() == true)
            {
                // Get the directory from the selected path
                var directory = Path.GetDirectoryName(dlg.FileName);
                if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                {
                    TargetPath = directory;
                }
            }
        }

        private void BrowseIcon()
        {
            var dlg = new OpenFileDialog
            {
                Title = "Select Icon",
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.ico|All Files|*.*"
            };

            if (dlg.ShowDialog() == true)
                IconPath = dlg.FileName;
        }

        private void ValidateForm()
        {
            ValidationError = string.Empty;
            IsValid = !string.IsNullOrWhiteSpace(ShortcutName) && !string.IsNullOrWhiteSpace(TargetPath);
            if (!IsValid)
                ValidationError = string.IsNullOrWhiteSpace(ShortcutName) ? "Name is required." : "Target path is required.";
        }
    }

    public class ShortcutTypeItem
    {
        public ShortcutType Type { get; }
        public string DisplayName { get; }

        public ShortcutTypeItem(ShortcutType type, string displayName)
        {
            Type = type;
            DisplayName = displayName;
        }
    }
}
