using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using AutomationHub.App.Runtime;
using AutomationHub.Core.Execution;

namespace AutomationHub.App.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private string _statusMessage = "Ready";
    private AutomationRuntimeHost? _runtimeHost;

    public ObservableCollection<JobListItem> Jobs { get; } = new();

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (_statusMessage == value)
                return;
            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public void LoadJobs()
    {
        try
        {
            Jobs.Clear();

            var host = (Application.Current as App)?.RuntimeHost;
            if (host is null)
            {
                StatusMessage = "Runtime host has not been initialized.";
                return;
            }

            AttachToRuntimeHost(host);

            foreach (var entry in host.JobEntries)
            {
                var item = new JobListItem(entry);
                var status = host.StatusService.GetStatus(entry.Job.Name)?.Message;
                item.UpdateStatus(status);
                Jobs.Add(item);
            }

            if (host.JobLoadErrors.Count > 0)
            {
                var firstError = host.JobLoadErrors[0];
                StatusMessage = $"Loaded {Jobs.Count} job(s) with {host.JobLoadErrors.Count} load error(s). First: {firstError.Message}";
            }
            else
            {
                StatusMessage = Jobs.Count == 0 ? "No jobs defined" : $"Loaded {Jobs.Count} job(s)";
            }

            if (host.RuntimeErrors.Count > 0)
            {
                StatusMessage += $" | Runtime warning: {host.RuntimeErrors[0]}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading jobs: {ex.Message}";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void AttachToRuntimeHost(AutomationRuntimeHost host)
    {
        if (host is null)
        {
            return;
        }

        if (!ReferenceEquals(_runtimeHost, host))
        {
            if (_runtimeHost is not null)
            {
                _runtimeHost.StatusService.JobStatusChanged -= OnJobStatusChanged;
            }

            _runtimeHost = host;
            _runtimeHost.StatusService.JobStatusChanged += OnJobStatusChanged;
        }
    }

    private void OnJobStatusChanged(object? sender, JobStatusChangedEventArgs e)
    {
        if (Application.Current?.Dispatcher is null)
        {
            return;
        }

        Application.Current.Dispatcher.Invoke(() =>
        {
            var target = Jobs.FirstOrDefault(j => string.Equals(j.Job.Name, e.JobName, StringComparison.OrdinalIgnoreCase));
            target?.UpdateStatus(e.Message);
        });
    }

    public void StartCheckedJobs()
    {
        var host = (Application.Current as App)?.RuntimeHost;
        if (host is null) return;

        var selected = Jobs.Where(j => j.IsChecked).ToList();
        if (selected.Count == 0)
        {
            StatusMessage = "No jobs selected.";
            return;
        }

        foreach (var item in selected)
            _ = host.RestartJobAsync(item.Job.Name);

        StatusMessage = $"Starting {selected.Count} job(s).";
    }

    public async Task StopCheckedJobsAsync()
    {
        var host = (Application.Current as App)?.RuntimeHost;
        if (host is null) return;

        foreach (var item in Jobs.Where(j => j.IsChecked).ToList())
            await host.StopJobAsync(item.Job.Name);

        StatusMessage = "Selected jobs stopped.";
    }

    public async Task RemoveCheckedJobsAsync()
    {
        var host = (Application.Current as App)?.RuntimeHost;
        if (host is null) return;

        foreach (var item in Jobs.Where(j => j.IsChecked).ToList())
            await host.RemoveJobAsync(item.Job.Name);

        LoadJobs();
    }

    public void SetAllChecked(bool isChecked)
    {
        foreach (var job in Jobs)
            job.IsChecked = isChecked;
    }
}
