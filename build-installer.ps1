# Build Installer for Automation Hub
# This script builds the application and creates an installer using Inno Setup

param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$SelfContained = $true,
    [switch]$SkipBuild = $false
)

$ErrorActionPreference = "Stop"

Write-Host "=== Automation Hub Installer Build Script ===" -ForegroundColor Cyan
Write-Host ""

# Get the script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$SolutionDir = $ScriptDir
$ProjectPath = Join-Path $SolutionDir "AutomationHub.App\AutomationHub.App.csproj"
$PublishDir = Join-Path $SolutionDir "publish\$Runtime"
$InnoSetupScript = Join-Path $SolutionDir "installer.iss"
$InstallerOutputDir = Join-Path $SolutionDir "installer-output"

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
if (-not $SkipBuild) {
    Write-Host ""
    Write-Host "Building Automation Hub..." -ForegroundColor Yellow
    Write-Host "  Configuration: $Configuration" -ForegroundColor Gray
    Write-Host "  Runtime: $Runtime" -ForegroundColor Gray
    Write-Host "  Self-contained: $SelfContained" -ForegroundColor Gray
    Write-Host "  Output: $PublishDir" -ForegroundColor Gray
    Write-Host ""

    # Clean previous build
    if (Test-Path $PublishDir) {
        Write-Host "Cleaning previous build..." -ForegroundColor Yellow
        Remove-Item $PublishDir -Recurse -Force
    }

    # Publish the application
    $publishArgs = @(
        "publish"
        $ProjectPath
        "-c", $Configuration
        "-r", $Runtime
        "-o", $PublishDir
    )

    if ($SelfContained) {
        $publishArgs += "--self-contained"
        $publishArgs += "-p:PublishSingleFile=false"
    } else {
        $publishArgs += "--no-self-contained"
    }

    Write-Host "Running: dotnet $($publishArgs -join ' ')" -ForegroundColor Gray
    & dotnet @publishArgs

    if ($LASTEXITCODE -ne 0) {
        Write-Host "✗ Build failed!" -ForegroundColor Red
        exit 1
    }

    Write-Host "✓ Build completed successfully" -ForegroundColor Green
} else {
    Write-Host "Skipping build (using existing build in $PublishDir)" -ForegroundColor Yellow
    if (-not (Test-Path $PublishDir)) {
        Write-Host "✗ Publish directory not found: $PublishDir" -ForegroundColor Red
        Write-Host "  Run without -SkipBuild flag first" -ForegroundColor Yellow
        exit 1
    }
}

# Check for Inno Setup
Write-Host ""
Write-Host "Checking for Inno Setup..." -ForegroundColor Yellow

$InnoSetupPaths = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
    "${env:ProgramFiles}\Inno Setup 6\ISCC.exe"
    "${env:ProgramFiles(x86)}\Inno Setup 7\ISCC.exe"
    "${env:ProgramFiles}\Inno Setup 7\ISCC.exe"
    "${env:ProgramFiles(x86)}\Inno Setup 5\ISCC.exe"
    "${env:ProgramFiles}\Inno Setup 5\ISCC.exe"
)

$InnoSetupExe = $null
foreach ($path in $InnoSetupPaths) {
    if (Test-Path $path) {
        $InnoSetupExe = $path
        break
    }
}

if ($null -eq $InnoSetupExe) {
    Write-Host "✗ Inno Setup not found!" -ForegroundColor Red
    Write-Host ""
    Write-Host "To create the installer, you need to install Inno Setup:" -ForegroundColor Yellow
    Write-Host "  1. Download from: https://jrsoftware.org/isdl.php" -ForegroundColor Yellow
    Write-Host "  2. Install Inno Setup 6 (or 5)" -ForegroundColor Yellow
    Write-Host "  3. Run this script again" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Alternatively, you can distribute the application by:" -ForegroundColor Yellow
    Write-Host "  - Zipping the contents of: $PublishDir" -ForegroundColor Yellow
    Write-Host "  - Users can extract and run AutomationHub.App.exe directly" -ForegroundColor Yellow
    exit 1
}

Write-Host "✓ Inno Setup found at: $InnoSetupExe" -ForegroundColor Green

# Create the installer
Write-Host ""
Write-Host "Creating installer..." -ForegroundColor Yellow
Write-Host "  Script: $InnoSetupScript" -ForegroundColor Gray
Write-Host "  Output: $InstallerOutputDir" -ForegroundColor Gray
Write-Host ""

& $InnoSetupExe $InnoSetupScript

if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Installer creation failed!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "=== Build Complete ===" -ForegroundColor Green
Write-Host ""
Write-Host "✓ Installer created successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Installer location:" -ForegroundColor Yellow
Get-ChildItem $InstallerOutputDir -Filter "*.exe" | ForEach-Object {
    Write-Host "  $($_.FullName)" -ForegroundColor Cyan
    Write-Host "  Size: $([math]::Round($_.Length / 1MB, 2)) MB" -ForegroundColor Gray
}
Write-Host ""
Write-Host "You can now distribute this installer to end users." -ForegroundColor Yellow
Write-Host "Users can double-click the installer to install and run Automation Hub." -ForegroundColor Yellow
