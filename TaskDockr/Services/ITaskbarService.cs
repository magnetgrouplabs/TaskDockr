using TaskDockr.Models;
using System;
using System.Threading.Tasks;

namespace TaskDockr.Services
{
    public interface ITaskbarService
    {
        Task<bool> CreateGroupIconAsync(Group group);
        Task<bool> UpdateGroupIconAsync(Group group);
        Task<bool> RemoveGroupIconAsync(string groupId);
        Task ShowGroupMenuAsync(string groupId, int x, int y);
        Task InitializeAsync();
        Task CleanupAsync();

        event EventHandler<TaskbarIconClickEventArgs>? IconClicked;
    }

    public class TaskbarIconClickEventArgs : EventArgs
    {
        public string GroupId { get; set; } = string.Empty;
        public int X { get; set; }
        public int Y { get; set; }
        public TaskbarIconClickType ClickType { get; set; }
    }

    public enum TaskbarIconClickType
    {
        LeftClick,
        RightClick,
        DoubleClick
    }
}
