using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutomationHub.Core.Configuration;
using AutomationHub.Core.Execution;
using AutomationHub.Core.Jobs;
using AutomationHub.Scheduler.Services;

namespace AutomationHub.App.Runtime;

public sealed class AutomationRuntimeHost : IAsyncDisposable
{
    private readonly JobActivityLogService _activityLog = new();
    private readonly JobStatusService _statusService = new();
    private readonly JobExecutionOrchestrator _orchestrator;
    private readonly JobSchedulerService _scheduler = new();
    private readonly Dictionary<string, FileTriggeredJobRunner> _fileRunners = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ScheduledJobHandle> _scheduledHandles = new(StringComparer.OrdinalIgnoreCase);
    private readonly CancellationTokenSource _cts = new();
    private readonly List<string> _runtimeErrors = new();

    private bool _started;

    public AutomationRuntimeHost()
    {
        _orchestrator = new JobExecutionOrchestrator(_activityLog, _statusService);
    }

    public IReadOnlyList<JobManifestEntry> JobEntries { get; private set; } = Array.Empty<JobManifestEntry>();
    public IReadOnlyList<JobManifestError> JobLoadErrors { get; private set; } = Array.Empty<JobManifestError>();
    public IReadOnlyList<JobDefinition> Jobs => JobEntries.Select(e => e.Job).ToList();

