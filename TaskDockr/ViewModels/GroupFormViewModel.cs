using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using TaskDockr.Models;
using TaskDockr.Services;
using TaskDockr.Utils;
using TaskDockr.Views;

namespace TaskDockr.ViewModels
{
    public class GroupFormViewModel : ObservableObject
    {
        private readonly IGroupService _groupService;

        // Internal Group object written to on Save
        private Group _group = new Group();
        public Group Group => _group;

        private bool _isEditMode;
        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        /// <summary>
        /// Two-way bound directly in XAML — this is what triggers ValidateForm() when the
        /// user types. Do NOT bind to Group.Name; Group is a plain POCO with no INPC.
        /// </summary>
        private string _groupName = string.Empty;
        public string GroupName
        {
            get => _groupName;
            set
            {
                if (SetProperty(ref _groupName, value))
                {
                    _group.Name = value;
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
                    OnPropertyChanged(nameof(IconDisplayText));
                    OnPropertyChanged(nameof(HasIconGlyph));
                    OnPropertyChanged(nameof(HasIconPath));
                    ValidateForm();
                }
            }
        }

        private string _iconGlyph = string.Empty;
        public string IconGlyph
        {
            get => _iconGlyph;
            set
            {
                if (SetProperty(ref _iconGlyph, value))
                {
                    OnPropertyChanged(nameof(IconDisplayText));
                    OnPropertyChanged(nameof(HasIconGlyph));
                    OnPropertyChanged(nameof(HasIconPath));
                    ValidateForm();
                }
            }
        }

        private string _iconColor = string.Empty;
        public string IconColor
        {
            get => _iconColor;
            set => SetProperty(ref _iconColor, value);
        }

        public bool HasIconGlyph => !string.IsNullOrEmpty(IconGlyph);
        public bool HasIconPath => !HasIconGlyph && !string.IsNullOrEmpty(IconPath);

        public string IconDisplayText
        {
            get
            {
                if (!string.IsNullOrEmpty(IconGlyph))
                {
                    // Show readable name from enum e.g. "Solid_Heart" -> "Heart (Solid)"
                    var parts = IconGlyph.Split('_', 2);
                    if (parts.Length == 2)
                        return $"{parts[1]} ({parts[0]})";
                    return IconGlyph;
                }
                if (!string.IsNullOrEmpty(IconPath))
                    return System.IO.Path.GetFileName(IconPath);
                return "No icon selected";
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

        public ObservableCollection<string> DefaultIcons { get; } = new();

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand BrowseIconCommand { get; }
        public ICommand SelectDefaultIconCommand { get; }
        public ICommand OpenIconPickerCommand { get; }

        /// <summary>Called by the hosting Window to close itself when Save/Cancel fires.</summary>
        public Action<bool>? CloseAction { get; set; }

        /// <summary>Set by the hosting Window so the icon picker can set Owner.</summary>
        public Window? OwnerWindow { get; set; }

        public GroupFormViewModel(IGroupService groupService, Group? group = null)
        {
            _groupService = groupService;

            if (group != null)
            {
                _group    = group;
                GroupName = group.Name ?? string.Empty;
                IconPath  = group.IconPath ?? string.Empty;
                _iconGlyph = group.IconGlyph ?? string.Empty;
                _iconColor = group.IconColor ?? string.Empty;
                IsEditMode = true;
            }
            else
            {
                _group = new Group { Name = string.Empty, IconPath = string.Empty, Position = 0 };
            }

            SaveCommand              = new RelayCommand(async () => await SaveAsync(), () => IsValid);
            CancelCommand            = new RelayCommand(() => CloseAction?.Invoke(false));
            BrowseIconCommand        = new RelayCommand(BrowseIcon);
            OpenIconPickerCommand    = new RelayCommand(OpenIconPicker);
            SelectDefaultIconCommand = new RelayCommand<string>(path =>
            {
                IconPath = path ?? string.Empty;
            });

            ValidateForm();
        }

private async Task SaveAsync()
{
    if (!IsValid) return;
    _group.Name = GroupName;
    _group.IconPath = IconPath;
    _group.IconGlyph = IconGlyph;
    _group.IconColor = IconColor;
    try
    {
        if (IsEditMode)
        {
            var updated = await _groupService.UpdateGroupAsync(_group);
            if (updated)
            {
                CloseAction?.Invoke(true);
            }
            else
            {
                ValidationError = "Failed to update group. Please check validation messages.";
            }
        }
        else
        {
            var result = await _groupService.CreateGroupAsync(_group.Name, _group.IconPath);
            if (result != null)
            {
                // Always update if we have glyph data — CreateGroupAsync creates a
                // new Group that doesn't have our glyph/color fields.
                if (!string.IsNullOrEmpty(IconGlyph) || !string.IsNullOrEmpty(IconColor))
                {
                    result.IconGlyph = IconGlyph;
                    result.IconColor = IconColor;
                    result.IconPath = IconPath; // Might have been cleared by picker
                    await _groupService.UpdateGroupAsync(result);
                }
                CloseAction?.Invoke(true);
            }
            else
            {
                ValidationError = "Failed to create group. Please check validation messages.";
            }
        }
    }
    catch (Exception ex)
    {
            ValidationError = $"Error: {ex.Message}";
        }
    }

        private void BrowseIcon()
        {
            var dlg = new OpenFileDialog
            {
                Title  = "Select Icon",
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.ico|All Files|*.*"
            };
            if (dlg.ShowDialog() == true)
            {
                IconPath = dlg.FileName;
                IconGlyph = string.Empty;
                IconColor = string.Empty;
            }
        }

        private void OpenIconPicker()
        {
            var dialog = new IconPickerDialog();
            if (OwnerWindow != null)
                dialog.Owner = OwnerWindow;

            if (dialog.ShowDialog() == true)
            {
                if (!string.IsNullOrEmpty(dialog.ResultIconGlyph))
                {
                    IconGlyph = dialog.ResultIconGlyph;
                    IconColor = dialog.ResultIconColor;
                    IconPath = string.Empty;
                }
                else if (!string.IsNullOrEmpty(dialog.ResultIconPath))
                {
                    IconPath = dialog.ResultIconPath;
                    IconGlyph = string.Empty;
                    IconColor = string.Empty;
                }
            }
        }

        private void ValidateForm()
        {
            IsValid         = false;
            ValidationError = string.Empty;

            if (string.IsNullOrWhiteSpace(GroupName))
            {
                ValidationError = "Group name is required.";
                return;
            }
            if (GroupName.Length > 50)
            {
                ValidationError = "Group name must be 50 characters or less.";
                return;
            }
            // Icon path validation only needed when there's no glyph and a path is specified
            if (string.IsNullOrEmpty(IconGlyph) && !string.IsNullOrEmpty(IconPath)
                && !File.Exists(IconPath) && !Uri.TryCreate(IconPath, UriKind.Absolute, out _))
            {
                ValidationError = "Invalid icon path.";
                return;
            }
            IsValid = true;
        }
    }
}
