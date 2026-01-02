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
    private const int ProcessStartupCheckDelayMs = 1000;
    private const int MaxStartupCheckAttempts = 5;

    /// <summary>
    /// Executes a job process with monitoring and timeout support.
    /// </summary>
    /// <param name="jobName">Name of the job being executed</param>
    /// <param name="processSettings">Process configuration settings</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the job execution</returns>
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

        try
        {
            // Create the process
            process = CreateProcess(processSettings);

            // Start the process
            var startSuccess = await StartProcessWithMonitoringAsync(process, cancellationToken);
            if (!startSuccess)
            {
                return new JobRunResult(
                    jobName,
                    startTime,
                    DateTime.Now,
                    success: false,
                    message: "Failed to start process");
            }

            // Determine timeout
            var timeoutMinutes = processSettings.TimeoutMinutes > 0
                ? processSettings.TimeoutMinutes
                : DefaultTimeoutMinutes;

            // Wait for process completion with timeout
            var completed = await WaitForProcessWithTimeoutAsync(
                process,
                TimeSpan.FromMinutes(timeoutMinutes),
                cancellationToken);

            if (!completed)
            {
                // Process timed out - kill it
                KillProcessTree(process);
                return new JobRunResult(
                    jobName,
                    startTime,
                    DateTime.Now,
                    success: false,
                    message: $"Process timed out after {timeoutMinutes} minutes");
            }

            // Process completed normally
            var exitCode = process.ExitCode;
            var success = exitCode == 0;
            var message = success
                ? $"Process completed successfully (exit code: {exitCode})"
                : $"Process failed with exit code: {exitCode}";

            return new JobRunResult(
                jobName,
                startTime,
                DateTime.Now,
                success,
                message);
        }
        catch (OperationCanceledException)
        {
            // Cancellation requested
            if (process != null && !process.HasExited)
            {
                KillProcessTree(process);
            }

            return new JobRunResult(
                jobName,
                startTime,
                DateTime.Now,
                success: false,
                message: "Process execution was cancelled");
        }
        catch (Exception ex)
        {
            // Unexpected error
            if (process != null && !process.HasExited)
            {
                KillProcessTree(process);
            }

            return new JobRunResult(
                jobName,
                startTime,
                DateTime.Now,
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
    /// </summary>
    private static Process CreateProcess(JobProcessSettings settings)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = settings.Command,
            Arguments = settings.Arguments ?? string.Empty,
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
    /// Starts a process and monitors it to ensure it starts successfully.
    /// </summary>
    private static async Task<bool> StartProcessWithMonitoringAsync(
        Process process,
        CancellationToken cancellationToken)
    {
        try
        {
            // Attempt to start the process
            if (!process.Start())
            {
                return false;
            }

            // Begin reading output streams to prevent buffer deadlock
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Monitor the process for a short time to ensure it doesn't immediately fail
            for (int attempt = 0; attempt < MaxStartupCheckAttempts; attempt++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return false;
                }

                // Check if process has exited prematurely
                if (process.HasExited)
                {
                    var exitCode = process.ExitCode;
                    if (exitCode != 0)
                    {
                        // Process started but failed immediately
                        return false;
                    }
                    // Process completed successfully in startup phase
                    return true;
                }

                // Wait a bit before next check
                await Task.Delay(ProcessStartupCheckDelayMs, cancellationToken);
            }

            // Process is still running after startup monitoring period
            return true;
        }
        catch (Exception)
        {
            return false;
        }
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
