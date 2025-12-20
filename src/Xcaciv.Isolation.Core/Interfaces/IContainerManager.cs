using Xcaciv.Isolation.Core.Models;

namespace Xcaciv.Isolation.Core.Interfaces;

/// <summary>
/// Interface for managing Windows containers
/// </summary>
public interface IContainerManager
{
    /// <summary>
    /// Starts a new container with the specified configuration
    /// </summary>
    /// <param name="configuration">Container configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Information about the started container</returns>
    Task<ContainerInfo> StartAsync(ContainerConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops a running container
    /// </summary>
    /// <param name="containerId">Container identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task StopAsync(string containerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets information about a specific container
    /// </summary>
    /// <param name="containerId">Container identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Container information if found, null otherwise</returns>
    Task<ContainerInfo?> GetContainerAsync(string containerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all containers
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of container information</returns>
    Task<IReadOnlyCollection<ContainerInfo>> ListContainersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a stopped container
    /// </summary>
    /// <param name="containerId">Container identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RemoveAsync(string containerId, CancellationToken cancellationToken = default);
}
