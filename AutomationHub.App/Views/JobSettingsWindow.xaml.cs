using System;
using System.Windows;
using AutomationHub.App.ViewModels;
using AutomationHub.Core.Configuration;
using AppNamespace = AutomationHub.App;

namespace AutomationHub.App.Views;

public partial class JobSettingsWindow : Window
{
    public JobSettingsWindow(JobSettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private JobSettingsViewModel ViewModel => (JobSettingsViewModel)DataContext;

    private async void OnSaveClicked(object sender, RoutedEventArgs e)
    {
        try
        {
            var job = ViewModel.ToJobDefinition();
            JobManifestWriter.Save(job, ViewModel.ManifestPath);

            if (Application.Current is AppNamespace.App app)
            {
                await app.ReloadRuntimeAsync();
            }

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Failed to save job: {ex.Message}", "Automation Hub", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnCancelClicked(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void OnAddFilterClicked(object sender, RoutedEventArgs e)
    {
        ViewModel.FileTrigger.AddFilter();
    }

    private void OnRemoveFilterClicked(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element)
        {
            return;
        }

        if (element.Tag is FileFilterRuleViewModel filter)
        {
            ViewModel.FileTrigger.RemoveFilter(filter);
        }
    }
}
