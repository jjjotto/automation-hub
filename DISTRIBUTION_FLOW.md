# Automation Hub - Build and Distribution Flow

## Build Process Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                         SOURCE CODE                                  │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │ AutomationHub.sln                                            │  │
│  │   ├── AutomationHub.App        (WPF UI)                      │  │
│  │   ├── AutomationHub.Core       (Models & Contracts)          │  │
│  │   ├── AutomationHub.Scheduler  (Job Scheduling)              │  │
│  │   └── AutomationHub.Watchers   (File Monitoring)             │  │
│  └──────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────┘
                                   │
                                   │ dotnet publish
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────────┐
│                    PUBLISHED APPLICATION                             │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │ publish/win-x64/                                             │  │
│  │   ├── AutomationHub.App.exe                                  │  │
│  │   ├── *.dll (all dependencies)                               │  │
│  │   └── config/jobs/sample-job.json                            │  │
│  └──────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────┘
                    │                              │
                    │                              │
          ┌─────────┴─────────┐         ┌────────┴─────────┐
          │                   │         │                  │
          ▼                   ▼         ▼                  ▼
┌──────────────────┐  ┌──────────────────┐  ┌─────────────────────┐
│  METHOD 1:       │  │  METHOD 2:       │  │  METHOD 3:          │
│  Inno Setup      │  │  Portable ZIP    │  │  Manual Copy        │
│  Installer       │  │  Package         │  │                     │
└──────────────────┘  └──────────────────┘  └─────────────────────┘
         │                     │                       │
         │ ISCC.exe            │ PowerShell           │ xcopy/robocopy
         │                     │ Compress-Archive     │
         ▼                     ▼                       ▼
┌──────────────────┐  ┌──────────────────┐  ┌─────────────────────┐
│ Setup.exe        │  │ ZIP Package      │  │ Folder Copy         │
│ 80-120 MB        │  │ 80-120 MB        │  │ 80-120 MB           │
│                  │  │                  │  │                     │
│ Features:        │  │ Features:        │  │ Features:           │
│ ✓ .NET Check    │  │ ✓ .NET Check    │  │ • Manual runtime    │
│ ✓ Start Menu    │  │ ✓ Portable      │  │ • No automation     │
│ ✓ Desktop Icon  │  │ ✓ No install    │  │ • Requires Y: drive │
│ ✓ Uninstaller   │  │ ✓ Extract & Run │  │                     │
│ ✓ Auto-update   │  │ • No shortcuts  │  │                     │
└──────────────────┘  └──────────────────┘  └─────────────────────┘
```

## Distribution Methods Comparison

| Feature | Windows Installer | Portable ZIP | Manual Copy |
|---------|------------------|--------------|-------------|
| **Build Command** | `build-installer.bat` | `build-portable.ps1` | Manual steps |
| **User Experience** | Professional installer | Extract and run | Copy to destination |
| **Requirements** | Inno Setup | PowerShell | None |
| **Start Menu** | ✓ Yes | ✗ No | ✗ No |
| **Desktop Icon** | ✓ Optional | ✗ No | ✗ No |
| **Uninstaller** | ✓ Yes | Manual delete | Manual delete |
| **Auto .NET Check** | ✓ Yes | ✓ Yes (via launcher) | ✗ No |
| **Best For** | End users | Testing, portable use | Network share |

## End User Installation Flow

### Method 1: Windows Installer
```
User downloads        User runs            Installer checks      App installs
Setup.exe          →  Setup.exe         →  .NET Runtime      →  to Program Files
                                            │
                                            ├─ If missing: Opens download page
                                            └─ If found: Proceeds with install
                                                         │
                                                         ▼
                                            ┌────────────────────────┐
                                            │ Installation Complete  │
                                            │ • Start Menu shortcut  │
                                            │ • Desktop icon (opt.)  │
                                            │ • Ready to launch!     │
                                            └────────────────────────┘
```

### Method 2: Portable ZIP
```
User downloads        User extracts       User runs              Launcher checks
ZIP file           →  to any folder    →  Start-AutomationHub  →  .NET Runtime
                                           .bat                    │
                                                                   ├─ If missing: Shows message
                                                                   └─ If found: Launches app
                                                                                │
                                                                                ▼
                                                                   ┌────────────────────────┐
                                                                   │ App Running            │
                                                                   │ • From extract folder  │
                                                                   │ • No installation      │
                                                                   │ • Portable!            │
                                                                   └────────────────────────┘
