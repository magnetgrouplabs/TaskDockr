# PowerShell Script to Build TaskDockr MSIX Package
# Requires: .NET 8.0 SDK, Windows App SDK

param(
    [string]$Configuration = "Release",
    [string]$OutputPath = "MSIX-Packages",
    [string]$CertificatePath = "TaskDockr-Test.pfx",
    [string]$CertificatePassword = "TaskDockrBeta123!"
)

Write-Host "Building TaskDockr MSIX Package..." -ForegroundColor Green

# Check prerequisites
$dotnetVersion = dotnet --version 2>$null
if (-not $dotnetVersion) {
    Write-Host "Error: .NET SDK not found. Please install .NET 8.0 SDK" -ForegroundColor Red
    exit 1
}

Write-Host "Using .NET SDK version: $dotnetVersion" -ForegroundColor Yellow

# Create output directory
if (-not (Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force
}

# Restore packages
Write-Host "Restoring packages..." -ForegroundColor Cyan
try {
    dotnet restore TaskDockr.sln
    if ($LASTEXITCODE -ne 0) {
        throw "Package restore failed"
    }
} catch {
    Write-Host "Error restoring packages: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Build the project
Write-Host "Building project..." -ForegroundColor Cyan
try {
    dotnet build TaskDockr.sln --configuration $Configuration --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed"
    }
} catch {
    Write-Host "Error building project: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Check if MSIX packaging tools are available
$msixPath = "TaskDockr\bin\$Configuration\net8.0-windows10.0.19041.0\win-x64\TaskDockr.exe"
if (-not (Test-Path $msixPath)) {
    Write-Host "Error: Built executable not found at: $msixPath" -ForegroundColor Red
    Write-Host "Please ensure the project builds successfully first" -ForegroundColor Yellow
    exit 1
}

Write-Host "Project built successfully!" -ForegroundColor Green

# Create MSIX package using MakeAppx
Write-Host "Creating MSIX package..." -ForegroundColor Cyan

# Create package manifest
$manifestContent = @"
<?xml version="1.0" encoding="utf-8"?>
<Package xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10">
  <Identity Name="TaskDockr" Publisher="CN=TaskDockr" Version="1.0.0.0" />
  <Properties>
    <DisplayName>TaskDockr</DisplayName>
    <PublisherDisplayName>TaskDockr</PublisherDisplayName>
    <Logo>Assets\StoreLogo.png</Logo>
    <Description>Task management application with taskbar integration</Description>
  </Properties>
  <Dependencies>
    <TargetDeviceFamily Name="Windows.Universal" MinVersion="10.0.17763.0" MaxVersionTested="10.0.22621.0" />
  </Dependencies>
  <Resources>
    <Resource Language="en-US" />
  </Resources>
  <Applications>
    <Application Id="App" Executable="TaskDockr.exe" EntryPoint="TaskDockr.App">
      <Extensions>
        <desktop:Extension Category="windows.fullTrustProcess" Executable="TaskDockr.exe" />
      </Extensions>
    </Application>
  </Applications>
  <Capabilities>
    <rescap:Capability Name="runFullTrust" />
    <Capability Name="internetClient" />
  </Capabilities>
</Package>
"@

$manifestPath = "$OutputPath\AppxManifest.xml"
$manifestContent | Out-File -FilePath $manifestPath -Encoding UTF8

# Copy required files to package directory
$packageDir = "$OutputPath\Package"
if (Test-Path $packageDir) {
    Remove-Item $packageDir -Recurse -Force
}
New-Item -ItemType Directory -Path $packageDir -Force

Copy-Item $msixPath -Destination $packageDir
Copy-Item "TaskDockr\Assets\*" -Destination $packageDir -ErrorAction SilentlyContinue

Write-Host "MSIX package structure created at: $packageDir" -ForegroundColor Green

# Instructions for final packaging
Write-Host "`nNext steps for final packaging:" -ForegroundColor Cyan
Write-Host "1. Use Visual Studio 2022: Right-click project -> Publish -> Create App Packages"
Write-Host "2. Use MSIX Packaging Tool from Microsoft Store"
Write-Host "3. Use MakeAppx.exe command line tool"
Write-Host "`nPackage directory ready at: $packageDir"
Write-Host "Manifest file at: $manifestPath"