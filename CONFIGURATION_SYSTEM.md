# TaskDockr Configuration System

## Overview

The TaskDockr configuration system provides a comprehensive settings management solution that stores application preferences in the Windows AppData folder. The system supports automatic backup, migration, and error recovery.

## Architecture

### Core Components

1. **AppConfig Model** (`Models/AppConfig.cs`)
   - Main configuration container with versioning
   - Supports theme preferences, window settings, startup options
   - Group display preferences and recent items tracking
   - Backup configuration settings

2. **ConfigurationService** (`Services/ConfigurationService.cs`)
   - Handles loading/saving configuration from `%APPDATA%\TaskDockr\config.json`
   - Automatic backup functionality
   - Migration support for future updates
   - Recent files/folders management

3. **WindowManagerService** (`Services/WindowManagerService.cs`)
   - Manages window positioning and sizing
   - Ensures windows stay within screen bounds
   - Centers windows automatically when needed

### Integration Points

- **MainViewModel**: Loads configuration on startup and applies theme settings
- **App.xaml.cs**: Applies window settings on launch and saves on close
- **TaskService**: Uses similar AppData storage pattern

## Configuration Structure

```json
{
  "version": "1.0.0",
  "theme": "auto",
  "windowSettings": {
    "left": 100,
    "top": 50,
    "width": 800,
    "height": 600,
    "isMaximized": false,
    "isMinimized": false
  },
  "startup": {
    "launchOnSystemStartup": false,
    "minimizeToTray": true,
    "restorePreviousSession": true,
    "checkForUpdates": true
  },
  "groupPreferences": {
    "iconSize": 32,
    "showGroupNames": true,
    "autoArrangeGroups": true,
    "groupSpacing": 20,
    "animationEnabled": true
  },
  "recentFiles": [],
  "recentFolders": [],
  "lastBackupDate": "2024-01-01T12:00:00",
  "backupEnabled": true,
  "backupIntervalDays": 7
}
```

## Key Features

### 1. AppData Storage
- Configuration stored in `%APPDATA%\TaskDockr\config.json`
- Automatic directory creation
- Error handling for corrupted files

### 2. Automatic Backup
- Creates timestamped backups in `%APPDATA%\TaskDockr\backups\`
- Configurable backup intervals
- Automatic backup before restore operations

### 3. Migration Support
- Version-based migration system
- Handles configuration format changes
- Creates backups before migration attempts

### 4. Error Recovery
- Graceful handling of corrupted configuration files
- Automatic fallback to default settings
- Backup creation before resetting

### 5. Recent Items Tracking
- Maintains lists of recently accessed files and folders
- Automatic cleanup (max 10 items each)
- Duplicate prevention

## Usage Examples

### Loading Configuration
```csharp
var configService = new ConfigurationService();
var config = await configService.LoadConfigAsync();
```

### Saving Configuration
```csharp
config.Theme = ThemePreference.Dark;
await configService.SaveConfigAsync(config);
```

### Managing Recent Items
```csharp
configService.AddRecentFile("C:\path\to\file.txt");
configService.ClearRecentFiles();
```

### Window Management
```csharp
var windowManager = new WindowManagerService();
windowManager.ApplyWindowSettings(window, config.WindowSettings);
```

## Integration with Existing Services

The configuration system seamlessly integrates with:

- **GroupService**: Uses similar AppData storage pattern
- **Window Management**: Applies saved window positions and sizes
- **Theme System**: Supports dark/light/auto theme preferences
- **Startup Behavior**: Configures launch and session restoration

## Error Handling

The system includes comprehensive error handling:

1. **File Corruption**: Creates backup and resets to defaults
2. **Migration Failures**: Falls back to default configuration
3. **Invalid Window Positions**: Centers windows automatically
4. **Missing Directories**: Creates required folders automatically

## Future Extensibility

The configuration system is designed for easy extension:

1. **New Settings**: Add properties to `AppConfig` model
2. **Migration Paths**: Add version-specific migration logic
3. **Storage Backends**: Implement alternative storage providers
4. **Validation**: Add configuration validation rules

## Files Created/Modified

### New Files
- `Models/AppConfig.cs` - Configuration data model
- `Services/ConfigurationService.cs` - Main configuration service
- `Services/WindowManagerService.cs` - Window management service
- `CONFIGURATION_SYSTEM.md` - This documentation

### Modified Files
- `ViewModels/MainViewModel.cs` - Integrated configuration loading
- `App.xaml.cs` - Window settings management

## Configuration Paths

- **Main Config**: `%APPDATA%\TaskDockr\config.json`
- **Backups**: `%APPDATA%\TaskDockr\backups\config_backup_YYYYMMDD_HHMMSS.json`
- **Groups Data**: `%APPDATA%\TaskDockr\groups.json` (existing)

This configuration system provides a robust foundation for TaskDockr's settings management with built-in reliability features and seamless integration with the existing architecture.