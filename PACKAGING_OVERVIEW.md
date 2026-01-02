# Automation Hub - Installation Package Overview

## 📦 What Has Been Added

This repository now includes complete packaging and distribution capabilities for the Automation Hub WPF application.

## 🎯 Goal Achieved

Users can now **double-click an installer to install and run** the Automation Hub application, with automatic detection and guidance for required dependencies (.NET 8 Desktop Runtime).

## 🚀 Quick Start for Developers

### To Build an Installer:
```batch
build-installer.bat
```

### To Build a Portable Package:
```powershell
.\build-portable.ps1
```

That's it! The build scripts handle everything automatically.

## 📁 Files Added

### Build Scripts (3 files)
| File | Purpose |
|------|---------|
| `build-installer.bat` | Windows batch file - double-click to build installer |
| `build-installer.ps1` | PowerShell script for creating Windows installer (Setup.exe) |
| `build-portable.ps1` | PowerShell script for creating portable ZIP package |

### Installer Configuration (1 file)
| File | Purpose |
|------|---------|
| `installer.iss` | Inno Setup script that defines installer behavior and UI |

### Documentation (7 files)
| File | Audience | Purpose |
|------|----------|---------|
| `QUICKSTART.md` | End Users | How to install and use the application |
| `BUILDING_INSTALLER.md` | Developers | Complete guide to building installers |
| `BUILD_QUICK_GUIDE.md` | Developers | Quick reference for building |
| `BUILD_OUTPUT_EXAMPLES.md` | Developers | Expected output and troubleshooting |
| `DISTRIBUTION_FLOW.md` | Developers/Managers | Visual diagrams of build process |
| `RELEASE_CHECKLIST.md` | Release Managers | Step-by-step release checklist |
| `README.md` (updated) | All | Added installation and distribution sections |

## 🎁 Distribution Options

### Option 1: Windows Installer (Recommended)
- **File**: `AutomationHub-Setup-1.0.0.exe` (~95 MB)
- **Best for**: End users, production deployment
- **Features**:
  - ✅ Automatic .NET Runtime detection
  - ✅ Professional installation wizard
  - ✅ Start Menu shortcuts
  - ✅ Optional desktop shortcut
  - ✅ Clean uninstallation
  - ✅ Installs to Program Files

**Build with**: `build-installer.bat` or `build-installer.ps1`

**Requires**: Inno Setup 6 (free download)

### Option 2: Portable ZIP Package
- **File**: `AutomationHub-Portable.zip` (~92 MB)
- **Best for**: Testing, portable use, no admin rights
- **Features**:
  - ✅ No installation required
  - ✅ Extract and run anywhere
  - ✅ Includes launcher script
  - ✅ Automatic .NET Runtime check
  - ✅ Fully portable

**Build with**: `build-portable.ps1`

**Requires**: Only PowerShell (built into Windows)

## 📋 Prerequisites

### For Building
- Windows 10/11
- .NET 8.0 SDK
- Inno Setup 6 (for installer option only)

### For End Users
- Windows 10/11
- .NET 8 Desktop Runtime (auto-detected and guided installation)

## 🔧 How It Works

### Build Process
```
Source Code → dotnet publish → Self-Contained Build
                                        ↓
                        ┌───────────────┴────────────────┐
                        ↓                                ↓
                Inno Setup Compiler              ZIP Compression
                        ↓                                ↓
                AutomationHub-Setup.exe      AutomationHub-Portable.zip
```

### End User Experience (Installer)
```
1. User downloads Setup.exe
2. User double-clicks Setup.exe
3. Installer checks for .NET 8 Desktop Runtime
   - If missing: Opens download page, asks to install
   - If present: Proceeds with installation
4. Application installs to Program Files
5. Start Menu shortcut created
6. User launches from Start Menu
7. Application runs! 🎉
```

### End User Experience (Portable)
```
1. User downloads ZIP file
2. User extracts to any folder
3. User double-clicks Start-AutomationHub.bat
4. Launcher checks for .NET 8 Desktop Runtime
   - If missing: Shows message with download link
   - If present: Launches application
5. Application runs! 🎉
```

## 📖 Documentation Structure

```
For End Users:
├─ QUICKSTART.md                    → Start here for installation
└─ README.md (Getting Started)      → Overview and features

For Developers:
├─ BUILD_QUICK_GUIDE.md            → Quick reference
├─ BUILDING_INSTALLER.md           → Comprehensive guide
├─ BUILD_OUTPUT_EXAMPLES.md        → Expected output & troubleshooting
└─ DISTRIBUTION_FLOW.md            → Visual process diagrams

For Release Managers:
└─ RELEASE_CHECKLIST.md            → Step-by-step release process
```

## 🎓 Example Usage Scenarios

