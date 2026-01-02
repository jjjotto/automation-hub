# Process Execution and Monitoring

This document describes the process execution and monitoring capabilities added to Automation Hub to ensure jobs run reliably without getting stuck.

## Overview

The process execution system provides three key capabilities:

1. **Process Execution** - Reliable process launching with proper configuration
2. **Startup Monitoring** - Ensures processes start successfully and don't fail immediately
3. **Timeout Protection** - Prevents processes from running indefinitely
4. **Health Monitoring** - Tracks running jobs and detects potentially stuck processes

## Architecture

### Core Components

#### 1. ProcessExecutionService
Located: `AutomationHub.Core/Execution/ProcessExecutionService.cs`

Handles the actual execution of job processes with the following features:

- **Process Creation**: Configures process with command, arguments, working directory, and environment variables
- **Startup Monitoring**: Monitors the process for the first 5 seconds to detect immediate failures
- **Timeout Enforcement**: Automatically terminates processes that exceed their configured timeout
- **Output Capture**: Redirects stdout/stderr to prevent buffer deadlocks
- **Process Tree Cleanup**: Kills parent and all child processes on timeout or cancellation
- **Error Handling**: Comprehensive exception handling with detailed error messages

**Key Methods:**
```csharp
Task<JobRunResult> ExecuteAsync(string jobName, JobProcessSettings settings, CancellationToken cancellationToken)
```

**Timeout Configuration:**
- Default timeout: 60 minutes
- Configurable per job via `JobProcessSettings.TimeoutMinutes`
- Set to 0 to use default timeout

#### 2. ProcessMonitorService
Located: `AutomationHub.Core/Execution/ProcessMonitorService.cs`

Tracks the status of running and completed jobs:

- **Running Jobs Registry**: Maintains a list of all currently executing jobs
- **Execution History**: Keeps the last 100 completed jobs
- **Status Updates**: Tracks when jobs last reported status
- **Stuck Job Detection**: Identifies jobs running longer than expected
- **Concurrent Access**: Thread-safe for use in multi-threaded scenarios

**Key Methods:**
```csharp
void RegisterRunningJob(string jobName, DateTime startTime)
void UpdateJobStatus(string jobName, string statusMessage)
void CompleteJob(JobRunResult result)
IReadOnlyList<JobExecutionInfo> GetPotentiallyStuckJobs(TimeSpan threshold)
bool IsJobRunning(string jobName)
```

#### 3. JobExecutionOrchestrator
Located: `AutomationHub.Core/Execution/JobExecutionOrchestrator.cs`

Coordinates execution and monitoring:

- **Job Validation**: Ensures job configuration is valid before execution
- **Duplicate Prevention**: Prevents the same job from running multiple times concurrently
- **Integrated Monitoring**: Automatically registers and tracks job execution
- **Error Recovery**: Handles exceptions and ensures jobs are properly cleaned up

**Key Methods:**
```csharp
Task<JobRunResult> ExecuteJobAsync(JobDefinition job, CancellationToken cancellationToken)
Task<IReadOnlyList<JobExecutionInfo>> CheckForStuckJobsAsync(TimeSpan? threshold)
ProcessMonitorService Monitor { get; }
```

### Enhanced Data Models

#### JobRunResult (Enhanced)
Located: `AutomationHub.Core/Execution/JobRunResult.cs`

Now includes:
- `ExitCode` - Process exit code
- `Status` - Enum indicating completion status
- `Duration` - Calculated execution time

**JobRunStatus Enum:**
- `Running` - Job is currently executing
- `CompletedSuccessfully` - Job finished with exit code 0
- `Failed` - Job finished with non-zero exit code
- `TimedOut` - Job exceeded timeout and was terminated
- `Cancelled` - Job was cancelled by user or system

#### JobExecutionInfo
Located: `AutomationHub.Core/Execution/ProcessMonitorService.cs`

Tracks real-time job execution:
- `JobName` - Name of the executing job
- `StartTime` - When execution began
- `LastStatusUpdate` - Last time status was updated
- `StatusMessage` - Current status message
- `RunningDuration` - How long job has been running
- `TimeSinceLastUpdate` - Time since last status update

## Usage Examples

### Basic Job Execution

