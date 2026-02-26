using Microsoft.Win32;
using System.Diagnostics;
using System.Reflection;
using TaskDockr.Models;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Security.Principal;
using System;

namespace TaskDockr.Services
{
    public interface IStartupService
    {
        bool IsAutoStartEnabled { get; }
        Task<bool> SetAutoStartAsync(bool enable);
        Task<bool> IsRunningAsAdminAsync();
        Task<bool> RequestAdminPrivilegesAsync();
        Task InitializeBackgroundOperationsAsync();
        Task CleanupBackgroundOperationsAsync();
        Task StartPerformanceMonitoringAsync();
        Task StopPerformanceMonitoringAsync();
        PerformanceMetrics GetPerformanceMetrics();
        Task<bool> OptimizeMemoryAsync();
        Task InitializeAsync(AppConfig config);
        Task ShutdownAsync();
    }

    public class StartupService : IStartupService
    {
        private readonly IConfigurationService _configService;
        private readonly IIconService _iconService;
        private Timer _monitoringTimer;
        private Timer _autoSaveTimer;
        private Timer _configMonitorTimer;
        private PerformanceMetrics _currentMetrics;
        private bool _isMonitoring;

        public bool IsAutoStartEnabled => CheckAutoStartRegistry();

        public StartupService(IConfigurationService configService, IIconService iconService)
        {
            _configService = configService;
            _iconService = iconService;
            _currentMetrics = new PerformanceMetrics();
        }

