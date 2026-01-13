using System;
using System.Threading.Tasks;
using System.Windows;
using AutomationHub.App.Runtime;

namespace AutomationHub.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
	public AutomationRuntimeHost? RuntimeHost { get; private set; }

	protected override async void OnStartup(StartupEventArgs e)
	{
		RuntimeHost = new AutomationRuntimeHost();
		try
		{
			await RuntimeHost.StartAsync();
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Failed to initialize Automation Hub runtime:\n{ex.Message}",
				"Automation Hub",
				MessageBoxButton.OK,
				MessageBoxImage.Error);
			Shutdown(-1);
			return;
		}

		base.OnStartup(e);
	}

	protected override async void OnExit(ExitEventArgs e)
	{
		if (RuntimeHost is not null)
		{
			try
			{
				await RuntimeHost.DisposeAsync();
			}
			catch
			{
				// Swallow exceptions during shutdown.
			}
		}

		base.OnExit(e);
	}

	public async Task ReloadRuntimeAsync()
	{
		if (RuntimeHost is not null)
		{
			try
			{
				await RuntimeHost.DisposeAsync();
			}
			catch
			{
				// Ignore reload-time disposal errors.
			}
		}

		RuntimeHost = new AutomationRuntimeHost();
		await RuntimeHost.StartAsync();
	}
}

