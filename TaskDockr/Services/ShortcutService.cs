using TaskDockr.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TaskDockr.Services
{
    public class ShortcutService : IShortcutService
    {
        private readonly IConfigurationService _configService;
        private readonly IIconService _iconService;
        private readonly IErrorHandlingService _errorHandlingService;
        private readonly List<string> _executableExtensions = new List<string> { ".exe", ".com", ".bat", ".cmd", ".msi", ".ps1" };
        private readonly List<string> _documentExtensions = new List<string> { ".txt", ".doc", ".docx", ".pdf", ".xls", ".xlsx", ".ppt", ".pptx" };

        public ShortcutService(IConfigurationService configService, IIconService iconService, IErrorHandlingService errorHandlingService)
        {
            _configService = configService;
            _iconService = iconService;
            _errorHandlingService = errorHandlingService;
        }

        public async Task<Shortcut> CreateShortcutAsync(string groupId, string name, string targetPath, ShortcutType type, string arguments = null, string iconPath = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(targetPath) || string.IsNullOrWhiteSpace(groupId))
                    return null;

                var config = _configService.CurrentConfig;
                var group = config.Groups?.FirstOrDefault(g => g.Id == groupId);
                if (group == null) return null;

                var shortcut = new Shortcut
                {
                    Name      = name.Trim(),
                    TargetPath = targetPath.Trim(),
                    Type      = type,
                    Arguments = arguments?.Trim() ?? string.Empty,
                    IconPath  = iconPath?.Trim()  ?? string.Empty
                };

                group.Shortcuts ??= new List<Shortcut>();
                group.Shortcuts.Add(shortcut);

                await _configService.SaveConfigAsync(config);
                return shortcut;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreateShortcutAsync error: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> UpdateShortcutAsync(Shortcut shortcut)
        {
            try
            {
                if (shortcut == null) return false;

                var config = _configService.CurrentConfig;
                var group = config.Groups?.FirstOrDefault(g => g.Shortcuts?.Any(s => s.Id == shortcut.Id) == true);
                if (group == null) return false;

                var existing = group.Shortcuts.FirstOrDefault(s => s.Id == shortcut.Id);
                if (existing == null) return false;

                existing.Name       = shortcut.Name.Trim();
                existing.TargetPath = shortcut.TargetPath.Trim();
                existing.Type       = shortcut.Type;
                existing.Arguments  = shortcut.Arguments?.Trim() ?? string.Empty;
                existing.IconPath   = shortcut.IconPath?.Trim()  ?? string.Empty;

                await _configService.SaveConfigAsync(config);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateShortcutAsync error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteShortcutAsync(string groupId, string shortcutId)
        {
            try
            {
                if (string.IsNullOrEmpty(groupId) || string.IsNullOrEmpty(shortcutId))
                {
                    await _errorHandlingService.ShowErrorMessageAsync("Invalid Input", "Group ID and Shortcut ID cannot be empty.");
                    return false;
                }

                var config = _configService.CurrentConfig;
                var group = config.Groups?.FirstOrDefault(g => g.Id == groupId);

                if (group == null)
                {
                    await _errorHandlingService.ShowErrorMessageAsync("Group Not Found", "The specified group could not be found.");
                    return false;
                }

                var shortcut = group.Shortcuts?.FirstOrDefault(s => s.Id == shortcutId);
                if (shortcut == null)
                {
                    await _errorHandlingService.ShowErrorMessageAsync("Shortcut Not Found", "The specified shortcut could not be found.");
                    return false;
                }

                group.Shortcuts.Remove(shortcut);

                await _configService.SaveConfigAsync(config);
                return true;
            }
            catch (Exception ex)
            {
                await _errorHandlingService.HandleErrorAsync(ex, "Failed to delete shortcut.");
                return false;
            }
        }

        public async Task<bool> MoveShortcutAsync(string groupId, string shortcutId, int newPosition)
        {
            try
            {
                if (string.IsNullOrEmpty(groupId) || string.IsNullOrEmpty(shortcutId))
                {
                    await _errorHandlingService.ShowErrorMessageAsync("Invalid Input", "Group ID and Shortcut ID cannot be empty.");
                    return false;
                }

                if (newPosition < 0)
                {
                    await _errorHandlingService.ShowErrorMessageAsync("Invalid Position", "Position cannot be negative.");
                    return false;
                }

                var config = _configService.CurrentConfig;
                var group = config.Groups?.FirstOrDefault(g => g.Id == groupId);

                if (group == null || group.Shortcuts == null)
                {
                    await _errorHandlingService.ShowErrorMessageAsync("Group Not Found", "The specified group could not be found.");
                    return false;
                }

                var shortcut = group.Shortcuts.FirstOrDefault(s => s.Id == shortcutId);
                if (shortcut == null)
                {
                    await _errorHandlingService.ShowErrorMessageAsync("Shortcut Not Found", "The specified shortcut could not be found.");
                    return false;
                }

                var shortcuts = group.Shortcuts.ToList();
                shortcuts.Remove(shortcut);
                shortcuts.Insert(Math.Min(newPosition, shortcuts.Count), shortcut);

                group.Shortcuts = shortcuts;

                await _configService.SaveConfigAsync(config);
                return true;
            }
            catch (Exception ex)
            {
                await _errorHandlingService.HandleErrorAsync(ex, "Failed to move shortcut.");
                return false;
            }
        }

        public Task<bool> ValidateShortcutAsync(Shortcut shortcut)
        {
            if (shortcut == null) return Task.FromResult(false);
            if (string.IsNullOrWhiteSpace(shortcut.Name)) return Task.FromResult(false);
            if (string.IsNullOrWhiteSpace(shortcut.TargetPath)) return Task.FromResult(false);
            return Task.FromResult(true);
        }

        public async Task<bool> LaunchShortcutAsync(Shortcut shortcut)
        {
            if (shortcut == null)
            {
                await _errorHandlingService.ShowErrorMessageAsync("Invalid Shortcut", "Shortcut cannot be null.");
                return false;
            }

            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    ErrorDialog = false // Don't show Windows error dialogs
                };

                switch (shortcut.Type)
                {
                    case ShortcutType.App:
                        processStartInfo.FileName = shortcut.TargetPath;
                        if (!string.IsNullOrEmpty(shortcut.Arguments))
                            processStartInfo.Arguments = shortcut.Arguments;
                        break;

                    case ShortcutType.File:
                        // ShellExecute opens the file with its default application
                        processStartInfo.FileName = shortcut.TargetPath;
                        break;

                    case ShortcutType.Folder:
                        // Use Windows Explorer to open folders
                        processStartInfo.FileName = "explorer.exe";
                        processStartInfo.Arguments = $"\"{shortcut.TargetPath}\"";
                        break;

                    case ShortcutType.URL:
                        // Use default browser for URLs
                        processStartInfo.FileName = shortcut.TargetPath;
                        break;

                    default:
                        await _errorHandlingService.ShowErrorMessageAsync("Invalid Type", $"Unknown shortcut type: {shortcut.Type}");
                        return false;
                }

                Process.Start(processStartInfo);
                return true;
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                await _errorHandlingService.HandleErrorAsync(ex, $"Failed to launch '{shortcut.Name}'. Access denied or insufficient permissions.");
                return false;
            }
            catch (FileNotFoundException ex)
            {
                await _errorHandlingService.HandleErrorAsync(ex, $"Failed to launch '{shortcut.Name}'. The target file was not found.");
                return false;
            }
            catch (Exception ex)
            {
                await _errorHandlingService.HandleErrorAsync(ex, $"Failed to launch '{shortcut.Name}'.");
                return false;
            }
        }

        public async Task<string> ResolveTargetPathAsync(string targetPath, ShortcutType type)
        {
            try
            {
                if (string.IsNullOrEmpty(targetPath))
                    return null;

                switch (type)
                {
                    case ShortcutType.App:
                        return await ResolveExecutablePathAsync(targetPath);
                    
                    case ShortcutType.File:
                    case ShortcutType.Folder:
                        return await ResolveFilePathAsync(targetPath);
                    
                    case ShortcutType.URL:
                        return await ResolveUrlPathAsync(targetPath);
                    
                    default:
                        return null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to resolve target path '{targetPath}': {ex.Message}");
                return null;
            }
        }

        public async Task<bool> TargetExistsAsync(string targetPath, ShortcutType type)
        {
            if (string.IsNullOrEmpty(targetPath))
                return false;

            try
            {
                switch (type)
                {
                    case ShortcutType.App:
                        // Check if file exists and has executable extension
                        if (File.Exists(targetPath))
                            return _executableExtensions.Contains(Path.GetExtension(targetPath).ToLowerInvariant());
                        
                        // Try to resolve executable in PATH
                        var resolvedPath = await ResolveExecutablePathAsync(targetPath);
                        return resolvedPath != null;
                    
                case ShortcutType.File:
                    // Check if file exists
                    if (File.Exists(targetPath))
                        return true;
                    
                    // Try to resolve relative paths
                    var resolvedFilePath = await ResolveFilePathAsync(targetPath);
                    return resolvedFilePath != null;
                
                case ShortcutType.Folder:
                    // Check if directory exists
                    if (Directory.Exists(targetPath))
                        return true;
                    
                    // Try to resolve relative paths
                    var resolvedFolderPath = await ResolveFilePathAsync(targetPath);
                    return resolvedFolderPath != null && Directory.Exists(resolvedFolderPath);
                    
                    case ShortcutType.URL:
                        // Validate URL format
                        if (Uri.TryCreate(targetPath, UriKind.Absolute, out var uriResult))
                        {
                            // Accept http, https, ftp, mailto, and file URLs
                            return uriResult.Scheme == Uri.UriSchemeHttp || 
                                   uriResult.Scheme == Uri.UriSchemeHttps ||
                                   uriResult.Scheme == Uri.UriSchemeFtp ||
                                   uriResult.Scheme == Uri.UriSchemeMailto ||
                                   uriResult.Scheme == Uri.UriSchemeFile;
                        }
                        
                        // Try to resolve URL
                        var resolvedUrl = await ResolveUrlPathAsync(targetPath);
                        return resolvedUrl != null;
                    
                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error validating target '{targetPath}': {ex.Message}");
                return false;
            }
        }

        public async Task<List<Shortcut>> GetShortcutsByGroupAsync(string groupId)
        {
            try
            {
                if (string.IsNullOrEmpty(groupId))
                    return new List<Shortcut>();

                var config = await _configService.LoadConfigAsync();
                var group = config.Groups?.FirstOrDefault(g => g.Id == groupId);
                return group?.Shortcuts?.ToList() ?? new List<Shortcut>();
            }
            catch (Exception ex)
            {
                await _errorHandlingService.HandleErrorAsync(ex, $"Failed to get shortcuts for group '{groupId}'.");
                return new List<Shortcut>();
            }
        }

        public async Task<Shortcut> GetShortcutByIdAsync(string groupId, string shortcutId)
        {
            try
            {
                if (string.IsNullOrEmpty(groupId) || string.IsNullOrEmpty(shortcutId))
                {
                    await _errorHandlingService.ShowErrorMessageAsync("Invalid Input", "Group ID and Shortcut ID cannot be empty.");
                    return null;
                }

                var config = await _configService.LoadConfigAsync();
                var group = config.Groups?.FirstOrDefault(g => g.Id == groupId);
                var shortcut = group?.Shortcuts?.FirstOrDefault(s => s.Id == shortcutId);
                
                if (shortcut == null)
                {
                    await _errorHandlingService.ShowErrorMessageAsync("Shortcut Not Found", $"Shortcut with ID '{shortcutId}' could not be found.");
                }
                
                return shortcut;
            }
            catch (Exception ex)
            {
                await _errorHandlingService.HandleErrorAsync(ex, $"Failed to get shortcut with ID '{shortcutId}'.");
                return null;
            }
        }

        public async Task ReorderShortcutsAsync(string groupId, List<Shortcut> shortcuts)
        {
            try
            {
                if (string.IsNullOrEmpty(groupId) || shortcuts == null)
                    return;

                var config = _configService.CurrentConfig;
                var group = config.Groups?.FirstOrDefault(g => g.Id == groupId);

                if (group == null)
                {
                    await _errorHandlingService.ShowErrorMessageAsync("Group Not Found", $"Group with ID '{groupId}' could not be found.");
                    return;
                }

                group.Shortcuts = shortcuts;
                await _configService.SaveConfigAsync(config);
            }
            catch (Exception ex)
            {
                await _errorHandlingService.HandleErrorAsync(ex, $"Failed to reorder shortcuts in group '{groupId}'.");
            }
        }

        private async Task<string> ResolveExecutablePathAsync(string targetPath)
        {
            if (File.Exists(targetPath) && _executableExtensions.Contains(Path.GetExtension(targetPath).ToLowerInvariant()))
                return targetPath;

            // Search in PATH environment variable
            var pathDirs = Environment.GetEnvironmentVariable("PATH")?.Split(';') ?? Array.Empty<string>();
            foreach (var dir in pathDirs)
            {
                if (Directory.Exists(dir))
                {
                    var fullPath = Path.Combine(dir, targetPath);
                    if (File.Exists(fullPath))
                        return fullPath;

                    // Try with executable extensions
                    foreach (var ext in _executableExtensions)
                    {
                        var pathWithExt = Path.Combine(dir, targetPath + ext);
                        if (File.Exists(pathWithExt))
                            return pathWithExt;
                    }
                }
            }

            return null;
        }

        private async Task<string> ResolveFilePathAsync(string targetPath)
        {
            if (File.Exists(targetPath) || Directory.Exists(targetPath))
                return targetPath;

            // Try to resolve relative paths
            var currentDir = Directory.GetCurrentDirectory();
            var fullPath = Path.Combine(currentDir, targetPath);
            if (File.Exists(fullPath) || Directory.Exists(fullPath))
                return fullPath;

            return null;
        }

        private async Task<string> ResolveUrlPathAsync(string targetPath)
        {
            // Try direct URL parsing
            if (Uri.TryCreate(targetPath, UriKind.Absolute, out var uriResult))
            {
                // Accept common URL schemes
                if (uriResult.Scheme == Uri.UriSchemeHttp || 
                    uriResult.Scheme == Uri.UriSchemeHttps ||
                    uriResult.Scheme == Uri.UriSchemeFtp ||
                    uriResult.Scheme == Uri.UriSchemeMailto ||
                    uriResult.Scheme == Uri.UriSchemeFile)
                    return targetPath;
            }

            // Try to add http:// prefix if missing
            if (!targetPath.Contains("://"))
            {
                // Try http://
                var urlWithHttp = "http://" + targetPath;
                if (Uri.TryCreate(urlWithHttp, UriKind.Absolute, out _))
                    return urlWithHttp;

                // Try https://
                var urlWithHttps = "https://" + targetPath;
                if (Uri.TryCreate(urlWithHttps, UriKind.Absolute, out _))
                    return urlWithHttps;

                // Try mailto:
                if (targetPath.Contains("@"))
                {
                    var urlWithMailto = "mailto:" + targetPath;
                    if (Uri.TryCreate(urlWithMailto, UriKind.Absolute, out _))
                        return urlWithMailto;
                }
            }

            return null;
        }
    }
}