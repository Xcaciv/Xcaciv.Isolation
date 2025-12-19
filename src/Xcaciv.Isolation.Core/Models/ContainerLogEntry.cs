namespace Xcaciv.Isolation.Core.Models;

/// <summary>
/// Container log entry
/// </summary>
public sealed record ContainerLogEntry
{
    /// <summary>
    /// Timestamp of the log entry
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Log stream (stdout or stderr)
    /// </summary>
    public required LogStream Stream { get; init; }

    /// <summary>
    /// Log message content
    /// </summary>
    public required string Message { get; init; }
}

/// <summary>
/// Log stream type
/// </summary>
public enum LogStream
{
    /// <summary>
    /// Standard output
    /// </summary>
    StdOut,

    /// <summary>
    /// Standard error
    /// </summary>
    StdErr
}
