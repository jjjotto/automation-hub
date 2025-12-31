using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AutomationHub.Core.Jobs;

public sealed class JobProcessSettings
{
    [JsonPropertyName("command")]
    public string Command { get; init; } = string.Empty;

    [JsonPropertyName("arguments")]
    public string? Arguments { get; init; }

    [JsonPropertyName("workingDirectory")]
    public string? WorkingDirectory { get; init; }

    [JsonPropertyName("environment")]
    public Dictionary<string, string>? EnvironmentVariables { get; init; }

    [JsonPropertyName("timeoutMinutes")]
    public int TimeoutMinutes { get; init; } = 0;

    public IEnumerable<string> Validate()
    {
        if (string.IsNullOrWhiteSpace(Command))
            yield return "Command path must be provided.";
    }
}
