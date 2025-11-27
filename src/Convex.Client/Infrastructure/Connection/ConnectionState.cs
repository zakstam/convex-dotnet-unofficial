namespace Convex.Client.Infrastructure.Connection;

/// <summary>
/// Represents the state of the WebSocket connection used for real-time subscriptions.
/// The connection state changes automatically as the client connects, disconnects, and reconnects.
/// </summary>
/// <remarks>
/// <para>
/// Connection states transition as follows:
/// <list type="bullet">
/// <item><see cref="Disconnected"/> → <see cref="Connecting"/> → <see cref="Connected"/></item>
/// <item><see cref="Connected"/> → <see cref="Reconnecting"/> (on network issues) → <see cref="Connected"/> or <see cref="Failed"/></item>
/// <item><see cref="Failed"/> → Manual reconnection required or automatic retry (if configured)</item>
/// </list>
/// </para>
/// <para>
/// Subscribe to <see cref="Convex.Client.IConvexClient.ConnectionStateChanges"/> to be notified when the connection state changes.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Monitor connection state
/// client.ConnectionStateChanges.Subscribe(state => {
///     Console.WriteLine($"Connection state: {state}");
///     switch (state)
///     {
///         case ConnectionState.Connected:
///             Console.WriteLine("Connected and ready!");
///             break;
///         case ConnectionState.Reconnecting:
///             Console.WriteLine("Reconnecting...");
///             break;
///         case ConnectionState.Failed:
///             Console.WriteLine("Connection failed");
///             break;
///     }
/// });
/// </code>
/// </example>
/// <seealso cref="Convex.Client.IConvexClient.ConnectionState"/>
/// <seealso cref="Convex.Client.IConvexClient.ConnectionStateChanges"/>
public enum ConnectionState
{
    /// <summary>
    /// Not connected to the server.
    /// </summary>
    Disconnected = 0,

    /// <summary>
    /// Currently connecting to the server.
    /// </summary>
    Connecting = 1,

    /// <summary>
    /// Connected and ready to receive updates.
    /// </summary>
    Connected = 2,

    /// <summary>
    /// Connection was lost and attempting to reconnect.
    /// </summary>
    Reconnecting = 3,

    /// <summary>
    /// Connection failed and not attempting to reconnect.
    /// </summary>
    Failed = 4
}
