using System.Collections.Concurrent;
using Xcaciv.Isolation.Core.Interfaces;
using Xcaciv.Isolation.Core.Models;

namespace Xcaciv.Isolation.Core.Services;

/// <summary>
/// Container networking service using Windows HNS (Host Networking Service)
/// </summary>
public sealed class ContainerNetworking : IContainerNetworking
{
    private readonly ConcurrentDictionary<string, NetworkConfiguration> containerNetworks = new();

    public Task ConfigureNetworkAsync(
        string containerId,
        NetworkConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        if (String.IsNullOrWhiteSpace(containerId))
        {
            throw new ArgumentException("Container ID cannot be null or whitespace", nameof(containerId));
        }

        if (configuration is null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        // Store network configuration
        // In a full implementation, this would call HNS APIs to configure the actual network
        containerNetworks[containerId] = configuration;

        // TODO: Integrate with Windows HNS API to:
        // 1. Create network namespace
        // 2. Create virtual network adapter
        // 3. Configure IP address and routing
        // 4. Set up NAT/port forwarding if needed

        return Task.CompletedTask;
    }

    public Task<NetworkConfiguration?> GetNetworkInfoAsync(
        string containerId,
        CancellationToken cancellationToken = default)
    {
        if (String.IsNullOrWhiteSpace(containerId))
        {
            return Task.FromResult<NetworkConfiguration?>(null);
        }

        containerNetworks.TryGetValue(containerId, out var config);
        return Task.FromResult(config);
    }

    public Task DisconnectNetworkAsync(
        string containerId,
        CancellationToken cancellationToken = default)
    {
        if (String.IsNullOrWhiteSpace(containerId))
        {
            throw new ArgumentException("Container ID cannot be null or whitespace", nameof(containerId));
        }

        containerNetworks.TryRemove(containerId, out _);

        // TODO: Integrate with Windows HNS API to:
        // 1. Remove network namespace
        // 2. Delete virtual network adapter
        // 3. Clean up routing rules

        return Task.CompletedTask;
    }

    /// <summary>
    /// Builds HCS network configuration JSON
    /// </summary>
    /// <param name="configuration">Network configuration</param>
    /// <returns>Network configuration object for HCS</returns>
    public object BuildHcsNetworkConfig(NetworkConfiguration configuration)
    {
        if (configuration.Mode == NetworkMode.None)
        {
            return new { };
        }

        var networkConfig = new
        {
            EndpointList = new[]
            {
                new
                {
                    Namespace = Guid.NewGuid().ToString(),
                    PortMappings = configuration.PortMappings?.Select(pm => new
                    {
                        Protocol = "tcp",
                        InternalPort = pm.Value,
                        ExternalPort = pm.Key
                    }).ToArray() ?? Array.Empty<object>(),
                    DnsConfig = configuration.DnsServers is not null ? new
                    {
                        NameServers = configuration.DnsServers
                    } : null,
                    IpAddress = configuration.IPAddress,
                    Gateway = configuration.Gateway,
                    SubnetPrefix = configuration.SubnetMask
                }
            }
        };

        return networkConfig;
    }

    /// <summary>
    /// Removes network configuration for a container
    /// </summary>
    /// <param name="containerId">Container identifier</param>
    public void RemoveContainer(string containerId)
    {
        containerNetworks.TryRemove(containerId, out _);
    }
}
