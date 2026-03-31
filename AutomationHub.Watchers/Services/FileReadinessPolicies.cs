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

    private readonly record struct TargetSnapshot(int FileCount, long TotalBytes, DateTime MaxLastWriteUtc);

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
        TargetSnapshot? lastSnapshot = null;
        DateTime? stableSince = null;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!TargetExists(path))
            {
                if (DateTime.UtcNow - firstSeen > MissingTolerance)
                {
                    throw new FileNotFoundException($"File or directory '{path}' no longer exists.");
                }

                // Target disappeared while waiting; reset stability tracking.
                lastSnapshot = null;
                stableSince = null;
            }
            else if (TryGetSnapshot(path, out var currentSnapshot))
            {
                if (lastSnapshot is null || currentSnapshot != lastSnapshot.Value)
                {
                    lastSnapshot = currentSnapshot;
                    stableSince = DateTime.UtcNow;
                }
                else if (stableSince is not null && DateTime.UtcNow - stableSince.Value >= StabilityWindow)
                {
                    return;
                }
            }
            else
            {
                // Unable to read target metadata (transient lock/access race). Keep waiting.
                lastSnapshot = null;
                stableSince = null;
            }

            await Task.Delay(PollInterval, cancellationToken).ConfigureAwait(false);
        }
    }

    private static bool TargetExists(string path) => File.Exists(path) || Directory.Exists(path);

    private static bool TryGetSnapshot(string path, out TargetSnapshot snapshot)
    {
        snapshot = default;

        if (Directory.Exists(path))
        {
            try
            {
                var maxLastWrite = Directory.GetLastWriteTimeUtc(path);
                var totalBytes = 0L;
                var fileCount = 0;

                foreach (var filePath in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    var fileInfo = new FileInfo(filePath);
                    if (!fileInfo.Exists)
                    {
                        continue;
                    }

                    fileCount++;
                    totalBytes += fileInfo.Length;

                    if (fileInfo.LastWriteTimeUtc > maxLastWrite)
                    {
                        maxLastWrite = fileInfo.LastWriteTimeUtc;
                    }
                }

                snapshot = new TargetSnapshot(fileCount, totalBytes, maxLastWrite);
                return true;
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

        try
        {
            var fileInfo = new FileInfo(path);
            if (!fileInfo.Exists)
            {
                return false;
            }

            snapshot = new TargetSnapshot(
                FileCount: 1,
                TotalBytes: fileInfo.Length,
                MaxLastWriteUtc: fileInfo.LastWriteTimeUtc);
            return true;
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
