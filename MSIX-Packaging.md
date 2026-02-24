# MSIX Packaging Configuration for TaskDockr

This document outlines the MSIX packaging configuration for TaskDockr beta distribution.

## Prerequisites

- Windows 10/11 with MSIX Packaging Tool or Visual Studio
- .NET 8.0 SDK
- Windows App SDK 1.5

## Package Configuration

### 1. Updated Package.appxmanifest

The existing manifest has been enhanced with proper metadata for beta distribution.

### 2. MSIX Project Configuration

Create a dedicated MSIX packaging project or use the existing WinUI project with MSIX tooling enabled.

### 3. Assets Required

Create the following asset files in `TaskDockr\Assets\`:
- StoreLogo.png (50x50)
- Square150x150Logo.png (150x150)
- Square44x44Logo.png (44x44)
- Wide310x150Logo.png (310x150)

## Building the MSIX Package

### Method 1: Using Visual Studio
1. Open `TaskDockr.sln` in Visual Studio 2022
2. Right-click on the TaskDockr project
3. Select "Publish" > "Create App Packages"
4. Choose "MSIX package" and configure distribution
5. Build and sign the package

### Method 2: Using MSIX Packaging Tool
1. Install MSIX Packaging Tool from Microsoft Store
2. Open the tool and create a new package
3. Select the TaskDockr executable and dependencies
4. Configure package metadata
5. Build and sign

### Method 3: Command Line (dotnet)
```bash
# Restore packages
dotnet restore

# Build the project
dotnet build --configuration Release

# Create MSIX package (requires additional tooling)
# This step may require Visual Studio or MSIX Packaging Tool
```

## Package Properties

- **Name**: TaskDockr
- **Publisher**: CN=TaskDockr (for beta testing)
- **Version**: 1.0.0.0
- **Min Version**: Windows 10 17763 (Build 1809)
- **Architecture**: x86, x64, Arm64

## Capabilities

Required capabilities for TaskDockr functionality:
- `runFullTrust`: Full system access for taskbar integration

## Distribution for Beta Testing

### Test Certificate
For beta distribution, create a test certificate:
```powershell
# Create test certificate
New-SelfSignedCertificate -Type Custom -Subject "CN=TaskDockr" -KeyUsage DigitalSignature -FriendlyName "TaskDockr Beta" -CertStoreLocation "Cert:\CurrentUser\My"
```

### Installation Methods
1. **Sideloading**: Enable developer mode and install via PowerShell
2. **Microsoft Store**: Submit for beta testing through Partner Center
3. **Enterprise Distribution**: Distribute via Intune or other MDM

## Validation Checklist

- [ ] Package builds successfully
- [ ] App launches correctly
- [ ] All features work in packaged environment
- [ ] Taskbar integration functions
- [ ] Shortcut launching works
- [ ] Drag-drop functionality works
- [ ] Popout windows function correctly
- [ ] Settings persist between sessions

## Next Steps

1. Create missing asset files
2. Test package installation
3. Validate functionality in packaged environment
4. Prepare for beta distribution