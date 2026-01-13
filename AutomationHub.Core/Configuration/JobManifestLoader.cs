using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using AutomationHub.Core.Jobs;

namespace AutomationHub.Core.Configuration;

public static class JobManifestLoader
{
    public static JobManifestLoadResult LoadAll(string? directoryOverride = null)
    {
        var jobsDirectory = string.IsNullOrWhiteSpace(directoryOverride)
            ? ConfigPaths.JobsDirectory
            : directoryOverride!;

        if (!Directory.Exists(jobsDirectory))
        {
            return new JobManifestLoadResult(Array.Empty<JobManifestEntry>(),
                new[] { new JobManifestError(jobsDirectory, "Jobs directory not found") });
        }

        var entries = new List<JobManifestEntry>();
        var errors = new List<JobManifestError>();

        foreach (var file in Directory.EnumerateFiles(jobsDirectory, "*.json", SearchOption.TopDirectoryOnly))
        {
            try
            {
                var json = File.ReadAllText(file);
                var job = JsonSerializer.Deserialize<JobDefinition>(json);
                if (job is null)
                {
                    errors.Add(new JobManifestError(file, "File did not deserialize to a job definition"));
                    continue;
                }

                entries.Add(new JobManifestEntry(job, file));
            }
            catch (Exception ex)
            {
                errors.Add(new JobManifestError(file, ex.Message));
            }
        }

        return new JobManifestLoadResult(entries, errors);
    }
}

public sealed record JobManifestLoadResult(IReadOnlyList<JobManifestEntry> Entries, IReadOnlyList<JobManifestError> Errors)
{
    public bool HasErrors => Errors.Count > 0;
    public IReadOnlyList<JobDefinition> Jobs => Entries.Select(e => e.Job).ToList();
}

public sealed record JobManifestError(string FilePath, string Message)
{
    public override string ToString() => $"{FilePath}: {Message}";
}

public sealed record JobManifestEntry(JobDefinition Job, string FilePath);
