using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutomationHub.Core.Jobs;

namespace AutomationHub.Core.Execution;

/// <summary>
/// Orchestrates job execution with monitoring and health checks.
/// </summary>
public sealed class JobExecutionOrchestrator
{
    private readonly ProcessExecutionService _executionService;
    private readonly ProcessMonitorService _monitorService;

    public JobExecutionOrchestrator()
    {
        _executionService = new ProcessExecutionService();
        _monitorService = new ProcessMonitorService();
    }

    public JobExecutionOrchestrator(ProcessExecutionService executionService, ProcessMonitorService monitorService)
    {
        _executionService = executionService ?? throw new ArgumentNullException(nameof(executionService));
        _monitorService = monitorService ?? throw new ArgumentNullException(nameof(monitorService));
    }

    /// <summary>
    /// Executes a job with full monitoring and health checking.
    /// </summary>
    public async Task<JobRunResult> ExecuteJobAsync(
        JobDefinition job,
        CancellationToken cancellationToken = default)
    {
        if (job is null)
            throw new ArgumentNullException(nameof(job));

        // Check if job is already running
        if (_monitorService.IsJobRunning(job.Name))
        {
            return new JobRunResult(
                job.Name,
                DateTime.Now,
                DateTime.Now,
                success: false,
                message: "Job is already running");
        }

        // Validate job can be executed
        var validationErrors = job.Validate().ToList();
        if (validationErrors.Any())
        {
            return new JobRunResult(
                job.Name,
                DateTime.Now,
                DateTime.Now,
                success: false,
                message: $"Job validation failed: {string.Join("; ", validationErrors)}");
        }

        var startTime = DateTime.Now;

        try
        {
            // Register job as running
            _monitorService.RegisterRunningJob(job.Name, startTime);
            _monitorService.UpdateJobStatus(job.Name, "Starting process...");

            // Execute the job process
            var result = await _executionService.ExecuteAsync(
                job.Name,
                job.Process,
                cancellationToken);

            // Update final status
            _monitorService.UpdateJobStatus(job.Name, result.Success ? "Completed successfully" : "Failed");

            // Move to completed history
            _monitorService.CompleteJob(result);

            return result;
        }
        catch (Exception ex)
        {
            var errorResult = new JobRunResult(
                job.Name,
                startTime,
                DateTime.Now,
                success: false,
                message: $"Unexpected error: {ex.Message}");

            _monitorService.CompleteJob(errorResult);
            return errorResult;
        }
    }

    /// <summary>
    /// Gets the monitor service for accessing job status information.
    /// </summary>
    public ProcessMonitorService Monitor => _monitorService;

    /// <summary>
    /// Checks for jobs that may be stuck and returns their information.
    /// </summary>
    public async Task<IReadOnlyList<JobExecutionInfo>> CheckForStuckJobsAsync(TimeSpan? threshold = null)
    {
        var checkThreshold = threshold ?? TimeSpan.FromHours(2);
        return await Task.Run(() => _monitorService.GetPotentiallyStuckJobs(checkThreshold));
    }
}
