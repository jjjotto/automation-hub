using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AutomationHub.Core.Execution;
using AutomationHub.Core.Jobs;
using AutomationHub.Watchers.Services;

namespace AutomationHub.App.Runtime;

public sealed class FileTriggeredJobRunner : IAsyncDisposable
{
<<<<<<< Updated upstream
    private const string TriggerFilePathToken = "{triggerFilePath}";
    private const string TriggerFileNameToken = "{triggerFileName}";
=======
    private static readonly Regex TriggerPathPercentTokenRegex = new(
        @"%\s*TRIGGER(?:\s+|_)+FILE(?:\s+|_)+PATH\s*%",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex TriggerPathBraceTokenRegex = new(
        @"\{\s*TRIGGER(?:\s+|_)+FILE(?:\s+|_)+PATH\s*\}",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
>>>>>>> Stashed changes

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
            var resolvedJob = CreateResolvedJob(filePath);
            var result = await _orchestrator.ExecuteJobAsync(resolvedJob, token).ConfigureAwait(false);
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

    private JobDefinition CreateResolvedJob(string filePath)
    {
        var commandArgs = _job.Process.Arguments;
        if (string.IsNullOrWhiteSpace(commandArgs))
        {
            return _job;
        }

        var fileName = Path.GetFileName(filePath) ?? string.Empty;
        var resolvedArguments = commandArgs
            .Replace(TriggerFilePathToken, filePath, StringComparison.OrdinalIgnoreCase)
            .Replace(TriggerFileNameToken, fileName, StringComparison.OrdinalIgnoreCase);

        // Support environment-variable style placeholders for convenience.
        resolvedArguments = Regex.Replace(
            resolvedArguments,
            "%TRIGGER_FILE_PATH%",
            filePath,
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        resolvedArguments = Regex.Replace(
            resolvedArguments,
            "%TRIGGER_FILE_NAME%",
            fileName,
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        if (string.Equals(commandArgs, resolvedArguments, StringComparison.Ordinal))
        {
            return _job;
        }

        return new JobDefinition
        {
            Name = _job.Name,
            Type = _job.Type,
            Enabled = _job.Enabled,
            Process = new JobProcessSettings
            {
                Command = _job.Process.Command,
                Arguments = resolvedArguments,
                WorkingDirectory = _job.Process.WorkingDirectory,
                EnvironmentVariables = _job.Process.EnvironmentVariables,
                TimeoutMinutes = _job.Process.TimeoutMinutes
            },
            FileTrigger = _job.FileTrigger,
            Schedule = _job.Schedule,
            OutputLogPath = _job.OutputLogPath,
            Tags = _job.Tags,
            Notes = _job.Notes
        };
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

<<<<<<< Updated upstream
=======
    private JobDefinition BuildExecutionJobForTrigger(string filePath)
    {
        var process = _job.Process;
        var replacedEnvironment = ReplaceTriggerPathTokens(process.EnvironmentVariables, filePath)
            ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Expose trigger context as environment variables for batch/cmd scripts that expand %VAR%.
        replacedEnvironment["TRIGGER_FILE_PATH"] = filePath;
        replacedEnvironment["TRIGGER FILE PATH"] = filePath;
        replacedEnvironment["TRIGGER_FILE_NAME"] = Path.GetFileName(filePath);
        replacedEnvironment["TRIGGER_FILE_DIRECTORY"] = Path.GetDirectoryName(filePath) ?? string.Empty;

        var originalArgs = process.Arguments;
        var replacedArgs = ReplaceTriggerPathTokens(process.Arguments, filePath);
        
        // Log the substitution for debugging
        if (!string.Equals(originalArgs, replacedArgs))
        {
            Trace.WriteLine($"[{_job.Name}] Arguments substitution: '{originalArgs}' -> '{replacedArgs}'");
        }
        else if (!string.IsNullOrEmpty(originalArgs))
        {
            Trace.WriteLine($"[{_job.Name}] Warning: Arguments contain potential token but no substitution occurred: '{originalArgs}'");
        }

        var replacedProcess = new JobProcessSettings
        {
            Command = ReplaceTriggerPathTokens(process.Command, filePath) ?? string.Empty,
            Arguments = replacedArgs,
            WorkingDirectory = ReplaceTriggerPathTokens(process.WorkingDirectory, filePath),
            EnvironmentVariables = replacedEnvironment,
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

        // Replace tokens with the actual file path
        var replaced = TriggerPathPercentTokenRegex.Replace(text, filePath);
        replaced = TriggerPathBraceTokenRegex.Replace(replaced, filePath);
        
        // Also handle quoted tokens: "%TRIGGER_FILE_PATH%" or '%TRIGGER_FILE_PATH%'
        // by replacing "quoted" and 'quoted' tokens with "quoted file path" and 'quoted file path'
        replaced = Regex.Replace(replaced, 
            @"""\s*(?:%\s*TRIGGER(?:\s+|_)+FILE(?:\s+|_)+PATH\s*%|\{\s*TRIGGER(?:\s+|_)+FILE(?:\s+|_)+PATH\s*\})\s*""", 
            match => $"\"{filePath}\"",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        
        replaced = Regex.Replace(replaced,
            @"'\s*(?:%\s*TRIGGER(?:\s+|_)+FILE(?:\s+|_)+PATH\s*%|\{\s*TRIGGER(?:\s+|_)+FILE(?:\s+|_)+PATH\s*\})\s*'",
            match => $"'{filePath}'",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        
        return replaced;
    }

>>>>>>> Stashed changes
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
