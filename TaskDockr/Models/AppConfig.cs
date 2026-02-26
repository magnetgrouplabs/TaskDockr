using System.Text.Json.Serialization;
using System;
using System.Collections.Generic;

namespace TaskDockr.Models
{
    public class AppConfig
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0.0";
        
        [JsonPropertyName("theme")]
        public ThemePreference Theme { get; set; } = ThemePreference.Auto;
        
        [JsonPropertyName("windowSettings")]
        public WindowSettings WindowSettings { get; set; } = new WindowSettings();
        
        [JsonPropertyName("startup")]
        public StartupSettings Startup { get; set; } = new StartupSettings();
        
        [JsonPropertyName("groupPreferences")]
        public GroupDisplayPreferences GroupPreferences { get; set; } = new GroupDisplayPreferences();
        
        [JsonPropertyName("groups")]
        public List<Group> Groups { get; set; } = new List<Group>();
        
        [JsonPropertyName("recentFiles")]
        public List<string> RecentFiles { get; set; } = new List<string>();
        
        [JsonPropertyName("recentFolders")]
        public List<string> RecentFolders { get; set; } = new List<string>();
        
        [JsonPropertyName("lastBackupDate")]
        public DateTime? LastBackupDate { get; set; }
        
        [JsonPropertyName("backupEnabled")]
        public bool BackupEnabled { get; set; } = true;
        
        [JsonPropertyName("backupIntervalDays")]
        public int BackupIntervalDays { get; set; } = 7;
    }
    
    public enum ThemePreference
    {
        [JsonPropertyName("dark")]
        Dark,
        [JsonPropertyName("light")]
        Light,
        [JsonPropertyName("auto")]
        Auto
    }
    
    public class WindowSettings
    {
        [JsonPropertyName("left")]
        public double Left { get; set; }
        
        [JsonPropertyName("top")]
        public double Top { get; set; }
        
        [JsonPropertyName("width")]
        public double Width { get; set; } = 800;
        
        [JsonPropertyName("height")]
        public double Height { get; set; } = 600;
        
        [JsonPropertyName("isMaximized")]
        public bool IsMaximized { get; set; }
        
        [JsonPropertyName("isMinimized")]
        public bool IsMinimized { get; set; }
    }
    
    public class StartupSettings
    {
        [JsonPropertyName("launchOnSystemStartup")]
        public bool LaunchOnSystemStartup { get; set; }
        
        [JsonPropertyName("minimizeToTray")]
        public bool MinimizeToTray { get; set; } = true;
        
        [JsonPropertyName("restorePreviousSession")]
        public bool RestorePreviousSession { get; set; } = true;
        
        [JsonPropertyName("checkForUpdates")]
        public bool CheckForUpdates { get; set; } = true;
        
        [JsonPropertyName("backgroundOperationsEnabled")]
        public bool BackgroundOperationsEnabled { get; set; } = true;
        
        [JsonPropertyName("autoSaveEnabled")]
        public bool AutoSaveEnabled { get; set; } = true;
        
        [JsonPropertyName("performanceMonitoringEnabled")]
        public bool PerformanceMonitoringEnabled { get; set; } = false;
        
        [JsonPropertyName("memoryOptimizationEnabled")]
        public bool MemoryOptimizationEnabled { get; set; } = true;
        
        [JsonPropertyName("trayIconPath")]
        public string TrayIconPath { get; set; }
        
        [JsonPropertyName("singleInstanceEnforced")]
        public bool SingleInstanceEnforced { get; set; } = true;
    }
    
    public class GroupDisplayPreferences
    {
        [JsonPropertyName("iconSize")]
        public int IconSize { get; set; } = 32;
        
        [JsonPropertyName("showGroupNames")]
        public bool ShowGroupNames { get; set; } = true;
        
        [JsonPropertyName("autoArrangeGroups")]
        public bool AutoArrangeGroups { get; set; } = true;
        
        [JsonPropertyName("groupSpacing")]
        public int GroupSpacing { get; set; } = 20;
        
        [JsonPropertyName("animationEnabled")]
        public bool AnimationEnabled { get; set; } = true;
    }
}