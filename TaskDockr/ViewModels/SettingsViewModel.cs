using System;
using System.IO;
using System.Threading.Tasks;
using TaskDockr.Models;
using TaskDockr.Services;
using TaskDockr.Utils;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace TaskDockr.ViewModels
{
    public class SettingsViewModel : ObservableObject
    {
        private readonly IConfigurationService _configService;
        private readonly IBackupManager _backupManager;
        
        private AppConfig _config;
        public AppConfig Config
        {
            get => _config;
            set
            {
                if (SetProperty(ref _config, value))
                {
                    // Update SelectedThemeIndex when Config changes
                    OnPropertyChanged(nameof(SelectedThemeIndex));
                }
            }
        }

        public int SelectedThemeIndex
        {
            get => (int)(_config?.Theme ?? ThemePreference.Auto);
            set
            {
                if (_config != null && (int)_config.Theme != value)
                {
                    _config.Theme = (ThemePreference)value;
                    OnPropertyChanged();
                }
            }
        }

        private ObservableCollection<string> _availableBackups;
        public ObservableCollection<string> AvailableBackups
        {
            get => _availableBackups;
            set => SetProperty(ref _availableBackups, value);
        }

        private string _selectedBackup;
        public string SelectedBackup
        {
            get => _selectedBackup;
            set => SetProperty(ref _selectedBackup, value);
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ICommand SaveSettingsCommand { get; }
        public ICommand RestoreDefaultsCommand { get; }
        public ICommand CreateBackupCommand { get; }
        public ICommand RestoreBackupCommand { get; }
        public ICommand CleanupBackupsCommand { get; }
        public ICommand RefreshBackupsCommand { get; }

        public SettingsViewModel()
        {
            _configService = App.GetService<IConfigurationService>();
            _backupManager = new BackupManager();
            _config = new AppConfig();
            _availableBackups = new ObservableCollection<string>();
            
            SaveSettingsCommand = new RelayCommand(async () => await SaveSettingsAsync());
            RestoreDefaultsCommand = new RelayCommand(async () => await RestoreDefaultsAsync());
            CreateBackupCommand = new RelayCommand(async () => await CreateBackupAsync());
            RestoreBackupCommand = new RelayCommand(async () => await RestoreBackupAsync());
            CleanupBackupsCommand = new RelayCommand(async () => await CleanupBackupsAsync());
            RefreshBackupsCommand = new RelayCommand(async () => await RefreshBackupsAsync());
            
            LoadSettingsAsync();
        }

        private async void LoadSettingsAsync()
        {
            try
            {
                Config = await _configService.LoadConfigAsync();
                await RefreshBackupsAsync();
                StatusMessage = "Settings loaded successfully";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to load settings: {ex.Message}";
            }
        }

        private async Task SaveSettingsAsync()
        {
            try
            {
                await _configService.SaveConfigAsync(Config);
                StatusMessage = "Settings saved successfully";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to save settings: {ex.Message}";
            }
        }

        private async Task RestoreDefaultsAsync()
        {
            try
            {
                // Create backup before restoring defaults
                await _backupManager.CreateManualBackupAsync();
                
                Config = new AppConfig();
                await _configService.SaveConfigAsync(Config);
                
                StatusMessage = "Default settings restored";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to restore defaults: {ex.Message}";
            }
        }

        private async Task CreateBackupAsync()
        {
            try
            {
                var backupPath = await _backupManager.CreateManualBackupAsync();
                if (!string.IsNullOrEmpty(backupPath))
                {
                    StatusMessage = $"Backup created: {Path.GetFileName(backupPath)}";
                    await RefreshBackupsAsync();
                }
                else
                {
                    StatusMessage = "Failed to create backup";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to create backup: {ex.Message}";
            }
        }

        private async Task RestoreBackupAsync()
        {
            if (string.IsNullOrEmpty(SelectedBackup))
            {
                StatusMessage = "Please select a backup to restore";
                return;
            }

            try
            {
                var success = await _backupManager.RestoreBackupAsync(SelectedBackup);
                if (success)
                {
                    // Reload settings
                    Config = await _configService.LoadConfigAsync();
                    StatusMessage = "Backup restored successfully";
                }
                else
                {
                    StatusMessage = "Failed to restore backup";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to restore backup: {ex.Message}";
            }
        }

        private async Task CleanupBackupsAsync()
        {
            try
            {
                await _backupManager.CleanupOldBackupsAsync();
                await RefreshBackupsAsync();
                StatusMessage = "Old backups cleaned up";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to cleanup backups: {ex.Message}";
            }
        }

        private async Task RefreshBackupsAsync()
        {
            try
            {
                var backups = await _backupManager.GetAvailableBackupsAsync();
                AvailableBackups = new ObservableCollection<string>(backups);
                StatusMessage = $"Found {backups.Count} backups";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to refresh backups: {ex.Message}";
            }
        }

        public void AddRecentFile(string filePath)
        {
            _configService.AddRecentFile(filePath);
        }

        public void AddRecentFolder(string folderPath)
        {
            _configService.AddRecentFolder(folderPath);
        }

        public void ClearRecentFiles()
        {
            _configService.ClearRecentFiles();
            Config.RecentFiles.Clear();
        }

        public void ClearRecentFolders()
        {
            _configService.ClearRecentFolders();
            Config.RecentFolders.Clear();
        }
    }
}