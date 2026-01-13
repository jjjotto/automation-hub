using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AutomationHub.Core.Jobs;

public sealed class FileTriggerSettings
{
    [JsonPropertyName("watchPath")]
    public string WatchPath { get; init; } = string.Empty;

    [JsonPropertyName("includeSubfolders")]
    public bool IncludeSubfolders { get; init; } = true;

    [JsonPropertyName("filters")]
    public IReadOnlyList<FileFilterSettings>? Filters { get; init; }

    [JsonPropertyName("filter")]
#pragma warning disable CA2227
    public FileFilterSettings? LegacyFilter { get; init; }
#pragma warning restore CA2227

    [JsonIgnore]
    public IReadOnlyList<FileFilterSettings> EffectiveFilters
    {
        get
        {
            if (Filters is { Count: > 0 } filters)
            {
                return filters;
            }

            if (LegacyFilter is not null)
            {
                return new[] { LegacyFilter };
            }

            return Array.Empty<FileFilterSettings>();
        }
    }
}

public sealed class FileFilterSettings
{
    [JsonPropertyName("kind")]
    public string Kind { get; init; } = "All";

    [JsonPropertyName("pattern")]
    public string Pattern { get; init; } = string.Empty;
}
