namespace Xcaciv.Isolation.Core.Exceptions;

/// <summary>
/// Exception thrown when a container is not found
/// </summary>
public class ContainerNotFoundException : ContainerException
{
    public ContainerNotFoundException()
    {
    }

    public ContainerNotFoundException(string containerId) 
        : base($"Container '{containerId}' not found")
    {
        ContainerId = containerId;
    }

    public ContainerNotFoundException(string containerId, Exception innerException) 
        : base($"Container '{containerId}' not found", innerException)
    {
        ContainerId = containerId;
    }

    public string? ContainerId { get; }
}
