using TaskDockr.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TaskDockr.Services
{
    public interface IGroupService
    {
        Task<Group> CreateGroupAsync(string name, string iconPath = null);
        Task<bool> UpdateGroupAsync(Group group);
        Task<bool> DeleteGroupAsync(string groupId);
        Task<bool> MoveGroupAsync(string groupId, int newPosition);
        Task<bool> ValidateGroupAsync(Group group);
        Task<string> ValidateIconPathAsync(string iconPath);
        Task<List<Group>> GetAllGroupsAsync();
        Task<Group> GetGroupByIdAsync(string groupId);
        Task ReorderGroupsAsync(List<Group> groups);
        Task<bool> GroupExistsAsync(string groupId);

        event EventHandler<GroupEventArgs> GroupCreated;
        event EventHandler<GroupEventArgs> GroupUpdated;
        event EventHandler<GroupEventArgs> GroupDeleted;
    }

    public class GroupEventArgs : EventArgs
    {
        public Group Group { get; }
        public string GroupId { get; }

        public GroupEventArgs(Group group)
        {
            Group = group;
            GroupId = group?.Id;
        }

        public GroupEventArgs(string groupId)
        {
            GroupId = groupId;
            Group = null;
        }
    }
}