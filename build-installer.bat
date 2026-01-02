@echo off
REM Build Installer for Automation Hub
REM This batch file calls the PowerShell build script

echo.
echo === Automation Hub Installer Build ===
echo.

REM Check if PowerShell is available
where powershell >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo Error: PowerShell not found!
    echo PowerShell is required to run the build script.
    pause
    exit /b 1
)

REM Run the PowerShell build script
powershell -ExecutionPolicy Bypass -File "%~dp0build-installer.ps1" %*

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo Build failed!
    pause
    exit /b 1
)

echo.
echo Build completed successfully!
echo.
pause
