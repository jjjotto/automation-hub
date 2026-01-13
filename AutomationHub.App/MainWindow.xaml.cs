using System;
using System.IO;
using System.Windows;
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

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
        Loaded += (_, _) => _viewModel.LoadJobs();
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
}