using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using AutomationHub.Core.Configuration;
using AutomationHub.Core.Jobs;

namespace AutomationHub.App.ViewModels;

public sealed class JobListItem : INotifyPropertyChanged
{
    private string _status = "";
    private bool _isChecked;

    public JobListItem(JobManifestEntry entry)
    {
        Entry = entry;
        Job = entry.Job;
    }

    public JobManifestEntry Entry { get; }
    public JobDefinition Job { get; }
    public string ManifestPath => Entry.FilePath;

    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (_isChecked == value) return;
            _isChecked = value;
            OnPropertyChanged();
        }
    }

    public string Status
    {
        get => _status;
        private set
        {
            if (_status == value) return;
            _status = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusColor));
        }
    }

    public Brush StatusColor
    {
        get
        {
            if (_status.StartsWith("Error", StringComparison.OrdinalIgnoreCase) ||
                _status.StartsWith("Failed", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(_status, "Stopped", StringComparison.OrdinalIgnoreCase))
                return Brushes.Firebrick;

            if (_status.Contains("waiting", StringComparison.OrdinalIgnoreCase) ||
                _status.Contains("Watching", StringComparison.OrdinalIgnoreCase) ||
                _status.Contains("Scheduled", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(_status, "Idle", StringComparison.OrdinalIgnoreCase))
                return Brushes.SeaGreen;

            if (_status.Contains("Processing", StringComparison.OrdinalIgnoreCase) ||
                _status.Contains("Running", StringComparison.OrdinalIgnoreCase) ||
                _status.Contains("Launching", StringComparison.OrdinalIgnoreCase) ||
                _status.Contains("Detected", StringComparison.OrdinalIgnoreCase) ||
                _status.Contains("Starting", StringComparison.OrdinalIgnoreCase))
                return Brushes.DarkOrange;

            return Brushes.DimGray;
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