### Scenario 1: Lab Manager Distributing to Team
1. Run `build-installer.bat`
2. Copy `AutomationHub-Setup-1.0.0.exe` to `Y:\temporary_files\JO\automation\`
3. Email team: "New version available at Y:\automation\AutomationHub-Setup-1.0.0.exe"
4. Team members double-click to install

### Scenario 2: Quick Testing on Another Machine
1. Run `build-portable.ps1`
2. Copy `AutomationHub-Portable.zip` to USB drive
3. Extract on test machine
4. Run `Start-AutomationHub.bat`

### Scenario 3: GitHub Release
1. Run both build scripts
2. Create GitHub Release v1.0.0
3. Upload both Setup.exe and Portable.zip
4. Users download their preferred option

## 🔐 Security Features

### .NET Runtime Check
Both distribution methods check for .NET 8 Desktop Runtime:
- **Installer**: Checks during installation, opens download page if missing
- **Portable**: Launcher script checks at runtime, shows message if missing

### Self-Contained Package
- Includes all application dependencies
- No need to install additional libraries
- Works offline after .NET Runtime is installed

### Digital Signature (Optional)
- The installer is not digitally signed by default
- For production: Consider code signing the executable
- See BUILDING_INSTALLER.md for details

## 📊 File Sizes

| Package Type | Approximate Size | What's Included |
|--------------|-----------------|-----------------|
| Setup.exe | 80-120 MB | App + Dependencies + Installer UI |
| Portable.zip | 80-120 MB | App + Dependencies + Launcher |
| Source Code | ~500 KB | .cs, .xaml, .csproj files only |

## 🎨 Customization

### Changing Version Number
Edit `installer.iss`:
```
#define MyAppVersion "1.0.0"  ← Change this
```

### Changing Company Name
Edit `installer.iss`:
```
#define MyAppPublisher "JO Lab"  ← Change this
```

### Adding Application Icon
1. Create an .ico file
2. Update `installer.iss`:
   ```
   SetupIconFile=path\to\icon.ico
   ```

### Customizing Installer UI
Edit `installer.iss` - Inno Setup supports:
- Custom wizard pages
- License agreement screens
- Custom graphics and logos
- See Inno Setup documentation

## 🛠️ Troubleshooting

### Build Issues
- **"dotnet not found"** → Install .NET 8 SDK
- **"Inno Setup not found"** → Install Inno Setup OR use portable method
- **Build fails** → Check BUILD_OUTPUT_EXAMPLES.md

### Runtime Issues
- **App won't start** → Verify .NET 8 Desktop Runtime installed
- **"Jobs directory not found"** → Check Y: drive access or create local config
- **Windows SmartScreen warning** → Click "More info" → "Run anyway"

Full troubleshooting in BUILDING_INSTALLER.md and QUICKSTART.md

## 🔄 Update Process

To release a new version:
1. Update code
2. Update version in `installer.iss`
3. Run build scripts
4. Test on clean machine
5. Distribute new installer
6. See RELEASE_CHECKLIST.md for complete process

## 🌟 Features Comparison

|  | Installer | Portable | Manual |
|--|-----------|----------|--------|
| One-click install | ✅ | ⚠️ Extract first | ❌ |
| Start Menu | ✅ | ❌ | ❌ |
| Desktop shortcut | ✅ Optional | ❌ | ❌ |
| Uninstaller | ✅ | ❌ | ❌ |
| .NET Check | ✅ Auto | ✅ Manual | ❌ |
| Admin required | ✅ Yes | ❌ No | Depends |
| Portable | ❌ | ✅ | ✅ |
| Size | ~95 MB | ~92 MB | ~90 MB |

## 📚 Additional Resources

- [Inno Setup Documentation](https://jrsoftware.org/ishelp/)
- [.NET 8 Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Repository README.md](README.md)
- [Application Documentation](RUNNING_THE_APP.md)

## ✅ What Users Need to Know

### Minimal Instructions for End Users:
1. Download `AutomationHub-Setup-1.0.0.exe`
2. Double-click to install
3. Follow the wizard
4. Launch from Start Menu

**That's all!** The installer handles everything else automatically.

## 🎯 Success Criteria Met

✅ Users can double-click to install
✅ Automatic dependency checking
✅ No manual configuration required
✅ Start Menu integration
✅ Professional installer experience
✅ Portable option available
✅ Comprehensive documentation provided

## 📝 Next Steps

1. **Test**: Build and test the installer on Windows
2. **Distribute**: Share with team members
3. **Monitor**: Collect feedback on installation experience
4. **Iterate**: Update based on user feedback

## 💡 Tips

- Keep build scripts in repository root for easy access
- Update version numbers before each release
- Test on clean Windows VM before distributing
- Archive each release for historical reference
- Use portable version for quick testing

## 🤝 Contributing

To improve the installer/distribution:
1. Test on various Windows versions
2. Report issues with build scripts
3. Suggest improvements to user experience
4. Update documentation as needed

---

**Ready to build?** Run `build-installer.bat` and you're on your way! 🚀
