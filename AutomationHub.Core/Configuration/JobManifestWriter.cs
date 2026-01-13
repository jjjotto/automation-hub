using System.IO;
using System.Text.Json;
using AutomationHub.Core.Jobs;

namespace AutomationHub.Core.Configuration;

public static class JobManifestWriter
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.General)
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static void Save(JobDefinition job, string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(job, Options);
        File.WriteAllText(filePath, json);
    }
}
