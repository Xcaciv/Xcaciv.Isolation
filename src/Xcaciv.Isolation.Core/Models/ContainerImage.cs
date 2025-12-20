namespace Xcaciv.Isolation.Core.Models;

/// <summary>
/// Container image information
/// </summary>
public sealed record ContainerImage
{
    /// <summary>
    /// Image ID
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Image name and tag
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Image tag
    /// </summary>
    public required string Tag { get; init; }

    /// <summary>
    /// Base image path
    /// </summary>
    public required string BasePath { get; init; }

    /// <summary>
    /// Layer paths
    /// </summary>
    public required string[] Layers { get; init; }

    /// <summary>
    /// Image size in bytes
    /// </summary>
    public long? SizeBytes { get; init; }

    /// <summary>
    /// Creation time
    /// </summary>
    public DateTime? CreatedAt { get; init; }

    /// <summary>
    /// Image metadata
    /// </summary>
    public Dictionary<string, string>? Metadata { get; init; }
}
