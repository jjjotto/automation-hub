using System;

namespace AutomationHub.Watchers.Models;

public sealed record MonitoredFileEvent(string FullPath, DateTime DetectedAt);