        public async Task<bool> SetAutoStartAsync(bool enable)
        {
            if (!await IsRunningAsAdminAsync())
            {
                if (!await RequestAdminPrivilegesAsync())
                    return false;
            }

            try
            {
                var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                if (key == null) return false;

                var appName = "TaskDockr";
                var exePath = Process.GetCurrentProcess().MainModule.FileName;

                if (enable)
                {
                    key.SetValue(appName, $"\"{exePath}\" -minimized");
                }
                else
                {
                    key.DeleteValue(appName, false);
                }

                key.Close();
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"Failed to set auto-start: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> IsRunningAsAdminAsync()
        {
            return await Task.Run(() =>
            {
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            });
        }

        public async Task<bool> RequestAdminPrivilegesAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var exePath = Process.GetCurrentProcess().MainModule.FileName;
                    var startInfo = new ProcessStartInfo
                    {
                        UseShellExecute = true,
                        WorkingDirectory = Environment.CurrentDirectory,
                        FileName = exePath,
                        Verb = "runas"
                    };

                    var process = Process.Start(startInfo);
                    return process != null;
                }
                catch
                {
                    return false;
                }
            });
        }

        public async Task InitializeBackgroundOperationsAsync()
        {
            var config = await _configService.LoadConfigAsync();
            
            // Initialize icon caching
            await InitializeIconCachingAsync();
            
            // Start configuration monitoring
            await StartConfigMonitoringAsync();
            
            // Start auto-save functionality
            await StartAutoSaveAsync();
            
            // Start performance monitoring if enabled
            if (config.Startup.CheckForUpdates)
            {
                await StartPerformanceMonitoringAsync();
            }
        }

        public async Task CleanupBackgroundOperationsAsync()
        {
            await StopPerformanceMonitoringAsync();
            
            _monitoringTimer?.Dispose();
            _autoSaveTimer?.Dispose();
            _configMonitorTimer?.Dispose();
            
            await ClearIconCacheAsync();
        }

        public async Task StartPerformanceMonitoringAsync()
        {
            if (_isMonitoring) return;
            
            _isMonitoring = true;
            _monitoringTimer = new Timer(async _ => await UpdatePerformanceMetricsAsync(), 
                null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
        }

        public async Task StopPerformanceMonitoringAsync()
        {
            _isMonitoring = false;
            _monitoringTimer?.Dispose();
            _monitoringTimer = null;
        }

        public PerformanceMetrics GetPerformanceMetrics()
        {
            return _currentMetrics;
        }

        public async Task<bool> OptimizeMemoryAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Clear icon cache if too large
                    if (_iconService.GetCacheSize() > 100)
                    {
                        _iconService.ClearIconCacheAsync().Wait();
                    }
                    
                    // Force garbage collection
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    
                    return true;
                }
                catch
                {
                    return false;
                }
            });
        }

        public async Task InitializeAsync(AppConfig config)
        {
            // Set auto-start if configured
            if (config.Startup.LaunchOnSystemStartup)
            {
                await SetAutoStartAsync(true);
            }

            // Initialize background operations
            if (config.Startup.BackgroundOperationsEnabled)
            {
                await InitializeBackgroundOperationsAsync();
            }

            // Start performance monitoring
            if (config.Startup.PerformanceMonitoringEnabled)
            {
                await StartPerformanceMonitoringAsync();
            }
        }

        public async Task ShutdownAsync()
        {
            await CleanupBackgroundOperationsAsync();
            await StopPerformanceMonitoringAsync();
            
            // Performance counters not used in this build
        }

        private bool CheckAutoStartRegistry()
        {
            try
            {
                var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run");
                if (key == null) return false;

                var value = key.GetValue("TaskDockr");
                key.Close();

                return value != null;
            }
            catch
            {
                return false;
            }
        }

        private async Task InitializeIconCachingAsync()
        {
            // Pre-cache frequently used icons
            var config = _configService.CurrentConfig;
            
            if (config.Groups != null)
            {
                foreach (var group in config.Groups)
                {
                    if (!string.IsNullOrEmpty(group.IconPath))
                    {
                        try
                        {
                            await _iconService.LoadIconAsync(group.IconPath, config.GroupPreferences.IconSize);
                        }
                        catch
                        {
                            // Ignore caching failures
                        }
                    }
                }
            }
        }

        private async Task StartConfigMonitoringAsync()
        {
            var configFile = GetConfigFilePath();
            var lastWriteTime = File.GetLastWriteTime(configFile);
            
            _configMonitorTimer = new Timer(async _ =>
            {
                var currentWriteTime = File.GetLastWriteTime(configFile);
                if (currentWriteTime > lastWriteTime)
                {
                    lastWriteTime = currentWriteTime;
                    await OnConfigFileChangedAsync();
                }
            }, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        }

        private async Task StartAutoSaveAsync()
        {
            _autoSaveTimer = new Timer(async _ =>
            {
                await AutoSaveConfigAsync();
            }, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        private async Task AutoSaveConfigAsync()
        {
            try
            {
                var config = _configService.CurrentConfig;
                if (config != null)
                {
                    await _configService.SaveConfigAsync(config);
                }
            }
            catch
            {
                // Ignore auto-save failures
            }
        }

        private async Task OnConfigFileChangedAsync()
        {
            try
            {
                await _configService.LoadConfigAsync();
                // TODO: Notify UI components about config changes
            }
            catch
            {
                // Ignore config reload failures
            }
        }

        private async Task UpdatePerformanceMetricsAsync()
        {
            try
            {
                var process = Process.GetCurrentProcess();
                
                _currentMetrics.MemoryUsageMB = process.WorkingSet64 / (1024 * 1024);
                _currentMetrics.CpuUsagePercent = 0; // PerformanceCounter not available on this platform
                _currentMetrics.ThreadCount = process.Threads.Count;
                _currentMetrics.HandleCount = process.HandleCount;
                _currentMetrics.IconCacheSize = _iconService.GetCacheSize();
                _currentMetrics.IconCacheHitRate = _iconService.GetCacheHitRate();
                
                // Trigger memory optimization if usage is high
                if (_currentMetrics.MemoryUsageMB > 100)
                {
                    await OptimizeMemoryAsync();
                }
            }
            catch
            {
                // Ignore performance monitoring failures
            }
        }

        private async Task ClearIconCacheAsync()
        {
            try
            {
                await _iconService.ClearIconCacheAsync();
            }
            catch
            {
                // Ignore cache clearing failures
            }
        }

        private string GetConfigFilePath()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appDataPath, "TaskDockr", "config.json");
        }
    }

    public class PerformanceMetrics
    {
        public double MemoryUsageMB { get; set; }
        public double CpuUsagePercent { get; set; }
        public int ThreadCount { get; set; }
        public int HandleCount { get; set; }
        public int IconCacheSize { get; set; }
        public int IconCacheHitRate { get; set; }
        public DateTime LastUpdate { get; set; } = DateTime.Now;
    }
}