    public ProcessMonitorService Monitor => _orchestrator.Monitor;
    public JobActivityLogService ActivityLog => _activityLog;
    public JobStatusService StatusService => _statusService;
    public IReadOnlyList<string> RuntimeErrors => _runtimeErrors;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_started)
        {
            return;
        }

        var loadResult = JobManifestLoader.LoadAll();
        JobEntries = loadResult.Entries;
        JobLoadErrors = loadResult.Errors;

        foreach (var entry in JobEntries)
        {
            var job = entry.Job;
            if (!job.Enabled)
            {
                _activityLog.Append(job.Name, "Job is disabled. Skipping runtime wiring.");
                _statusService.Update(job.Name, "Disabled");
                continue;
            }

            cancellationToken.ThrowIfCancellationRequested();
            await WireJobAsync(job).ConfigureAwait(false);
        }

        _started = true;
    }

    // Re-starts a job that has been stopped. For Manual-only jobs, triggers an immediate run.
    // For FileTrigger / Scheduled / Hybrid jobs, re-creates the watcher / schedule.
    public async Task RestartJobAsync(string jobName)
    {
        var entry = JobEntries.FirstOrDefault(e => string.Equals(e.Job.Name, jobName, StringComparison.OrdinalIgnoreCase));
        if (entry is null)
        {
            return;
        }

        var job = entry.Job;

        // Stop any still-active infrastructure for this job before re-wiring.
        if (_fileRunners.ContainsKey(jobName) || _scheduledHandles.ContainsKey(jobName))
        {
            await StopJobAsync(jobName).ConfigureAwait(false);
        }

        // Manual-only jobs: run immediately instead of re-registering a watcher.
        if (job.Type == JobType.Manual)
        {
            _activityLog.Append(job.Name, "Manual run requested.");
            _statusService.Update(job.Name, "Manual run requested");
            _ = _orchestrator.ExecuteJobAsync(job, _cts.Token);
            return;
        }

        _activityLog.Append(job.Name, "Restarting job.");
        await WireJobAsync(job).ConfigureAwait(false);
    }

    private async Task WireJobAsync(JobDefinition job)
    {
        _statusService.Update(job.Name, "Idle");

        if (job.Type.HasFlag(JobType.FileTrigger) && job.FileTrigger is not null)
        {
            if (!Directory.Exists(job.FileTrigger.WatchPath))
            {
                _runtimeErrors.Add($"[{job.Name}] Watch path '{job.FileTrigger.WatchPath}' is not accessible.");
                _activityLog.Append(job.Name, $"Watch path '{job.FileTrigger.WatchPath}' is not accessible.");
                _statusService.Update(job.Name, "Watch path not accessible");
            }
            else
            {
                try
                {
                    var runner = new FileTriggeredJobRunner(job, _orchestrator, _activityLog, _statusService);
                    await runner.StartAsync(_cts.Token).ConfigureAwait(false);
                    _fileRunners[job.Name] = runner;
                    _activityLog.Append(job.Name, $"File trigger active on '{job.FileTrigger.WatchPath}'.");
                    _statusService.Update(job.Name, "File trigger active - waiting for files");
                }
                catch (Exception ex)
                {
                    _runtimeErrors.Add($"[{job.Name}] Failed to start file trigger: {ex.Message}");
                    _activityLog.Append(job.Name, $"Failed to start file trigger: {ex.Message}");
                    _statusService.Update(job.Name, $"File trigger error: {ex.Message}");
                }
            }
        }

        if (job.Type.HasFlag(JobType.Scheduled) && job.Schedule is not null)
        {
            try
            {
                var handle = _scheduler.RegisterMinutePollJob(job, async ct =>
                {
                    _activityLog.Append(job.Name, "Scheduled trigger fired.");
                    _statusService.Update(job.Name, "Scheduled trigger running");
                    var result = await _orchestrator.ExecuteJobAsync(job, ct).ConfigureAwait(false);
                    _activityLog.Append(job.Name, result.Success
                        ? "Scheduled run completed. Waiting for next occurrence."
                        : $"Scheduled run failed: {result.Message}");
                    _statusService.Update(job.Name, result.Success
                        ? "Scheduled - waiting for next run"
                        : $"Error: {result.Message}");
                }, _cts.Token);

                _scheduledHandles[job.Name] = handle;
                _activityLog.Append(job.Name, "Schedule registered and active.");
                _statusService.Update(job.Name, "Scheduled - waiting for next run");
            }
            catch (Exception ex)
            {
                _runtimeErrors.Add($"[{job.Name}] Failed to register schedule: {ex.Message}");
                _activityLog.Append(job.Name, $"Failed to register schedule: {ex.Message}");
                _statusService.Update(job.Name, $"Schedule error: {ex.Message}");
            }
        }
    }

    public Task<JobRunResult> RunJobAsync(string jobName, CancellationToken cancellationToken = default)
    {
        var entry = JobEntries.FirstOrDefault(e => string.Equals(e.Job.Name, jobName, StringComparison.OrdinalIgnoreCase));
        if (entry is null)
        {
            throw new InvalidOperationException($"Job '{jobName}' is not defined.");
        }

        var job = entry.Job;
        if (!job.Enabled)
        {
            throw new InvalidOperationException($"Job '{jobName}' is disabled.");
        }

        _activityLog.Append(job.Name, "Manual run requested.");
        _statusService.Update(job.Name, "Manual run requested");
        return _orchestrator.ExecuteJobAsync(job, cancellationToken);
    }

    public async Task StopJobAsync(string jobName)
    {
        if (_fileRunners.TryGetValue(jobName, out var runner))
        {
            await runner.StopAsync().ConfigureAwait(false);
            _fileRunners.Remove(jobName);
        }

        if (_scheduledHandles.TryGetValue(jobName, out var handle))
        {
            await handle.StopAsync().ConfigureAwait(false);
            _scheduledHandles.Remove(jobName);
        }

        _activityLog.Append(jobName, "Job stopped by user.");
        _statusService.Update(jobName, "Stopped");
    }

    public async Task RemoveJobAsync(string jobName)
    {
        await StopJobAsync(jobName).ConfigureAwait(false);

        var entry = JobEntries.FirstOrDefault(e => string.Equals(e.Job.Name, jobName, StringComparison.OrdinalIgnoreCase));
        if (entry is not null && File.Exists(entry.FilePath))
            File.Delete(entry.FilePath);

        var loadResult = JobManifestLoader.LoadAll();
        JobEntries = loadResult.Entries;
        JobLoadErrors = loadResult.Errors;
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();

        foreach (var handle in _scheduledHandles.Values)
        {
            try
            {
                await handle.StopAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        foreach (var runner in _fileRunners.Values)
        {
            await runner.StopAsync().ConfigureAwait(false);
        }

        _cts.Dispose();
    }
}
