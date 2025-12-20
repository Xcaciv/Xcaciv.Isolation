namespace Xcaciv.Isolation.Core.Models;

/// <summary>
/// Configuration for creating or updating a Windows container
/// </summary>
public sealed record ContainerConfiguration
{
    /// <summary>
    /// Name for the container
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Path to the executable to run in the container
    /// </summary>
    public required string ExecutablePath { get; init; }

    /// <summary>
    /// Arguments to pass to the executable
    /// </summary>
    public string[]? Arguments { get; init; }

    /// <summary>
    /// Working directory for the container process
    /// </summary>
    public string? WorkingDirectory { get; init; }

    /// <summary>
    /// Path to the container's root filesystem
    /// </summary>
    public string? RootPath { get; init; }

    /// <summary>
    /// Memory limit in megabytes (null for no limit)
    /// </summary>
    public long? MemoryLimitMB { get; init; }

    /// <summary>
    /// CPU limit as a percentage (1-100)
    /// </summary>
    public int? CpuLimitPercent { get; init; }

    /// <summary>
    /// Environment variables for the container
    /// </summary>
    public Dictionary<string, string>? EnvironmentVariables { get; init; }

    /// <summary>
    /// Network configuration
    /// </summary>
    public NetworkConfiguration? Network { get; init; }

    /// <summary>
    /// Container image to use
    /// </summary>
    public string? ImageName { get; init; }

    /// <summary>
    /// Enable log capture
    /// </summary>
    public bool EnableLogging { get; init; } = true;
}
