namespace Xcaciv.Isolation.Core.Models;

/// <summary>
/// Network configuration for a container
/// </summary>
public sealed record NetworkConfiguration
{
    /// <summary>
    /// Network mode
    /// </summary>
    public required NetworkMode Mode { get; init; }

    /// <summary>
    /// Static IP address (optional)
    /// </summary>
    public string? IPAddress { get; init; }

    /// <summary>
    /// Subnet mask
    /// </summary>
    public string? SubnetMask { get; init; }

    /// <summary>
    /// Gateway address
    /// </summary>
    public string? Gateway { get; init; }

    /// <summary>
    /// DNS servers
    /// </summary>
    public string[]? DnsServers { get; init; }

    /// <summary>
    /// Port mappings (host:container)
    /// </summary>
    public Dictionary<int, int>? PortMappings { get; init; }
}

/// <summary>
/// Network mode for containers
/// </summary>
public enum NetworkMode
{
    /// <summary>
    /// No network (isolated)
    /// </summary>
    None,

    /// <summary>
    /// NAT network (default)
    /// </summary>
    NAT,

    /// <summary>
    /// Transparent network
    /// </summary>
    Transparent,

    /// <summary>
    /// L2Bridge network
    /// </summary>
    L2Bridge,

    /// <summary>
    /// L2Tunnel network
    /// </summary>
    L2Tunnel
}
