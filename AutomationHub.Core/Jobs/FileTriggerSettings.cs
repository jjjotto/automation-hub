using System.Text.Json.Serialization;

namespace AutomationHub.Core.Jobs;

public sealed class FileTriggerSettings
{
    [JsonPropertyName("watchPath")]
    public string WatchPath { get; init; } = string.Empty;

    [JsonPropertyName("includeSubfolders")]
    public bool IncludeSubfolders { get; init; } = true;

    [JsonPropertyName("instrumentType")]
    public string InstrumentType { get; init; } = "Thermo";

    [JsonPropertyName("acquisitionMinutes")]
    public int AcquisitionMinutes { get; init; } = 60;

    [JsonPropertyName("filter")]
    public FileFilterSettings Filter { get; init; } = new();
}

public sealed class FileFilterSettings
{
    [JsonPropertyName("kind")]
    public string Kind { get; init; } = "All";

    [JsonPropertyName("pattern")]
    public string Pattern { get; init; } = string.Empty;
}
