using System.IO;

namespace AutomationHub.Core.Configuration;

public static class ConfigPaths
{
    public const string DefaultRoot = @"Y:\temporary_files\JO\automation";

    public static string JobsDirectory => Path.Combine(DefaultRoot, "config", "jobs");

    public static string LogsDirectory => Path.Combine(DefaultRoot, "logs");

    public static string TemplatesDirectory => Path.Combine(DefaultRoot, "config", "templates");
}
