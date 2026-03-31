@echo off
REM Automation Hub Launcher
REM This script checks for .NET Desktop Runtime and launches the application

echo.
echo === Automation Hub ===
echo.

REM Check if .NET Desktop Runtime 8.x is installed
REM Pattern matches "8." followed by any digit, which correctly identifies all 8.x versions
REM Examples: 8.0.1, 8.1.5, 8.10.3, etc. (but not 18.x or 28.x)
dotnet --list-runtimes | findstr /R "Microsoft.WindowsDesktop.App 8\.[0-9]" >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo Error: .NET 8 Desktop Runtime is required but not installed.
    echo.
    echo Please install .NET 8 Desktop Runtime from:
    echo https://dotnet.microsoft.com/download/dotnet/8.0
    echo.
    echo After installation, run this script again.
    echo.
    pause
    exit /b 1
)

REM Launch the application
echo Starting Automation Hub...
start "" "%~dp0AutomationHub.App.exe"
