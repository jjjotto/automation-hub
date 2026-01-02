using System.IO;
using System.Reflection;

namespace AutomationHub.Core.Configuration;

public static class ConfigPaths
{
    public const string DefaultRoot = @"Y:\temporary_files\JO\automation";

    private static string GetRootDirectory()
    {
        // Use network drive if available, otherwise use local-config for development
        if (Directory.Exists(DefaultRoot))
        {
            return DefaultRoot;
        }

        // For local development, use local-config directory relative to the application
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var appDir = Path.GetDirectoryName(assembly.Location) ?? Directory.GetCurrentDirectory();
        var localConfig = Path.Combine(appDir, "..", "..", "..", "..", "local-config");
        
        // Try the local-config path
        if (Directory.Exists(localConfig))
        {
            return Path.GetFullPath(localConfig);
        }

        // Fallback to repo root local-config
        var repoRoot = FindRepoRoot(appDir);
        if (repoRoot != null)
        {
            var repoLocalConfig = Path.Combine(repoRoot, "local-config");
            if (Directory.Exists(repoLocalConfig))
            {
                return repoLocalConfig;
            }
        }

        return DefaultRoot; // Fallback to network drive path
    }

    private static string? FindRepoRoot(string startPath)
    {
        var dir = new DirectoryInfo(startPath);
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, ".git")) ||
                File.Exists(Path.Combine(dir.FullName, "AutomationHub.sln")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        return null;
    }

    public static string JobsDirectory => Path.Combine(GetRootDirectory(), "config", "jobs");

    public static string LogsDirectory => Path.Combine(GetRootDirectory(), "logs");

    public static string TemplatesDirectory => Path.Combine(GetRootDirectory(), "config", "templates");
}
