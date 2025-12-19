using Xcaciv.Isolation.Core.Models;

namespace Xcaciv.Isolation.Core.Interfaces;

/// <summary>
/// Interface for container networking operations
/// </summary>
public interface IContainerNetworking
{
    /// <summary>
    /// Configures network for a container
    /// </summary>
    /// <param name="containerId">Container identifier</param>
    /// <param name="configuration">Network configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ConfigureNetworkAsync(
        string containerId,
        NetworkConfiguration configuration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets network information for a container
    /// </summary>
    /// <param name="containerId">Container identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Network configuration</returns>
    Task<NetworkConfiguration?> GetNetworkInfoAsync(
        string containerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnects container from network
    /// </summary>
    /// <param name="containerId">Container identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DisconnectNetworkAsync(
        string containerId,
        CancellationToken cancellationToken = default);
}
