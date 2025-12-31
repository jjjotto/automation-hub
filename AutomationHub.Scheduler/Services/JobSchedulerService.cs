using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using AutomationHub.Core.Jobs;

namespace AutomationHub.Scheduler.Services;

public sealed class JobSchedulerService
{
    private readonly ConcurrentDictionary<string, ScheduledJobHandle> _handles = new();

    public ScheduledJobHandle RegisterMinutePollJob(JobDefinition job, Func<CancellationToken, Task> callback, CancellationToken cancellationToken)
    {
        if (job.Schedule is null)
        {
            throw new InvalidOperationException("Schedule settings are required to register a scheduled job.");
        }

        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var runnerTask = Task.Run(async () =>
        {
            var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
            while (await timer.WaitForNextTickAsync(linkedCts.Token).ConfigureAwait(false))
            {
                if (!job.Enabled)
                {
                    continue;
                }

                if (job.Schedule.StartPaused)
                {
                    continue;
                }

                await callback(linkedCts.Token).ConfigureAwait(false);
            }
        }, linkedCts.Token);

        var handle = new ScheduledJobHandle(job.Name, linkedCts, runnerTask);
        _handles[job.Name] = handle;
        return handle;
    }

    public async Task StopAsync(string jobName)
    {
        if (_handles.TryRemove(jobName, out var handle))
        {
            await handle.StopAsync().ConfigureAwait(false);
        }
    }
}

public sealed class ScheduledJobHandle
{
    private readonly CancellationTokenSource _cts;
    private readonly Task _runnerTask;

    public string JobName { get; }

    public ScheduledJobHandle(string jobName, CancellationTokenSource cts, Task runnerTask)
    {
        JobName = jobName;
        _cts = cts;
        _runnerTask = runnerTask;
    }

    public async Task StopAsync()
    {
        _cts.Cancel();
        try
        {
            await _runnerTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // expected when stopping
        }
        finally
        {
            _cts.Dispose();
        }
    }
}
