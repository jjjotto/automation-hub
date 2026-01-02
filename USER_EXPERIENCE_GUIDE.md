# User Experience Guide - Installing Automation Hub

This guide shows the complete user experience from download to running the application.

## Scenario 1: Installing with Setup.exe

### Step 1: Download
User receives or downloads: `AutomationHub-Setup-1.0.0.exe`

**File Properties:**
- Size: ~95 MB
- Type: Application (.exe)
- Icon: Default Inno Setup icon (can be customized)

### Step 2: Run Installer
User double-clicks the Setup.exe file.

**Windows SmartScreen (if not signed):**
```
┌─────────────────────────────────────────┐
│  Windows protected your PC              │
│                                         │
│  Windows Defender SmartScreen prevented │
│  an unrecognized app from starting.     │
│                                         │
│  App: AutomationHub-Setup-1.0.0.exe    │
│  Publisher: Unknown publisher           │
│                                         │
│  [ More info ]                          │
└─────────────────────────────────────────┘
```

**User Action:** Click "More info" → "Run anyway"

### Step 3: .NET Runtime Check

**If .NET 8 Desktop Runtime is NOT installed:**
```
┌─────────────────────────────────────────────────┐
│  Automation Hub Setup                           │
├─────────────────────────────────────────────────┤
│                                                 │
│  .NET 8 Desktop Runtime is required but not     │
│  installed.                                     │
│                                                 │
│  Would you like to open the download page?      │
│                                                 │
│  After installing .NET 8 Desktop Runtime,       │
│  please run this installer again.               │
│                                                 │
│                                                 │
│                    [ Yes ]  [ No ]              │
└─────────────────────────────────────────────────┘
```

**User Action:**
- Click "Yes" → Browser opens to Microsoft download page
- User downloads and installs .NET 8 Desktop Runtime
- User runs the Automation Hub installer again

**If .NET 8 Desktop Runtime IS installed:**
Installer proceeds immediately to Step 4.

### Step 4: Welcome Screen
```
┌─────────────────────────────────────────────────┐
│  Setup - Automation Hub                         │
├─────────────────────────────────────────────────┤
│                                                 │
│  Welcome to the Automation Hub Setup Wizard    │
│                                                 │
│  This will install Automation Hub on your       │
│  computer.                                      │
│                                                 │
│  It is recommended that you close all other     │
│  applications before continuing.                │
│                                                 │
│  Click Next to continue, or Cancel to exit      │
│  Setup.                                         │
│                                                 │
│              [ Next > ]  [ Cancel ]             │
└─────────────────────────────────────────────────┘
```

### Step 5: Select Destination
```
┌─────────────────────────────────────────────────┐
│  Setup - Automation Hub                         │
├─────────────────────────────────────────────────┤
│                                                 │
│  Select Destination Location                    │
│                                                 │
│  Where should Automation Hub be installed?      │
│                                                 │
│  Setup will install Automation Hub into the     │
│  following folder.                              │
│                                                 │
│  ┌────────────────────────────────────────┐    │
│  │ C:\Program Files\Automation Hub        │    │
│  └────────────────────────────────────────┘    │
│                                   [ Browse... ] │
│                                                 │
│  Disk Space Required: 120 MB                    │
│  Disk Space Available: 234 GB                   │
│                                                 │
│        [ < Back ]  [ Next > ]  [ Cancel ]       │
└─────────────────────────────────────────────────┘
```

### Step 6: Start Menu Folder
```
┌─────────────────────────────────────────────────┐
│  Setup - Automation Hub                         │
├─────────────────────────────────────────────────┤
│                                                 │
│  Select Start Menu Folder                       │
│                                                 │
│  Where should Setup place the program's         │
│  shortcuts?                                     │
│                                                 │
│  Setup will create the program's shortcuts in   │
│  the following Start Menu folder.               │
│                                                 │
│  ┌────────────────────────────────────────┐    │
│  │ Automation Hub                         │    │
│  └────────────────────────────────────────┘    │
│                                   [ Browse... ] │
│                                                 │
│  □ Don't create a Start Menu folder             │
│                                                 │
│        [ < Back ]  [ Next > ]  [ Cancel ]       │
└─────────────────────────────────────────────────┘
```

