# Automation Hub - Portable Distribution

## Installation

1. Extract this ZIP file to any location on your computer
2. Make sure .NET 8 Desktop Runtime is installed
   - Download from: https://dotnet.microsoft.com/download/dotnet/8.0
   - Choose "Desktop Runtime" for Windows x64

## Running the Application

### Option 1: Use the Launcher (Recommended)
Double-click Start-AutomationHub.bat to launch the application.
The launcher will check if .NET Runtime is installed and guide you if needed.

### Option 2: Direct Launch
Double-click AutomationHub.App.exe to run directly.

## Configuration

- Job definitions: Place .json files in config/jobs/
- See config/jobs/sample-job.json for an example
- Logs will be created in the logs/ directory

## Network Drive Setup

For production use with shared network drive:
- The application can use Y:\temporary_files\JO\automation as the root directory
- If Y: drive is available, it will be used automatically
- Otherwise, local config/ and logs/ directories will be used

## Support

For issues or questions, visit:
https://github.com/jjjotto/automation-hub
