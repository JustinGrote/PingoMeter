# PingoMeter Installer

This directory contains the WiX Toolset installer project for PingoMeter.

## Prerequisites

To build the MSI installer, you need:
- .NET SDK 10.0 or later
- WiX Toolset 5.x (installed automatically via NuGet)

## Building the Installer

### Using the Build Scripts (Recommended)

**Build for a specific architecture:**
```powershell
.\build-msi.ps1 -Architecture x64
.\build-msi.ps1 -Architecture arm64
```

**Build for all architectures:**
```powershell
.\build-all-msi.ps1
```

### Manual Build

1. First, publish the application:
```powershell
dotnet publish Source/PingoMeter.csproj --configuration Release --output Release-x64 --arch x64
```

2. Then build the installer:
```powershell
dotnet build Installer/PingoMeter.Installer.wixproj --configuration Release /p:Architecture=x64
```

The MSI file will be created in: `artifacts/installer/Release/{Architecture}/PingoMeter.msi`

## Project Files

- **PingoMeter.Installer.wixproj** - WiX project file
- **Product.wxs** - Main installer definition (components, features, UI)
- **License.rtf** - License agreement shown during installation

## Installer Features

- Installs PingoMeter to Program Files
- Creates desktop shortcut
- Creates Start Menu shortcut
- Supports both x64 and ARM64 architectures
- Handles upgrades and uninstallation cleanly
- Includes all required resources and configuration files

## Customization

To modify the installer:
- Edit `Product.wxs` to change installation behavior, shortcuts, or included files
- Update `License.rtf` to change the license agreement text
- Modify `PingoMeter.Installer.wixproj` for build configuration changes

## Upgrade Code

The installer uses a fixed UpgradeCode GUID to enable automatic upgrades. Do not change this GUID unless you want to prevent automatic upgrades from previous versions.

Current UpgradeCode: `A1B2C3D4-E5F6-4A5B-8C9D-1E2F3A4B5C6D`
