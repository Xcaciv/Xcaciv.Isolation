namespace Xcaciv.Isolation.Core.Exceptions;

/// <summary>
/// Base exception for container operations
/// </summary>
public class ContainerException : Exception
{
    public ContainerException()
    {
    }

    public ContainerException(string message) : base(message)
    {
    }

    public ContainerException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
