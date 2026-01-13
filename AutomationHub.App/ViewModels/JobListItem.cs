using System.ComponentModel;
using System.Runtime.CompilerServices;
using AutomationHub.Core.Configuration;
using AutomationHub.Core.Jobs;

namespace AutomationHub.App.ViewModels;

public sealed class JobListItem : INotifyPropertyChanged
{
    private string _status = "";

    public JobListItem(JobManifestEntry entry)
    {
        Entry = entry;
        Job = entry.Job;
    }

    public JobManifestEntry Entry { get; }
    public JobDefinition Job { get; }
    public string ManifestPath => Entry.FilePath;

    public string Status
    {
        get => _status;
        private set
        {
            if (_status == value)
            {
                return;
            }

            _status = value;
            OnPropertyChanged();
        }
    }

    public void UpdateStatus(string? status)
    {
        Status = string.IsNullOrWhiteSpace(status) ? "Idle" : status;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
