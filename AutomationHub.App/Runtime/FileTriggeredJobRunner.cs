using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutomationHub.Core.Execution;
using AutomationHub.Core.Jobs;
using AutomationHub.Watchers.Services;

namespace AutomationHub.App.Runtime;

public sealed class FileTriggeredJobRunner : IAsyncDisposable
{
    private readonly JobDefinition _job;
    private readonly JobExecutionOrchestrator _orchestrator;
    private readonly JobActivityLogService _activityLog;
    private readonly JobStatusService _statusService;
    private readonly FileMonitoringService _monitoringService;
    private readonly IFileReadinessPolicy _readinessPolicy;
    private readonly ConcurrentDictionary<string, byte> _inFlightFiles = new(StringComparer.OrdinalIgnoreCase);
    private CancellationTokenSource? _cts;

    private Task? _processingTask;

    public FileTriggeredJobRunner(JobDefinition job, JobExecutionOrchestrator orchestrator, JobActivityLogService activityLog, JobStatusService statusService)
    {
        _job = job ?? throw new ArgumentNullException(nameof(job));
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _activityLog = activityLog ?? throw new ArgumentNullException(nameof(activityLog));
        _statusService = statusService ?? throw new ArgumentNullException(nameof(statusService));

        if (job.FileTrigger is null)
        {
            throw new ArgumentException("File trigger configuration is required.", nameof(job));
        }

        _monitoringService = new FileMonitoringService(job.FileTrigger);
        _readinessPolicy = FileReadinessPolicyFactory.Create(job.FileTrigger);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_cts is not null)
        {
            throw new InvalidOperationException("The file-triggered runner has already been started.");
        }

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var startupMatches = await _monitoringService.StartAsync(_cts.Token).ConfigureAwait(false);
        if (startupMatches > 0)
        {
            Log($"Queued {startupMatches} file(s) detected at startup.");
        }
        var watchPath = _job.FileTrigger?.WatchPath ?? string.Empty;
        Log($"Watching '{watchPath}' for {DescribeFilters()}.");
        SetStatus($"Watching {watchPath}. Waiting for new files...");
        _processingTask = Task.Run(() => ProcessEventsAsync(_cts.Token), CancellationToken.None);
    }

    private async Task ProcessEventsAsync(CancellationToken token)
    {
        try
        {
            await foreach (var fileEvent in _monitoringService.GetEventsAsync(token).ConfigureAwait(false))
            {
                var normalizedPath = NormalizePath(fileEvent.FullPath);

                if (!_inFlightFiles.TryAdd(normalizedPath, 0))
                {
                    continue;
                }

                try
                {
                    await HandleFileAsync(normalizedPath, token).ConfigureAwait(false);
                }
                finally
                {
                    _inFlightFiles.TryRemove(normalizedPath, out _);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[{_job.Name}] File trigger runner terminated: {ex}");
        }
    }

    private async Task HandleFileAsync(string filePath, CancellationToken token)
    {
        try
        {
            var fileName = Path.GetFileName(filePath);
            Log($"Detected '{fileName}'. Waiting for stability...");
            SetStatus($"Detected {fileName}. Waiting for stability...");
            await _readinessPolicy.WaitUntilReadyAsync(filePath, token).ConfigureAwait(false);
            Log($"Launching job for '{fileName}'.");
            SetStatus($"Launching job for {fileName}...");
            var executionJob = BuildExecutionJobForTrigger(filePath);
            var result = await _orchestrator.ExecuteJobAsync(executionJob, token).ConfigureAwait(false);
            Trace.WriteLine($"[{_job.Name}] Triggered by '{filePath}' - Success: {result.Success} - {result.Message}");
            var idleMessage = result.Success
                ? $"Run triggered by '{fileName}' finished successfully. Waiting for new files..."
                : $"Run triggered by '{fileName}' failed: {result.Message}";
            Log(idleMessage);
            SetStatus(result.Success
                ? "Waiting for new files..."
                : $"Error: {result.Message}");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[{_job.Name}] Error processing '{filePath}': {ex}");
            Log($"Error processing '{Path.GetFileName(filePath)}': {ex.Message}");
            SetStatus($"Error processing file: {ex.Message}");
        }
    }

    private static string NormalizePath(string path)
    {
        try
        {
            return Path.GetFullPath(path);
        }
        catch (Exception)
        {
            return path;
        }
    }

    private JobDefinition BuildExecutionJobForTrigger(string filePath)
    {
        var process = _job.Process;
        var replacedProcess = new JobProcessSettings
        {
            Command = ReplaceTriggerPathTokens(process.Command, filePath) ?? string.Empty,
            Arguments = ReplaceTriggerPathTokens(process.Arguments, filePath),
            WorkingDirectory = ReplaceTriggerPathTokens(process.WorkingDirectory, filePath),
            EnvironmentVariables = ReplaceTriggerPathTokens(process.EnvironmentVariables, filePath),
            TimeoutMinutes = process.TimeoutMinutes
        };

        return new JobDefinition
        {
            Name = _job.Name,
            Type = _job.Type,
            Enabled = _job.Enabled,
            Process = replacedProcess,
            FileTrigger = _job.FileTrigger,
            Schedule = _job.Schedule,
            OutputLogPath = ReplaceTriggerPathTokens(_job.OutputLogPath, filePath),
            Tags = _job.Tags,
            Notes = _job.Notes
        };
    }

    private static Dictionary<string, string>? ReplaceTriggerPathTokens(
        Dictionary<string, string>? environment,
        string filePath)
    {
        if (environment is null || environment.Count == 0)
        {
            return environment;
        }

        var replaced = new Dictionary<string, string>(environment.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var entry in environment)
        {
            var value = ReplaceTriggerPathTokens(entry.Value, filePath) ?? string.Empty;
            replaced[entry.Key] = value;
        }

        return replaced;
    }

    private static string? ReplaceTriggerPathTokens(string? text, string filePath)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        var replaced = text;
        replaced = replaced.Replace("%TRIGGER_FILE_PATH%", filePath, StringComparison.OrdinalIgnoreCase);
        replaced = replaced.Replace("%TRIGGER FILE PATH%", filePath, StringComparison.OrdinalIgnoreCase);
        replaced = replaced.Replace("{TRIGGER_FILE_PATH}", filePath, StringComparison.OrdinalIgnoreCase);
        replaced = replaced.Replace("{TRIGGER FILE PATH}", filePath, StringComparison.OrdinalIgnoreCase);
        return replaced;
    }

    public async Task StopAsync()
    {
        _cts?.Cancel();

        if (_processingTask is not null)
        {
            try
            {
                await _processingTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        await _monitoringService.DisposeAsync().ConfigureAwait(false);
        _cts?.Dispose();
        _cts = null;
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
    }

    private void Log(string message)
    {
        _activityLog.Append(_job.Name, message);
    }

    private void SetStatus(string message)
    {
        _statusService.Update(_job.Name, message);
    }

    private string DescribeFilters()
    {
        var filters = _job.FileTrigger?.EffectiveFilters;
        if (filters is null || filters.Count == 0)
        {
            return "all files";
        }

        return string.Join(" AND ", filters.Select(f => $"{f.Kind}:{f.Pattern}"));
    }
}
