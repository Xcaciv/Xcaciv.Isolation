namespace Xcaciv.Isolation.Core.Models;

/// <summary>
/// Represents the state of a Windows container
/// </summary>
public enum ContainerState
{
    /// <summary>
    /// Container is not running
    /// </summary>
    Stopped,

    /// <summary>
    /// Container is currently running
    /// </summary>
    Running,

    /// <summary>
    /// Container is paused
    /// </summary>
    Paused,

    /// <summary>
    /// Container state is unknown
    /// </summary>
    Unknown
}