```

## Configuration Flow

```
┌─────────────────────────────────────────────────────────┐
│                    APPLICATION START                     │
└─────────────────────────────────────────────────────────┘
                           │
                           ▼
                 ┌─────────────────┐
                 │ Check for Y:    │
                 │ drive access    │
                 └─────────────────┘
                     │           │
        ┌────────────┴──────┐    └────────────┐
        │ Y: Available      │                 │ Y: Not Available
        ▼                   │                 ▼
┌──────────────────┐        │        ┌──────────────────┐
│ Production Mode  │        │        │ Development Mode │
│                  │        │        │                  │
│ Config Path:     │        │        │ Config Path:     │
│ Y:\temporary_    │        │        │ [InstallDir]\    │
│ files\JO\        │        │        │ config\jobs\     │
│ automation\      │        │        │                  │
│ config\jobs\     │        │        │ Logs Path:       │
│                  │        │        │ [InstallDir]\    │
│ Logs Path:       │        │        │ logs\            │
│ Y:\temporary_    │        │        │                  │
│ files\JO\        │        │        │                  │
│ automation\logs\ │        │        │                  │
└──────────────────┘        │        └──────────────────┘
        │                   │                 │
        └───────────────────┴─────────────────┘
                           │
                           ▼
                 ┌─────────────────┐
                 │ Load job files  │
                 │ from config\    │
                 │ jobs\*.json     │
                 └─────────────────┘
                           │
                           ▼
                 ┌─────────────────┐
                 │ Display in UI   │
                 │ • Job list      │
                 │ • Status info   │
                 └─────────────────┘
```

## Build Scripts Architecture

```
┌──────────────────────────────────────────────────────────────┐
│                    BUILD SCRIPTS                             │
│                                                              │
│  build-installer.bat                                        │
│  └─► Calls PowerShell script                               │
│       │                                                      │
│  build-installer.ps1                                        │
│  ├─► Check .NET SDK                                         │
│  ├─► Run: dotnet publish                                    │
│  │    └─► Creates: publish/win-x64/                        │
│  │                                                           │
│  ├─► Check for Inno Setup                                  │
│  │    └─► Found: ISCC.exe                                  │
│  │                                                           │
│  └─► Run: ISCC.exe installer.iss                           │
│       └─► Creates: installer-output/                        │
│                   AutomationHub-Setup-1.0.0.exe            │
│                                                              │
│  build-portable.ps1                                         │
│  ├─► Check .NET SDK                                         │
│  ├─► Run: dotnet publish                                    │
│  │    └─► Creates: publish/win-x64/                        │
│  │                                                           │
│  ├─► Create package structure                              │
│  │    ├─► Copy published files                             │
│  │    ├─► Copy config files                                │
│  │    ├─► Generate launcher batch file                     │
│  │    └─► Generate README.txt                              │
│  │                                                           │
│  └─► Create ZIP archive                                     │
│       └─► Creates: AutomationHub-Portable.zip              │
└──────────────────────────────────────────────────────────────┘
```

## File Structure After Installation

### Installed via Setup.exe
```
C:\Program Files\Automation Hub\
├── AutomationHub.App.exe          (Main executable)
├── *.dll                           (Dependencies)
├── config\
│   └── jobs\
│       └── sample-job.json        (Sample configuration)
└── logs\                          (Created on first run)

Start Menu:
├── Automation Hub                 (Launch shortcut)
└── Uninstall Automation Hub       (Uninstaller)

Desktop:
└── Automation Hub                 (Optional shortcut)
```

### Extracted from ZIP
```
[User's chosen location]\AutomationHub\
├── AutomationHub.App.exe          (Main executable)
├── *.dll                           (Dependencies)
├── Start-AutomationHub.bat        (Launcher script)
├── README.txt                     (User guide)
├── config\
│   └── jobs\
│       └── sample-job.json        (Sample configuration)
└── logs\                          (Created on first run)
```

## Version Management

To update version number:

1. **Edit `installer.iss`**
   ```
   #define MyAppVersion "1.0.0"  ← Change this
   ```

2. **Rebuild**
   ```powershell
   .\build-installer.ps1
   ```

3. **Result**
   ```
   AutomationHub-Setup-1.0.0.exe   (with new version)
   ```

Version number appears in:
- Installer filename
- Windows Add/Remove Programs
- Application properties
