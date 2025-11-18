namespace Convex.Client.Shared.Connection;

/// <summary>
/// Represents the state of the WebSocket connection.
/// </summary>
public enum ConnectionState
{
    /// <summary>
    /// Not connected to the server.
    /// </summary>
    Disconnected,

    /// <summary>
    /// Currently connecting to the server.
    /// </summary>
    Connecting,

    /// <summary>
    /// Connected and ready to receive updates.
    /// </summary>
    Connected,

    /// <summary>
    /// Connection was lost and attempting to reconnect.
    /// </summary>
    Reconnecting,

    /// <summary>
    /// Connection failed and not attempting to reconnect.
    /// </summary>
    Failed
}
