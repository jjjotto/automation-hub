using System.Text.Json.Serialization;

namespace AutomationHub.Core.Jobs;

public sealed class ScheduleSettings
{
    [JsonPropertyName("cron")]
    public string CronExpression { get; init; } = "0 0 6 ? * MON-FRI";

    [JsonPropertyName("timezone")]
    public string TimeZoneId { get; init; } = "Central Standard Time";

    [JsonPropertyName("startPaused")]
    public bool StartPaused { get; init; }
}
