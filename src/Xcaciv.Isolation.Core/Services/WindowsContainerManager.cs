using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using Xcaciv.Isolation.Core.Exceptions;
using Xcaciv.Isolation.Core.Interfaces;
using Xcaciv.Isolation.Core.Models;

namespace Xcaciv.Isolation.Core.Services;

/// <summary>
/// Windows container manager using Job Objects for isolation
/// </summary>
public sealed class WindowsContainerManager : IContainerManager, IDisposable
{
    private readonly ConcurrentDictionary<string, ContainerInstance> containers = new();
    private bool disposed;

    public async Task<ContainerInfo> StartAsync(ContainerConfiguration configuration, CancellationToken cancellationToken = default)
    {
        ValidateConfiguration(configuration);

        var containerId = Guid.NewGuid().ToString("N");
        var jobName = $"Container_{containerId}";

        var jobObject = new WindowsJobObject(jobName);
        
        try
        {
            // Apply resource limits if specified
            if (configuration.MemoryLimitMB.HasValue)
            {
                jobObject.SetMemoryLimit(configuration.MemoryLimitMB.Value * 1024 * 1024);
            }

            if (configuration.CpuLimitPercent.HasValue)
            {
                jobObject.SetCpuLimit(configuration.CpuLimitPercent.Value);
            }

            // Start the process
            var startInfo = CreateProcessStartInfo(configuration);
            var process = Process.Start(startInfo) 
                ?? throw new ContainerException("Failed to start container process");

            // Assign process to job object for isolation
            jobObject.AssignProcess(process.Handle);

            var instance = new ContainerInstance
            {
                Id = containerId,
                Name = configuration.Name,
                Process = process,
                JobObject = jobObject,
                Configuration = configuration,
                CreatedAt = DateTime.UtcNow,
                StartedAt = DateTime.UtcNow
            };

            if (!containers.TryAdd(containerId, instance))
            {
                process.Kill();
                throw new ContainerException("Failed to register container");
            }

            // Monitor process exit
            _ = MonitorProcessAsync(containerId, process);

            return CreateContainerInfo(instance);
        }
        catch
        {
            jobObject.Dispose();
            throw;
        }
    }

    public Task StopAsync(string containerId, CancellationToken cancellationToken = default)
    {
        if (String.IsNullOrWhiteSpace(containerId))
        {
            throw new ArgumentException("Container ID cannot be null or whitespace", nameof(containerId));
        }

        if (!containers.TryGetValue(containerId, out var instance))
        {
            throw new ContainerNotFoundException(containerId);
        }

        try
        {
            if (!instance.Process.HasExited)
            {
                instance.Process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // Process already exited
        }

        return Task.CompletedTask;
    }

    public Task<ContainerInfo?> GetContainerAsync(string containerId, CancellationToken cancellationToken = default)
    {
        if (String.IsNullOrWhiteSpace(containerId))
        {
            return Task.FromResult<ContainerInfo?>(null);
        }

        if (!containers.TryGetValue(containerId, out var instance))
        {
            return Task.FromResult<ContainerInfo?>(null);
        }

        return Task.FromResult<ContainerInfo?>(CreateContainerInfo(instance));
    }

    public Task<IReadOnlyCollection<ContainerInfo>> ListContainersAsync(CancellationToken cancellationToken = default)
    {
        var containerList = containers.Values
            .Select(CreateContainerInfo)
            .ToList();

        return Task.FromResult<IReadOnlyCollection<ContainerInfo>>(containerList);
    }

    public Task RemoveAsync(string containerId, CancellationToken cancellationToken = default)
    {
        if (String.IsNullOrWhiteSpace(containerId))
        {
            throw new ArgumentException("Container ID cannot be null or whitespace", nameof(containerId));
        }

        if (!containers.TryRemove(containerId, out var instance))
        {
            throw new ContainerNotFoundException(containerId);
        }

        if (!instance.Process.HasExited)
        {
            throw new ContainerException($"Cannot remove running container '{containerId}'. Stop it first.");
        }

        instance.JobObject.Dispose();
        instance.Process.Dispose();

        return Task.CompletedTask;
    }

    private static void ValidateConfiguration(ContainerConfiguration configuration)
    {
        if (String.IsNullOrWhiteSpace(configuration.Name))
        {
            throw new ContainerConfigurationException("Container name is required");
        }

        if (String.IsNullOrWhiteSpace(configuration.ExecutablePath))
        {
            throw new ContainerConfigurationException("Executable path is required");
        }

        if (!File.Exists(configuration.ExecutablePath))
        {
            throw new ContainerConfigurationException($"Executable not found: {configuration.ExecutablePath}");
        }

        if (configuration.CpuLimitPercent.HasValue && 
            (configuration.CpuLimitPercent.Value < 1 || configuration.CpuLimitPercent.Value > 100))
        {
            throw new ContainerConfigurationException("CPU limit must be between 1 and 100 percent");
        }

        if (configuration.MemoryLimitMB.HasValue && configuration.MemoryLimitMB.Value < 1)
        {
            throw new ContainerConfigurationException("Memory limit must be at least 1 MB");
        }
    }

    private static ProcessStartInfo CreateProcessStartInfo(ContainerConfiguration configuration)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = configuration.ExecutablePath,
            UseShellExecute = false,
            CreateNoWindow = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            WorkingDirectory = configuration.WorkingDirectory ?? Path.GetDirectoryName(configuration.ExecutablePath) ?? Environment.CurrentDirectory
        };

