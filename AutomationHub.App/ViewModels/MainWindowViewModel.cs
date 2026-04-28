using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using AutomationHub.App.Runtime;
using AutomationHub.Core.Execution;

namespace AutomationHub.App.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private const string WebAppsConfigFileName = "webapps.json";
    private string _statusMessage = "Ready";
    private AutomationRuntimeHost? _runtimeHost;
    private readonly Dictionary<string, HashSet<int>> _preLaunchListeningPorts = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> SupportedLaunchExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".bat",
        ".cmd",
        ".lnk",
        ".exe"
    };
    private static readonly JsonSerializerOptions WebAppsJsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public ObservableCollection<JobListItem> Jobs { get; } = new();
    public ObservableCollection<WebAppItemViewModel> WebApps { get; } = new();

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

    public void AddWebAppsFromDrop(IReadOnlyCollection<string> droppedPaths)
    {
        if (droppedPaths.Count == 0)
        {
            StatusMessage = "No files were dropped.";
            return;
        }

        var added = 0;
        var started = 0;
        var errors = new List<string>();

        foreach (var path in droppedPaths)
        {
            if (!TryAddWebApp(path, out var item, out var addError))
            {
                if (!string.IsNullOrWhiteSpace(addError))
                {
                    errors.Add(addError);
                }

                continue;
            }

            added++;
            string? startError = null;
            if (item is not null && TryStartWebApp(item, out startError))
            {
                started++;
            }
            else if (!string.IsNullOrWhiteSpace(startError))
            {
                errors.Add(startError!);
            }
        }

        StatusMessage = errors.Count == 0
            ? $"Added {added} web app(s). Auto-started {started}."
            : $"Added {added} web app(s), auto-started {started}, with {errors.Count} issue(s). First: {errors[0]}";

        SaveWebAppsConfiguration();
    }

    public void LoadWebAppsConfiguration()
    {
        try
        {
            WebApps.Clear();
            _preLaunchListeningPorts.Clear();

            var configPath = GetWebAppsConfigPath();
            if (!File.Exists(configPath))
            {
                return;
            }

            var json = File.ReadAllText(configPath);
            var entries = JsonSerializer.Deserialize<List<WebAppConfigEntry>>(json, WebAppsJsonOptions) ?? new List<WebAppConfigEntry>();

            foreach (var entry in entries)
            {
                if (string.IsNullOrWhiteSpace(entry.SourcePath))
                {
                    continue;
                }

                if (!TryAddWebApp(entry.SourcePath, out var item, out _, allowMissingFile: false))
                {
                    continue;
                }

                if (item is not null)
                {
                    item.LastStartedAt = entry.LastStartedAt;
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load web apps: {ex.Message}";
        }
    }

    public void SaveWebAppsConfiguration()
    {
        try
        {
            var configPath = GetWebAppsConfigPath();
            var configDir = Path.GetDirectoryName(configPath);
            if (!string.IsNullOrWhiteSpace(configDir))
            {
                Directory.CreateDirectory(configDir);
            }

            var entries = WebApps
                .Select(app => new WebAppConfigEntry(app.SourcePath, app.LastStartedAt))
                .ToList();

            var json = JsonSerializer.Serialize(entries, WebAppsJsonOptions);
            File.WriteAllText(configPath, json);
        }
        catch
        {
            // Keep runtime resilient even if config cannot be written.
        }
    }

    public bool StartWebApp(WebAppItemViewModel item, out string? error)
    {
        if (item is null)
        {
            error = "No web app selected.";
            return false;
        }

        var started = TryStartWebApp(item, out error);
        if (started)
        {
            StatusMessage = $"Started '{item.DisplayName}'.";
        }
        else if (!string.IsNullOrWhiteSpace(error))
        {
            StatusMessage = error!;
        }

        return started;
    }

    public void RemoveWebApp(WebAppItemViewModel item)
    {
        if (item is null)
        {
            return;
        }

        WebApps.Remove(item);
        _preLaunchListeningPorts.Remove(item.SourcePath);
        StatusMessage = $"Removed '{item.DisplayName}'.";
        SaveWebAppsConfiguration();
    }

    public void RefreshWebAppsStatus()
    {
        foreach (var item in WebApps)
        {
            RefreshWebAppStatus(item);
        }
    }

    private bool TryAddWebApp(string droppedPath, out WebAppItemViewModel? item, out string? error, bool allowMissingFile = false)
    {
        item = null;
        error = null;

        if (string.IsNullOrWhiteSpace(droppedPath))
        {
            error = "Dropped path is empty.";
            return false;
        }

        if (!allowMissingFile && !File.Exists(droppedPath))
        {
            error = $"File not found: {droppedPath}";
            return false;
        }

        if (!IsSupportedLaunchFile(droppedPath))
        {
            error = $"Unsupported file type: {Path.GetExtension(droppedPath)}";
            return false;
        }

        if (WebApps.Any(w => string.Equals(w.SourcePath, droppedPath, StringComparison.OrdinalIgnoreCase)))
        {
            error = $"Already added: {Path.GetFileName(droppedPath)}";
            return false;
        }

        if (!TryResolveLaunchTarget(droppedPath, out var launchPath, out var launchArguments, out var resolveError))
        {
            error = resolveError;
            return false;
        }

        var displayName = Path.GetFileNameWithoutExtension(droppedPath);
        item = new WebAppItemViewModel(displayName, droppedPath, launchPath, launchArguments);
        WebApps.Add(item);
        return true;
    }

    private bool TryStartWebApp(WebAppItemViewModel item, out string? error)
    {
        error = null;

        try
        {
            var baselinePorts = GetAllListeningPorts();
            var startInfo = new ProcessStartInfo
            {
                FileName = item.LaunchPath,
                Arguments = item.LaunchArguments,
                UseShellExecute = true,
                WorkingDirectory = ResolveWorkingDirectory(item.LaunchPath)
            };

            var process = Process.Start(startInfo);
            item.LastStartedAt = DateTime.Now;
            item.TrackedProcessId = process?.HasExited == false ? process.Id : null;
            _preLaunchListeningPorts[item.SourcePath] = baselinePorts;
            RefreshWebAppStatus(item);
            return true;
        }
        catch (Exception ex)
        {
            item.IsRunning = false;
            item.Status = $"Start failed: {ex.Message}";
            error = $"Failed to start '{item.DisplayName}': {ex.Message}";
            return false;
        }
    }

    private static string ResolveWorkingDirectory(string launchPath)
    {
        var directory = Path.GetDirectoryName(launchPath);
        return string.IsNullOrWhiteSpace(directory) ? Environment.CurrentDirectory : directory;
    }

    private static bool IsSupportedLaunchFile(string path)
    {
        var extension = Path.GetExtension(path);
        return SupportedLaunchExtensions.Contains(extension);
    }

    private static string GetWebAppsConfigPath()
    {
        var configDirectory = Path.Combine(AppContext.BaseDirectory, "config");
        return Path.Combine(configDirectory, WebAppsConfigFileName);
    }

    private static bool TryResolveLaunchTarget(
        string sourcePath,
        out string launchPath,
        out string launchArguments,
        out string? error)
    {
        launchPath = sourcePath;
        launchArguments = string.Empty;
        error = null;

        var extension = Path.GetExtension(sourcePath);
        if (!string.Equals(extension, ".lnk", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!TryResolveShortcut(sourcePath, out var resolvedPath, out var resolvedArguments, out error))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(resolvedPath))
        {
            launchPath = resolvedPath;
        }

        launchArguments = resolvedArguments ?? string.Empty;
        return true;
    }

    private static bool TryResolveShortcut(
        string shortcutPath,
        out string? targetPath,
        out string? arguments,
        out string? error)
    {
        targetPath = null;
        arguments = null;
        error = null;

        object? shell = null;
        object? shortcut = null;

        try
        {
            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType is null)
            {
                error = "Shortcut resolution is not available on this system.";
                return false;
            }

            shell = Activator.CreateInstance(shellType);
            if (shell is null)
            {
                error = "Unable to initialize shortcut resolver.";
                return false;
            }

            shortcut = shellType.InvokeMember(
                "CreateShortcut",
                BindingFlags.InvokeMethod,
                binder: null,
                target: shell,
                args: new object[] { shortcutPath });

            if (shortcut is null)
            {
                error = "Shortcut target is empty.";
                return false;
            }

            var shortcutType = shortcut.GetType();
            targetPath = shortcutType.InvokeMember(
                "TargetPath",
                BindingFlags.GetProperty,
                binder: null,
                target: shortcut,
                args: null) as string;

            arguments = shortcutType.InvokeMember(
                "Arguments",
                BindingFlags.GetProperty,
                binder: null,
                target: shortcut,
                args: null) as string;

            if (string.IsNullOrWhiteSpace(targetPath))
            {
                error = $"Shortcut has no target: {shortcutPath}";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            error = $"Could not resolve shortcut '{Path.GetFileName(shortcutPath)}': {ex.Message}";
            return false;
        }
        finally
        {
            ReleaseComObject(shortcut);
            ReleaseComObject(shell);
        }
    }

    private static void ReleaseComObject(object? value)
    {
        if (value is null)
        {
            return;
        }

        try
        {
            if (System.Runtime.InteropServices.Marshal.IsComObject(value))
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(value);
            }
        }
        catch
        {
        }
    }

    private void RefreshWebAppStatus(WebAppItemViewModel item)
    {
        var running = false;
        var status = "Not running";

        if (item.TrackedProcessId.HasValue)
        {
            try
            {
                var process = Process.GetProcessById(item.TrackedProcessId.Value);
                running = !process.HasExited;
                if (running)
                {
                    status = $"Running (PID {process.Id})";
                }
            }
            catch
            {
                item.TrackedProcessId = null;
            }
        }

        if (!running)
        {
            var names = GetCandidateProcessNames(item);
            foreach (var processName in names)
            {
                if (string.IsNullOrWhiteSpace(processName))
                {
                    continue;
                }

                var matches = Process.GetProcessesByName(processName);
                if (matches.Length > 0)
                {
                    running = true;
                    status = $"Running ({processName})";
                    break;
                }
            }
        }

        if (!running && item.LastStartedAt.HasValue)
        {
            status = "Launch command executed. Waiting for detectable process.";
        }

        item.IsRunning = running;
        item.Status = status;

        if (running)
        {
            var ports = GetPortsForProcess(item);
            if (ports.Count > 0)
            {
                item.Ports = string.Join(", ", ports.OrderBy(p => p));
            }
            else
            {
                item.Ports = "Not detected";
            }
        }
        else
        {
            item.Ports = "Not detected";
        }
    }

    private static IReadOnlyCollection<string> GetCandidateProcessNames(WebAppItemViewModel item)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddProcessNameCandidate(names, item.SourcePath);
        AddProcessNameCandidate(names, item.LaunchPath);
        names.Remove("cmd");
        names.Remove("powershell");
        names.Remove("pwsh");
        names.Remove("start");
        return names;
    }

    private static void AddProcessNameCandidate(ISet<string> names, string path)
    {
        var fileName = Path.GetFileNameWithoutExtension(path);
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            names.Add(fileName);
        }
    }

    private IReadOnlyList<int> GetPortsForProcess(WebAppItemViewModel item)
    {
        var ports = new HashSet<int>();
        var candidatePids = new HashSet<int>();

        if (item.TrackedProcessId.HasValue)
        {
            try
            {
                var tracked = Process.GetProcessById(item.TrackedProcessId.Value);
                if (!tracked.HasExited)
                {
                    candidatePids.Add(tracked.Id);
                }
            }
            catch
            {
                item.TrackedProcessId = null;
            }
        }

        var names = GetCandidateProcessNames(item);
        foreach (var processName in names)
        {
            if (string.IsNullOrWhiteSpace(processName))
            {
                continue;
            }

            foreach (var match in Process.GetProcessesByName(processName))
            {
                try
                {
                    if (!match.HasExited)
                    {
                        candidatePids.Add(match.Id);
                    }
                }
                catch
                {
                }
            }
        }

        if (candidatePids.Count == 0)
            return ports.ToList();

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "netstat",
                Arguments = "-ano -p tcp",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using (var netstatProcess = Process.Start(psi))
            {
                if (netstatProcess is null)
                    return ports.ToList();

                using (var reader = netstatProcess.StandardOutput)
                {
                    var output = reader.ReadToEnd();
                    var lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var line in lines)
                    {
                        var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length < 5 || !string.Equals(parts[0], "TCP", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        if (!string.Equals(parts[3], "LISTENING", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        if (!int.TryParse(parts[4], out var pid) || !candidatePids.Contains(pid))
                        {
                            continue;
                        }

                        var localAddress = parts[1];
                        var portSeparator = localAddress.LastIndexOf(':');
                        if (portSeparator <= 0 || portSeparator >= localAddress.Length - 1)
                        {
                            continue;
                        }

                        var portText = localAddress[(portSeparator + 1)..];
                        if (int.TryParse(portText, out var port) && port > 0)
                        {
                            ports.Add(port);
                        }
                    }
                }
            }
        }
        catch
        {
        }

        if (ports.Count == 0)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "netstat",
                    Arguments = "-ano -p udp",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using (var netstatProcess = Process.Start(psi))
                {
                    if (netstatProcess is null)
                    {
                        return ports.ToList();
                    }

                    using (var reader = netstatProcess.StandardOutput)
                    {
                        var output = reader.ReadToEnd();
                        var lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                        foreach (var line in lines)
                        {
                            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length < 4 || !string.Equals(parts[0], "UDP", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            if (!int.TryParse(parts[3], out var pid) || !candidatePids.Contains(pid))
                            {
                                continue;
                            }

                            var localAddress = parts[1];
                            var portSeparator = localAddress.LastIndexOf(':');
                            if (portSeparator <= 0 || portSeparator >= localAddress.Length - 1)
                            {
                                continue;
                            }

                            var portText = localAddress[(portSeparator + 1)..];
                            if (int.TryParse(portText, out var port) && port > 0)
                            {
                                ports.Add(port);
                            }
                        }
                    }
                }
            }
            catch
            {
            }
        }

        if (ports.Count == 0)
        {
            foreach (var deltaPort in GetPortsFromLaunchDelta(item))
            {
                ports.Add(deltaPort);
            }
        }

        return ports.ToList();
    }

    private IReadOnlyCollection<int> GetPortsFromLaunchDelta(WebAppItemViewModel item)
    {
        if (!_preLaunchListeningPorts.TryGetValue(item.SourcePath, out var baseline))
        {
            return Array.Empty<int>();
        }

        var current = GetAllListeningPorts();
        if (current.Count == 0)
        {
            return Array.Empty<int>();
        }

        var diff = current.Where(port => !baseline.Contains(port)).ToList();
        return diff;
    }

    private static HashSet<int> GetAllListeningPorts()
    {
        var allPorts = new HashSet<int>();

        CollectPortsFromNetstat("-ano -p tcp", isTcp: true, allPorts);
        CollectPortsFromNetstat("-ano -p udp", isTcp: false, allPorts);

        return allPorts;
    }

    private static void CollectPortsFromNetstat(string arguments, bool isTcp, ISet<int> output)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "netstat",
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using var netstatProcess = Process.Start(psi);
            if (netstatProcess is null)
            {
                return;
            }

            using var reader = netstatProcess.StandardOutput;
            var outputText = reader.ReadToEnd();
            var lines = outputText.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (isTcp)
                {
                    if (parts.Length < 5 || !string.Equals(parts[0], "TCP", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!string.Equals(parts[3], "LISTENING", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (TryParseLocalPort(parts[1], out var tcpPort))
                    {
                        output.Add(tcpPort);
                    }

                    continue;
                }

                if (parts.Length < 4 || !string.Equals(parts[0], "UDP", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (TryParseLocalPort(parts[1], out var udpPort))
                {
                    output.Add(udpPort);
                }
            }
        }
        catch
        {
        }
    }

    private static bool TryParseLocalPort(string localAddress, out int port)
    {
        port = 0;
        if (string.IsNullOrWhiteSpace(localAddress))
        {
            return false;
        }

        var portSeparator = localAddress.LastIndexOf(':');
        if (portSeparator <= 0 || portSeparator >= localAddress.Length - 1)
        {
            return false;
        }

        return int.TryParse(localAddress[(portSeparator + 1)..], out port) && port > 0;
    }

    private sealed record WebAppConfigEntry(string SourcePath, DateTime? LastStartedAt);
}
