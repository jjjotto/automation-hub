# Automation Hub Requirements

This document outlines the software and environment prerequisites for building, running, and deploying the Automation Hub desktop application.

## Target environment

- **Operating system:** Windows 10 (22H2+) or Windows 11 with access to the shared drive `Y:`
- **Network:** Persistent connectivity to the on-prem share `Y:\temporary_files\JO\automation` (read/write)
- **User permissions:** Ability to execute PowerShell/BAT/R/Python scripts referenced by jobs

## Developer workstation prerequisites

| Component | Minimum version | Notes |
|-----------|-----------------|-------|
| .NET SDK  | 8.0.100 or newer | Required to build all projects in the solution |
| .NET Desktop Runtime | 8.0.x | Needed to run the published WPF application |
| Visual Studio 2022 | 17.8+ (optional) | Install with ".NET desktop development" workload; VS Code + C# extension also works |
| Git | Latest | For source control |

### Recommended Visual Studio workloads/components

- .NET desktop development
- Windows 10/11 SDK (10.0.19041 or newer)
- NuGet package manager (installed by default)

### Optional tooling

- PowerShell 7 for richer scripting (PowerShell 5.1 works if already deployed)
- R 4.x and Python 3.10+ if jobs invoke the scripts from `C:\automation`
- 7-Zip for packaging artifacts copied to `Y:`

## Shared drive layout

Automation Hub expects the following structure on the network share:

```
Y:\temporary_files\JO\automation
├── AutomationHub\        # Published binaries
├── config\
│   └── jobs\             # JSON job definitions
├── logs\                 # Job output logs
└── scripts\              # Optional helper scripts invoked by jobs
```

## External NuGet dependencies

The scaffold currently compiles without third-party packages so it can be cloned offline. When internet access is available, install the planned packages:

```
# Scheduler project
cd AutomationHub.Scheduler
 dotnet add package Quartz

# WPF application (MVVM helper)
cd ..\AutomationHub.App
 dotnet add package CommunityToolkit.Mvvm
```

After installing packages, run `dotnet restore` at the solution root.

## Build commands

From `automation_hub/`:

```
dotnet restore
dotnet build
```

## Publish command (self-contained x64)

```
dotnet publish AutomationHub.App/AutomationHub.App.csproj -c Release -r win-x64 -p:PublishSingleFile=true -o Y:\temporary_files\JO\automation\AutomationHub
```

This produces a portable EXE that other lab machines can launch directly from the share.