### Step 7: Additional Tasks
```
┌─────────────────────────────────────────────────┐
│  Setup - Automation Hub                         │
├─────────────────────────────────────────────────┤
│                                                 │
│  Select Additional Tasks                        │
│                                                 │
│  Which additional tasks should be performed?    │
│                                                 │
│  Select the additional tasks you would like     │
│  Setup to perform while installing Automation   │
│  Hub, then click Next.                          │
│                                                 │
│  Additional icons:                              │
│    ☐ Create a desktop icon                     │
│                                                 │
│                                                 │
│        [ < Back ]  [ Next > ]  [ Cancel ]       │
└─────────────────────────────────────────────────┘
```

### Step 8: Ready to Install
```
┌─────────────────────────────────────────────────┐
│  Setup - Automation Hub                         │
├─────────────────────────────────────────────────┤
│                                                 │
│  Ready to Install                               │
│                                                 │
│  Setup is now ready to begin installing         │
│  Automation Hub on your computer.               │
│                                                 │
│  Click Install to continue with the             │
│  installation, or click Back if you want to     │
│  review or change any settings.                 │
│                                                 │
│  Destination location:                          │
│    C:\Program Files\Automation Hub              │
│                                                 │
│  Start Menu folder:                             │
│    Automation Hub                               │
│                                                 │
│        [ < Back ]  [ Install ]  [ Cancel ]      │
└─────────────────────────────────────────────────┘
```

### Step 9: Installing
```
┌─────────────────────────────────────────────────┐
│  Setup - Automation Hub                         │
├─────────────────────────────────────────────────┤
│                                                 │
│  Installing                                     │
│                                                 │
│  Please wait while Setup installs Automation    │
│  Hub on your computer.                          │
│                                                 │
│  Extracting files...                            │
│  ████████████████░░░░░░░░░░░░░░░░░░░░ 67%      │
│                                                 │
│  Current file:                                  │
│  AutomationHub.App.dll                          │
│                                                 │
│  Status: Copying files...                       │
│                                                 │
│                        [ Cancel ]               │
└─────────────────────────────────────────────────┘
```

**Installation Time:** Approximately 10-30 seconds

### Step 10: Completing Setup
```
┌─────────────────────────────────────────────────┐
│  Setup - Automation Hub                         │
├─────────────────────────────────────────────────┤
│                                                 │
│  Completing the Automation Hub Setup Wizard     │
│                                                 │
│  Setup has finished installing Automation Hub   │
│  on your computer. The application may be       │
│  launched by selecting the installed shortcuts. │
│                                                 │
│  Click Finish to exit Setup.                    │
│                                                 │
│                                                 │
│  ☑ Launch Automation Hub                        │
│                                                 │
│                                                 │
│                           [ Finish ]            │
└─────────────────────────────────────────────────┘
```

**User Action:** Click "Finish" (optionally with "Launch" checked)

### Step 11: Application Runs
The Automation Hub application window opens!

```
┌──────────────────────────────────────────────────────┐
│  Automation Hub                            [_][□][X] │
├──────────────────────────────────────────────────────┤
│                                                      │
│  Automation Hub Jobs                                 │
│                                                      │
│  ┌────────────────────────────────────────────────┐ │
│  │ Name    │ Type │ Enabled │ Command │ Trigger  │ │
│  ├────────────────────────────────────────────────┤ │
│  │ Sample  │Hybrid│   ☑     │ batch.. │ Y:/...   │ │
│  └────────────────────────────────────────────────┘ │
│                                                      │
│  Loaded 1 job(s)                                     │
└──────────────────────────────────────────────────────┘
```

### What Got Installed

**File System:**
```
C:\Program Files\Automation Hub\
├── AutomationHub.App.exe          (Main application)
├── *.dll                          (Dependencies)
├── config\
│   └── jobs\
│       └── sample-job.json
└── (many .NET runtime files)
```

**Start Menu:**
```
Start Menu > Programs > Automation Hub
├── Automation Hub (shortcut)
└── Uninstall Automation Hub (shortcut)
```

**Desktop** (if selected):
```
Desktop\Automation Hub (shortcut)
```

---

## Scenario 2: Using Portable ZIP

### Step 1: Download
User receives or downloads: `AutomationHub-Portable.zip`

