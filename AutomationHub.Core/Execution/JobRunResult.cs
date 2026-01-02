using System;

namespace AutomationHub.Core.Execution;

public sealed class JobRunResult
{
    public string JobName { get; }
    public DateTime StartedAt { get; }
    public DateTime? CompletedAt { get; }
    public bool Success { get; }
    public string? Message { get; }
    public int? ExitCode { get; }
    public JobRunStatus Status { get; }

    public JobRunResult(string jobName, DateTime startedAt, DateTime? completedAt, bool success, string? message = null, int? exitCode = null)
    {
        JobName = jobName;
        StartedAt = startedAt;
        CompletedAt = completedAt;
        Success = success;
        Message = message;
        ExitCode = exitCode;
        
        // Determine status based on completion and success
        if (completedAt.HasValue)
        {
            Status = success ? JobRunStatus.CompletedSuccessfully : JobRunStatus.Failed;
        }
        else
        {
            Status = JobRunStatus.Running;
        }
    }

    public TimeSpan? Duration => CompletedAt.HasValue 
        ? CompletedAt.Value - StartedAt 
        : null;
}

public enum JobRunStatus
{
    Running,
    CompletedSuccessfully,
    Failed,
    TimedOut,
    Cancelled
}
