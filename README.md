# Automation Hub

Automation Hub is a WPF desktop utility that centralizes automation jobs for the JO lab. It provides a single GUI to enable/disable jobs, inspect schedules, manually trigger scripts, and react to file-based events similar to AutoQC.

## Solution structure

| Project | Description |
|---------|-------------|
| **AutomationHub.App** | WPF front-end that lists jobs and will host control widgets. |
| **AutomationHub.Core** | Shared contracts for job definitions, configuration paths, and execution results. |
| **AutomationHub.Watchers** | File monitoring primitives that wrap `FileSystemWatcher` with filtering logic. |
| **AutomationHub.Scheduler** | Placeholder scheduler service that will evolve into a Quartz-based engine. |

Supporting assets:

- `config/jobs/sample-job.json` – starter job manifest.
- `requirements.md` – up-to-date installation and tooling checklist.

## Documentation and Screenshots

For detailed information about running and using the application:

- **[RUNNING_THE_APP.md](RUNNING_THE_APP.md)** – Complete guide on how to build, run, and configure the application
- **[UI_SCREENSHOTS.md](UI_SCREENSHOTS.md)** – Detailed UI specifications and visual mockups
- **[ui-mockup.html](ui-mockup.html)** – Interactive HTML mockup of the application interface
- **[docs/](docs/)** – Screenshots and additional documentation

### Application Preview

![Automation Hub Window](docs/automation-hub-window-only.png)

The application displays a data grid showing all configured automation jobs with their type, enabled status, command, and trigger path.

## Getting started

1. Install the prerequisites listed in `requirements.md`.
2. Clone the repository and open `AutomationHub.sln` in Visual Studio or VS Code.
3. Restore dependencies and build:
   ```
   dotnet restore
   dotnet build
   ```
4. Launch the WPF project (`AutomationHub.App`) to see the sample job list loaded from the shared configuration directory.

## Next steps

- Port the AutoQC watcher logic into `AutomationHub.Watchers` for acquisition-aware file readiness checks.
- Integrate Quartz for accurate cron scheduling.
- Wire the GUI controls to scheduler + watcher services so jobs can be started/stopped interactively.
- Add persistence for runtime state (enable/disable flags, last-run status) using JSON or LiteDB files on `Y:`.
- Package the app using `dotnet publish` and distribute it on `Y:\temporary_files\JO\automation`.
