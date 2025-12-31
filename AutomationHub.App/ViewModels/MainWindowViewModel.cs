using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using AutomationHub.Core.Configuration;
using AutomationHub.Core.Jobs;

namespace AutomationHub.App.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private string _statusMessage = "Ready";

    public ObservableCollection<JobDefinition> Jobs { get; } = new();

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

            var jobsDir = ConfigPaths.JobsDirectory;
            if (!Directory.Exists(jobsDir))
            {
                StatusMessage = $"Jobs directory not found: {jobsDir}";
                return;
            }

            foreach (var jobFile in Directory.EnumerateFiles(jobsDir, "*.json", SearchOption.TopDirectoryOnly))
            {
                var json = File.ReadAllText(jobFile);
                var job = JsonSerializer.Deserialize<JobDefinition>(json);
                if (job is not null)
                {
                    Jobs.Add(job);
                }
            }

            StatusMessage = Jobs.Count == 0 ? "No jobs defined" : $"Loaded {Jobs.Count} job(s)";
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
}
