using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutomationHub.Core.Jobs;

namespace AutomationHub.Core.Execution;

/// <summary>
/// Service for executing and monitoring job processes with timeout and health check capabilities.
/// </summary>
public sealed class ProcessExecutionService
{
    private const int DefaultTimeoutMinutes = 60;

    /// <summary>
    /// Executes a job process with monitoring and timeout support.
    /// </summary>
    public async Task<JobRunResult> ExecuteAsync(
        string jobName,
        JobProcessSettings processSettings,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jobName))
            throw new ArgumentException("Job name must be provided", nameof(jobName));

        if (processSettings is null)
            throw new ArgumentNullException(nameof(processSettings));

        var startTime = DateTime.Now;
        Process? process = null;
        // Only set true after process.Start() succeeds so catch blocks never
        // call HasExited on an unstarted process (which throws InvalidOperationException).
        bool processStarted = false;

        var outputBuilder = new StringBuilder();
        var errorBuilder  = new StringBuilder();

        try
        {
            process = CreateProcess(processSettings);

            // Wire up async output capture before starting.
            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data is not null)
                    outputBuilder.AppendLine(e.Data);
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data is not null)
                    errorBuilder.AppendLine(e.Data);
            };

            bool started;
            try
            {
                started = process.Start();
            }
            catch (Exception ex)
            {
                // process.Start() threw (e.g. Win32Exception: file not found, access denied).
                // The OS never created a process, so HasExited must NOT be called.
                return new JobRunResult(
                    jobName, startTime, DateTime.Now,
                    success: false,
                    message: $"Failed to start '{process.StartInfo.FileName}': {ex.Message}");
            }

            if (!started)
            {
                return new JobRunResult(
                    jobName, startTime, DateTime.Now,
                    success: false,
                    message: $"Failed to start '{process.StartInfo.FileName}': Process.Start() returned false");
            }

            processStarted = true;
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var timeoutMinutes = processSettings.TimeoutMinutes > 0
                ? processSettings.TimeoutMinutes
                : DefaultTimeoutMinutes;

            var completed = await WaitForProcessWithTimeoutAsync(
                process,
                TimeSpan.FromMinutes(timeoutMinutes),
                cancellationToken);

            if (!completed)
            {
                KillProcessTree(process);
                return new JobRunResult(
                    jobName, startTime, DateTime.Now,
                    success: false,
                    message: $"Process timed out after {timeoutMinutes} minutes");
            }

            var exitCode = process.ExitCode;
            var success  = exitCode == 0;

            string message;
            if (success)
            {
                message = $"Process completed successfully (exit code: {exitCode})";
            }
            else
            {
                var stderr = errorBuilder.ToString().Trim();
                message = string.IsNullOrEmpty(stderr)
                    ? $"Process failed with exit code: {exitCode}"
                    : $"Process failed with exit code: {exitCode} — {stderr}";
            }

            return new JobRunResult(jobName, startTime, DateTime.Now, success, message, exitCode);
        }
        catch (OperationCanceledException)
        {
            if (processStarted && process is not null && !process.HasExited)
                KillProcessTree(process);

            return new JobRunResult(
                jobName, startTime, DateTime.Now,
                success: false,
                message: "Process execution was cancelled");
        }
        catch (Exception ex)
        {
            if (processStarted && process is not null && !process.HasExited)
                KillProcessTree(process);

            return new JobRunResult(
                jobName, startTime, DateTime.Now,
                success: false,
                message: $"Process execution failed: {ex.Message}");
        }
        finally
        {
            process?.Dispose();
        }
    }

    /// <summary>
    /// Creates a process from settings without starting it.
    /// .bat and .cmd files must be run through cmd.exe when UseShellExecute=false;
    /// otherwise Process.Start() throws a Win32Exception on Windows.
    /// </summary>
    private static Process CreateProcess(JobProcessSettings settings)
    {
        var command = settings.Command?.Trim() ?? string.Empty;
        string fileName;
        string arguments;

        var ext = System.IO.Path.GetExtension(command);
        if (string.Equals(ext, ".bat", StringComparison.OrdinalIgnoreCase)
            || string.Equals(ext, ".cmd", StringComparison.OrdinalIgnoreCase))
        {
            // Batch files require a shell host when UseShellExecute=false.
            // cmd.exe /c quoting rule: when the command or args contain spaces we must wrap
            // the entire /c payload in an extra outer pair of quotes so that cmd.exe's
            // special /c parsing strips those outer quotes and correctly interprets the inner
            // quoted tokens.  The safe form is always:
            //   cmd.exe /c ""path\script.bat" "arg1" …"
            fileName  = "cmd.exe";
            var args  = settings.Arguments;
            if (string.IsNullOrWhiteSpace(args))
            {
                arguments = $"/c \"\"{command}\"\"";
            }
            else
            {
                // Preserve existing quoting on args; only add quotes if the caller
                // passed a bare (unquoted) value.
                var trimmedArgs = args.Trim();
                var quotedArgs = (trimmedArgs.StartsWith("\"") && trimmedArgs.EndsWith("\""))
                    ? trimmedArgs
                    : $"\"{trimmedArgs}\"";
                arguments = $"/c \"\"{command}\" {quotedArgs}\"";
            }
        }
        else
        {
            fileName  = command;
            arguments = settings.Arguments ?? string.Empty;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = settings.WorkingDirectory ?? Environment.CurrentDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        // Apply environment variables if specified
        if (settings.EnvironmentVariables != null)
        {
            foreach (var kvp in settings.EnvironmentVariables)
            {
                startInfo.EnvironmentVariables[kvp.Key] = kvp.Value;
            }
        }

        return new Process { StartInfo = startInfo };
    }

    /// <summary>
    /// Waits for a process to complete with a timeout.
    /// </summary>
    private static async Task<bool> WaitForProcessWithTimeoutAsync(
        Process process,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutCts.Token);

        try
        {
            await process.WaitForExitAsync(linkedCts.Token);
            return true; // Process completed within timeout
        }
        catch (OperationCanceledException)
        {
            // Check which token was cancelled
            if (timeoutCts.Token.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                // Timeout occurred
                return false;
            }

            // External cancellation
            throw;
        }
    }

    /// <summary>
    /// Kills a process and all its child processes.
    /// </summary>
    private static void KillProcessTree(Process process)
    {
        if (process.HasExited)
        {
            return;
        }

        try
        {
            // Kill the process tree (parent + children)
            process.Kill(entireProcessTree: true);
        }
        catch
        {
            // Best effort - process might have already exited
        }
    }
}
