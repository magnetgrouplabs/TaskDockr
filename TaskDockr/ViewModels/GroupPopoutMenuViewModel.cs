using System.Collections.ObjectModel;
using System.Threading.Tasks;
using TaskDockr.Models;
using TaskDockr.Services;
using TaskDockr.Utils;

namespace TaskDockr.ViewModels
{
    public class GroupPopoutMenuViewModel : ObservableObject
    {
        private readonly IGroupService    _groupService;
        private readonly IShortcutService _shortcutService;

        private Group? _group;
        public Group? Group
        {
            get => _group;
            set => SetProperty(ref _group, value);
        }

        public ObservableCollection<Shortcut> Shortcuts { get; } = new();

public GroupPopoutMenuViewModel()
        {
            _groupService = App.GetService<IGroupService>();
            _shortcutService = App.GetService<IShortcutService>();
        }

        public async Task LoadGroupAsync(string groupId)
        {
            Group = await _groupService.GetGroupByIdAsync(groupId);
            Shortcuts.Clear();
            if (Group?.Shortcuts != null)
                foreach (var s in Group.Shortcuts)
                    Shortcuts.Add(s);
        }

        public async void LaunchShortcut(Shortcut shortcut)
        {
            if (shortcut != null)
                await _shortcutService.LaunchShortcutAsync(shortcut);
        }

        public async void RemoveShortcut(Shortcut shortcut)
        {
            if (shortcut != null && Group != null)
            {
                await _shortcutService.DeleteShortcutAsync(Group.Id, shortcut.Id);
                Group.Shortcuts.Remove(shortcut);
                Shortcuts.Remove(shortcut);
                await _groupService.UpdateGroupAsync(Group);
            }
        }
    }
}
