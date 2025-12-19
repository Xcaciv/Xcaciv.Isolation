using Xcaciv.Isolation.Core.Models;

namespace Xcaciv.Isolation.Core.Interfaces;

/// <summary>
/// Interface for container logging operations
/// </summary>
public interface IContainerLogger
{
    /// <summary>
    /// Gets logs from a container
    /// </summary>
    /// <param name="containerId">Container identifier</param>
    /// <param name="follow">Follow log output (streaming)</param>
    /// <param name="tail">Number of lines to return from the end (null for all)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of log entries</returns>
    IAsyncEnumerable<ContainerLogEntry> GetLogsAsync(
        string containerId,
        bool follow = false,
        int? tail = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears logs for a container
    /// </summary>
    /// <param name="containerId">Container identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ClearLogsAsync(string containerId, CancellationToken cancellationToken = default);
}
