using TaskDockr.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TaskDockr.Services
{
    public interface IShortcutService
    {
        Task<Shortcut> CreateShortcutAsync(string groupId, string name, string targetPath, ShortcutType type, string arguments = null, string iconPath = null);
        Task<bool> UpdateShortcutAsync(Shortcut shortcut);
        Task<bool> DeleteShortcutAsync(string groupId, string shortcutId);
        Task<bool> MoveShortcutAsync(string groupId, string shortcutId, int newPosition);
        Task<bool> ValidateShortcutAsync(Shortcut shortcut);
        Task<bool> LaunchShortcutAsync(Shortcut shortcut);
        Task<string> ResolveTargetPathAsync(string targetPath, ShortcutType type);
        Task<bool> TargetExistsAsync(string targetPath, ShortcutType type);
        Task<List<Shortcut>> GetShortcutsByGroupAsync(string groupId);
        Task<Shortcut> GetShortcutByIdAsync(string groupId, string shortcutId);
        Task ReorderShortcutsAsync(string groupId, List<Shortcut> shortcuts);
    }
}