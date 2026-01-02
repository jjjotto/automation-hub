# Automation Hub - Quick Start Guide for End Users

## What is Automation Hub?

Automation Hub is a desktop application that helps you manage and monitor automated jobs in the lab. It provides a simple interface to view, enable/disable, and trigger automation scripts.

## Installation

### Option 1: Windows Installer (Easiest)

1. **Download** the installer file: `AutomationHub-Setup-x.x.x.exe`
2. **Double-click** the installer
3. If prompted about .NET Runtime:
   - Click "Yes" to open the download page
   - Download and install ".NET 8 Desktop Runtime" for Windows x64
   - Run the Automation Hub installer again
4. Follow the installation wizard
5. **Launch** from Start Menu → "Automation Hub"

### Option 2: Portable Package

1. **Download** the ZIP file: `AutomationHub-Portable.zip`
2. **Extract** to any location (e.g., your Desktop or Documents folder)
3. Open the extracted folder
4. **Double-click** `Start-AutomationHub.bat`
5. If prompted about .NET Runtime, download and install it, then try again

## First Run

When you first open Automation Hub:

1. A window will open showing "Automation Hub Jobs"
2. The application will try to load jobs from:
   - `Y:\temporary_files\JO\automation\config\jobs\` (if Y: drive is available)
   - Or local `config\jobs\` directory
3. You should see a list of configured automation jobs

## Using the Application

### Main Window

The main window displays all automation jobs with:

- **Name**: Job description
- **Type**: How the job is triggered (Manual, Scheduled, FileTrigger, or Hybrid)
- **Enabled**: Whether the job is currently active
- **Command**: The script or program that runs
- **Trigger Path**: The folder being monitored (for file-triggered jobs)

### Current Features

The current version displays job configurations and their status. Future updates will add:
- Buttons to manually trigger jobs
- Enable/disable toggles
- View job execution logs
- Real-time status updates

## Configuration

### Adding New Jobs

To add a new automation job:

1. Navigate to the jobs directory:
   - Network: `Y:\temporary_files\JO\automation\config\jobs\`
   - Local: `[InstallDir]\config\jobs\`
2. Create a new `.json` file (copy `sample-job.json` as a template)
3. Edit the file with your job settings
4. Restart Automation Hub to load the new job

### Sample Job Configuration

```json
{
  "name": "My Automation Job",
  "type": "Hybrid",
  "enabled": true,
  "process": {
    "command": "C:\\path\\to\\script.bat",
    "workingDirectory": "C:\\path\\to"
  },
  "fileTrigger": {
    "watchPath": "C:\\path\\to\\watch",
    "includeSubfolders": true,
    "instrumentType": "Thermo",
    "acquisitionMinutes": 90
  },
  "schedule": {
    "cron": "0 0 6 ? * MON-FRI",
    "timezone": "Central Standard Time"
  }
}
```

## System Requirements

- **Operating System**: Windows 10 (version 22H2 or newer) or Windows 11
- **.NET 8 Desktop Runtime**: Downloaded automatically during installation if needed
- **Disk Space**: ~150 MB for application and runtime
- **Memory**: 100 MB RAM
- **Network**: Access to Y: drive (for production use)

## Troubleshooting

### "Application won't start"

**Problem**: Double-clicking does nothing or shows an error.

**Solution**:
1. Open Command Prompt (Windows Key + R, type `cmd`, press Enter)
2. Type: `dotnet --list-runtimes`
3. Look for a line containing `Microsoft.WindowsDesktop.App 8.`
4. If not found, install .NET 8 Desktop Runtime:
   - Go to: https://dotnet.microsoft.com/download/dotnet/8.0
   - Download "Desktop Runtime" for Windows x64
   - Install and restart

### "No jobs defined" message

**Problem**: Application starts but shows "No jobs defined".

**Solution**:
1. Check if the jobs directory exists:
   - Network: `Y:\temporary_files\JO\automation\config\jobs\`
   - Local: `[InstallDir]\config\jobs\`
2. Ensure there are `.json` files in the directory
3. Verify the JSON files are valid (copy from `sample-job.json`)

### "Jobs directory not found" message

**Problem**: Application can't find the configuration directory.

**Solution**:
- **For network drive**: Ensure Y: drive is mapped and accessible
- **For portable version**: Create a `config\jobs\` folder next to the executable
- Copy sample job: From the installation, copy `config\jobs\sample-job.json`

### Windows SmartScreen Warning

**Problem**: Windows shows "Windows protected your PC" when running the installer.

**Solution**:
1. Click "More info"
2. Click "Run anyway"
3. This is normal for applications without a code signature

## Getting Help

For additional support:

- **Documentation**: See `README.md` and `RUNNING_THE_APP.md` in the installation directory
- **Issues**: Report bugs at https://github.com/jjjotto/automation-hub/issues
- **Lab Support**: Contact your lab IT support

## Uninstalling

### If Installed with Installer:
1. Open Windows Settings
2. Go to "Apps" → "Installed apps"
3. Find "Automation Hub"
4. Click "..." → "Uninstall"

### If Using Portable Version:
1. Close the application
2. Delete the extracted folder
3. (Optional) Delete any local config folders you created

## Updates

To update to a new version:

1. Download the new installer or portable package
2. If using installer: Run the new installer (will replace old version)
3. If using portable: Extract to a new location or overwrite existing files

Your job configurations in `config/jobs/` will be preserved.

## Privacy and Security

- Automation Hub runs locally on your computer
- No data is sent to external servers
- Job definitions and logs are stored locally or on the Y: drive
- The application only accesses files and folders you configure in job definitions

## What's Next?

Future versions of Automation Hub will include:

- Interactive job controls (start, stop, enable, disable)
- Real-time job execution monitoring
- Viewing job logs within the application
- Job execution history and statistics
- Notifications for job completion or failures

Stay tuned for updates!
