namespace Xcaciv.Isolation.Core.Exceptions;

/// <summary>
/// Exception thrown when container configuration is invalid
/// </summary>
public class ContainerConfigurationException : ContainerException
{
    public ContainerConfigurationException()
    {
    }

    public ContainerConfigurationException(string message) : base(message)
    {
    }

    public ContainerConfigurationException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}
