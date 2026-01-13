using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace AutomationHub.Core.Execution;

public sealed record JobActivityLogEntry(string JobName, DateTime Timestamp, string Message);

public sealed class JobActivityLogChangedEventArgs : EventArgs
{
    public JobActivityLogChangedEventArgs(JobActivityLogEntry entry)
    {
        Entry = entry;
    }

    public JobActivityLogEntry Entry { get; }
    public string JobName => Entry.JobName;
}

public sealed class JobActivityLogService
{
    private const int MaxEntriesPerJob = 200;
    private readonly ConcurrentDictionary<string, ConcurrentQueue<JobActivityLogEntry>> _logEntries = new(StringComparer.OrdinalIgnoreCase);

    public event EventHandler<JobActivityLogChangedEventArgs>? JobLogChanged;

    public void Append(string jobName, string message)
    {
        if (string.IsNullOrWhiteSpace(jobName))
        {
            return;
        }

        var entry = new JobActivityLogEntry(jobName, DateTime.Now, message);
        var queue = _logEntries.GetOrAdd(jobName, _ => new ConcurrentQueue<JobActivityLogEntry>());
        queue.Enqueue(entry);

        while (queue.Count > MaxEntriesPerJob && queue.TryDequeue(out _))
        {
        }

        JobLogChanged?.Invoke(this, new JobActivityLogChangedEventArgs(entry));
    }

    public IReadOnlyList<JobActivityLogEntry> GetEntries(string jobName, int maxCount = 100)
    {
        if (string.IsNullOrWhiteSpace(jobName))
        {
            return Array.Empty<JobActivityLogEntry>();
        }

        if (!_logEntries.TryGetValue(jobName, out var queue))
        {
            return Array.Empty<JobActivityLogEntry>();
        }

        return queue.Reverse().Take(maxCount).Reverse().ToList();
    }

    public void Clear(string jobName)
    {
        if (string.IsNullOrWhiteSpace(jobName))
        {
            return;
        }

        _logEntries.TryRemove(jobName, out _);
    }
}
