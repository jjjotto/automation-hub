using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using AutomationHub.Core.Execution;

namespace AutomationHub.App.ViewModels;

public sealed class JobLogViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly JobActivityLogService _logService;
    private readonly SynchronizationContext _syncContext;
    private bool _disposed;

    public JobLogViewModel(string jobName, JobActivityLogService logService)
    {
        JobName = jobName ?? throw new ArgumentNullException(nameof(jobName));
        _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        _syncContext = SynchronizationContext.Current ?? new SynchronizationContext();
        Entries = new ObservableCollection<JobLogEntryViewModel>();

        LoadInitial();
        _logService.JobLogChanged += OnJobLogChanged;
    }

    public string JobName { get; }
    public string Title => $"{JobName} Activity Log";
    public ObservableCollection<JobLogEntryViewModel> Entries { get; }

    private void LoadInitial()
    {
        foreach (var entry in _logService.GetEntries(JobName, 200))
        {
            Entries.Add(new JobLogEntryViewModel(entry));
        }
    }

    private void OnJobLogChanged(object? sender, JobActivityLogChangedEventArgs e)
    {
        if (!string.Equals(e.JobName, JobName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _syncContext.Post(_ =>
        {
            Entries.Add(new JobLogEntryViewModel(e.Entry));
            while (Entries.Count > 300)
            {
                Entries.RemoveAt(0);
            }
        }, null);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _logService.JobLogChanged -= OnJobLogChanged;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed class JobLogEntryViewModel
{
    public JobLogEntryViewModel(JobActivityLogEntry entry)
    {
        Entry = entry ?? throw new ArgumentNullException(nameof(entry));
    }

    public JobActivityLogEntry Entry { get; }
    public DateTime Timestamp => Entry.Timestamp;
    public string Message => Entry.Message;
}
