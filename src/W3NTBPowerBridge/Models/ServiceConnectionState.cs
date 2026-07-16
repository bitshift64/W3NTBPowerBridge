namespace W3NTBPowerBridge.Models;

/// <summary>
/// Describes the connection state of a background TCP service.
/// </summary>
public enum ServiceConnectionState
{
    /// <summary>
    /// The service is not connected.
    /// </summary>
    Disconnected,

    /// <summary>
    /// The service is attempting to connect.
    /// </summary>
    Connecting,

    /// <summary>
    /// The service is connected.
    /// </summary>
    Connected,

    /// <summary>
    /// The service is waiting before retrying.
    /// </summary>
    Waiting
}
