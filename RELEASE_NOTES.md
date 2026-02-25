# Beta Release Notes

## Version 0.1.0-beta

## Summary
First public beta of TaskDockr — a Windows 11 desktop utility for organizing and launching application shortcuts, files, URLs, and folders in named groups. This release focuses on core functionality and stability.

## New Features
- **Group management** — Create, edit, delete, and reorder named shortcut groups
- **Shortcut management** — Add shortcuts of four types: App, File, URL, and Folder
- **System tray icon** — TaskDockr runs in the background via a system tray icon
- **Popout window** — A compact popup panel near the taskbar shows your groups and shortcuts
- **Drag-and-drop reordering** — Reorder groups and shortcuts by dragging
- **Per-group icon support** — Assign custom icons to groups
- **Persistent configuration** — Settings and groups are saved to `%APPDATA%\TaskDockr\config.json` with automatic backups
- **Single-instance enforcement** — Only one instance of TaskDockr runs at a time

## Known Issues
- **Icon rendering limitations** — Custom icon rendering from Fluent glyphs is stubbed in this release; a default app icon is shown instead. Full icon rendering will be available in a future update.
- **SVG icon support** — SVG icons are not rendered in this beta; the feature is deferred to a future release.
- **Taskbar icon detection** — The popout window position is calculated using a fixed 40 px taskbar height estimate. Accurate taskbar detection via Win32 API will be added in a future update.
- **Tray minimize/restore** — Minimize-to-tray and restore-from-tray are not yet functional; the app window must be closed via standard window controls.

## Performance Improvements
- Background icon processing queue prevents UI thread blocking during icon loading
- Icon caching reduces repeated disk/network reads

## Upgrade Instructions
1. Download the latest `.msix` package from the releases page.
2. If a previous beta is installed, uninstall it first via **Settings → Apps**.
3. Double-click the `.msix` package and follow the installation wizard.
4. Launch TaskDockr from the Start menu or system tray.

## Feedback
Please report bugs and feature requests at: <https://github.com/anthropics/claude-code/issues>

Include the following in your report:
- Windows version (`winver`)
- Steps to reproduce the issue
- Expected vs. actual behaviour
- Any relevant error messages from `%APPDATA%\TaskDockr\`
