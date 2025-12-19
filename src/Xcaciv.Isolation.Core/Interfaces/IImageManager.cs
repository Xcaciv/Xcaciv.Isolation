using Xcaciv.Isolation.Core.Models;

namespace Xcaciv.Isolation.Core.Interfaces;

/// <summary>
/// Interface for container image management
/// </summary>
public interface IImageManager
{
    /// <summary>
    /// Lists all available container images
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of container images</returns>
    Task<IReadOnlyCollection<ContainerImage>> ListImagesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets information about a specific image
    /// </summary>
    /// <param name="imageName">Image name with optional tag</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Container image information</returns>
    Task<ContainerImage?> GetImageAsync(
        string imageName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Pulls an image from a registry (not implemented - placeholder)
    /// </summary>
    /// <param name="imageName">Image name with tag</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task PullImageAsync(
        string imageName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an image
    /// </summary>
    /// <param name="imageName">Image name with tag</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RemoveImageAsync(
        string imageName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports an image from a directory containing layers
    /// </summary>
    /// <param name="imageName">Name to give the imported image</param>
    /// <param name="tag">Image tag</param>
    /// <param name="basePath">Path to the base image directory</param>
    /// <param name="layers">Array of layer directory paths</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ImportImageAsync(
        string imageName,
        string tag,
        string basePath,
        string[] layers,
        CancellationToken cancellationToken = default);
}
