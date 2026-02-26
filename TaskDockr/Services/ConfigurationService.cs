using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using TaskDockr.Models;

namespace TaskDockr.Services
{
    public interface IConfigurationService
    {
        AppConfig CurrentConfig { get; }
        
        Task<AppConfig> LoadConfigAsync();
        Task SaveConfigAsync(AppConfig config);
        Task BackupConfigAsync();
        Task<bool> RestoreConfigAsync(string backupPath);
        Task<bool> MigrateConfigAsync(string oldVersion);
        void AddRecentFile(string filePath);
        void AddRecentFolder(string folderPath);
        void ClearRecentFiles();
        void ClearRecentFolders();
    }

    public class ConfigurationService : IConfigurationService
    {
        private readonly string _configFilePath;
        private readonly string _backupFolderPath;
        private readonly IErrorHandlingService _errorHandlingService;
        private AppConfig _currentConfig;

        public AppConfig CurrentConfig => _currentConfig;

        public ConfigurationService(IErrorHandlingService errorHandlingService)
        {
            _errorHandlingService = errorHandlingService;
            
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "TaskDockr");
            Directory.CreateDirectory(appFolder);
            
            _configFilePath = Path.Combine(appFolder, "config.json");
            _backupFolderPath = Path.Combine(appFolder, "backups");
            Directory.CreateDirectory(_backupFolderPath);
            
            _currentConfig = new AppConfig();
        }

