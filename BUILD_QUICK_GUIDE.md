# Building the Installer

## Quick Build Guide

### Prerequisites
- Windows 10/11
- .NET 8 SDK installed
- Inno Setup 6 installed (for installer method)

### Method 1: Windows Installer (Recommended)

1. **Double-click** `build-installer.bat`

   OR run in PowerShell:
   ```powershell
   .\build-installer.ps1
   ```

2. Wait for the build to complete (~2-5 minutes)

3. Find your installer in `installer-output\AutomationHub-Setup-1.0.0.exe`

### Method 2: Portable ZIP

1. Run in PowerShell:
   ```powershell
   .\build-portable.ps1
   ```

2. Wait for the build to complete (~2-5 minutes)

3. Find your portable package: `AutomationHub-Portable.zip`

## What Gets Created

### Windows Installer
```
installer-output/
└── AutomationHub-Setup-1.0.0.exe  (80-120 MB)
```

### Portable ZIP
```
AutomationHub-Portable.zip  (80-120 MB)
└── Contains:
    └── AutomationHub/
        ├── AutomationHub.App.exe
        ├── [All dependencies]
        ├── config/
        │   └── jobs/
        │       └── sample-job.json
        ├── Start-AutomationHub.bat
        └── README.txt
```

## Distribution

### For Lab Users
1. Build the installer or portable package
2. Copy to shared drive: `Y:\temporary_files\JO\automation\`
3. Send users instructions to download from shared drive

### File Naming
- Installers are automatically versioned: `AutomationHub-Setup-1.0.0.exe`
- To change version, edit `installer.iss` line: `#define MyAppVersion "1.0.0"`

## Troubleshooting

### "dotnet not found"
Install .NET 8 SDK: https://dotnet.microsoft.com/download/dotnet/8.0

### "Inno Setup not found"
- Install from: https://jrsoftware.org/isdl.php
- OR use portable method instead (no Inno Setup needed)

### Build takes too long
First build is slower. Subsequent builds are faster due to caching.

## Advanced Options

### Build Configuration
```powershell
# Debug build
.\build-installer.ps1 -Configuration Debug

# Skip rebuild if already built
.\build-installer.ps1 -SkipBuild

# Framework-dependent (smaller, requires .NET pre-installed)
.\build-installer.ps1 -SelfContained:$false
```

### Manual Steps
If you prefer manual control:

1. Build application:
   ```powershell
   dotnet publish AutomationHub.App/AutomationHub.App.csproj -c Release -r win-x64 --self-contained -o publish/win-x64
   ```

2. Create installer (if you have Inno Setup):
   - Open `installer.iss` in Inno Setup Compiler
   - Press F9 to compile

3. Or create ZIP manually:
   - Copy `publish/win-x64/` folder contents
   - Add `config/` directory
   - Add a launcher batch file
   - ZIP everything

## Next Steps

After building:
1. Test the installer on a clean Windows machine
2. Verify .NET Runtime detection works
3. Check that the application starts correctly
4. Test job loading from config directory

## Support

See full documentation in `BUILDING_INSTALLER.md`
