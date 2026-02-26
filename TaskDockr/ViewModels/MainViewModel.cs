using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TaskDockr.Models;
using TaskDockr.Services;
using TaskDockr.Utils;

namespace TaskDockr.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private ObservableCollection<Group> _groups = new();
        public ObservableCollection<Group> Groups
        {
            get => _groups;
            set => SetProperty(ref _groups, value);
        }

        private Group? _selectedGroup;
        public Group? SelectedGroup
        {
            get => _selectedGroup;
            set => SetProperty(ref _selectedGroup, value);
        }

        public int TotalShortcuts => Groups?.Sum(g => g.Shortcuts?.Count ?? 0) ?? 0;

    private readonly IConfigurationService _configService;
    private readonly IGroupService _groupService;
    private readonly IShortcutService _shortcutService;
    private readonly DragDropService _dragDropService;
    private readonly IErrorHandlingService _errorHandlingService;
    private readonly ITaskbarService _taskbarService;

        public ICommand AddGroupCommand { get; }
        public ICommand EditGroupCommand { get; }
        public ICommand DeleteGroupCommand { get; }
        public ICommand PinGroupCommand { get; }
        public ICommand AddShortcutCommand { get; }
        public ICommand EditShortcutCommand { get; }
        public ICommand DeleteShortcutCommand { get; }
        public ICommand LaunchShortcutCommand { get; }
        public ICommand ReorderGroupsCommand { get; }
        public ICommand ReorderShortcutsCommand { get; }

    public MainViewModel(
        IConfigurationService configService,
        IGroupService groupService,
        IShortcutService shortcutService,
        DragDropService dragDropService,
        IErrorHandlingService errorHandlingService,
        ITaskbarService taskbarService)
    {
        _configService = configService;
        _groupService = groupService;
        _shortcutService = shortcutService;
        _dragDropService = dragDropService;
        _errorHandlingService = errorHandlingService;
        _taskbarService = taskbarService;

            AddGroupCommand    = new RelayCommand(AddGroup);
            EditGroupCommand   = new RelayCommand<Group>(EditGroup);
            DeleteGroupCommand = new RelayCommand<Group>(DeleteGroup);
            PinGroupCommand    = new RelayCommand<Group>(PinGroup);
            AddShortcutCommand = new RelayCommand<Group>(AddShortcut);
            EditShortcutCommand = new RelayCommand<Shortcut>(EditShortcut);
            DeleteShortcutCommand = new RelayCommand<Shortcut>(DeleteShortcut);
            LaunchShortcutCommand = new RelayCommand<Shortcut>(LaunchShortcut);
            ReorderGroupsCommand = new RelayCommand<ReorderGroupsEventArgs>(ReorderGroups);
            ReorderShortcutsCommand = new RelayCommand<ReorderShortcutsEventArgs>(ReorderShortcuts);

            _dragDropService.DragStarted  += OnDragStarted;
            _dragDropService.DragCompleted += OnDragCompleted;
            _dragDropService.DropCompleted += OnDropCompleted;

            LoadConfigurationAsync();
            LoadGroupsAsync();
        }

        private async void LoadConfigurationAsync()
        {
            try
            {
                var config = await _configService.LoadConfigAsync();
                // Theme applied in App.xaml.cs — no per-VM action needed
            }
            catch (Exception ex)
            {
                await _errorHandlingService.HandleErrorAsync(ex, "Failed to load configuration.");
            }
        }

        internal async Task LoadGroupsAsync()
        {
            try
            {
                var config = await _configService.LoadConfigAsync();
                var groups = config.Groups ?? new List<Group>();
                Groups = new ObservableCollection<Group>(groups);

            if (_selectedGroup == null && Groups.Count > 0)
                SelectedGroup = Groups[0];

            // Pin every group to the taskbar
            foreach (var group in Groups)
                await _taskbarService.CreateGroupIconAsync(group);
        }
        catch (Exception ex)
        {
            await _errorHandlingService.HandleErrorAsync(ex, "Failed to load groups.");
        }
    }

        public async Task SaveWindowSettingsAsync(double left, double top, double width, double height, bool isMaximized, bool isMinimized)
        {
            try
            {
                var config = _configService.CurrentConfig;
                config.WindowSettings.Left        = left;
                config.WindowSettings.Top         = top;
                config.WindowSettings.Width       = width;
                config.WindowSettings.Height      = height;
                config.WindowSettings.IsMaximized = isMaximized;
                config.WindowSettings.IsMinimized = isMinimized;
                await _configService.SaveConfigAsync(config);
            }
            catch (Exception ex)
            {
                await _errorHandlingService.HandleErrorAsync(ex, "Failed to save window settings.");
            }
        }



        private async void AddGroup()
        {
            try
            {
                var form = new Views.GroupCreationForm();
                if (form.ShowDialog() == true)
                {
                    await LoadGroupsAsync();
                    // New group is already pinned by LoadGroupsAsync → CreateGroupIconAsync
                }
            }
            catch (Exception ex)
            {
                await _errorHandlingService.HandleErrorAsync(ex, "Failed to add group.");
            }
        }

        private async void EditGroup(Group? group)
        {
            if (group == null) return;
            try
            {
                var form = new Views.GroupEditForm(group);
                if (form.ShowDialog() == true)
                {
                    await _taskbarService.UpdateGroupIconAsync(group);
                    await LoadGroupsAsync();
                }
            }
            catch (Exception ex)
            {
                await _errorHandlingService.HandleErrorAsync(ex, $"Failed to edit group '{group.Name}'.");
            }
        }

        private async void DeleteGroup(Group? group)
        {
            if (group == null) return;
            try
            {
                var result = MessageBox.Show(
                    $"Delete group '{group.Name}' and all its shortcuts?",
                    "Delete Group",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    await _taskbarService.RemoveGroupIconAsync(group.Id);
                    await _groupService.DeleteGroupAsync(group.Id);
                    Groups.Remove(group);
                    if (SelectedGroup == group)
                        SelectedGroup = Groups.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                await _errorHandlingService.HandleErrorAsync(ex, $"Failed to delete group '{group.Name}'.");
            }
        }

        private void PinGroup(Group? group)
        {
            if (group == null) return;
            try
            {
                var lnkPath = ShortcutPinner.CreateDesktopShortcut(
                    group.Id, group.Name, group.IconPath,
                    group.IconGlyph, group.IconColor);

                // Open Desktop so the shortcut is visible
                System.Diagnostics.Process.Start("explorer.exe",
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop));

                MessageBox.Show(
                    $"Shortcut \"{Path.GetFileName(lnkPath)}\" saved to your Desktop.\n\n" +
                    "To add it to the taskbar:\n" +
                    "  1. Right-click the shortcut on your Desktop\n" +
                    "  2. Choose \"Pin to taskbar\"\n" +
                    "  3. You can then delete the Desktop shortcut\n\n" +
                    "Once pinned, clicking the taskbar icon opens the group popup.",
                    "Pin to Taskbar", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not create shortcut:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void AddShortcut(Group? group)
        {
            if (group == null) return;
            try
            {
                var form = new Views.ShortcutCreationForm(group);
                if (form.ShowDialog() == true)
                {
                    await LoadGroupsAsync();
                }
            }
            catch (Exception ex)
            {
                await _errorHandlingService.HandleErrorAsync(ex, $"Failed to add shortcut to '{group.Name}'.");
            }
        }

        private async void EditShortcut(Shortcut? shortcut)
        {
            if (shortcut == null) return;
            try
            {
                // Find the group containing this shortcut
                var group = Groups.FirstOrDefault(g => g.Shortcuts?.Any(s => s.Id == shortcut.Id) == true);
                if (group == null)
                {
                    await _errorHandlingService.ShowErrorMessageAsync("Error", "Could not find the group containing this shortcut.");
                    return;
                }

                var form = new Views.ShortcutEditForm(group, shortcut);
                if (form.ShowDialog() == true)
                {
                    await LoadGroupsAsync();
                }
            }
            catch (Exception ex)
            {
                await _errorHandlingService.HandleErrorAsync(ex, $"Failed to edit shortcut '{shortcut.Name}'.");
            }
        }

        private async void DeleteShortcut(Shortcut? shortcut)
        {
            if (shortcut == null) return;
            try
            {
                // Find the group containing this shortcut
                var group = Groups.FirstOrDefault(g => g.Shortcuts?.Any(s => s.Id == shortcut.Id) == true);
                if (group == null) return;

                var result = MessageBox.Show(
                    $"Delete shortcut '{shortcut.Name}'?",
                    "Delete Shortcut",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    var deleted = await _shortcutService.DeleteShortcutAsync(group.Id, shortcut.Id);
                    if (deleted)
                    {
                        await LoadGroupsAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                await _errorHandlingService.HandleErrorAsync(ex, $"Failed to delete shortcut '{shortcut.Name}'.");
            }
        }

        private async void LaunchShortcut(Shortcut? shortcut)
        {
            if (shortcut == null) return;
            try
            {
                var success = await _shortcutService.LaunchShortcutAsync(shortcut);
                if (!success)
                    await _errorHandlingService.ShowErrorMessageAsync("Launch Failed", $"Could not launch '{shortcut.Name}'.");
            }
            catch (Exception ex)
            {
                await _errorHandlingService.HandleErrorAsync(ex, $"Failed to launch '{shortcut.Name}'.");
            }
        }

        private void OnDragStarted(object? sender, DragDropEventArgs e)
            => System.Diagnostics.Debug.WriteLine($"DragStarted: {(e.Data as Group)?.Name ?? (e.Data as Shortcut)?.Name}");

        private void OnDragCompleted(object? sender, DragDropEventArgs e)
            => System.Diagnostics.Debug.WriteLine($"DragCompleted: {(e.Data as Group)?.Name ?? (e.Data as Shortcut)?.Name}");

        private async void OnDropCompleted(object? sender, DragDropEventArgs e)
        {
            await LoadGroupsAsync();
        }

        private async void ReorderGroups(ReorderGroupsEventArgs? args)
        {
            if (args?.DraggedGroup == null || args.TargetGroup == null) return;
            try
            {
                var groups = await _groupService.GetAllGroupsAsync();
                var targetIndex = groups.FindIndex(g => g.Id == args.TargetGroup.Id);
                if (targetIndex >= 0)
                {
                    await _groupService.MoveGroupAsync(args.DraggedGroup.Id, targetIndex);
                    await LoadGroupsAsync();
                }
            }
            catch (Exception ex)
            {
                await _errorHandlingService.HandleErrorAsync(ex, "Failed to reorder groups.");
            }
        }

        private async void ReorderShortcuts(ReorderShortcutsEventArgs? args)
        {
            if (args?.DraggedShortcut == null || args.TargetGroup == null) return;
            try
            {
                var shortcuts = await _shortcutService.GetShortcutsByGroupAsync(args.TargetGroup.Id);
                var targetIndex = shortcuts.FindIndex(s => s.Id == (args.TargetShortcut?.Id ?? ""));
                if (targetIndex >= 0)
                {
                    await _shortcutService.MoveShortcutAsync(args.TargetGroup.Id, args.DraggedShortcut.Id, targetIndex);
                    await LoadGroupsAsync();
                }
            }
            catch (Exception ex)
            {
                await _errorHandlingService.HandleErrorAsync(ex, "Failed to reorder shortcuts.");
            }
        }

        public class ReorderGroupsEventArgs
        {
            public Group? DraggedGroup { get; set; }
            public Group? TargetGroup  { get; set; }
            public int    TargetIndex  { get; set; }
        }

        public class ReorderShortcutsEventArgs
        {
            public Shortcut? DraggedShortcut { get; set; }
            public Shortcut? TargetShortcut  { get; set; }
            public Group?    TargetGroup     { get; set; }
            public int       TargetIndex     { get; set; }
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute    = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add    { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
        public void Execute(object? parameter)     => _execute();
        public void RaiseCanExecuteChanged()       => CommandManager.InvalidateRequerySuggested();
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Func<T?, bool>? _canExecute;

        public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
        {
            _execute    = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add    { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
        public bool CanExecute(object? parameter) => _canExecute?.Invoke((T?)parameter) ?? true;
        public void Execute(object? parameter)     => _execute((T?)parameter);
        public void RaiseCanExecuteChanged()       => CommandManager.InvalidateRequerySuggested();
    }
}
