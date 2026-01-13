using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AutomationHub.Core.Jobs;

namespace AutomationHub.Watchers.Services;

public interface IFileReadinessPolicy
{
    Task WaitUntilReadyAsync(string path, CancellationToken cancellationToken);
}

internal sealed class FileStabilityReadinessPolicy : IFileReadinessPolicy
{
    private static readonly TimeSpan StabilityWindow = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan MissingTolerance = TimeSpan.FromSeconds(30);

    public static FileStabilityReadinessPolicy Instance { get; } = new();

    private FileStabilityReadinessPolicy()
    {
    }

    public async Task WaitUntilReadyAsync(string path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path must be provided", nameof(path));
        }

        var firstSeen = DateTime.UtcNow;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!TargetExists(path))
            {
                if (DateTime.UtcNow - firstSeen > MissingTolerance)
                {
                    throw new FileNotFoundException($"File or directory '{path}' no longer exists.");
                }
            }
            else if (HasSettled(path))
            {
                return;
            }

            await Task.Delay(PollInterval, cancellationToken).ConfigureAwait(false);
        }
    }

    private static bool TargetExists(string path) => File.Exists(path) || Directory.Exists(path);

    private static bool HasSettled(string path)
    {
        if (Directory.Exists(path))
        {
            return DateTime.UtcNow - Directory.GetLastWriteTimeUtc(path) >= StabilityWindow;
        }

        try
        {
            var lastWrite = File.GetLastWriteTimeUtc(path);
            return DateTime.UtcNow - lastWrite >= StabilityWindow;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}

public static class FileReadinessPolicyFactory
{
    public static IFileReadinessPolicy Create(FileTriggerSettings settings)
    {
        if (settings is null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        return FileStabilityReadinessPolicy.Instance;
    }
}
