# TaskDockr Error Handling & Validation System

## Overview
Comprehensive error handling and validation system implemented throughout TaskDockr to prevent crashes and provide helpful user feedback.

## Key Features Implemented

### 1. Centralized Error Handling Service (`ErrorHandlingService.cs`)
- **User-Friendly Error Messages**: Converts technical exceptions into understandable user messages
- **Validation Methods**: Group name, file path, URL, and icon path validation
- **Dialog Integration**: Confirmation, error, and success message dialogs
- **Recovery Mechanisms**: Automatic backup creation for configuration errors

### 2. Enhanced Group Service Validation
- **Group Name Validation**: Length (1-50 chars), character restrictions (letters, numbers, spaces, underscores, hyphens)
- **Icon Path Validation**: File existence, supported formats (.png, .jpg, .jpeg, .gif, .bmp, .ico, .svg)
- **Duplicate Detection**: Case-insensitive duplicate name prevention
- **Error Recovery**: Graceful fallback with user feedback

### 3. Enhanced Shortcut Service Validation
- **Target Path Validation**: File existence, URL format verification
- **Executable Validation**: PATH resolution for executables
- **Launch Error Handling**: Comprehensive process launch error handling
- **Input Validation**: Name length, duplicate detection within groups

### 4. Enhanced Configuration Service
- **Corrupted Config Recovery**: Automatic backup and default config fallback
- **Migration Error Handling**: Version migration with user feedback
- **File Operation Safety**: Permission and I/O error handling
- **Backup Integration**: Automatic backup creation for critical operations

### 5. UI Layer Integration
- **MainViewModel Updates**: All operations wrapped in try-catch with error handling
- **User Feedback**: Success/failure messages for all user actions
- **Async Error Handling**: Proper async/await error propagation

## Validation Rules

### Group Validation
- **Name**: 1-50 characters, alphanumeric + spaces/underscores/hyphens
- **Icon Path**: Must exist, supported image formats or resource paths
- **Uniqueness**: Case-insensitive duplicate name prevention

### Shortcut Validation
- **Name**: 1-100 characters
- **Target Path**: Must exist (file/folder) or valid URL format
- **Type-Specific Validation**:
  - **App**: Executable file validation
  - **File**: File existence check
  - **Folder**: Directory existence check
  - **URL**: Valid URL scheme (http/https/ftp/mailto/file)

### File Path Validation
- **Existence**: File or directory must exist
- **Invalid Characters**: Path validation against system invalid characters
- **Relative Path Resolution**: Automatic resolution of relative paths

### URL Validation
- **Scheme Validation**: Supported schemes only
- **Format Validation**: Proper URI parsing
- **Auto-Completion**: Automatic http/https prefix addition

## Error Recovery Mechanisms

### Configuration Recovery
- **Corrupted Config**: Automatic backup + default config
- **Migration Failure**: Backup + user notification
- **Save Failures**: Permission checks + user guidance

### Process Launch Recovery
- **File Not Found**: User-friendly error message
- **Permission Errors**: Access denied guidance
- **Process Failures**: Exit code analysis + feedback

### User Interface Recovery
- **Operation Failures**: Clear error messages
- **Data Loss Prevention**: Confirmation dialogs for destructive operations
- **State Recovery**: Automatic UI state restoration

## Implementation Details

### Service Dependencies
- All services now depend on `IErrorHandlingService`
- Proper dependency injection via `ServiceManager`
- Consistent error handling patterns across all services

### Error Message Hierarchy
1. **Technical Errors**: Logged for debugging
2. **User-Friendly Messages**: Shown to users
3. **Recovery Actions**: Automatic where possible

### Validation Flow
1. **Input Validation**: Before processing
2. **Operation Validation**: During execution
3. **Result Validation**: After completion
4. **User Feedback**: Success/failure notification

## Benefits

### User Experience
- **No Crashes**: Comprehensive exception handling
- **Clear Feedback**: Understandable error messages
- **Guidance**: Helpful suggestions for resolution
- **Recovery**: Automatic recovery where possible

### Development Benefits
- **Consistency**: Unified error handling patterns
- **Maintainability**: Centralized error handling logic
- **Debugging**: Enhanced error logging
- **Testing**: Easier error scenario testing

### Security & Reliability
- **Input Sanitization**: Prevents injection attacks
- **File Safety**: Prevents path traversal issues
- **Process Safety**: Prevents malicious execution
- **Data Integrity**: Prevents configuration corruption

## Files Modified

### New Files
- `ErrorHandlingService.cs` - Central error handling service
- `ERROR_HANDLING_SUMMARY.md` - This documentation

### Modified Files
- `GroupService.cs` - Enhanced validation and error handling
- `ShortcutService.cs` - Enhanced validation and error handling
- `ConfigurationService.cs` - Enhanced error recovery
- `ServiceManager.cs` - Added error handling service registration
- `MainViewModel.cs` - UI layer error handling integration

## Testing Recommendations

### Error Scenarios to Test
1. **Invalid Group Names**: Empty, too long, invalid characters
2. **Missing Files/Icons**: Non-existent paths
3. **Invalid URLs**: Malformed URLs, unsupported schemes
4. **Permission Issues**: Read-only files, protected directories
5. **Corrupted Config**: Malformed JSON, missing properties
6. **Process Launch**: Non-existent executables, permission denied

### Validation Testing
1. **Boundary Testing**: Minimum/maximum length inputs
2. **Edge Cases**: Special characters, empty strings
3. **Integration Testing**: Cross-service error propagation
4. **Recovery Testing**: Automatic recovery mechanisms

This implementation provides enterprise-grade error handling and validation, ensuring TaskDockr is robust, user-friendly, and reliable.