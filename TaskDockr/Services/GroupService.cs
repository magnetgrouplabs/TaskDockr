using TaskDockr.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TaskDockr.Services
{
    public class GroupService : IGroupService
    {
        private readonly IConfigurationService _configService;
        private readonly IIconService _iconService;
        private readonly IErrorHandlingService _errorHandlingService;

        public event EventHandler<GroupEventArgs> GroupCreated;
        public event EventHandler<GroupEventArgs> GroupUpdated;
        public event EventHandler<GroupEventArgs> GroupDeleted;

        public GroupService(IConfigurationService configService, IIconService iconService, IErrorHandlingService errorHandlingService)
        {
            _configService = configService;
            _iconService = iconService;
            _errorHandlingService = errorHandlingService;
        }

        protected virtual void OnGroupCreated(Group group)
        {
            GroupCreated?.Invoke(this, new GroupEventArgs(group));
        }

        protected virtual void OnGroupUpdated(Group group)
        {
            GroupUpdated?.Invoke(this, new GroupEventArgs(group));
        }

        protected virtual void OnGroupDeleted(string groupId)
        {
            GroupDeleted?.Invoke(this, new GroupEventArgs(groupId));
        }

public async Task<Group> CreateGroupAsync(string name, string iconPath = null)
{
Debug.WriteLine($"[GroupService] CreateGroupAsync START - Name: '{name}', IconPath: '{iconPath}'");

try
{
Debug.WriteLine($"[GroupService] Step 1: Validating group name...");
if (!await _errorHandlingService.ValidateGroupNameAsync(name))
{
Debug.WriteLine($"[GroupService] Group name validation failed for: '{name}'");
return null;
}
Debug.WriteLine($"[GroupService] Group name validation passed");

if (!string.IsNullOrEmpty(iconPath))
{
Debug.WriteLine($"[GroupService] Step 2: Validating icon path...");
if (!await _errorHandlingService.ValidateIconPathAsync(iconPath))
{
Debug.WriteLine($"[GroupService] Icon path validation failed for: '{iconPath}'");
return null;
}
Debug.WriteLine($"[GroupService] Icon path validation passed");
}

Debug.WriteLine($"[GroupService] Step 3: Creating group object...");
var group = new Group
{
Name = name.Trim(),
IconPath = iconPath ?? string.Empty,
Position = await GetNextGroupPositionAsync()
};
Debug.WriteLine($"[GroupService] Group created - ID: {group.Id}, Position: {group.Position}");

Debug.WriteLine($"[GroupService] Step 4: Validating group...");
if (!await ValidateGroupAsync(group))
{
Debug.WriteLine($"[GroupService] Group validation failed for: '{group.Name}'");
await _errorHandlingService.ShowErrorMessageAsync("Validation Failed", "Group validation failed. Please check the group name and icon.");
return null;
}
Debug.WriteLine($"[GroupService] Group validation passed");

Debug.WriteLine($"[GroupService] Step 5: Adding group to config...");
var config = _configService.CurrentConfig;
config.Groups ??= new List<Group>();
config.Groups.Add(group);
Debug.WriteLine($"[GroupService] Group added to config. Total groups: {config.Groups.Count}");

Debug.WriteLine($"[GroupService] Step 6: Saving config...");
await _configService.SaveConfigAsync(config);
Debug.WriteLine($"[GroupService] Config saved successfully");

            Debug.WriteLine($"[GroupService] Step 7: Raising GroupCreated event...");
            OnGroupCreated(group);
            Debug.WriteLine($"[GroupService] GroupCreated event raised successfully");

            Debug.WriteLine($"[GroupService] CreateGroupAsync COMPLETED - Group '{name}' (ID: {group.Id})");
return group;
}
catch (Exception ex)
{
Debug.WriteLine($"[GroupService] CRITICAL ERROR in CreateGroupAsync: {ex.Message}");
Debug.WriteLine($"[GroupService] Stack trace: {ex.StackTrace}");
if (ex.InnerException != null)
{
Debug.WriteLine($"[GroupService] Inner exception: {ex.InnerException.Message}");
}
await _errorHandlingService.HandleErrorAsync(ex, "Failed to create group.");
return null;
}
}

        public async Task<bool> UpdateGroupAsync(Group group)
        {
            try
            {
                if (group == null)
                {
                    await _errorHandlingService.ShowErrorMessageAsync("Invalid Group", "Group cannot be null.");
                    return false;
                }

                if (!await _errorHandlingService.ValidateGroupNameAsync(group.Name))
                    return false;

                if (!string.IsNullOrEmpty(group.IconPath) && !await _errorHandlingService.ValidateIconPathAsync(group.IconPath))
                    return false;

                if (!await ValidateGroupAsync(group))
                {
                    await _errorHandlingService.ShowErrorMessageAsync("Validation Failed", "Group validation failed. Please check the group name and icon.");
                    return false;
                }

                var config = _configService.CurrentConfig;
                var existingGroup = config.Groups?.FirstOrDefault(g => g.Id == group.Id);

                if (existingGroup == null)
                {
                    await _errorHandlingService.ShowErrorMessageAsync("Group Not Found", "The specified group could not be found.");
                    return false;
                }

            existingGroup.Name = group.Name.Trim();
            existingGroup.IconPath = group.IconPath ?? string.Empty;
            existingGroup.IconGlyph = group.IconGlyph ?? string.Empty;
            existingGroup.IconColor = group.IconColor ?? string.Empty;

            await _configService.SaveConfigAsync(config);

            // Raise event for taskbar icon update (pass existingGroup so it has the merged data)
            OnGroupUpdated(existingGroup);

            Debug.WriteLine($"[GroupService] UpdateGroupAsync COMPLETED - Group '{group.Name}'");
                return true;
            }
            catch (Exception ex)
            {
                await _errorHandlingService.HandleErrorAsync(ex, "Failed to update group.");
                return false;
            }
        }

        public async Task<bool> DeleteGroupAsync(string groupId)
        {
            try
            {
                if (string.IsNullOrEmpty(groupId))
                {
                    await _errorHandlingService.ShowErrorMessageAsync("Invalid Group ID", "Group ID cannot be empty.");
                    return false;
                }

                var config = _configService.CurrentConfig;
                var group = config.Groups?.FirstOrDefault(g => g.Id == groupId);

                if (group == null)
                {
                    await _errorHandlingService.ShowErrorMessageAsync("Group Not Found", "The specified group could not be found.");
                    return false;
                }

            config.Groups.Remove(group);

            // Reorder remaining groups
            await ReorderGroupsAsync(config.Groups);

            await _configService.SaveConfigAsync(config);

            // Raise event for taskbar icon removal
            OnGroupDeleted(groupId);

            Debug.WriteLine($"[GroupService] DeleteGroupAsync COMPLETED - Group '{group.Name}'");
                return true;
            }
            catch (Exception ex)
            {
                await _errorHandlingService.HandleErrorAsync(ex, "Failed to delete group.");
                return false;
            }
        }

        public async Task<bool> MoveGroupAsync(string groupId, int newPosition)
        {
            try
            {
                if (string.IsNullOrEmpty(groupId))
                {
                    await _errorHandlingService.ShowErrorMessageAsync("Invalid Group ID", "Group ID cannot be empty.");
                    return false;
                }

                if (newPosition < 0)
                {
                    await _errorHandlingService.ShowErrorMessageAsync("Invalid Position", "Position cannot be negative.");
                    return false;
                }

                var config = _configService.CurrentConfig;
                var groups = config.Groups?.ToList() ?? new List<Group>();
                var group = groups.FirstOrDefault(g => g.Id == groupId);

                if (group == null)
                {
                    await _errorHandlingService.ShowErrorMessageAsync("Group Not Found", "The specified group could not be found.");
                    return false;
                }

                groups.Remove(group);
                groups.Insert(Math.Min(newPosition, groups.Count), group);

                await ReorderGroupsAsync(groups);
                config.Groups = groups;

                await _configService.SaveConfigAsync(config);
                return true;
            }
            catch (Exception ex)
            {
                await _errorHandlingService.HandleErrorAsync(ex, "Failed to move group.");
                return false;
            }
        }

        public async Task<bool> ValidateGroupAsync(Group group)
        {
            if (group == null)
                return false;

            if (string.IsNullOrWhiteSpace(group.Name))
                return false;

            if (group.Name.Length > 50)
                return false;

            // Check for duplicate names (case-insensitive)
            var config = _configService.CurrentConfig;
            var existingGroup = config.Groups?.FirstOrDefault(g => 
                g.Id != group.Id && 
                string.Equals(g.Name, group.Name, StringComparison.OrdinalIgnoreCase));

            if (existingGroup != null)
                return false;

            // Validate icon path if provided
            if (!string.IsNullOrEmpty(group.IconPath))
            {
                var validatedIconPath = await ValidateIconPathAsync(group.IconPath);
                if (string.IsNullOrEmpty(validatedIconPath))
                    return false;
            }

            return true;
        }

        public async Task<string> ValidateIconPathAsync(string iconPath)
        {
            if (string.IsNullOrEmpty(iconPath))
                return string.Empty;

            try
            {
                // Use IconService for comprehensive validation
                var isValid = await _iconService.IsValidIconSourceAsync(iconPath);
                return isValid ? iconPath : string.Empty;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to validate icon path '{iconPath}': {ex.Message}");
                return string.Empty;
            }
        }

        public async Task<List<Group>> GetAllGroupsAsync()
        {
            try
            {
                var config = await _configService.LoadConfigAsync();
                return config.Groups?.OrderBy(g => g.Position).ToList() ?? new List<Group>();
            }
            catch (Exception ex)
            {
                await _errorHandlingService.HandleErrorAsync(ex, "Failed to load groups.");
                return new List<Group>();
            }
        }

        public async Task<Group> GetGroupByIdAsync(string groupId)
        {
            try
            {
                if (string.IsNullOrEmpty(groupId))
                {
                    await _errorHandlingService.ShowErrorMessageAsync("Invalid Group ID", "Group ID cannot be empty.");
                    return null;
                }

                var config = await _configService.LoadConfigAsync();
                var group = config.Groups?.FirstOrDefault(g => g.Id == groupId);
                
                if (group == null)
                {
                    await _errorHandlingService.ShowErrorMessageAsync("Group Not Found", $"Group with ID '{groupId}' could not be found.");
                }
                
                return group;
            }
            catch (Exception ex)
            {
                await _errorHandlingService.HandleErrorAsync(ex, $"Failed to get group with ID '{groupId}'.");
                return null;
            }
        }

        public async Task ReorderGroupsAsync(List<Group> groups)
        {
            try
            {
                if (groups == null)
                    return;

                for (int i = 0; i < groups.Count; i++)
                {
                    groups[i].Position = i;
                }

                var config = _configService.CurrentConfig;
                config.Groups = groups;
                await _configService.SaveConfigAsync(config);
            }
            catch (Exception ex)
            {
                await _errorHandlingService.HandleErrorAsync(ex, "Failed to reorder groups.");
            }
        }

        public async Task<bool> GroupExistsAsync(string groupId)
        {
            try
            {
                if (string.IsNullOrEmpty(groupId))
                    return false;

                var config = await _configService.LoadConfigAsync();
                return config.Groups?.Any(g => g.Id == groupId) == true;
            }
            catch (Exception ex)
            {
                await _errorHandlingService.HandleErrorAsync(ex, $"Failed to check if group '{groupId}' exists.");
                return false;
            }
        }

        private async Task<int> GetNextGroupPositionAsync()
        {
            try
            {
                var config = await _configService.LoadConfigAsync();
                var groups = config.Groups?.ToList() ?? new List<Group>();
                return groups.Count > 0 ? groups.Max(g => g.Position) + 1 : 0;
            }
            catch (Exception ex)
            {
                await _errorHandlingService.HandleErrorAsync(ex, "Failed to get next group position.");
                return 0;
            }
        }
    }
}