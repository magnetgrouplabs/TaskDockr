# TaskDockr

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![WPF](https://img.shields.io/badge/WPF-Windows%20Presentation%20Foundation-0078D4?style=flat-square&logo=windows)](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/)
[![WPF-UI](https://img.shields.io/badge/WPF--UI-3.0.5-512BD4?style=flat-square)](https://github.com/lepoco/wpfui)
[![Platform](https://img.shields.io/badge/Platform-Windows%2011-0078D4?style=flat-square&logo=windows11)](https://www.microsoft.com/windows/windows-11)
[![License](https://img.shields.io/badge/License-MIT-green.svg?style=flat-square)](LICENSE)
[![Beta](https://img.shields.io/badge/Status-Beta-orange?style=flat-square)]()

> **A Windows 11 desktop utility that organizes and launches application shortcuts, files, URLs, and folders in named groups. Each group can be pinned to the taskbar for one-click access.**

---

## Beta

**TaskDockr is currently in beta.** Expect bugs and breaking changes between releases.

- **Backup your data** — stored in `%APPDATA%\TaskDockr\config.json`
- **Report issues** — [GitHub Issues](https://github.com/magnetgrouplabs/TaskDockr/issues)

---

## Features

- **Named Groups** — Organize shortcuts into custom named groups
- **Pin to Taskbar** — Pin any group to the Windows taskbar; clicking it opens a compact popup with that group's shortcuts
- **Smart Popups** — Dynamically sized, edge-aligned popups positioned near the taskbar icon you clicked
- **Drag & Drop** — Add shortcuts by dragging files from Windows Explorer
- **Multiple Types** — Applications, Files, URLs, and Folders
- **Custom Group Icons** — Font Awesome glyphs with custom colors, or image files
- **Windows 11 Fluent Design** — Dark theme with WPF-UI (Lepo) controls
- **Single-Click Launch** — Click any shortcut tile to launch it immediately
- **Right-Click Context Menu** — Launch, edit, or delete shortcuts from the popup

---

## System Requirements

- **OS**: Windows 10 build 17763+ or Windows 11
- **.NET**: .NET 8.0 Runtime
- **RAM**: 512 MB minimum
- **Disk**: 50 MB free space

---

## Installation

### Download

1. Download the latest release from [Releases](https://github.com/magnetgrouplabs/TaskDockr/releases)
2. Extract the ZIP file
3. Run `TaskDockr.exe`

### Build from Source

```bash
git clone https://github.com/magnetgrouplabs/TaskDockr.git
cd TaskDockr
dotnet build TaskDockr/TaskDockr.csproj --configuration Release
./TaskDockr/bin/Release/net8.0-windows/TaskDockr.exe
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

1. **Create a Group** — Click "+" in the sidebar
2. **Add Shortcuts** — Use "Add Shortcut" button or drag & drop from Explorer
3. **Pin to Taskbar** — Click the pin icon next to a group, then pin the desktop shortcut to your taskbar
4. **Launch** — Click the pinned taskbar icon to open the group popup, then click any shortcut

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
