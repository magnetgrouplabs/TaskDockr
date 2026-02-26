# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What is TaskDockr

A Windows 11 desktop utility that organizes and launches application shortcuts, files, URLs, and
folders in named groups. **Each group appears as a separate taskbar icon** that, when clicked, shows
a popup with that group's shortcuts.

**IMPORTANT: This is a TASKBAR-ONLY application.** No system tray. Ported from WinUI 3 to WPF +
WPF-UI (Lepo) in February 2026.

---

## Build & Run

**Prerequisites:** .NET 8.0 SDK, Windows 10 build 17763+

```bash
# Build
dotnet build TaskDockr/TaskDockr.csproj --configuration Debug

# Run (use executable directly — dotnet run appears to hang for GUI apps)
"TaskDockr/bin/Debug/net8.0-windows/TaskDockr.exe"

# Tests (may fail due to missing VS components — environment issue, not code)
dotnet test TaskDockr.UnitTests/
dotnet test TaskDockr.Tests/
```

---

## Solution Layout

```
TaskDockr/                    # Main WPF application (net8.0-windows, UseWPF=true)
  Models/                     # Group, Shortcut, AppConfig
  ViewModels/                 # MainViewModel, PopoutViewModel, SettingsViewModel, etc.
  Views/                      # XAML Windows and UserControls
  Services/                   # All business logic services
  Utils/                      # ObservableObject base class
TaskDockr.Tests/              # Integration tests (xUnit, Moq)
TaskDockr.UnitTests/          # Unit tests (xUnit, Moq)
TaskDockr.IntegrationTests/   # Integration tests
TaskDockr.PerformanceTests/   # Performance tests
TaskDockr.UITests/            # UI tests
```

Test project folders are stubs — all production source lives under `TaskDockr/`.

---

## Architecture

MVVM with constructor-injected services. `App.xaml.cs` builds the DI container via
`ServiceManager.AddTaskDockrServices()`, and `App.GetService<T>()` is the static accessor.

### Core Flow

1. `App.xaml.cs` → builds DI container → creates `MainWindow`
2. `MainWindow` has sidebar (groups list) + `ContentControl` for navigation
3. `NavigationService` swaps `UserControl` content (no WPF `Frame`/`Page`)
4. Each group gets a hidden WPF Window via `TaskbarService` → appears as taskbar icon
5. Clicking taskbar icon → `PopoutWindow` appears edge-aligned near the icon
6. `PopoutWindow` closes on `Deactivated` event

### Key Services

| Interface | Implementation | Responsibility |
|-----------|---------------|----------------|
| `IConfigurationService` | `ConfigurationService` | Load/save `AppConfig` as JSON |
| `IGroupService` | `GroupService` | CRUD + reorder for `Group` objects |
| `IShortcutService` | `ShortcutService` | CRUD + launch + reorder for `Shortcut` objects |
| `IIconService` | `EnhancedIconService` | Icon loading, extraction, caching |
| `ITaskbarService` | `TaskbarService` | Creates taskbar windows for groups, popup positioning |
| `IPopoutService` | `PopoutService` | Show/hide/toggle `PopoutWindow` |
| `INavigationService` | `NavigationService` | Swaps `UserControl` content in `ContentControl` |

### Data Persistence

All data lives in `%APPDATA%\TaskDockr\`:
- `config.json` — single source of truth (camelCase JSON), contains groups + shortcuts + settings
- `backups/` — automatic config backups
- `crash.log`, `startup.log` — diagnostics

---

## Critical Dependency Rules

**Circular dependency: ConfigurationService ↔ ErrorHandlingService**
`ErrorHandlingService` resolves `IConfigurationService` **lazily** via `App.GetService<>()` (not
constructor injection) to break the cycle. **Do not** add `IConfigurationService` to
`ErrorHandlingService`'s constructor.

**Circular dependency: TaskbarService ↔ GroupService**
`GroupService` raises events that `TaskbarService` subscribes to (event-driven pattern). **Do not**
have `GroupService` call `App.GetService<ITaskbarService>()` directly.

---

## Key Conventions

- **Data binding:** Use `{Binding}` (WPF). `{x:Bind}` is WinUI only and will not compile.
- **Theme resources:** Use `{DynamicResource <key>}` for WPF-UI brush keys.
  Common keys: `TextFillColorPrimaryBrush`, `TextFillColorSecondaryBrush`,
  `CardBackgroundFillColorDefaultBrush`, `SystemAccentColorBrush`,
  `ControlStrokeColorDefaultBrush`, `ApplicationBackgroundBrush`
- **CornerRadius:** Works on `Border` and WPF-UI controls. Standard WPF `Button`/`TextBox` do NOT
  support `CornerRadius` — use WPF-UI variants or wrap in a `Border`.
- **Hover effects:** Use `Style.Triggers` with `IsMouseOver`/`IsPressed` (WPF). WinUI
  `VisualStateManager` `PointerOver` state does not exist in WPF.
- **Services:** Always access via `App.GetService<T>()`. Never `new` a service directly.
- **Navigation:** To add a new view: create a `UserControl`, add to `NavigationTarget` enum in
  `NavigationService.cs`, add a case to `CreateViewForTarget()`.
- **View DataContext:** `GroupManagementView` and `ShortcutManagementView` get their `DataContext`
  set by `NavigationService.CreateViewForTarget()` — do NOT set `DataContext` in their XAML.
- **Dialogs:** WPF `Window` subclasses shown via `ShowDialog()`. Set
  `WindowStartupLocation="CenterOwner"` and `window.Owner = Window.GetWindow(this)`.
- **Theme switching:** `Wpf.Ui.Appearance.ApplicationThemeManager.Apply(ApplicationTheme.Dark|Light)`
- **RelayCommand:** Uses `CommandManager.RequerySuggested` for `CanExecuteChanged` — defined in
  `MainViewModel.cs`.

---

## Taskbar Integration

Each group gets a minimized WPF Window (`TaskbarService.CreateTaskbarWindowForGroup`) that shows as
a taskbar icon. Popup positioning uses user32.dll P/Invoke APIs to find exact taskbar button
coordinates, with a fallback to work-area-based estimation.

Key files: `TaskbarService.cs` (positioning + window creation), `PopoutService.cs` (show/hide logic)

**Current limitation:** Positioning uses primary screen only; DPI-aware via `SystemParameters` but
raw Windows API calls return physical pixels.

---

## Known Issues

1. **Popup positioning offset** — Popups appear offset from taskbar button instead of edge-aligned.
   Exact button detection via `EnumChildWindows` is failing; fallback approximate positioning has
   horizontal offset. Key files: `TaskbarService.cs`, `PopoutService.cs`
2. **Group creation validation** — Form opens but validation flow may be broken.
   Check `GroupFormViewModel.ValidateForm()` and `IsValid` property.
3. **Theme switching persistence** — Changes may not persist across restart.
   Check `App.xaml.cs:ApplyTheme()` and `SettingsView.xaml.cs:OnThemeChanged()`.
4. **Test projects** — May fail to build due to missing VS component
   (`Microsoft.Build.Packaging.Pri.Tasks.dll`), unrelated to app code.
