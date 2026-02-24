# TaskDockr Core Data Models

## Overview

TaskDockr has been refactored from a simple task management system to a Windows 11 utility application for managing application shortcuts in organized groups.

## Models

### Group Model
- **Id**: Unique identifier (auto-generated GUID)
- **Name**: Display name of the group
- **IconPath**: Path to group icon image
- **Shortcuts**: List of Shortcut objects belonging to this group
- **Position**: Order position for display sorting

### Shortcut Model
- **Id**: Unique identifier (auto-generated GUID)
- **Name**: Display name of the shortcut
- **TargetPath**: Path to executable, file, or URL
- **Arguments**: Command-line arguments (for applications)
- **IconPath**: Path to shortcut icon image
- **Type**: Shortcut type (App, File, URL)

## Shortcut Types
- **App**: Application shortcuts (.exe files)
- **File**: Document/file shortcuts
- **URL**: Web URL shortcuts

## Data Persistence

- Data is stored in `%APPDATA%\TaskDockr\groups.json`
- JSON serialization uses camelCase naming
- Automatic backup and restore functionality
- Sample data included for initial setup

## Sample Data Structure

```json
[
  {
    "id": "guid",
    "name": "Development",
    "iconPath": "ms-appx:///Assets/Icons/code.png",
    "position": 0,
    "shortcuts": [
      {
        "id": "guid",
        "name": "Visual Studio",
        "targetPath": "devenv.exe",
        "arguments": "",
        "iconPath": "",
        "type": "app"
      }
    ]
  }
]
```