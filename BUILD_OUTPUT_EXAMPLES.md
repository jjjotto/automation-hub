# Expected Build Output

This document shows what you should see when running the build scripts.

## Running build-installer.ps1

### Expected Console Output

```
=== Automation Hub Installer Build Script ===

Checking for .NET SDK...
✓ .NET SDK version 8.0.101 found

Building Automation Hub...
  Configuration: Release
  Runtime: win-x64
  Self-contained: True
  Output: C:\automation-hub\publish\win-x64

Cleaning previous build...

Running: dotnet publish AutomationHub.App/AutomationHub.App.csproj -c Release -r win-x64 -o C:\automation-hub\publish\win-x64 --self-contained -p:PublishSingleFile=false
MSBuild version 17.8.5+b5265ef37 for .NET
  Determining projects to restore...
  Restored C:\automation-hub\AutomationHub.Core\AutomationHub.Core.csproj (in 245 ms).
  Restored C:\automation-hub\AutomationHub.Watchers\AutomationHub.Watchers.csproj (in 245 ms).
  Restored C:\automation-hub\AutomationHub.Scheduler\AutomationHub.Scheduler.csproj (in 245 ms).
  Restored C:\automation-hub\AutomationHub.App\AutomationHub.App.csproj (in 2.1 sec).
  AutomationHub.Core -> C:\automation-hub\AutomationHub.Core\bin\Release\net8.0-windows\AutomationHub.Core.dll
  AutomationHub.Watchers -> C:\automation-hub\AutomationHub.Watchers\bin\Release\net8.0-windows\AutomationHub.Watchers.dll
  AutomationHub.Scheduler -> C:\automation-hub\AutomationHub.Scheduler\bin\Release\net8.0-windows\AutomationHub.Scheduler.dll
  AutomationHub.App -> C:\automation-hub\AutomationHub.App\bin\Release\net8.0-windows\win-x64\AutomationHub.App.dll
  AutomationHub.App -> C:\automation-hub\publish\win-x64\
✓ Build completed successfully

Checking for Inno Setup...
✓ Inno Setup found at: C:\Program Files (x86)\Inno Setup 6\ISCC.exe

Creating installer...
  Script: C:\automation-hub\installer.iss
  Output: C:\automation-hub\installer-output

Inno Setup 6 Command-Line Compiler
Copyright (C) 1997-2024 Jordan Russell. All rights reserved.

Compiling...
[Lines Processed] 1
[Lines Processed] 50
[Lines Processed] 100
[Preprocessing] Processing Pascal script...
[Compiling] Compiling Pascal script...
[Finished successfully]

Output: C:\automation-hub\installer-output\AutomationHub-Setup-1.0.0.exe

=== Build Complete ===

✓ Installer created successfully!

Installer location:
  C:\automation-hub\installer-output\AutomationHub-Setup-1.0.0.exe
  Size: 95.43 MB

You can now distribute this installer to end users.
Users can double-click the installer to install and run Automation Hub.
```

### Success Indicators

✓ No error messages in output
✓ "Build completed successfully" message appears
✓ "Installer created successfully" message appears
✓ File size is reasonable (80-120 MB)
✓ Process exits normally

## Running build-portable.ps1

### Expected Console Output

```
=== Automation Hub Portable Build Script ===

Checking for .NET SDK...
✓ .NET SDK version 8.0.101 found

Building Automation Hub...
  Configuration: Release
  Runtime: win-x64
  Self-contained: Yes
  Output: C:\automation-hub\publish\win-x64

Cleaning previous build...

Running: dotnet publish AutomationHub.App/AutomationHub.App.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=false -o C:\automation-hub\publish\win-x64
MSBuild version 17.8.5+b5265ef37 for .NET
  Determining projects to restore...
  Restored C:\automation-hub\AutomationHub.Core\AutomationHub.Core.csproj (in 198 ms).
  Restored C:\automation-hub\AutomationHub.Watchers\AutomationHub.Watchers.csproj (in 198 ms).
  Restored C:\automation-hub\AutomationHub.Scheduler\AutomationHub.Scheduler.csproj (in 198 ms).
  Restored C:\automation-hub\AutomationHub.App\AutomationHub.App.csproj (in 1.8 sec).
  AutomationHub.Core -> C:\automation-hub\AutomationHub.Core\bin\Release\net8.0-windows\AutomationHub.Core.dll
  AutomationHub.Watchers -> C:\automation-hub\AutomationHub.Watchers\bin\Release\net8.0-windows\AutomationHub.Watchers.dll
  AutomationHub.Scheduler -> C:\automation-hub\AutomationHub.Scheduler\bin\Release\net8.0-windows\AutomationHub.Scheduler.dll
  AutomationHub.App -> C:\automation-hub\AutomationHub.App\bin\Release\net8.0-windows\win-x64\AutomationHub.App.dll
  AutomationHub.App -> C:\automation-hub\publish\win-x64\
✓ Build completed successfully

Creating portable package...
✓ ZIP archive created

=== Build Complete ===

✓ Portable package created successfully!

Package location:
  C:\automation-hub\AutomationHub-Portable.zip
  Size: 92.18 MB

Distribution instructions:
  1. Share the ZIP file with end users
  2. Users extract the ZIP to any location
  3. Users double-click 'Start-AutomationHub.bat' to run

The launcher will check for .NET 8 Desktop Runtime and guide users if needed.
```

