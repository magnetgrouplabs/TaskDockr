# PowerShell Script for Beta Testers to Install TaskDockr MSIX
# Run this script as Administrator

param(
    [string]$PackagePath = "TaskDockr_1.0.0.0_x64.msix",
    [switch]$EnableSideloading = $true
)

Write-Host "Installing TaskDockr Beta Package..." -ForegroundColor Green

# Check if running as Administrator
if (-not ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Host "This script requires Administrator privileges. Please run as Administrator." -ForegroundColor Red
    exit 1
}

# Enable sideloading if requested
if ($EnableSideloading) {
    Write-Host "Enabling sideloading..." -ForegroundColor Cyan
    try {
        # Check current developer mode setting
        $devMode = Get-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock" -Name "AllowDevelopmentWithoutDevLicense" -ErrorAction SilentlyContinue
        
        if (-not $devMode -or $devMode.AllowDevelopmentWithoutDevLicense -ne 1) {
            Set-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock" -Name "AllowDevelopmentWithoutDevLicense" -Value 1
            Write-Host "Sideloading enabled successfully" -ForegroundColor Green
        } else {
            Write-Host "Sideloading already enabled" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "Warning: Could not enable sideloading. You may need to enable developer mode manually." -ForegroundColor Yellow
    }
}

# Check if package exists
if (-not (Test-Path $PackagePath)) {
    Write-Host "Error: Package file not found: $PackagePath" -ForegroundColor Red
    Write-Host "Please ensure the MSIX package exists in the current directory" -ForegroundColor Yellow
    exit 1
}

# Install the package
Write-Host "Installing package: $PackagePath" -ForegroundColor Cyan
try {
    Add-AppxPackage -Path $PackagePath
    Write-Host "Package installed successfully!" -ForegroundColor Green
    
    # Show installation details
    $package = Get-AppxPackage -Name "TaskDockr"
    if ($package) {
        Write-Host "`nInstallation Details:" -ForegroundColor Cyan
        Write-Host "- Name: $($package.Name)"
        Write-Host "- Version: $($package.Version)"
        Write-Host "- Install Location: $($package.InstallLocation)"
        
        # Create start menu shortcut
        Write-Host "`nCreating Start Menu shortcut..." -ForegroundColor Cyan
        $startMenuPath = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\TaskDockr.lnk"
        $shell = New-Object -ComObject WScript.Shell
        $shortcut = $shell.CreateShortcut($startMenuPath)
        $shortcut.TargetPath = "shell:AppsFolder\$($package.PackageFamilyName)!App"
        $shortcut.Save()
        
        Write-Host "Start Menu shortcut created" -ForegroundColor Green
    }
    
} catch {
    Write-Host "Error installing package: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "`nTroubleshooting steps:" -ForegroundColor Yellow
    Write-Host "1. Ensure developer mode is enabled in Windows Settings"
    Write-Host "2. Check if the package is properly signed"
    Write-Host "3. Try installing with: Add-AppxPackage -Path '$PackagePath' -ForceApplicationShutdown"
}

Write-Host "`nTaskDockr should now be available in your Start Menu and can be launched from there." -ForegroundColor Green