        public async Task<AppConfig> LoadConfigAsync()
        {
            try
            {
                if (!File.Exists(_configFilePath))
                {
                    _currentConfig = CreateDefaultConfig();
                    await SaveConfigAsync(_currentConfig);
                    return _currentConfig;
                }

                var json = await File.ReadAllTextAsync(_configFilePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                
                var loadedConfig = JsonSerializer.Deserialize<AppConfig>(json, options);
                
                _currentConfig = loadedConfig ?? CreateDefaultConfig();
                return _currentConfig;
            }
            catch (System.Text.Json.JsonException ex)
            {
                await _errorHandlingService.HandleErrorAsync(ex, "Configuration file is corrupted. Creating backup and using default settings.");
                await BackupConfigAsync();
                _currentConfig = CreateDefaultConfig();
                await SaveConfigAsync(_currentConfig);
                return _currentConfig;
            }
            catch (Exception ex)
            {
                await _errorHandlingService.HandleErrorAsync(ex, "Failed to load configuration. Using default settings.");
                
                // Create backup of corrupted config
                await BackupConfigAsync();
                
                // Use default config
                _currentConfig = CreateDefaultConfig();
                await SaveConfigAsync(_currentConfig);
                return _currentConfig;
            }
        }

        public async Task SaveConfigAsync(AppConfig config)
        {
            try
            {
                var options = new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                
                var json = JsonSerializer.Serialize(config, options);
                await File.WriteAllTextAsync(_configFilePath, json);
                
                _currentConfig = config;
                
                // Auto-backup if enabled and time interval passed
                if (config.BackupEnabled && ShouldBackup(config))
                {
                    await BackupConfigAsync();
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                await _errorHandlingService.HandleErrorAsync(ex, "Access denied while saving configuration. Check file permissions.");
                throw;
            }
            catch (IOException ex)
            {
                await _errorHandlingService.HandleErrorAsync(ex, "I/O error occurred while saving configuration. The file may be in use.");
                throw;
            }
            catch (Exception ex)
            {
                await _errorHandlingService.HandleErrorAsync(ex, "Failed to save configuration.");
                throw;
            }
        }

        public async Task BackupConfigAsync()
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupPath = Path.Combine(_backupFolderPath, $"config_backup_{timestamp}.json");

                if (File.Exists(_configFilePath))
                {
                    File.Copy(_configFilePath, backupPath, true);

                    // Update last backup date directly without going through SaveConfigAsync
                    // (to avoid recursive backup triggering)
                    _currentConfig.LastBackupDate = DateTime.Now;
                    var options = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                    var json = JsonSerializer.Serialize(_currentConfig, options);
                    await File.WriteAllTextAsync(_configFilePath, json);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Backup failed: {ex.Message}");
            }
        }

        public async Task<bool> RestoreConfigAsync(string backupPath)
        {
            try
            {
                if (!File.Exists(backupPath))
                {
                    await _errorHandlingService.ShowErrorMessageAsync("Backup Not Found", "The specified backup file does not exist.");
                    return false;
                }

                var confirmed = await _errorHandlingService.ShowConfirmationAsync(
                    "Restore Configuration", 
                    "Are you sure you want to restore from backup? This will replace your current configuration.");
                
                if (!confirmed)
                    return false;

                // Create backup of current config before restore
                await BackupConfigAsync();
                
                File.Copy(backupPath, _configFilePath, true);
                
                // Reload the restored config
                await LoadConfigAsync();
                await _errorHandlingService.ShowSuccessMessageAsync("Configuration Restored", "Configuration successfully restored from backup.");
                return true;
            }
            catch (Exception ex)
            {
                await _errorHandlingService.HandleErrorAsync(ex, "Failed to restore configuration from backup.");
                return false;
            }
        }

        public async Task<bool> MigrateConfigAsync(string oldVersion)
        {
            try
            {
                // Handle different migration scenarios based on version
                switch (oldVersion)
                {
                    case "1.0.0":
                        // No migration needed for same version
                        return true;
                        
                    // Future version migrations will be added here
                    default:
                        // For unknown versions, create backup and use default
                        await BackupConfigAsync();
                        await _errorHandlingService.ShowErrorMessageAsync("Migration Failed", $"Configuration migration from version '{oldVersion}' is not supported.");
                        return false;
                }
            }
            catch (Exception ex)
            {
                await _errorHandlingService.HandleErrorAsync(ex, $"Failed to migrate configuration from version '{oldVersion}'.");
                return false;
            }
        }

        public void AddRecentFile(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                    return;

                // Remove if already exists
                _currentConfig.RecentFiles.Remove(filePath);
                
                // Add to beginning
                _currentConfig.RecentFiles.Insert(0, filePath);
                
                // Keep only last 10 files
                if (_currentConfig.RecentFiles.Count > 10)
                {
                    _currentConfig.RecentFiles = _currentConfig.RecentFiles.Take(10).ToList();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to add recent file: {ex.Message}");
            }
        }

        public void AddRecentFolder(string folderPath)
        {
            try
            {
                if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
                    return;

                // Remove if already exists
                _currentConfig.RecentFolders.Remove(folderPath);
                
                // Add to beginning
                _currentConfig.RecentFolders.Insert(0, folderPath);
                
                // Keep only last 10 folders
                if (_currentConfig.RecentFolders.Count > 10)
                {
                    _currentConfig.RecentFolders = _currentConfig.RecentFolders.Take(10).ToList();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to add recent folder: {ex.Message}");
            }
        }

        public void ClearRecentFiles()
        {
            _currentConfig.RecentFiles.Clear();
        }

        public void ClearRecentFolders()
        {
            _currentConfig.RecentFolders.Clear();
        }

        private AppConfig CreateDefaultConfig()
        {
            return new AppConfig
            {
                Version = "1.0.0",
                Theme = ThemePreference.Auto,
                WindowSettings = new WindowSettings
                {
                    Width = 800,
                    Height = 600
                },
                Startup = new StartupSettings
                {
                    MinimizeToTray = true,
                    RestorePreviousSession = true,
                    CheckForUpdates = true
                },
                GroupPreferences = new GroupDisplayPreferences
                {
                    IconSize = 32,
                    ShowGroupNames = true,
                    AutoArrangeGroups = true,
                    GroupSpacing = 20,
                    AnimationEnabled = true
                },
                Groups = new List<Group>()
            };
        }

        private bool ShouldBackup(AppConfig config)
        {
            if (config.LastBackupDate == null)
                return true;
                
            var timeSinceLastBackup = DateTime.Now - config.LastBackupDate.Value;
            return timeSinceLastBackup.TotalDays >= config.BackupIntervalDays;
        }
    }
}