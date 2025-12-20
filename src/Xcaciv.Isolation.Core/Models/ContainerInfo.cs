namespace Xcaciv.Isolation.Core.Models;

/// <summary>
/// Information about a Windows container
/// </summary>
public sealed record ContainerInfo
{
    /// <summary>
    /// Unique identifier for the container
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Name of the container
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Current state of the container
    /// </summary>
    public required ContainerState State { get; init; }

    /// <summary>
    /// Process ID of the container's main process
    /// </summary>
    public int? ProcessId { get; init; }

    /// <summary>
    /// Path to the container's root filesystem
    /// </summary>
    public string? RootPath { get; init; }

    /// <summary>
    /// Path to the executable running in the container
    /// </summary>
    public string? ExecutablePath { get; init; }

    /// <summary>
    /// Time when the container was created
    /// </summary>
    public DateTime? CreatedAt { get; init; }

    /// <summary>
    /// Time when the container was started
    /// </summary>
    public DateTime? StartedAt { get; init; }

    /// <summary>
    /// Memory limit in bytes
    /// </summary>
    public long? MemoryLimitBytes { get; init; }

    /// <summary>
    /// CPU limit as a percentage
    /// </summary>
    public int? CpuLimitPercent { get; init; }
}
