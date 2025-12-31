using System;

namespace AutomationHub.Core.Execution;

public sealed class JobRunResult
{
    public string JobName { get; }
    public DateTime StartedAt { get; }
    public DateTime? CompletedAt { get; }
    public bool Success { get; }
    public string? Message { get; }

    public JobRunResult(string jobName, DateTime startedAt, DateTime? completedAt, bool success, string? message = null)
    {
        JobName = jobName;
        StartedAt = startedAt;
        CompletedAt = completedAt;
        Success = success;
        Message = message;
    }
}
