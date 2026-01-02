# Build Portable Distribution for Automation Hub
# This script creates a ZIP file that can be distributed without an installer

param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

Write-Host "=== Automation Hub Portable Build Script ===" -ForegroundColor Cyan
Write-Host ""

# Get the script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$SolutionDir = $ScriptDir
$ProjectPath = Join-Path $SolutionDir "AutomationHub.App\AutomationHub.App.csproj"
$PublishDir = Join-Path $SolutionDir "publish\$Runtime"
$PortableDir = Join-Path $SolutionDir "portable-output"

# Check if .NET SDK is installed
Write-Host "Checking for .NET SDK..." -ForegroundColor Yellow
try {
    $dotnetVersion = dotnet --version
    Write-Host "✓ .NET SDK version $dotnetVersion found" -ForegroundColor Green
} catch {
    Write-Host "✗ .NET SDK not found. Please install .NET 8.0 SDK or later." -ForegroundColor Red
    Write-Host "  Download from: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Yellow
    exit 1
}

# Build the application
Write-Host ""
Write-Host "Building Automation Hub..." -ForegroundColor Yellow
Write-Host "  Configuration: $Configuration" -ForegroundColor Gray
Write-Host "  Runtime: $Runtime" -ForegroundColor Gray
Write-Host "  Self-contained: Yes" -ForegroundColor Gray
Write-Host "  Output: $PublishDir" -ForegroundColor Gray
Write-Host ""

# Clean previous build
if (Test-Path $PublishDir) {
    Write-Host "Cleaning previous build..." -ForegroundColor Yellow
    Remove-Item $PublishDir -Recurse -Force
}

# Publish the application (self-contained)
$publishArgs = @(
    "publish"
    $ProjectPath
    "-c", $Configuration
    "-r", $Runtime
    "--self-contained"
    "-p:PublishSingleFile=false"
    "-o", $PublishDir
)

Write-Host "Running: dotnet $($publishArgs -join ' ')" -ForegroundColor Gray
& dotnet @publishArgs

if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "✓ Build completed successfully" -ForegroundColor Green

# Create portable package directory
Write-Host ""
Write-Host "Creating portable package..." -ForegroundColor Yellow

if (Test-Path $PortableDir) {
    Remove-Item $PortableDir -Recurse -Force
}
New-Item -ItemType Directory -Path $PortableDir | Out-Null

# Copy published files
$PackageDir = Join-Path $PortableDir "AutomationHub"
Copy-Item $PublishDir $PackageDir -Recurse

# Copy config files
$ConfigSource = Join-Path $SolutionDir "config"
$ConfigDest = Join-Path $PackageDir "config"
if (Test-Path $ConfigSource) {
    Copy-Item $ConfigSource $ConfigDest -Recurse
}

# Create a launcher script
$LauncherScript = @"
@echo off
REM Automation Hub Launcher
REM This script checks for .NET Desktop Runtime and launches the application

echo.
echo === Automation Hub ===
echo.

REM Check if .NET Desktop Runtime 8.x is installed (matches 8.0.x, 8.1.x, etc., but not 18.x)
dotnet --list-runtimes | findstr "Microsoft.WindowsDesktop.App 8\." >nul 2>nul
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
"@

$LauncherPath = Join-Path $PackageDir "Start-AutomationHub.bat"
$LauncherScript | Out-File -FilePath $LauncherPath -Encoding ASCII

# Create README
$ReadmeContent = @"
# Automation Hub - Portable Distribution

## Installation

1. Extract this ZIP file to any location on your computer
2. Make sure .NET 8 Desktop Runtime is installed
   - Download from: https://dotnet.microsoft.com/download/dotnet/8.0
   - Choose "Desktop Runtime" for Windows x64

## Running the Application

### Option 1: Use the Launcher (Recommended)
Double-click `Start-AutomationHub.bat` to launch the application.
The launcher will check if .NET Runtime is installed and guide you if needed.

### Option 2: Direct Launch
Double-click `AutomationHub.App.exe` to run directly.

## Configuration

- Job definitions: Place .json files in `config/jobs/`
- See `config/jobs/sample-job.json` for an example
- Logs will be created in the `logs/` directory

## Network Drive Setup

For production use with shared network drive:
- The application can use `Y:\temporary_files\JO\automation` as the root directory
- If Y: drive is available, it will be used automatically
- Otherwise, local `config/` and `logs/` directories will be used

## Support

For issues or questions, visit:
https://github.com/jjjotto/automation-hub
"@

$ReadmePath = Join-Path $PortableDir "README.txt"
$ReadmeContent | Out-File -FilePath $ReadmePath -Encoding UTF8

# Create ZIP file
Write-Host ""
Write-Host "Creating ZIP archive..." -ForegroundColor Yellow

$ZipPath = Join-Path $SolutionDir "AutomationHub-Portable.zip"
if (Test-Path $ZipPath) {
    Remove-Item $ZipPath -Force
}

# Use PowerShell's Compress-Archive for better compatibility
try {
    Compress-Archive -Path "$PortableDir\*" -DestinationPath $ZipPath -CompressionLevel Optimal
} catch {
    # Fallback to .NET compression if Compress-Archive fails
    Add-Type -Assembly System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::CreateFromDirectory($PortableDir, $ZipPath, 'Optimal', $false)
}

Write-Host "✓ ZIP archive created" -ForegroundColor Green

# Cleanup temporary directory
Remove-Item $PortableDir -Recurse -Force

Write-Host ""
Write-Host "=== Build Complete ===" -ForegroundColor Green
Write-Host ""
Write-Host "✓ Portable package created successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Package location:" -ForegroundColor Yellow
$ZipFile = Get-Item $ZipPath
Write-Host "  $($ZipFile.FullName)" -ForegroundColor Cyan
Write-Host "  Size: $([math]::Round($ZipFile.Length / 1MB, 2)) MB" -ForegroundColor Gray
Write-Host ""
Write-Host "Distribution instructions:" -ForegroundColor Yellow
Write-Host "  1. Share the ZIP file with end users" -ForegroundColor White
Write-Host "  2. Users extract the ZIP to any location" -ForegroundColor White
Write-Host "  3. Users double-click 'Start-AutomationHub.bat' to run" -ForegroundColor White
Write-Host ""
Write-Host "The launcher will check for .NET 8 Desktop Runtime and guide users if needed." -ForegroundColor Yellow
