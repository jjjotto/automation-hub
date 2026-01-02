# Building and Distributing Automation Hub

This document explains how to build and distribute the Automation Hub application as an installer or portable package.

## Overview

Automation Hub can be distributed in two ways:

1. **Windows Installer (Setup.exe)** - Professional installer using Inno Setup (recommended)
2. **Portable ZIP Package** - Self-contained folder that can be extracted anywhere

Both methods create a self-contained package that includes all dependencies except the .NET Desktop Runtime.

## Prerequisites

### For Building the Application
- Windows 10 or Windows 11
- .NET 8.0 SDK or newer
- Git (for cloning the repository)

### For Creating the Installer (Optional)
- Inno Setup 6 or 5 - Download from https://jrsoftware.org/isdl.php

## Method 1: Windows Installer (Recommended)

The installer provides the best user experience with automatic .NET Runtime detection, start menu shortcuts, and clean uninstallation.

### Step 1: Install Inno Setup

1. Download Inno Setup from https://jrsoftware.org/isdl.php
2. Install using default settings
3. Inno Setup will be installed to `C:\Program Files (x86)\Inno Setup 6\`

### Step 2: Build the Installer

Open PowerShell in the repository root and run:

```powershell
.\build-installer.ps1
```

Or simply double-click `build-installer.bat` in Windows Explorer.

### Step 3: Distribute

The installer will be created in the `installer-output\` directory:
- `AutomationHub-Setup-1.0.0.exe`

Share this file with end users. They can double-click to install.

### What the Installer Does

- Checks for .NET 8 Desktop Runtime
- If missing, prompts to download it from Microsoft
- Installs Automation Hub to `C:\Program Files\Automation Hub\`
- Creates Start Menu shortcuts
- Optionally creates desktop shortcut
- Registers uninstaller in Windows Settings

### Custom Build Options

The PowerShell script accepts optional parameters:

```powershell
# Build with Debug configuration
.\build-installer.ps1 -Configuration Debug

# Skip build if already built
.\build-installer.ps1 -SkipBuild

# Build framework-dependent version (smaller, requires .NET installed)
.\build-installer.ps1 -SelfContained:$false
```

## Method 2: Portable ZIP Package

If you don't have Inno Setup or prefer a simpler distribution, you can create a portable ZIP package.

### Step 1: Build the Portable Package

Open PowerShell in the repository root and run:

```powershell
.\build-portable.ps1
```

### Step 2: Distribute

The package will be created as `AutomationHub-Portable.zip` in the repository root.

Share this ZIP file with end users. They should:
1. Extract the ZIP to any location
2. Double-click `Start-AutomationHub.bat` to run

### What's Included

The ZIP package contains:
- All application files
- Sample configuration
- `Start-AutomationHub.bat` - Launcher that checks for .NET Runtime
- `README.txt` - Installation and usage instructions

## End User Requirements

Both distribution methods require:

- **Operating System**: Windows 10 (22H2+) or Windows 11
- **.NET 8 Desktop Runtime**: Will be automatically checked
  - Download: https://dotnet.microsoft.com/download/dotnet/8.0
  - Choose "Desktop Runtime" for Windows x64
  - Size: ~55 MB download

## Configuration After Installation

### Production (Network Drive)

If the Y: drive is available, the application will use:
- Jobs: `Y:\temporary_files\JO\automation\config\jobs\`
- Logs: `Y:\temporary_files\JO\automation\logs\`

### Development (Local)

If Y: drive is not available, the application will use:
- Jobs: `[InstallDir]\config\jobs\`
- Logs: `[InstallDir]\logs\`

Users can place job definition files (`.json`) in the appropriate `config/jobs/` directory.

## Manual Build (Without Scripts)

If you prefer to build manually:

### Step 1: Publish the Application

```powershell
dotnet publish AutomationHub.App/AutomationHub.App.csproj -c Release -r win-x64 --self-contained -o publish/win-x64
```

### Step 2: Create Installer (with Inno Setup)

1. Open `installer.iss` in Inno Setup Compiler
2. Click "Compile" or press F9
3. Installer will be created in `installer-output\`

### Step 3: Or Create ZIP

1. Copy contents of `publish/win-x64/` to a new folder
2. Copy `config/` directory into the folder
3. Create a batch file to launch the application
4. ZIP the folder

## Troubleshooting

### "dotnet command not found"

Install .NET 8.0 SDK from https://dotnet.microsoft.com/download/dotnet/8.0

### "Inno Setup not found"

- Install Inno Setup from https://jrsoftware.org/isdl.php, or
- Use the portable ZIP method instead (no Inno Setup required)

### Build fails with ".NET Desktop Runtime" error

This error occurs when building on Linux/Mac. These scripts are designed for Windows only. If you need to build on Linux/Mac, you can still generate the published files with:

```bash
dotnet publish AutomationHub.App/AutomationHub.App.csproj -c Release -r win-x64 --self-contained -o publish/win-x64
```

Then transfer the files to a Windows machine to create the installer.

### Application won't start for end users

Ensure they have .NET 8 Desktop Runtime installed:
1. Open Command Prompt
2. Run: `dotnet --list-runtimes`
3. Look for: `Microsoft.WindowsDesktop.App 8.x.x`
4. If missing, install from https://dotnet.microsoft.com/download/dotnet/8.0

## Version Management

To update the version number:

1. Edit `installer.iss` and change `MyAppVersion`
2. Rebuild the installer

The installer filename will automatically include the version number.

## Distribution Best Practices

### For Internal Lab Use

1. Build the installer or portable package
2. Upload to shared drive: `Y:\temporary_files\JO\automation\`
3. Send users a link or instructions to download from the shared drive

### For External Distribution

1. Build the installer or portable package
2. Upload to GitHub Releases
3. Share the download link

### File Sizes (Approximate)

- **Installer (self-contained)**: 80-120 MB
- **Portable ZIP (self-contained)**: 80-120 MB
- **Installer (framework-dependent)**: 5-10 MB (requires .NET pre-installed)

The self-contained version is larger but more user-friendly as it includes all dependencies.

## Security Notes

- The installer is not digitally signed by default
- Windows may show "Unknown publisher" warning
- Users can safely proceed by clicking "More info" → "Run anyway"
- For production use, consider code signing the executable

## Support

For issues or questions:
- GitHub Repository: https://github.com/jjjotto/automation-hub
- See `README.md` for application overview
- See `RUNNING_THE_APP.md` for detailed usage instructions