```csharp
var orchestrator = new JobExecutionOrchestrator();

// Execute a job
var result = await orchestrator.ExecuteJobAsync(jobDefinition, cancellationToken);

if (result.Success)
{
    Console.WriteLine($"Job completed in {result.Duration?.TotalSeconds} seconds");
}
else
{
    Console.WriteLine($"Job failed: {result.Message}");
}
```

### Monitoring Running Jobs

```csharp
var orchestrator = new JobExecutionOrchestrator();

// Get all running jobs
var runningJobs = orchestrator.Monitor.GetAllRunningJobs();
foreach (var job in runningJobs)
{
    Console.WriteLine($"{job.JobName} has been running for {job.RunningDuration.TotalMinutes:F1} minutes");
}

// Check for stuck jobs (running > 2 hours)
var stuckJobs = await orchestrator.CheckForStuckJobsAsync(TimeSpan.FromHours(2));
foreach (var job in stuckJobs)
{
    Console.WriteLine($"WARNING: {job.JobName} may be stuck (running {job.RunningDuration.TotalHours:F1} hours)");
}
```

### Viewing Execution History

```csharp
var orchestrator = new JobExecutionOrchestrator();

// Get recent completed jobs
var completedJobs = orchestrator.Monitor.GetCompletedJobs(maxCount: 10);
foreach (var job in completedJobs)
{
    var status = job.Success ? "✓" : "✗";
    Console.WriteLine($"{status} {job.JobName} - {job.Message}");
}
```

### Preventing Duplicate Execution

```csharp
var orchestrator = new JobExecutionOrchestrator();

// Check if already running
if (orchestrator.Monitor.IsJobRunning("MyJob"))
{
    Console.WriteLine("Job is already running, skipping...");
    return;
}

// Execute job
await orchestrator.ExecuteJobAsync(jobDefinition, cancellationToken);
```

## Configuration

### Job Process Settings

Configure process behavior in job JSON files:

```json
{
  "name": "My Job",
  "process": {
    "command": "C:/scripts/myjob.bat",
    "arguments": "--input data.txt",
    "workingDirectory": "C:/workspace",
    "timeoutMinutes": 30,
    "environment": {
      "MY_VAR": "value"
    }
  }
}
```

**Settings:**
- `command` (required) - Path to executable or script
- `arguments` (optional) - Command line arguments
- `workingDirectory` (optional) - Working directory (defaults to current directory)
- `timeoutMinutes` (optional) - Maximum runtime in minutes (0 = 60 minute default)
- `environment` (optional) - Additional environment variables

### Timeout Behavior

**Default Timeout**: 60 minutes

**Custom Timeout**: Set `timeoutMinutes` in process settings

**What Happens on Timeout:**
1. Process is killed along with all child processes
2. JobRunResult returns with `Success = false`
3. Message indicates timeout duration
4. Status is set to `TimedOut`

**Example:**
```json
{
  "process": {
    "command": "longrunning.exe",
    "timeoutMinutes": 120
  }
}
```

### Startup Monitoring

Processes are monitored for 5 seconds after launch:

- **Check Interval**: 1 second
- **Total Checks**: 5 attempts
- **Failure Detection**: Process exits with non-zero code during startup

**Purpose**: Detect immediate failures like missing files, permission errors, or invalid configurations before considering the job "running".

## Integration with Existing Services

### With JobSchedulerService

```csharp
var orchestrator = new JobExecutionOrchestrator();
var scheduler = new JobSchedulerService();

// Register scheduled job that executes via orchestrator
var handle = scheduler.RegisterMinutePollJob(
    jobDefinition,
    async (ct) => await orchestrator.ExecuteJobAsync(jobDefinition, ct),
    cancellationToken);
```

### With FileMonitoringService

```csharp
var orchestrator = new JobExecutionOrchestrator();
var fileMonitor = new FileMonitoringService(fileTriggerSettings);

await fileMonitor.StartAsync(cancellationToken);

await foreach (var fileEvent in fileMonitor.GetEventsAsync(cancellationToken))
{
    // Execute job when file detected
    var result = await orchestrator.ExecuteJobAsync(jobDefinition, cancellationToken);
    Console.WriteLine($"Job triggered by file: {fileEvent.FilePath} - Result: {result.Success}");
}
```

## Best Practices

### 1. Always Set Appropriate Timeouts
```csharp
// Bad - no timeout, could run forever
"timeoutMinutes": 0

// Good - reasonable timeout for the task
"timeoutMinutes": 30
```