### Success Indicators

✓ No error messages in output
✓ "Build completed successfully" message appears
✓ "Portable package created successfully" message appears
✓ ZIP file created in root directory
✓ File size is reasonable (80-120 MB)

## Common Error Messages and Solutions

### Error: "dotnet command not found"

```
Checking for .NET SDK...
dotnet : The term 'dotnet' is not recognized as the name of a cmdlet, function, script file...
✗ .NET SDK not found. Please install .NET 8.0 SDK or later.
  Download from: https://dotnet.microsoft.com/download/dotnet/8.0
```

**Solution:** Install .NET 8.0 SDK from the provided link

### Error: "Inno Setup not found"

```
Checking for Inno Setup...
✗ Inno Setup not found!

To create the installer, you need to install Inno Setup:
  1. Download from: https://jrsoftware.org/isdl.php
  2. Install Inno Setup 6 (or 5)
  3. Run this script again

Alternatively, you can distribute the application by:
  - Zipping the contents of: C:\automation-hub\publish\win-x64
  - Users can extract and run AutomationHub.App.exe directly
```

**Solution:** Either install Inno Setup or use the portable build method instead

### Error: "Build failed"

```
Building Automation Hub...
...
error MSBuild1234: Some build error occurred
✗ Build failed!
```

**Solution:**
1. Check the error message details
2. Ensure all .csproj files are present
3. Try running `dotnet restore` first
4. Verify .NET 8.0 SDK is correctly installed

### Error: "Publish directory not found" (with -SkipBuild flag)

```
Skipping build (using existing build in C:\automation-hub\publish\win-x64)
✗ Publish directory not found: C:\automation-hub\publish\win-x64
  Run without -SkipBuild flag first
```

**Solution:** Run without the `-SkipBuild` flag to build first

## File Structure After Build

### After build-installer.ps1

```
automation-hub/
├── installer-output/
│   └── AutomationHub-Setup-1.0.0.exe    ← Distributable installer
├── publish/
│   └── win-x64/
│       ├── AutomationHub.App.exe
│       ├── [All DLLs and dependencies]
│       └── config/
└── [Source files...]
```

### After build-portable.ps1

```
automation-hub/
├── AutomationHub-Portable.zip           ← Distributable package
├── publish/
│   └── win-x64/
│       ├── AutomationHub.App.exe
│       ├── [All DLLs and dependencies]
│       └── config/
└── [Source files...]
```

## Build Time Expectations

| Operation | First Time | Subsequent Builds |
|-----------|-----------|-------------------|
| dotnet restore | 5-15 seconds | 1-2 seconds (cached) |
| dotnet publish | 30-90 seconds | 15-30 seconds (incremental) |
| Inno Setup compile | 10-30 seconds | 10-30 seconds |
| ZIP compression | 5-15 seconds | 5-15 seconds |
| **Total** | **2-5 minutes** | **1-2 minutes** |

*Times vary based on machine performance and internet speed*

## Next Steps After Successful Build

1. ✓ Build completed successfully
2. → Test the installer on your machine
3. → Test on a clean machine without development tools
4. → Copy to distribution location (Y: drive or GitHub)
5. → Notify users about new version
6. → Monitor for any installation issues

## Getting Help

If you encounter issues not covered here:

1. Check the error message carefully
2. Review `BUILDING_INSTALLER.md` for detailed troubleshooting
3. Ensure prerequisites are correctly installed
4. Try a clean build (delete `publish/` and rebuild)
5. Check GitHub issues for similar problems

## Verification After Build

### For Installer
```powershell
# Verify file exists
Test-Path "installer-output\AutomationHub-Setup-*.exe"

# Check file size (should be 80-120 MB)
(Get-Item "installer-output\AutomationHub-Setup-*.exe").Length / 1MB
```

### For Portable ZIP
```powershell
# Verify file exists
Test-Path "AutomationHub-Portable.zip"

# Check file size (should be 80-120 MB)
(Get-Item "AutomationHub-Portable.zip").Length / 1MB

# List contents
Add-Type -Assembly System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::OpenRead("AutomationHub-Portable.zip").Entries.FullName
```
