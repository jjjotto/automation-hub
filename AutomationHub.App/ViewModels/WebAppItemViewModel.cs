using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AutomationHub.App.ViewModels;

public sealed class WebAppItemViewModel : INotifyPropertyChanged
{
    private bool _isRunning;
    private string _status = "Not running";
    private DateTime? _lastStartedAt;
    private int? _trackedProcessId;
    private string _ports = "Not detected";

    public WebAppItemViewModel(string displayName, string sourcePath, string launchPath, string launchArguments)
    {
        DisplayName = displayName;
        SourcePath = sourcePath;
        LaunchPath = launchPath;
        LaunchArguments = launchArguments;
    }

    public string DisplayName { get; }
    public string SourcePath { get; }
    public string LaunchPath { get; }
    public string LaunchArguments { get; }

    public bool IsRunning
    {
        get => _isRunning;
        set => SetField(ref _isRunning, value);
    }

    public string Status
    {
        get => _status;
        set => SetField(ref _status, value);
    }

    public DateTime? LastStartedAt
    {
        get => _lastStartedAt;
        set => SetField(ref _lastStartedAt, value);
    }

    public int? TrackedProcessId
    {
        get => _trackedProcessId;
        set => SetField(ref _trackedProcessId, value);
    }

    public string Ports
    {
        get => _ports;
        set => SetField(ref _ports, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetField<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(storage, value))
        {
            return false;
        }

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
