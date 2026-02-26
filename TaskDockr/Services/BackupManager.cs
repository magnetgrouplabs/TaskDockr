using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TaskDockr.Models;

namespace TaskDockr.Services
{
    public interface IBackupManager
    {
        Task<List<string>> GetAvailableBackupsAsync();
        Task<bool> RestoreBackupAsync(string backupPath);
        Task CleanupOldBackupsAsync(int keepLast = 10);
        Task<string> CreateManualBackupAsync();
    }

    public class BackupManager : IBackupManager
    {
        private readonly string _backupFolderPath;
        private readonly string _configFilePath;

        public BackupManager()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "TaskDockr");
            _backupFolderPath = Path.Combine(appFolder, "backups");
            _configFilePath = Path.Combine(appFolder, "config.json");
            
            Directory.CreateDirectory(_backupFolderPath);
        }

        public async Task<List<string>> GetAvailableBackupsAsync()
        {
            try
            {
                if (!Directory.Exists(_backupFolderPath))
                    return new List<string>();

                var backupFiles = Directory.GetFiles(_backupFolderPath, "config_backup_*.json")
                    .OrderByDescending(f => f)
                    .ToList();

                return backupFiles;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to list backups: {ex.Message}");
                return new List<string>();
            }
        }

        public async Task<bool> RestoreBackupAsync(string backupPath)
        {
            try
            {
                if (!File.Exists(backupPath))
                    return false;

                // Create backup of current config before restore
                var currentBackupPath = await CreateManualBackupAsync();
                
                File.Copy(backupPath, _configFilePath, true);
                
                // Verify the restored config is valid
                var json = await File.ReadAllTextAsync(_configFilePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                
                var config = JsonSerializer.Deserialize<AppConfig>(json, options);
                if (config == null)
                {
                    // Restore from the backup we just created
                    if (File.Exists(currentBackupPath))
                    {
                        File.Copy(currentBackupPath, _configFilePath, true);
                    }
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to restore backup: {ex.Message}");
                return false;
            }
        }

        public async Task CleanupOldBackupsAsync(int keepLast = 10)
        {
            try
            {
                var backupFiles = await GetAvailableBackupsAsync();
                
                if (backupFiles.Count <= keepLast)
                    return;

                var filesToDelete = backupFiles.Skip(keepLast).ToList();
                
                foreach (var file in filesToDelete)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to delete backup {file}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to cleanup backups: {ex.Message}");
            }
        }

        public async Task<string> CreateManualBackupAsync()
        {
            try
            {
                if (!File.Exists(_configFilePath))
                    return string.Empty;

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupPath = Path.Combine(_backupFolderPath, $"config_manual_backup_{timestamp}.json");
                
                File.Copy(_configFilePath, backupPath, true);
                
                return backupPath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create manual backup: {ex.Message}");
                return string.Empty;
            }
        }

        public async Task<BackupInfo> GetBackupInfoAsync(string backupPath)
        {
            try
            {
                if (!File.Exists(backupPath))
                    return new BackupInfo();

                var fileInfo = new FileInfo(backupPath);
                var json = await File.ReadAllTextAsync(backupPath);
                
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                
                var config = JsonSerializer.Deserialize<AppConfig>(json, options);
                
                return new BackupInfo
                {
                    FilePath = backupPath,
                    CreatedDate = fileInfo.CreationTime,
                    Size = fileInfo.Length,
                    ConfigVersion = config?.Version ?? "Unknown",
                    IsValid = config != null
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to get backup info: {ex.Message}");
                return new BackupInfo();
            }
        }
    }

    public class BackupInfo
    {
        public string FilePath { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public long Size { get; set; }
        public string ConfigVersion { get; set; } = string.Empty;
        public bool IsValid { get; set; }
    }
}