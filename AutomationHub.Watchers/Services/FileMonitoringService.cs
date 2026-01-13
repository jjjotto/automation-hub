using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using AutomationHub.Core.Jobs;
using AutomationHub.Watchers.Filters;
using AutomationHub.Watchers.Models;

namespace AutomationHub.Watchers.Services;

public sealed class FileMonitoringService : IAsyncDisposable
{
    private readonly FileTriggerSettings _settings;
    private readonly FileSystemWatcher _watcher;
    private readonly IReadOnlyList<FileFilter> _filters;
    private readonly Channel<MonitoredFileEvent> _eventChannel = Channel.CreateUnbounded<MonitoredFileEvent>();

    public FileMonitoringService(FileTriggerSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        if (string.IsNullOrWhiteSpace(settings.WatchPath))
            throw new ArgumentException("Watch path must be provided", nameof(settings));

        _filters = BuildFilters(settings);
        var watcherFilter = DetermineWatcherFilter(settings);
        _watcher = new FileSystemWatcher(settings.WatchPath)
        {
            IncludeSubdirectories = settings.IncludeSubfolders,
            Filter = watcherFilter,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime | NotifyFilters.DirectoryName
        };

        _watcher.Created += OnFileDetected;
        _watcher.Renamed += OnFileDetected;
        _watcher.Error += (_, args) => _eventChannel.Writer.TryWrite(new MonitoredFileEvent($"ERROR::{args.GetException()?.Message}", DateTime.Now));
    }

    public async Task<int> StartAsync(CancellationToken cancellationToken)
    {
        var files = await Task.Run(() => EnumerateExistingFiles(), cancellationToken).ConfigureAwait(false);
        var count = 0;
        foreach (var file in files)
        {
            count++;
            _eventChannel.Writer.TryWrite(new MonitoredFileEvent(file, DateTime.Now));
        }

        _watcher.EnableRaisingEvents = true;
        return count;
    }

    public IAsyncEnumerable<MonitoredFileEvent> GetEventsAsync(CancellationToken cancellationToken)
    {
        return _eventChannel.Reader.ReadAllAsync(cancellationToken);
    }

    private IEnumerable<string> EnumerateExistingFiles()
    {
        var searchOption = _settings.IncludeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        if (!Directory.Exists(_settings.WatchPath))
        {
            return Enumerable.Empty<string>();
        }

        return Directory
            .EnumerateFileSystemEntries(_settings.WatchPath, "*", searchOption)
            .Where(MatchesAll);
    }

    private void OnFileDetected(object sender, FileSystemEventArgs e)
    {
        if (!MatchesAll(e.FullPath))
        {
            return;
        }

        _eventChannel.Writer.TryWrite(new MonitoredFileEvent(e.FullPath, DateTime.Now));
    }

    private static IReadOnlyList<FileFilter> BuildFilters(FileTriggerSettings settings)
    {
        var filterSettings = settings.EffectiveFilters;
        if (filterSettings.Count == 0)
        {
            return Array.Empty<FileFilter>();
        }

        return filterSettings
            .Select(filter => FileFilter.Create(filter.Kind, filter.Pattern))
            .ToArray();
    }

    private bool MatchesAll(string path)
    {
        if (_filters.Count == 0)
        {
            return true;
        }

        foreach (var filter in _filters)
        {
            if (!filter.Matches(path))
            {
                return false;
            }
        }

        return true;
    }

    private static string DetermineWatcherFilter(FileTriggerSettings settings)
    {
        var endsWithFilter = settings.EffectiveFilters
            .FirstOrDefault(filter => string.Equals(filter.Kind, "endsWith", StringComparison.OrdinalIgnoreCase)
                                      && !string.IsNullOrWhiteSpace(filter.Pattern));

        return endsWithFilter is null
            ? "*.*"
            : $"*{endsWithFilter.Pattern}";
    }

    public async ValueTask DisposeAsync()
    {
        _watcher.EnableRaisingEvents = false;
        _watcher.Dispose();
        _eventChannel.Writer.Complete();
        while (await _eventChannel.Reader.WaitToReadAsync().ConfigureAwait(false))
        {
            while (_eventChannel.Reader.TryRead(out _))
            {
                // Drain remaining events
            }
        }
    }
}
