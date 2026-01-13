using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using AutomationHub.Core.Jobs;

namespace AutomationHub.App.ViewModels;

public sealed class JobSettingsViewModel : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private bool _enabled;
    private JobType _type;

    public JobSettingsViewModel(JobDefinition source, string manifestPath)
    {
        ManifestPath = manifestPath;
        _originalJob = source;

        _name = source.Name;
        _enabled = source.Enabled;
        _type = source.Type;

        Process = JobProcessSettingsViewModel.FromSettings(source.Process);
        FileTrigger = FileTriggerSettingsViewModel.FromSettings(source.FileTrigger);
        Schedule = source.Schedule;
        Tags = source.Tags;
        Notes = source.Notes;
        OutputLogPath = source.OutputLogPath;
    }

    private readonly JobDefinition _originalJob;

    public string ManifestPath { get; }
    public JobProcessSettingsViewModel Process { get; }
    public FileTriggerSettingsViewModel FileTrigger { get; }

    public ScheduleSettings? Schedule { get; }
    public IReadOnlyCollection<string>? Tags { get; }
    public string? Notes { get; }
    public string? OutputLogPath { get; }

    public IReadOnlyList<JobType> JobTypeOptions { get; } = new[]
    {
        JobType.Manual,
        JobType.FileTrigger,
        JobType.Scheduled,
        JobType.Hybrid
    };

    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    public bool Enabled
    {
        get => _enabled;
        set => SetField(ref _enabled, value);
    }

    public JobType Type
    {
        get => _type;
        set => SetField(ref _type, value);
    }

    public bool IsFileTriggerVisible => Type.HasFlag(JobType.FileTrigger);

    public JobDefinition ToJobDefinition()
    {
        return new JobDefinition
        {
            Name = Name,
            Enabled = Enabled,
            Type = Type,
            Process = Process.ToSettings(),
            FileTrigger = Type.HasFlag(JobType.FileTrigger) ? FileTrigger.ToSettings() : null,
            Schedule = _originalJob.Schedule,
            OutputLogPath = _originalJob.OutputLogPath,
            Tags = _originalJob.Tags,
            Notes = _originalJob.Notes
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private bool SetField<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        OnPropertyChanged(propertyName);

        if (propertyName == nameof(Type))
        {
            OnPropertyChanged(nameof(IsFileTriggerVisible));
        }

        return true;
    }
}

public sealed class JobProcessSettingsViewModel : INotifyPropertyChanged
{
    private string _command = string.Empty;
    private string? _arguments;
    private string? _workingDirectory;
    private int _timeoutMinutes;

    public static JobProcessSettingsViewModel FromSettings(JobProcessSettings? settings)
    {
        settings ??= new JobProcessSettings();
        return new JobProcessSettingsViewModel
        {
            _command = settings.Command ?? string.Empty,
            _arguments = settings.Arguments,
            _workingDirectory = settings.WorkingDirectory,
            _timeoutMinutes = settings.TimeoutMinutes
        };
    }

    public string Command
    {
        get => _command;
        set => SetField(ref _command, value);
    }

    public string? Arguments
    {
        get => _arguments;
        set => SetField(ref _arguments, value);
    }

    public string? WorkingDirectory
    {
        get => _workingDirectory;
        set => SetField(ref _workingDirectory, value);
    }

    public int TimeoutMinutes
    {
        get => _timeoutMinutes;
        set => SetField(ref _timeoutMinutes, value);
    }

    public JobProcessSettings ToSettings()
    {
        return new JobProcessSettings
        {
            Command = Command,
            Arguments = Arguments,
            WorkingDirectory = WorkingDirectory,
            TimeoutMinutes = TimeoutMinutes
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private bool SetField<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public sealed class FileTriggerSettingsViewModel : INotifyPropertyChanged
{
    private string _watchPath = string.Empty;
    private bool _includeSubfolders;

    public ObservableCollection<FileFilterRuleViewModel> Filters { get; } = new();

    public IReadOnlyList<string> FilterKindOptions { get; } = new[]
    {
        "startsWith",
        "endsWith",
        "contains",
        "regex",
        "All"
    };

    public static FileTriggerSettingsViewModel FromSettings(FileTriggerSettings? settings)
    {
        settings ??= new FileTriggerSettings();
        var viewModel = new FileTriggerSettingsViewModel
        {
            _watchPath = settings.WatchPath ?? string.Empty,
            _includeSubfolders = settings.IncludeSubfolders
        };

        foreach (var filter in settings.EffectiveFilters)
        {
            viewModel.Filters.Add(FileFilterRuleViewModel.FromSettings(filter));
        }

        if (viewModel.Filters.Count == 0)
        {
            viewModel.Filters.Add(FileFilterRuleViewModel.CreateDefault());
        }

        return viewModel;
    }

    public string WatchPath
    {
        get => _watchPath;
        set => SetField(ref _watchPath, value);
    }

    public bool IncludeSubfolders
    {
        get => _includeSubfolders;
        set => SetField(ref _includeSubfolders, value);
    }

    public void AddFilter()
    {
        Filters.Add(FileFilterRuleViewModel.CreateDefault());
    }

    public void RemoveFilter(FileFilterRuleViewModel? filter)
    {
        if (filter is null)
        {
            return;
        }

        if (Filters.Count <= 1)
        {
            Filters[0].Reset();
            return;
        }

        Filters.Remove(filter);
    }

    public FileTriggerSettings ToSettings()
    {
        var normalizedFilters = Filters
            .Select(filter => filter.ToSettings())
            .ToArray();

        return new FileTriggerSettings
        {
            WatchPath = WatchPath,
            IncludeSubfolders = IncludeSubfolders,
            Filters = normalizedFilters
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private bool SetField<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public sealed class FileFilterRuleViewModel : INotifyPropertyChanged
{
    private string _kind;
    private string _pattern;

    private FileFilterRuleViewModel(string kind, string pattern)
    {
        _kind = string.IsNullOrWhiteSpace(kind) ? "startsWith" : kind;
        _pattern = pattern ?? string.Empty;
    }

    public static FileFilterRuleViewModel CreateDefault() => new("startsWith", string.Empty);

    public static FileFilterRuleViewModel FromSettings(FileFilterSettings? settings)
    {
        settings ??= new FileFilterSettings();
        return new FileFilterRuleViewModel(settings.Kind, settings.Pattern);
    }

    public string Kind
    {
        get => _kind;
        set => SetField(ref _kind, value);
    }

    public string Pattern
    {
        get => _pattern;
        set => SetField(ref _pattern, value);
    }

    public FileFilterSettings ToSettings()
    {
        return new FileFilterSettings
        {
            Kind = Kind,
            Pattern = Pattern
        };
    }

    public void Reset()
    {
        Kind = "startsWith";
        Pattern = string.Empty;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private bool SetField<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
