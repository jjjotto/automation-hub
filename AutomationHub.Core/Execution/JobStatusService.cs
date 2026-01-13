using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace AutomationHub.Core.Execution;

public sealed record JobStatusSnapshot(string JobName, DateTime Timestamp, string Message);

public sealed class JobStatusChangedEventArgs : EventArgs
{
    public JobStatusChangedEventArgs(JobStatusSnapshot snapshot)
    {
        Snapshot = snapshot;
    }

    public JobStatusSnapshot Snapshot { get; }
    public string JobName => Snapshot.JobName;
    public string Message => Snapshot.Message;
}

public sealed class JobStatusService
{
    private readonly ConcurrentDictionary<string, JobStatusSnapshot> _statuses = new(StringComparer.OrdinalIgnoreCase);

    public event EventHandler<JobStatusChangedEventArgs>? JobStatusChanged;

    public void Update(string jobName, string message)
    {
        if (string.IsNullOrWhiteSpace(jobName) || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        var snapshot = new JobStatusSnapshot(jobName, DateTime.Now, message);
        _statuses[jobName] = snapshot;
        JobStatusChanged?.Invoke(this, new JobStatusChangedEventArgs(snapshot));
    }

    public JobStatusSnapshot? GetStatus(string jobName)
    {
        if (string.IsNullOrWhiteSpace(jobName))
        {
            return null;
        }

        return _statuses.TryGetValue(jobName, out var snapshot) ? snapshot : null;
    }

    public IReadOnlyCollection<JobStatusSnapshot> GetAllStatuses() => _statuses.Values.ToList();
}
