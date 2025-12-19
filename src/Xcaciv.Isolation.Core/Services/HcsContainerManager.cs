using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Xcaciv.Isolation.Core.Exceptions;
using Xcaciv.Isolation.Core.Interfaces;
using Xcaciv.Isolation.Core.Models;

namespace Xcaciv.Isolation.Core.Services;

/// <summary>
/// Windows container manager using Host Compute Service (HCS) API
/// </summary>
public sealed class HcsContainerManager : IContainerManager, IDisposable
{
    private readonly ConcurrentDictionary<string, HcsContainerInstance> containers = new();
    private readonly ContainerLogger logger;
    private readonly ContainerNetworking networking;
    private bool disposed;

    public HcsContainerManager(ContainerLogger? logger = null, ContainerNetworking? networking = null)
    {
        this.logger = logger ?? new ContainerLogger();
        this.networking = networking ?? new ContainerNetworking();
    }

    /// <summary>
    /// Gets the container logger
    /// </summary>
    public IContainerLogger Logger => logger;

    /// <summary>
    /// Gets the container networking service
    /// </summary>
    public IContainerNetworking Networking => networking;

    public async Task<ContainerInfo> StartAsync(ContainerConfiguration configuration, CancellationToken cancellationToken = default)
    {
        ValidateConfiguration(configuration);

        var containerId = Guid.NewGuid().ToString("N");
        
        // Create HCS configuration JSON
        var hcsConfig = CreateHcsConfiguration(configuration, containerId);
        var configJson = JsonSerializer.Serialize(hcsConfig);

        // Create operation handle
        var operation = HcsNativeMethods.HcsCreateOperation(IntPtr.Zero, null!);
        if (operation == IntPtr.Zero)
        {
            throw new ContainerException("Failed to create HCS operation");
        }

        try
        {
            // Create the container
            var result = HcsNativeMethods.HcsCreateComputeSystem(
                containerId,
                configJson,
                operation,
                IntPtr.Zero,
                out var computeSystem);

            if (!HcsNativeMethods.Succeeded(result))
            {
                var errorMsg = GetOperationErrorMessage(operation);
                throw new ContainerException($"Failed to create container: {errorMsg}");
            }

            // Wait for creation to complete
            await Task.Delay(500, cancellationToken);

            // Start the container
            var startOperation = HcsNativeMethods.HcsCreateOperation(IntPtr.Zero, null!);
            result = HcsNativeMethods.HcsStartComputeSystem(computeSystem, startOperation, null);

            if (!HcsNativeMethods.Succeeded(result))
            {
                HcsNativeMethods.HcsCloseComputeSystem(computeSystem);
                var errorMsg = GetOperationErrorMessage(startOperation);
                throw new ContainerException($"Failed to start container: {errorMsg}");
            }

            HcsNativeMethods.HcsCloseOperation(startOperation);

            var instance = new HcsContainerInstance
            {
                Id = containerId,
                Name = configuration.Name,
                ComputeSystem = computeSystem,
                Configuration = configuration,
                CreatedAt = DateTime.UtcNow,
                StartedAt = DateTime.UtcNow,
                State = ContainerState.Running
            };

            if (!containers.TryAdd(containerId, instance))
            {
                HcsNativeMethods.HcsTerminateComputeSystem(computeSystem, IntPtr.Zero, null);
                HcsNativeMethods.HcsCloseComputeSystem(computeSystem);
                throw new ContainerException("Failed to register container");
            }

            // Configure networking if specified
            if (configuration.Network is not null)
            {
                try
                {
                    await networking.ConfigureNetworkAsync(containerId, configuration.Network, cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.AddLog(containerId, LogStream.StdErr, $"Network configuration warning: {ex.Message}");
                }
            }

            // Log container start
            if (configuration.EnableLogging)
            {
                logger.AddLog(containerId, LogStream.StdOut, $"Container '{configuration.Name}' started successfully");
            }

            return CreateContainerInfo(instance);
        }
        finally
        {
            HcsNativeMethods.HcsCloseOperation(operation);
        }
    }

    public async Task StopAsync(string containerId, CancellationToken cancellationToken = default)
    {
        if (String.IsNullOrWhiteSpace(containerId))
        {
            throw new ArgumentException("Container ID cannot be null or whitespace", nameof(containerId));
        }

        if (!containers.TryGetValue(containerId, out var instance))
        {
            throw new ContainerNotFoundException(containerId);
        }

        var operation = HcsNativeMethods.HcsCreateOperation(IntPtr.Zero, null!);
        try
        {
            // Try graceful shutdown first
            var result = HcsNativeMethods.HcsShutDownComputeSystem(instance.ComputeSystem, operation, null);
            
            if (HcsNativeMethods.Succeeded(result))
            {
                // Wait for shutdown
                await Task.Delay(2000, cancellationToken);
            }

            // Force terminate if still running
            result = HcsNativeMethods.HcsTerminateComputeSystem(instance.ComputeSystem, operation, null);
            
            if (HcsNativeMethods.Succeeded(result))
            {
                instance.State = ContainerState.Stopped;
            }
        }
        finally
        {
            HcsNativeMethods.HcsCloseOperation(operation);
        }
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

        if (instance.State == ContainerState.Running)
        {
            throw new ContainerException($"Cannot remove running container '{containerId}'. Stop it first.");
        }

        HcsNativeMethods.HcsCloseComputeSystem(instance.ComputeSystem);

        // Clean up logging and networking
        logger.RemoveContainer(containerId);
        networking.RemoveContainer(containerId);

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

    private static object CreateHcsConfiguration(ContainerConfiguration configuration, string containerId)
    {
        // Build HCS configuration according to schema v2.1
        var memorySizeInMB = configuration.MemoryLimitMB ?? 2048L;
        var processorCount = 1;
        var processorLimit = configuration.CpuLimitPercent.HasValue 
            ? configuration.CpuLimitPercent.Value * 100 
            : 10000;

        var config = new
        {
            SchemaVersion = new { Major = 2, Minor = 1 },
            Owner = "Xcaciv.Isolation",
            ShouldTerminateOnLastHandleClosed = true,
            VirtualMachine = new
            {
                StopOnReset = true,
                Chipset = new { },
                ComputeTopology = new
                {
                    Memory = new { SizeInMB = memorySizeInMB },
                    Processor = new { Count = processorCount, Limit = processorLimit }
                },
                Devices = new
                {
                    HvSocket = new
                    {
                        HvSocketConfig = new
                        {
                            DefaultBindSecurityDescriptor = "D:P(A;;FA;;;WD)"
                        }
                    }
                }
            },
            Container = new
            {
                MappedDirectories = new[]
                {
                    new
                    {
                        HostPath = Path.GetDirectoryName(configuration.ExecutablePath) ?? Environment.CurrentDirectory,
                        ContainerPath = "C:\\app",
                        ReadOnly = false
                    }
                },
                LayerFolders = Array.Empty<string>(),
                HvPartition = true
            }
        };

        return config;
    }

    private static ContainerInfo CreateContainerInfo(HcsContainerInstance instance)
    {
        return new ContainerInfo
        {
            Id = instance.Id,
            Name = instance.Name,
            State = instance.State,
            ProcessId = null, // HCS containers don't expose host PIDs directly
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

    private static string GetOperationErrorMessage(IntPtr operation)
    {
        if (operation == IntPtr.Zero)
        {
            return "Unknown error";
        }

        var result = HcsNativeMethods.HcsGetOperationResult(operation, out var resultDoc);
        
        if (HcsNativeMethods.Succeeded(result) && resultDoc != IntPtr.Zero)
        {
            try
            {
                var errorJson = Marshal.PtrToStringUni(resultDoc);
                return errorJson ?? "Unknown error";
            }
            finally
            {
                HcsNativeMethods.HcsFreeMemory(resultDoc);
            }
        }

        return HcsNativeMethods.GetErrorMessage(result);
    }

    public void Dispose()
    {
        if (!disposed)
        {
            foreach (var instance in containers.Values)
            {
                try
                {
                    if (instance.State == ContainerState.Running)
                    {
                        HcsNativeMethods.HcsTerminateComputeSystem(instance.ComputeSystem, IntPtr.Zero, null);
                    }
                    HcsNativeMethods.HcsCloseComputeSystem(instance.ComputeSystem);

                    // Clean up logging and networking
                    logger.RemoveContainer(instance.Id);
                    networking.RemoveContainer(instance.Id);
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

    private sealed class HcsContainerInstance
    {
        public required string Id { get; init; }
        public required string Name { get; init; }
        public required IntPtr ComputeSystem { get; init; }
        public required ContainerConfiguration Configuration { get; init; }
        public required DateTime CreatedAt { get; init; }
        public required DateTime? StartedAt { get; init; }
        public ContainerState State { get; set; }
    }
}
