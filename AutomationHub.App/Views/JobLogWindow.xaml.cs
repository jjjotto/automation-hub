using System;
using System.Windows;
using AutomationHub.App.ViewModels;

namespace AutomationHub.App.Views;

public partial class JobLogWindow : Window
{
    public JobLogWindow(JobLogViewModel viewModel)
    {
        DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
    }

    private JobLogViewModel ViewModel => (JobLogViewModel)DataContext;

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        ViewModel.Dispose();
    }
}
