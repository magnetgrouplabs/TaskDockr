# TaskDockr Error Handling & Validation Test Scenarios

## Test Scenarios for Comprehensive Validation

### 1. Group Name Validation Tests

**Valid Names:**
- "Development" ✅
- "Productivity Tools" ✅
- "My_Group-123" ✅

**Invalid Names:**
- "" (empty) ❌ - Shows "Group name cannot be empty"
- "A" * 51 (51 characters) ❌ - Shows "Group name cannot exceed 50 characters"
- "Group@Special" ❌ - Shows "Group name can only contain letters, numbers, spaces, underscores, and hyphens"
- "Development" (duplicate) ❌ - Shows "Group name already exists"

### 2. File Path Validation Tests

**Valid Paths:**
- "C:\\Program Files\\app.exe" ✅
- ".\\relative\\path.exe" ✅
- "notepad.exe" (resolves from PATH) ✅

**Invalid Paths:**
- "" (empty) ❌ - Shows "File path cannot be empty"
- "C:\\nonexistent\\file.exe" ❌ - Shows "File not found"
- "file|with|invalid|chars.exe" ❌ - Shows "File path contains invalid characters"

### 3. URL Validation Tests

**Valid URLs:**
- "https://github.com" ✅
- "http://example.com" ✅
- "ftp://files.example.com" ✅
- "mailto:user@example.com" ✅
- "file:///C:/path/file.txt" ✅

**Invalid URLs:**
- "" (empty) ❌ - Shows "URL cannot be empty"
- "invalid-url" ❌ - Shows "Please enter a valid URL"
- "javascript:alert('xss')" ❌ - Shows "Invalid URL scheme"

### 4. Icon Path Validation Tests

**Valid Icons:**
- "ms-appx:///Assets/Icons/code.png" ✅
- "C:\\icons\\app.ico" ✅
- "C:\\images\\icon.png" ✅

**Invalid Icons:**
- "C:\\icons\\app.txt" ❌ - Shows "Icon file type not supported"
- "C:\\nonexistent\\icon.png" ❌ - Shows "Icon file not found"
- "invalid|path|icon.ico" ❌ - Shows "Invalid icon path"

### 5. Configuration Error Recovery Tests

**Corrupted Config:**
- Delete config.json → Creates default config ✅
- Malformed JSON in config.json → Creates backup + default config ✅
- Invalid version migration → Backup + user notification ✅

**Permission Errors:**
- Read-only config file → Shows access denied message ✅
- Missing permissions → Shows permission guidance ✅

### 6. Process Launch Error Tests

**Successful Launches:**
- "notepad.exe" → Opens Notepad ✅
- "calc.exe" → Opens Calculator ✅
- "explorer.exe" → Opens File Explorer ✅

**Failed Launches:**
- "nonexistent.exe" → Shows "File not found" ❌
- Protected system file → Shows "Access denied" ❌
- Invalid URL → Shows "Invalid URL" ❌

### 7. Service Integration Tests

**Group Service:**
- Create group with invalid name → Shows validation error ✅
- Delete non-existent group → Shows "Group not found" ✅
- Move group to invalid position → Shows error ✅

**Shortcut Service:**
- Create shortcut with invalid target → Validation error ✅
- Launch shortcut with missing file → Error message ✅
- Update shortcut with duplicate name → Shows error ✅

**Configuration Service:**
- Save config with invalid data → Error handling ✅
- Restore from non-existent backup → Shows error ✅
- Auto-backup on save → Creates backup ✅

### 8. UI Layer Tests

**MainViewModel Operations:**
- Load corrupted config → Error recovery ✅
- Add group with invalid data → Validation feedback ✅
- Launch invalid shortcut → Error message ✅
- Drag-drop invalid data → Graceful handling ✅

**User Feedback:**
- Success messages for completed operations ✅
- Error messages with helpful guidance ✅
- Confirmation dialogs for destructive actions ✅

### 9. Error Recovery Tests

**Automatic Recovery:**
- Config corruption → Backup + default config ✅
- File not found → User notification ✅
- Permission issues → Guidance message ✅

**Manual Recovery:**
- Restore from backup → Confirmation dialog ✅
- Fix invalid input → Clear error messages ✅
- Retry failed operations → Second attempt allowed ✅

### 10. Edge Case Tests

**Boundary Conditions:**
- Empty strings → Proper validation ✅
- Maximum length inputs → Length checking ✅
- Special characters → Character validation ✅

**System Integration:**
- Network paths → Proper resolution ✅
- Environment variables → PATH resolution ✅
- Relative paths → Absolute path conversion ✅

## Expected Behavior Summary

### Success Scenarios
- Clear success messages for all operations
- Automatic state updates in UI
- Proper data persistence
- Smooth user experience

### Error Scenarios
- User-friendly error messages
- No application crashes
- Graceful recovery where possible
- Clear guidance for resolution

### Validation Scenarios
- Real-time input validation
- Comprehensive error prevention
- Consistent validation rules
- Helpful error messages

This test suite ensures comprehensive coverage of the error handling and validation system, providing robust protection against crashes and delivering excellent user experience.