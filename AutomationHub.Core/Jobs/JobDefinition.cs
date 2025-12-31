using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AutomationHub.Core.Jobs;

public sealed class JobDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public JobType Type { get; init; } = JobType.FileTrigger;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("process")]
    public JobProcessSettings Process { get; init; } = new();

    [JsonPropertyName("fileTrigger")]
    public FileTriggerSettings? FileTrigger { get; init; }

    [JsonPropertyName("schedule")]
    public ScheduleSettings? Schedule { get; init; }

    [JsonPropertyName("outputLog")]
    public string? OutputLogPath { get; init; }

    [JsonPropertyName("tags")]
    public IReadOnlyCollection<string>? Tags { get; init; }

    [JsonPropertyName("notes")]
    public string? Notes { get; init; }

    public IEnumerable<string> Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            yield return "Job name is required.";

        foreach (var message in Process.Validate())
            yield return message;

        if (Type.HasFlag(JobType.FileTrigger) && FileTrigger is null)
            yield return "File trigger settings are required for file-based jobs.";

        if (Type.HasFlag(JobType.Scheduled) && Schedule is null)
            yield return "Schedule settings are required for scheduled jobs.";
    }
}

[Flags]
public enum JobType
{
    Manual = 1,
    Scheduled = 2,
    FileTrigger = 4,
    ManualAndFile = Manual | FileTrigger,
    ManualAndScheduled = Manual | Scheduled,
    Hybrid = Manual | Scheduled | FileTrigger
}
