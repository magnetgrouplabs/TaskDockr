# TaskDockr MSIX Packaging Guide

This guide provides complete instructions for packaging TaskDockr as an MSIX installer for beta distribution.

## 📦 Package Overview

TaskDockr is packaged as an MSIX application with the following specifications:

- **Package Name**: TaskDockr
- **Version**: 1.0.0.0 (Beta)
- **Target Platforms**: x86, x64, Arm64
- **Minimum OS**: Windows 10 1809 (Build 17763)
- **Architecture**: Multi-platform support

## 🛠️ Prerequisites

### Required Tools
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Windows App SDK 1.5](https://aka.ms/windowsappsdk)
- Visual Studio 2022 (recommended)
- MSIX Packaging Tool (optional)

### System Requirements
- Windows 10/11 (Build 17763 or later)
- Administrator access for package creation
- Developer mode enabled for testing

## 📁 File Structure

```
TaskDockr/
├── MSIX-Packaging.md          # This guide
├── MSIX-README.md             # Quick start guide
├── Create-TestCertificate.ps1 # Certificate creation script
├── Build-MsixPackage.ps1     # Package building script
├── Install-BetaPackage.ps1   # Installation script
└── TaskDockr/
    ├── Package.appxmanifest   # Enhanced MSIX manifest
    ├── app.manifest          # Application manifest
    └── Assets/               # Application assets
        ├── StoreLogo.png.txt
        ├── Square150x150Logo.png.txt
        ├── Square44x44Logo.png.txt
        ├── Wide310x150Logo.png.txt
        └── SplashScreen.png.txt
```

## 🚀 Quick Start

### Step 1: Create Test Certificate
```powershell
# Run as Administrator
.\Create-TestCertificate.ps1
```

### Step 2: Build MSIX Package
```powershell
# Build the package
.\Build-MsixPackage.ps1
```

### Step 3: Install for Testing
```powershell
# Install on test machine (run as Admin)
.\Install-BetaPackage.ps1 -PackagePath "MSIX-Packages\TaskDockr_1.0.0.0_x64.msix"
```

## 🔧 Detailed Instructions

### 1. Certificate Creation

The test certificate allows you to sign the MSIX package for beta distribution.

```powershell
# Create certificate with custom parameters
.\Create-TestCertificate.ps1 -CertificateName "My TaskDockr Beta" -Subject "CN=MyCompany"
```

### 2. Package Building

Build the MSIX package using Visual Studio or command line:

#### Option A: Visual Studio (Recommended)
1. Open `TaskDockr.sln` in Visual Studio 2022
2. Right-click the TaskDockr project
3. Select "Publish" → "Create App Packages"
4. Choose "MSIX package"
5. Select "Sideloading" for beta distribution
6. Use the test certificate for signing
7. Build all architectures (x86, x64, Arm64)

#### Option B: Command Line
```powershell
# Build using PowerShell script
.\Build-MsixPackage.ps1 -Configuration Release -OutputPath "BetaPackages"
```

### 3. Beta Distribution

#### Distribution Methods
1. **Direct Sideloading**: Share MSIX file with testers
2. **Microsoft Store**: Submit to Partner Center for beta testing
3. **Enterprise**: Distribute via Intune or MDM

#### Installation Instructions for Testers
```powershell
# Enable developer mode first (Windows Settings → Update & Security → For developers)
# Then install the package
Add-AppxPackage -Path "TaskDockr_1.0.0.0_x64.msix"
```

## ⚙️ Package Configuration

### Manifest Features
- **Full Trust**: Required for taskbar integration
- **Multi-architecture**: Supports x86, x64, Arm64
- **Modern UI**: WinUI 3 and Windows 11 design
- **Auto-start**: Can be configured to start with Windows

### Capabilities
- `runFullTrust`: Full system access
- `internetClient`: Network access (if needed)
- `removableStorage`: USB drive access

## 🧪 Testing Checklist

### Pre-Installation
- [ ] Developer mode enabled
- [ ] Test certificate installed
- [ ] Windows version compatible

### Post-Installation
- [ ] Application launches successfully
- [ ] Taskbar integration works
- [ ] Shortcut creation functions
- [ ] Settings persist between sessions
- [ ] All features operational

### Uninstallation
```powershell
# Remove package
Get-AppxPackage -Name "TaskDockr" | Remove-AppxPackage
```

## 🔒 Security Considerations

### Certificate Management
- Use proper code signing certificates for production
- Keep private keys secure
- Consider certificate revocation procedures

### Package Security
- Validate package integrity before distribution
- Use secure distribution channels
- Monitor for tampering

## 📊 Distribution Metrics

Track the following for beta testing:
- Installation success rate
- Crash reports
- Feature usage statistics
- User feedback

## 🆘 Troubleshooting

### Common Issues

**Certificate Errors**
```powershell
# Reinstall certificate
Import-PfxCertificate -FilePath "TaskDockr-Test.pfx" -CertStoreLocation "Cert:\CurrentUser\TrustedPeople"
```

**Installation Failures**
- Ensure developer mode is enabled
- Check Windows version compatibility
- Verify package signature

**Launch Issues**
- Check event logs for errors
- Verify dependencies are installed
- Test on clean Windows installation

## 📈 Next Steps

1. **Beta Testing**: Distribute to testers and collect feedback
2. **Store Submission**: Prepare for Microsoft Store submission
3. **Automation**: Set up CI/CD pipeline for package creation
4. **Monitoring**: Implement crash reporting and analytics

## 📞 Support

For issues with MSIX packaging:
- Check Windows Event Logs
- Review [MSIX Documentation](https://docs.microsoft.com/windows/msix/)
- Consult Windows App SDK documentation

---

*This MSIX configuration is optimized for beta distribution and can be adapted for production use.*