### 2. Monitor for Stuck Jobs Periodically
```csharp
// Run periodically (e.g., every 15 minutes)
var stuckJobs = await orchestrator.CheckForStuckJobsAsync(TimeSpan.FromHours(1));
if (stuckJobs.Count > 0)
{
    // Alert, log, or take corrective action
}
```

### 3. Use Cancellation Tokens
```csharp
// Pass cancellation token for graceful shutdown
using var cts = new CancellationTokenSource();
var result = await orchestrator.ExecuteJobAsync(job, cts.Token);
```

### 4. Check Execution History
```csharp
// Regularly review failed jobs
var failed = orchestrator.Monitor.GetCompletedJobs()
    .Where(r => !r.Success)
    .ToList();
```

### 5. Prevent Duplicate Execution
```csharp
if (!orchestrator.Monitor.IsJobRunning(job.Name))
{
    await orchestrator.ExecuteJobAsync(job, cancellationToken);
}
```

## Error Handling

The system handles various failure scenarios:

| Scenario | Behavior | Result |
|----------|----------|--------|
| Process not found | Returns failure immediately | `Success = false`, message explains issue |
| Immediate crash | Detected during startup monitoring | `Success = false`, exit code reported |
| Timeout exceeded | Process killed forcefully | `Success = false`, timeout message |
| Cancellation | Process killed, cleanup performed | `Success = false`, cancellation message |
| Non-zero exit | Treated as failure | `Success = false`, exit code reported |
| Exception | Caught, process cleaned up | `Success = false`, exception message |

## Thread Safety

All services are designed for concurrent access:

- `ProcessMonitorService` uses `ConcurrentDictionary` and `ConcurrentQueue`
- `ProcessExecutionService` is stateless and thread-safe
- `JobExecutionOrchestrator` coordinates thread-safe operations

**Safe for:**
- Multiple jobs executing simultaneously
- Multiple threads querying status
- Background monitoring tasks

## Performance Considerations

- **Memory**: Each running job tracks minimal state (~1KB)
- **History Limit**: Only last 100 completed jobs retained
- **No Polling**: Event-driven monitoring, no background threads
- **Async/Await**: Non-blocking execution model

## Future Enhancements

Potential improvements for future releases:

1. **Process Output Capture** - Store stdout/stderr in results
2. **Retry Logic** - Automatic retry on transient failures
3. **Progress Reporting** - Real-time progress updates from jobs
4. **Resource Limits** - CPU/memory constraints
5. **Job Priorities** - Queue management for concurrent execution
6. **Health Checks** - Ping mechanism for long-running jobs
7. **Notification System** - Alerts on failure or stuck jobs

## Testing

Example unit test structure:

```csharp
[Test]
public async Task ExecuteAsync_ProcessTimesOut_ReturnsFailure()
{
    var service = new ProcessExecutionService();
    var settings = new JobProcessSettings 
    { 
        Command = "longrunning.exe",
        TimeoutMinutes = 1
    };

    var result = await service.ExecuteAsync("TestJob", settings);

    Assert.False(result.Success);
    Assert.Contains("timed out", result.Message);
}
```

## Troubleshooting

### Job Reports as Failed But Should Have Succeeded

**Check:**
- Process exit code (non-zero = failure)
- Timeout setting (job may need more time)
- Process output/error streams

### Job Shows as Running But Has Finished

**Cause:** Process didn't exit properly

**Solution:** Ensure job processes exit cleanly with appropriate exit codes

### Job Gets Stuck Despite Timeout

**Check:**
- Timeout is properly configured in JSON
- Process isn't ignoring termination signals
- Child processes are being killed (process tree cleanup)

### Multiple Instances Running

**Cause:** Not checking if job is already running

**Solution:** Use `IsJobRunning()` before execution

## Summary

The process execution and monitoring system ensures:

✓ **Reliable Execution** - Proper process configuration and launch  
✓ **Startup Verification** - Detects immediate failures  
✓ **Timeout Protection** - No runaway processes  
✓ **Status Tracking** - Real-time monitoring of running jobs  
✓ **Stuck Detection** - Identifies problematic long-running jobs  
✓ **Duplicate Prevention** - Avoids concurrent execution  
✓ **History Tracking** - Maintains execution audit trail  
✓ **Thread Safety** - Safe for concurrent operations  

For questions or issues, refer to the source code documentation in the `AutomationHub.Core/Execution` namespace.
