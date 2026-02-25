# TaskDockr

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![WPF](https://img.shields.io/badge/WPF-Windows%20Presentation%20Foundation-0078D4?style=flat-square&logo=windows)](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/)
[![WPF-UI](https://img.shields.io/badge/WPF--UI-3.0.5-512BD4?style=flat-square)](https://github.com/lepoco/wpfui)
[![Platform](https://img.shields.io/badge/Platform-Windows%2011-0078D4?style=flat-square&logo=windows11)](https://www.microsoft.com/windows/windows-11)
[![License](https://img.shields.io/badge/License-MIT-green.svg?style=flat-square)](LICENSE)
[![Beta](https://img.shields.io/badge/Status-Beta-orange?style=flat-square)]()

> **A Windows 11 desktop utility that organizes and launches application shortcuts, files, URLs, and folders in named groups. Each group appears as a separate taskbar icon.**

![TaskDockr Screenshot](docs/screenshot.png)

---

## ⚠️ Beta Disclaimer

**TaskDockr is currently in beta.** This software is actively under development and may contain bugs or incomplete features. Beta releases are intended for testing and feedback purposes.

- **Backup your data regularly** - Configuration is stored in `%APPDATA%\TaskDockr\config.json`
- **Expect changes** - Features and UI may change between releases
- **Report issues** - Please report bugs via [GitHub Issues](https://github.com/magnetgrouplabs/TaskDockr/issues)

---

## Features

- **Named Groups** - Organize shortcuts into custom named groups
- **Taskbar Integration** - Each group appears as its own taskbar icon
- **Smart Popups** - Click taskbar icons to reveal shortcuts in edge-aligned popups
- **Drag & Drop** - Add shortcuts by dragging from Windows Explorer
- **Multiple Types** - Support for Applications, Files, URLs, and Folders
- **Windows 11 Fluent Design** - Modern UI with WPF-UI (Lepo) theming
- **Dark/Light Theme** - Automatic and manual theme switching
- **Auto-Start** - Optionally start with Windows

---

## System Requirements

- **OS**: Windows 10 build 17763+ or Windows 11
- **.NET**: .NET 8.0 Runtime
- **RAM**: 512 MB minimum
- **Disk**: 50 MB free space

---

## Installation

### Beta Releases

1. **Download** the latest beta release from the [Releases](https://github.com/magnetgrouplabs/TaskDockr/releases) page
2. **Extract** the ZIP file to your preferred location
3. **Run** `TaskDockr.exe`

### Building from Source

```bash
# Clone the repository
git clone https://github.com/magnetgrouplabs/TaskDockr.git
cd TaskDockr

# Build the project
dotnet build TaskDockr/TaskDockr.csproj --configuration Release

# Run the executable
.\TaskDockr\bin\Release\net8.0-windows\TaskDockr.exe
```

---

## Architecture

TaskDockr uses **WPF + WPF-UI** for a modern Windows 11 Fluent experience.

| Technology | Version | Purpose |
|------------|---------|---------|
| .NET | 8.0 | Core framework |
| WPF | net8.0-windows | UI framework |
| WPF-UI (Lepo) | 3.0.5 | Windows 11 Fluent controls |
| Dependency Injection | 8.0.0 | Service management |

### Architecture Pattern

**MVVM** with constructor-injected services:

- **Models** - `Group`, `Shortcut`, `AppConfig`
- **ViewModels** - `MainViewModel`, `PopoutViewModel`, `SettingsViewModel`
- **Views** - XAML UserControls and Windows
- **Services** - Configuration, Group, Shortcut, Taskbar, Icon services

---

## Data Storage

Configuration is stored in:
- **Config**: `%APPDATA%\TaskDockr\config.json`
- **Backups**: `%APPDATA%\TaskDockr\backups\`
- **Crash logs**: `%APPDATA%\TaskDockr\crash.log`

---

## Usage

1. **Create a Group** - Click "Add Group" and give it a name
2. **Add Shortcuts** - Use the "+" button or drag & drop from Explorer
3. **Launch** - Click the taskbar icon to reveal shortcuts
4. **Launch Items** - Click any shortcut in the popup to launch it

---

## Development

### Project Structure

```
TaskDockr/
├── Models/                 # Data models
│   ├── Group.cs
│   └── Shortcut.cs
├── ViewModels/            # MVVM ViewModels
│   ├── MainViewModel.cs
│   ├── PopoutViewModel.cs
│   └── SettingsViewModel.cs
├── Views/                 # UI Views
│   ├── MainWindow.xaml
│   ├── PopoutWindow.xaml
│   └── SettingsView.xaml
├── Services/              # Business logic
│   ├── ConfigurationService.cs
│   ├── GroupService.cs
│   └── TaskbarService.cs
└── Utils/                 # Utilities
    └── ObservableObject.cs
```

### Running Tests

```bash
dotnet test TaskDockr.Tests/
dotnet test TaskDockr.UnitTests/
```

---

## Contributing

Contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a [Pull Request](https://github.com/magnetgrouplabs/TaskDockr/pulls)

---

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## Acknowledgments

- [WPF-UI (Lepo)](https://github.com/lepoco/wpfui) - Modern WPF controls
- Windows App SDK team - For the original WinUI 3 foundation (port completed February 2026)

---

<p align="center">
  Made with ❤️ by <a href="https://github.com/magnetgrouplabs">magnetgrouplabs</a>
</p>
