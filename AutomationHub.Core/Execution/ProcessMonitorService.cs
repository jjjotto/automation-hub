using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AutomationHub.Core.Execution;

/// <summary>
/// Service for monitoring the health and status of running job processes.
/// </summary>
public sealed class ProcessMonitorService
{
    private readonly ConcurrentDictionary<string, JobExecutionInfo> _runningJobs = new();
    private readonly ConcurrentQueue<JobRunResult> _completedJobs = new();
    private const int MaxCompletedJobsHistory = 100;

    /// <summary>
    /// Registers a job as running.
    /// </summary>
    public void RegisterRunningJob(string jobName, DateTime startTime)
    {
        _runningJobs[jobName] = new JobExecutionInfo(jobName, startTime);
    }

    /// <summary>
    /// Updates the status of a running job.
    /// </summary>
    public void UpdateJobStatus(string jobName, string statusMessage)
    {
        if (_runningJobs.TryGetValue(jobName, out var info))
        {
            info.LastStatusUpdate = DateTime.Now;
            info.StatusMessage = statusMessage;
        }
    }

    /// <summary>
    /// Marks a job as completed and moves it to history.
    /// </summary>
    public void CompleteJob(JobRunResult result)
    {
        if (_runningJobs.TryRemove(result.JobName, out _))
        {
            _completedJobs.Enqueue(result);

            // Limit history size
            while (_completedJobs.Count > MaxCompletedJobsHistory)
            {
                _completedJobs.TryDequeue(out _);
            }
        }
    }

    /// <summary>
    /// Gets information about a currently running job.
    /// </summary>
    public JobExecutionInfo? GetRunningJob(string jobName)
    {
        _runningJobs.TryGetValue(jobName, out var info);
        return info;
    }

    /// <summary>
    /// Gets all currently running jobs.
    /// </summary>
    public IReadOnlyList<JobExecutionInfo> GetAllRunningJobs()
    {
        return _runningJobs.Values.ToList();
    }

    /// <summary>
    /// Gets recent completed jobs.
    /// </summary>
    public IReadOnlyList<JobRunResult> GetCompletedJobs(int maxCount = 20)
    {
        return _completedJobs.Reverse().Take(maxCount).ToList();
    }

    /// <summary>
    /// Checks for jobs that may be stuck (running longer than expected).
    /// </summary>
    public IReadOnlyList<JobExecutionInfo> GetPotentiallyStuckJobs(TimeSpan threshold)
    {
        var now = DateTime.Now;
        return _runningJobs.Values
            .Where(info => (now - info.StartTime) > threshold)
            .ToList();
    }

    /// <summary>
    /// Checks if a job is currently running.
    /// </summary>
    public bool IsJobRunning(string jobName)
    {
        return _runningJobs.ContainsKey(jobName);
    }

    /// <summary>
    /// Gets the total count of running jobs.
    /// </summary>
    public int RunningJobCount => _runningJobs.Count;
}

/// <summary>
/// Information about a job that is currently executing.
/// </summary>
public sealed class JobExecutionInfo
{
    public string JobName { get; }
    public DateTime StartTime { get; }
    public DateTime LastStatusUpdate { get; set; }
    public string? StatusMessage { get; set; }

    public JobExecutionInfo(string jobName, DateTime startTime)
    {
        JobName = jobName;
        StartTime = startTime;
        LastStatusUpdate = startTime;
    }

    public TimeSpan RunningDuration => DateTime.Now - StartTime;

    public TimeSpan TimeSinceLastUpdate => DateTime.Now - LastStatusUpdate;
}
