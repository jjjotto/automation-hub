# Running the Automation Hub Application

## Overview

Automation Hub is a WPF desktop application that provides a centralized GUI for managing automation jobs in the JO lab. This document explains how to run the application and provides visual documentation of the user interface.

## Prerequisites

Before running the application, ensure you have:

1. **Windows 10 (22H2+) or Windows 11** - This is a WPF application and requires Windows
2. **.NET 8.0 SDK or newer** - Download from https://dotnet.microsoft.com/download/dotnet/8.0
3. **Visual Studio 2022** (optional, recommended for development) - Version 17.8+ with ".NET desktop development" workload
4. **Network Access** - Access to the `Y:\temporary_files\JO\automation` share (for production use)

## Configuration Options

The application supports two configuration modes:

### Production Mode (Network Drive)
- Uses `Y:\temporary_files\JO\automation` as the root directory
- Jobs are loaded from `Y:\temporary_files\JO\automation\config\jobs\`
- Logs are written to `Y:\temporary_files\JO\automation\logs\`

### Development Mode (Local Config)
- If the Y: drive is not available, the app will automatically fall back to a `local-config` directory
- The `local-config` directory should be placed in the repository root
- Jobs are loaded from `local-config/config/jobs/`
- Logs are written to `local-config/logs/`

To set up local development:
```bash
mkdir -p local-config/config/jobs
mkdir -p local-config/logs
cp config/jobs/sample-job.json local-config/config/jobs/
```

## Building the Application

### Using Command Line

1. Clone the repository:
   ```bash
   git clone https://github.com/jjjotto/automation-hub.git
   cd automation-hub
   ```

2. Restore dependencies:
   ```bash
   dotnet restore
   ```

3. Build the solution:
   ```bash
   dotnet build
   ```

4. Run the application:
   ```bash
   dotnet run --project AutomationHub.App/AutomationHub.App.csproj
   ```

### Using Visual Studio

1. Open `AutomationHub.sln` in Visual Studio 2022
2. Set `AutomationHub.App` as the startup project (right-click → Set as Startup Project)
3. Press F5 or click the "Start" button to run with debugging
4. Press Ctrl+F5 to run without debugging

## Publishing for Deployment

To create a standalone executable for distribution:

```bash
dotnet publish AutomationHub.App/AutomationHub.App.csproj -c Release -r win-x64 -p:PublishSingleFile=true -o publish
```

This creates a self-contained executable in the `publish` folder that can be distributed to other machines.

For deployment to the shared drive:
```bash
dotnet publish AutomationHub.App/AutomationHub.App.csproj -c Release -r win-x64 -p:PublishSingleFile=true -o Y:\temporary_files\JO\automation\AutomationHub
```

## User Interface

### Main Window

The Automation Hub main window displays a list of all configured automation jobs with the following features:

**Window Title:** "Automation Hub"
**Window Size:** 960 x 450 pixels (default)

**Layout:**
```
┌────────────────────────────────────────────────────────────────┐
│ Automation Hub                                            [_][□][X] │
├────────────────────────────────────────────────────────────────┤
│  Automation Hub Jobs                                           │
│                                                                │
│ ┌──────────────────────────────────────────────────────────┐ │
│ │ Name         │ Type    │ Enabled │ Command        │ Trigger Path     │ │
│ ├──────────────────────────────────────────────────────────┤ │
│ │ Sample ECL1  │ Hybrid  │ ☑      │ Y:/temp...bat │ Y:/temp...ECL1  │ │
│ │ QC Export    │         │        │                │                  │ │
│ │              │         │        │                │                  │ │
│ └──────────────────────────────────────────────────────────┘ │
│                                                                │
│ Loaded 1 job(s)                                               │
└────────────────────────────────────────────────────────────────┘
```

### UI Components

1. **Header Section**
   - Large, bold title: "Automation Hub Jobs" (20pt font)

2. **Jobs Data Grid** (Main Content Area)
   - **Name Column** (2x width) - Display name of the job
   - **Type Column** (1x width) - Job type (Manual, Scheduled, FileTrigger, Hybrid, etc.)
   - **Enabled Column** (Auto width) - Checkbox showing if job is enabled
   - **Command Column** (2x width) - The command/script that will be executed
   - **Trigger Path Column** (2x width) - The file path being monitored for file-trigger jobs

3. **Status Bar** (Bottom)
   - Italic, gray text showing current status
   - Examples: "Ready", "Loaded 1 job(s)", "No jobs defined", "Error loading jobs: ..."

### Job Types

The application supports several job types:

- **Manual (1)**: Jobs that are triggered manually only
- **Scheduled (2)**: Jobs that run on a schedule (cron expression)
- **FileTrigger (4)**: Jobs that run when files are detected
- **Hybrid (7)**: Combination of Manual, Scheduled, and FileTrigger
- **ManualAndFile (5)**: Manual + FileTrigger
- **ManualAndScheduled (3)**: Manual + Scheduled

### Sample Job Data

The application comes with a sample job configuration (`config/jobs/sample-job.json`):

```json
{
  "name": "Sample ECL1 QC Export",
  "type": "Hybrid",
  "enabled": true,
  "process": {
    "command": "Y:/temporary_files/JO/automation/AutoQC-UTSW/Batch_ImportNew_ExportRT.bat",
    "workingDirectory": "Y:/temporary_files/JO/automation/AutoQC-UTSW"
  },
  "fileTrigger": {
    "watchPath": "Y:/temporary_files/JO/automation/ECL1",
    "includeSubfolders": true,
    "instrumentType": "Thermo",
    "acquisitionMinutes": 90,
    "filter": {
      "kind": "startsWith",
      "pattern": "ECL1_HeLa_"
    }
  },
  "schedule": {
    "cron": "0 0 6 ? * MON-FRI",
    "timezone": "Central Standard Time"
  }
}
```

## Application Behavior

### Startup
1. Application window opens with title "Automation Hub"
2. Attempts to load jobs from the configured directory
3. Displays status message indicating success or failure
4. Populates the data grid with loaded jobs

### Status Messages
- **"Ready"** - Initial state before loading jobs
- **"Loaded N job(s)"** - Successfully loaded N jobs
- **"No jobs defined"** - Jobs directory exists but contains no .json files
- **"Jobs directory not found: [path]"** - Configuration directory doesn't exist
- **"Error loading jobs: [error]"** - An exception occurred during loading

## Troubleshooting

### "Jobs directory not found" Error

**Problem:** The application can't find the jobs configuration directory.

**Solutions:**
1. For production: Ensure you have access to `Y:\temporary_files\JO\automation\config\jobs\`
2. For development: Create a `local-config/config/jobs/` directory in the repository root
3. Verify network connectivity to the Y: drive
4. Check that the share is mapped correctly on your system

### Application Won't Start

**Problem:** The application fails to launch.

**Solutions:**
1. Verify .NET 8.0 Desktop Runtime is installed: `dotnet --list-runtimes`
2. If missing, install from: https://dotnet.microsoft.com/download/dotnet/8.0
3. Check Windows Event Viewer for .NET runtime errors
4. Try running from command line to see detailed error messages

### Empty Job List

**Problem:** The application starts but shows "No jobs defined".

**Solutions:**
1. Verify .json files exist in the jobs directory
2. Check that .json files are valid JSON format
3. Ensure files match the expected schema (see sample-job.json)
4. Check file permissions - application needs read access

## Future Enhancements

The application is designed to evolve with the following planned features:
- Interactive controls to enable/disable jobs
- Manual trigger buttons for each job
- Start/Stop controls for scheduler and watchers
- Real-time status updates for running jobs
- Job execution history and logs viewer
- Configuration editor for adding/modifying jobs
- Integration with Quartz.NET for advanced scheduling
- File system watcher integration for real-time file triggers

## Support

For issues or questions:
1. Check the GitHub repository: https://github.com/jjjotto/automation-hub
2. Review `requirements.md` for detailed prerequisites
3. Check the `README.md` for architecture overview
4. Review sample configurations in `config/jobs/`