        if (configuration.Arguments is not null && configuration.Arguments.Length > 0)
        {
            foreach (var arg in configuration.Arguments)
            {
                startInfo.ArgumentList.Add(arg);
            }
        }

        if (configuration.EnvironmentVariables is not null)
        {
            foreach (var kvp in configuration.EnvironmentVariables)
            {
                startInfo.Environment[kvp.Key] = kvp.Value;
            }
        }

        return startInfo;
    }

    private static ContainerInfo CreateContainerInfo(ContainerInstance instance)
    {
        var state = instance.Process.HasExited ? ContainerState.Stopped : ContainerState.Running;

        return new ContainerInfo
        {
            Id = instance.Id,
            Name = instance.Name,
            State = state,
            ProcessId = instance.Process.HasExited ? null : instance.Process.Id,
            ExecutablePath = instance.Configuration.ExecutablePath,
            RootPath = instance.Configuration.RootPath,
            CreatedAt = instance.CreatedAt,
            StartedAt = instance.StartedAt,
            MemoryLimitBytes = instance.Configuration.MemoryLimitMB.HasValue 
                ? instance.Configuration.MemoryLimitMB.Value * 1024 * 1024 
                : null,
            CpuLimitPercent = instance.Configuration.CpuLimitPercent
        };
    }

    private async Task MonitorProcessAsync(string containerId, Process process)
    {
        try
        {
            await process.WaitForExitAsync();
        }
        catch (Exception)
        {
            // Process monitoring failed, continue
        }

        // Process has exited, clean up after a delay
        await Task.Delay(TimeSpan.FromSeconds(5));
        
        if (containers.TryGetValue(containerId, out var instance))
        {
            // Container still exists, user hasn't removed it
            // Keep it in the list for inspection
        }
    }

    public void Dispose()
    {
        if (!disposed)
        {
            foreach (var instance in containers.Values)
            {
                try
                {
                    if (!instance.Process.HasExited)
                    {
                        instance.Process.Kill(entireProcessTree: true);
                    }
                    instance.Process.Dispose();
                    instance.JobObject.Dispose();
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }

            containers.Clear();
            disposed = true;
        }
    }

    private sealed class ContainerInstance
    {
        public required string Id { get; init; }
        public required string Name { get; init; }
        public required Process Process { get; init; }
        public required WindowsJobObject JobObject { get; init; }
        public required ContainerConfiguration Configuration { get; init; }
        public required DateTime CreatedAt { get; init; }
        public required DateTime? StartedAt { get; init; }
    }
}
