# Release Notes

## v2026.2.1-beta

### Fixes
- **Popup positioning** — Popups now appear correctly aligned near the taskbar icon that was clicked. Fixed DPI scaling bug where cursor coordinates (physical pixels) were compared against DIP values.
- **Dynamic popup sizing** — Popup height now adjusts based on the number of shortcuts in the group instead of using a fixed 480px height.
- **Gear button** — Clicking the gear icon in the popup now opens the main TaskDockr window.

### Visual Improvements
- Shortcut icons in popups are larger (36x36, up from 28x28)
- Shortcut tiles no longer have borders or card backgrounds — cleaner, more modern look
- Subtle hover effect highlights tiles on mouseover
- Gear icon uses a lighter secondary color for better visibility
- Popup width reduced from 360px to 310px for a more compact appearance

### Internal
- Fixed `.gitignore` that was excluding all source code from version control
- Added position debug logging to `%APPDATA%\TaskDockr\position_debug.log`