**File Properties:**
- Size: ~92 MB
- Type: Compressed (zipped) Folder
- Contains: AutomationHub folder

### Step 2: Extract
User right-clicks → "Extract All..." → Chooses destination

**Extracted Contents:**
```
[Chosen Location]\AutomationHub\
├── AutomationHub.App.exe
├── Start-AutomationHub.bat        ← Launch this
├── README.txt
├── *.dll (many files)
├── config\
│   └── jobs\
│       └── sample-job.json
└── (many .NET runtime files)
```

### Step 3: Run Launcher
User double-clicks `Start-AutomationHub.bat`

**Console Window Appears:**

**If .NET 8 Desktop Runtime is NOT installed:**
```
═══════════════════════════════════════
=== Automation Hub ===

Error: .NET 8 Desktop Runtime is required but not installed.

Please install .NET 8 Desktop Runtime from:
https://dotnet.microsoft.com/download/dotnet/8.0

After installation, run this script again.

Press any key to continue...
═══════════════════════════════════════
```

**User Action:**
- Visit the URL
- Download and install .NET 8 Desktop Runtime
- Run Start-AutomationHub.bat again

**If .NET 8 Desktop Runtime IS installed:**
```
═══════════════════════════════════════
=== Automation Hub ===

Starting Automation Hub...
═══════════════════════════════════════
```

**Console closes, and the application window opens:**
```
┌──────────────────────────────────────────────────────┐
│  Automation Hub                            [_][□][X] │
├──────────────────────────────────────────────────────┤
│                                                      │
│  Automation Hub Jobs                                 │
│                                                      │
│  ┌────────────────────────────────────────────────┐ │
│  │ Name    │ Type │ Enabled │ Command │ Trigger  │ │
│  ├────────────────────────────────────────────────┤ │
│  │ Sample  │Hybrid│   ☑     │ batch.. │ Y:/...   │ │
│  └────────────────────────────────────────────────┘ │
│                                                      │
│  Loaded 1 job(s)                                     │
└──────────────────────────────────────────────────────┘
```

---

## Comparison: Which Method to Choose?

### For IT/Lab Administrators
✅ Use **Windows Installer** for:
- Deploying to multiple machines
- Central management
- Standard lab installations
- Professional appearance

### For Individual Users
✅ Use **Portable ZIP** for:
- No admin rights
- Testing on different machines
- Keeping on USB drive
- Temporary usage

### For Developers
✅ Use **Portable ZIP** for:
- Quick testing
- Development environments
- Moving between machines

---

## Uninstalling

### Windows Installer Method
1. Open Windows Settings
2. Go to "Apps" → "Installed apps"
3. Find "Automation Hub"
4. Click "..." → "Uninstall"
5. Confirm uninstallation
6. Application and shortcuts are removed

### Portable ZIP Method
1. Close the application
2. Delete the extracted folder
3. That's it!

---

## Troubleshooting Common User Issues

### Issue: "Windows protected your PC" warning

**Cause:** Application is not digitally signed

**Solution:**
1. Click "More info"
2. Click "Run anyway"
3. This is safe - it's just not signed with a code certificate

### Issue: ".NET 8 Desktop Runtime is required"

**Cause:** .NET Runtime not installed

**Solution:**
1. Visit https://dotnet.microsoft.com/download/dotnet/8.0
2. Download "Desktop Runtime" for Windows x64
3. Install it
4. Run the Automation Hub installer/launcher again

### Issue: "Jobs directory not found"

**Cause:** Application can't find configuration directory

**Solution:**
- Check if Y: drive is mapped (for production)
- Create local config folder: `[InstallDir]\config\jobs\`
- Copy sample-job.json to get started

### Issue: Application won't start

**Cause:** Various - missing dependencies, corrupted installation

**Solution:**
1. Verify .NET 8 Desktop Runtime: Open CMD, run `dotnet --list-runtimes`
2. Look for: `Microsoft.WindowsDesktop.App 8.x.x`
3. If missing, install from Microsoft
4. If present, try reinstalling Automation Hub

---

## User Success Story

**Before:**
"I had to manually configure .NET, copy files, set up paths... it was complicated."

**After:**
"I just double-clicked the installer, pressed Next a few times, and it works! The .NET check was automatic."

**Result:**
Users can focus on using the application, not installing it! ✅
