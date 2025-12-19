using System.Collections.Concurrent;
using System.Text.Json;
using Xcaciv.Isolation.Core.Exceptions;
using Xcaciv.Isolation.Core.Interfaces;
using Xcaciv.Isolation.Core.Models;

namespace Xcaciv.Isolation.Core.Services;

/// <summary>
/// Container image manager for Windows containers
/// </summary>
public sealed class ImageManager : IImageManager
{
    private readonly ConcurrentDictionary<string, ContainerImage> images = new();
    private readonly string imageStorePath;

    public ImageManager(string? imageStorePath = null)
    {
        this.imageStorePath = imageStorePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Xcaciv.Isolation",
            "Images");

        Directory.CreateDirectory(this.imageStorePath);
    }

    public Task<IReadOnlyCollection<ContainerImage>> ListImagesAsync(
        CancellationToken cancellationToken = default)
    {
        var imageList = images.Values.ToList();
        return Task.FromResult<IReadOnlyCollection<ContainerImage>>(imageList);
    }

    public Task<ContainerImage?> GetImageAsync(
        string imageName,
        CancellationToken cancellationToken = default)
    {
        if (String.IsNullOrWhiteSpace(imageName))
        {
            return Task.FromResult<ContainerImage?>(null);
        }

        // Parse name:tag format
        var parts = imageName.Split(':', 2);
        var name = parts[0];
        var tag = parts.Length > 1 ? parts[1] : "latest";
        var key = $"{name}:{tag}";

        images.TryGetValue(key, out var image);
        return Task.FromResult(image);
    }

    public Task PullImageAsync(
        string imageName,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement image pulling from a registry
        // This would involve:
        // 1. Downloading image manifest
        // 2. Downloading each layer
        // 3. Extracting layers to the image store
        // 4. Registering the image

        throw new NotImplementedException(
            "Image pulling from registries is not yet implemented. " +
            "Use ImportImageAsync to import images from local directories.");
    }

    public Task RemoveImageAsync(
        string imageName,
        CancellationToken cancellationToken = default)
    {
        if (String.IsNullOrWhiteSpace(imageName))
        {
            throw new ArgumentException("Image name cannot be null or whitespace", nameof(imageName));
        }

        var parts = imageName.Split(':', 2);
        var name = parts[0];
        var tag = parts.Length > 1 ? parts[1] : "latest";
        var key = $"{name}:{tag}";

        if (!images.TryRemove(key, out var image))
        {
            throw new ContainerException($"Image '{imageName}' not found");
        }

        // Remove image metadata file
        var metadataPath = Path.Combine(imageStorePath, $"{key.Replace(':', '_')}.json");
        if (File.Exists(metadataPath))
        {
            File.Delete(metadataPath);
        }

        return Task.CompletedTask;
    }

    public async Task ImportImageAsync(
        string imageName,
        string tag,
        string basePath,
        string[] layers,
        CancellationToken cancellationToken = default)
    {
        if (String.IsNullOrWhiteSpace(imageName))
        {
            throw new ArgumentException("Image name cannot be null or whitespace", nameof(imageName));
        }

        if (String.IsNullOrWhiteSpace(tag))
        {
            throw new ArgumentException("Tag cannot be null or whitespace", nameof(tag));
        }

        if (String.IsNullOrWhiteSpace(basePath) || !Directory.Exists(basePath))
        {
            throw new ArgumentException("Base path must be a valid directory", nameof(basePath));
        }

        // Validate all layer paths exist
        foreach (var layer in layers)
        {
            if (!Directory.Exists(layer))
            {
                throw new ArgumentException($"Layer path does not exist: {layer}", nameof(layers));
            }
        }

        var imageId = Guid.NewGuid().ToString("N");
        var key = $"{imageName}:{tag}";

        // Calculate total size
        long totalSize = 0;
        try
        {
            totalSize += GetDirectorySize(basePath);
            foreach (var layer in layers)
            {
                totalSize += GetDirectorySize(layer);
            }
        }
        catch
        {
            // Size calculation is optional
        }

        var image = new ContainerImage
        {
            Id = imageId,
            Name = imageName,
            Tag = tag,
            BasePath = basePath,
            Layers = layers,
            SizeBytes = totalSize > 0 ? totalSize : null,
            CreatedAt = DateTime.UtcNow,
            Metadata = new Dictionary<string, string>
            {
                ["ImportedAt"] = DateTime.UtcNow.ToString("o"),
                ["LayerCount"] = layers.Length.ToString()
            }
        };

        images[key] = image;

        // Save image metadata
        await SaveImageMetadataAsync(image, cancellationToken);
    }

    private async Task SaveImageMetadataAsync(ContainerImage image, CancellationToken cancellationToken)
    {
        var key = $"{image.Name}:{image.Tag}";
        var metadataPath = Path.Combine(imageStorePath, $"{key.Replace(':', '_')}.json");

        var json = JsonSerializer.Serialize(image, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(metadataPath, json, cancellationToken);
    }

    private static long GetDirectorySize(string path)
    {
        var directory = new DirectoryInfo(path);
        return directory.GetFiles("*", SearchOption.AllDirectories).Sum(file => file.Length);
    }

    /// <summary>
    /// Loads images from the image store on initialization
    /// </summary>
    public async Task LoadImagesAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(imageStorePath))
        {
            return;
        }

        var metadataFiles = Directory.GetFiles(imageStorePath, "*.json");

        foreach (var file in metadataFiles)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file, cancellationToken);
                var image = JsonSerializer.Deserialize<ContainerImage>(json);

                if (image is not null)
                {
                    var key = $"{image.Name}:{image.Tag}";
                    images[key] = image;
                }
            }
            catch
            {
                // Skip corrupted metadata files
            }
        }
    }
}
