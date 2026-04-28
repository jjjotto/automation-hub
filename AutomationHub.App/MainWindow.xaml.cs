using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using AutomationHub.App.ViewModels;
using AutomationHub.App.Views;
using AutomationHub.Core.Configuration;
using AutomationHub.Core.Jobs;

namespace AutomationHub.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel = new();
    private readonly DispatcherTimer _webAppMonitorTimer = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
        Loaded += OnLoaded;
        Closed += OnClosed;

        _webAppMonitorTimer.Interval = TimeSpan.FromSeconds(3);
        _webAppMonitorTimer.Tick += (_, _) => _viewModel.RefreshWebAppsStatus();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _viewModel.LoadJobs();
        _viewModel.LoadWebAppsConfiguration();
        _viewModel.RefreshWebAppsStatus();
        _webAppMonitorTimer.Start();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _webAppMonitorTimer.Stop();
        _viewModel.SaveWebAppsConfiguration();
    }

    private void OnJobSettingsClicked(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element)
        {
            return;
        }

        if (element.DataContext is not JobListItem jobItem)
        {
            return;
        }

        var dialog = new JobSettingsWindow(new JobSettingsViewModel(jobItem.Job, jobItem.ManifestPath))
        {
            Owner = this
        };

        var result = dialog.ShowDialog();
        if (result == true)
        {
            _viewModel.LoadJobs();
        }
    }

    private void OnAddJobClicked(object sender, RoutedEventArgs e)
    {
        try
        {
            var jobsDirectory = ConfigPaths.JobsDirectory;
            Directory.CreateDirectory(jobsDirectory);

            var fileName = $"job-{DateTime.Now:yyyyMMddHHmmss}.json";
            var manifestPath = Path.Combine(jobsDirectory, fileName);

            var newJob = new JobDefinition
            {
                Name = "New Job",
                Enabled = true,
                Type = JobType.FileTrigger,
                Process = new JobProcessSettings
                {
                    Command = string.Empty,
                    WorkingDirectory = jobsDirectory,
                    TimeoutMinutes = 60
                },
                FileTrigger = new FileTriggerSettings
                {
                    WatchPath = jobsDirectory,
                    IncludeSubfolders = true,
                    Filters = new[]
                    {
                        new FileFilterSettings
                        {
                            Kind = "startsWith",
                            Pattern = string.Empty
                        }
                    }
                }
            };

            var dialog = new JobSettingsWindow(new JobSettingsViewModel(newJob, manifestPath))
            {
                Owner = this
            };

            var result = dialog.ShowDialog();
            if (result == true)
            {
                _viewModel.LoadJobs();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Unable to create job: {ex.Message}", "Automation Hub", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnViewLogClicked(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element)
        {
            return;
        }

        if (element.DataContext is not JobListItem jobItem)
        {
            return;
        }

        if (Application.Current is not App app || app.RuntimeHost is null)
        {
            MessageBox.Show(this, "Runtime host is not available.", "Automation Hub", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var viewModel = new JobLogViewModel(jobItem.Job.Name, app.RuntimeHost.ActivityLog);
        var window = new JobLogWindow(viewModel)
        {
            Owner = this
        };

        window.Show();
    }

    private void OnStartCheckedClicked(object sender, RoutedEventArgs e)
    {
        _viewModel.StartCheckedJobs();
    }

    private async void OnStopCheckedClicked(object sender, RoutedEventArgs e)
    {
        await _viewModel.StopCheckedJobsAsync();
    }

    private async void OnRemoveCheckedClicked(object sender, RoutedEventArgs e)
    {
        var checkedJobs = new List<string>();
        foreach (var job in _viewModel.Jobs)
        {
            if (job.IsChecked)
                checkedJobs.Add(job.Job.Name);
        }

        if (checkedJobs.Count == 0)
            return;

        var names = "  \u2022 " + string.Join("\n  \u2022 ", checkedJobs);
        var result = MessageBox.Show(
            this,
            $"Permanently remove {checkedJobs.Count} job(s)?\n\n{names}\n\nThis will delete their configuration files and cannot be undone.",
            "Remove Jobs",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
            await _viewModel.RemoveCheckedJobsAsync();
    }

    private void OnToggleSelectAll(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox cb)
            _viewModel.SetAllChecked(cb.IsChecked == true);
    }

    private void OnWebAppsDropZoneDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnWebAppsDropZoneDrop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            return;
        }

        if (e.Data.GetData(DataFormats.FileDrop) is not string[] droppedFiles)
        {
            return;
        }

        _viewModel.AddWebAppsFromDrop(droppedFiles);
        _viewModel.RefreshWebAppsStatus();
    }

    private void OnWebAppStartClicked(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element)
        {
            return;
        }

        if (element.DataContext is not WebAppItemViewModel item)
        {
            return;
        }

        _viewModel.StartWebApp(item, out _);
        _viewModel.RefreshWebAppsStatus();
    }

    private void OnWebAppRemoveClicked(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element)
        {
            return;
        }

        if (element.DataContext is not WebAppItemViewModel item)
        {
            return;
        }

        _viewModel.RemoveWebApp(item);
    }
}