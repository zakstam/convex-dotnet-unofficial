using System.Text.Json;
using System.Text.Json.Serialization;

namespace Convex.Client.Shared.Http;

/// <summary>
/// WebSocket protocol implementation for Convex real-time communication.
/// This implementation must exactly match the TypeScript client's binary protocol.
/// </summary>
public static class ConvexWebSocketProtocol
{
    /// <summary>
    /// Encodes a client message for transmission over WebSocket.
    /// </summary>
    /// <param name="message">The client message to encode.</param>
    /// <returns>The encoded message bytes.</returns>
    public static ReadOnlyMemory<byte> EncodeClientMessage(ClientMessage message)
    {
        // Use JsonDocument to build JSON directly, avoiding anonymous types that can't be serialized when trimmed
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        writer.WriteStartObject();

        switch (message)
        {
            case ConnectMessage connect:
                writer.WriteString("type", "Connect");
                writer.WriteString("sessionId", connect.SessionId);
                writer.WriteNumber("connectionCount", connect.ConnectionCount);
                if (connect.LastCloseReason != null)
                {
                    writer.WriteString("lastCloseReason", connect.LastCloseReason);
                }

                writer.WriteNumber("baseVersion", connect.BaseVersion);
                if (connect.MaxObservedTimestamp.HasValue)
                {
                    writer.WriteString("maxObservedTimestamp", connect.MaxObservedTimestamp.Value.ToBase64String());
                }

                writer.WriteNumber("clientTs", connect.ClientTs);
                break;

            case AuthenticateMessage auth:
                writer.WriteString("type", "Authenticate");
                writer.WriteString("tokenType", auth.TokenType);
                writer.WriteString("value", auth.Value);
                writer.WriteNumber("baseVersion", auth.BaseVersion);
                break;

            case ModifyQuerySetMessage querySet:
                writer.WriteString("type", "ModifyQuerySet");
                writer.WriteNumber("baseVersion", querySet.BaseVersion);
                writer.WriteNumber("newVersion", querySet.NewVersion);
                writer.WriteStartArray("modifications");
                foreach (var mod in querySet.Modifications)
                {
                    writer.WriteStartObject();
                    writer.WriteString("type", mod.Type);
                    writer.WriteNumber("queryId", mod.QueryId);
                    if (mod.Type == "Add")
                    {
                        if (mod.UdfPath != null)
                        {
                            writer.WriteString("udfPath", mod.UdfPath);
                        }

                        if (mod.Args != null)
                        {
                            writer.WriteStartArray("args");
                            foreach (var arg in mod.Args)
                            {
                                // Serialize each arg individually to avoid anonymous type issues
                                JsonSerializer.Serialize(writer, arg);
                            }
                            writer.WriteEndArray();
                        }
                        if (mod.ComponentPath != null)
                        {
                            writer.WriteString("componentPath", mod.ComponentPath);
                        }
                    }
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                break;

            case MutationMessage mutation:
                writer.WriteString("type", "Mutation");
                writer.WriteString("requestId", mutation.RequestId);
                writer.WriteString("udfPath", mutation.UdfPath);
                writer.WriteStartArray("args");
                foreach (var arg in mutation.Args ?? [])
                {
                    JsonSerializer.Serialize(writer, arg);
                }
                writer.WriteEndArray();
                if (mutation.ComponentPath != null)
                {
                    writer.WriteString("componentPath", mutation.ComponentPath);
                }

                break;

            case ActionMessage action:
                writer.WriteString("type", "Action");
                writer.WriteString("requestId", action.RequestId);
                writer.WriteString("udfPath", action.UdfPath);
                writer.WriteStartArray("args");
                foreach (var arg in action.Args ?? [])
                {
                    JsonSerializer.Serialize(writer, arg);
                }
                writer.WriteEndArray();
                if (action.ComponentPath != null)
                {
                    writer.WriteString("componentPath", action.ComponentPath);
                }

                break;

            default:
                throw new NotSupportedException($"Message type {message.Type} not supported");
        }

        writer.WriteEndObject();
        writer.Flush();

        return stream.ToArray();
    }

    /// <summary>
    /// Decodes a server message received over WebSocket.
    /// </summary>
    /// <param name="messageBytes">The raw message bytes.</param>
    /// <returns>The decoded server message.</returns>
    public static ServerMessage DecodeServerMessage(ReadOnlySpan<byte> messageBytes)
    {
        try
        {
            var jsonDoc = JsonDocument.Parse(messageBytes.ToArray());

            if (!jsonDoc.RootElement.TryGetProperty("type", out var typeElement))
            {
                return new PingMessage(); // Default to ping if no type
            }

            var messageType = typeElement.GetString();

            return messageType switch
            {
                "FatalError" => new FatalErrorMessage
                {
                    ErrorMessage = jsonDoc.RootElement.TryGetProperty("error", out var errorProp)
                        ? errorProp.GetString()!
                        : jsonDoc.RootElement.TryGetProperty("errorMessage", out var errorMsgProp)
                            ? errorMsgProp.GetString()!
                            : "Unknown fatal error",
                    ErrorCode = jsonDoc.RootElement.TryGetProperty("errorCode", out var errorCode)
                        ? errorCode.GetString() : null
                },
                "AuthError" => new AuthErrorMessage
                {
                    ErrorMessage = jsonDoc.RootElement.GetProperty("errorMessage").GetString()!,
                    ErrorCode = jsonDoc.RootElement.TryGetProperty("errorCode", out var authErrorCode)
                        ? authErrorCode.GetString() : null
                },
                "Ping" => new PingMessage(),
                "Connected" => new ConnectedMessage(),
                "Transition" => new TransitionMessage
                {
                    EndVersion = jsonDoc.RootElement.GetProperty("endVersion"),
                    Modifications = [.. jsonDoc.RootElement.GetProperty("modifications").EnumerateArray()]
                },
                "MutationResponse" => new MutationResponseMessage
                {
                    RequestId = jsonDoc.RootElement.GetProperty("requestId").GetString()!,
                    Success = jsonDoc.RootElement.GetProperty("success").GetBoolean(),
                    Result = jsonDoc.RootElement.TryGetProperty("result", out var result) ? result : null,
                    ErrorMessage = jsonDoc.RootElement.TryGetProperty("errorMessage", out var error)
                        ? error.GetString() : null
                },
                "ActionResponse" => new ActionResponseMessage
                {
                    RequestId = jsonDoc.RootElement.GetProperty("requestId").GetString()!,
                    Success = jsonDoc.RootElement.GetProperty("success").GetBoolean(),
                    Result = jsonDoc.RootElement.TryGetProperty("result", out var actionResult) ? actionResult : null,
                    ErrorMessage = jsonDoc.RootElement.TryGetProperty("errorMessage", out var actionError)
                        ? actionError.GetString() : null
                },
                _ => throw new NotSupportedException($"Server message type {messageType} not supported")
            };
        }
        catch (Exception)
        {
            // Return a safe default message to prevent connection from failing
            return new PingMessage();
        }
    }

    /// <summary>
    /// Converts a timestamp to base64-encoded little-endian u64 format (matching TypeScript).
    /// </summary>
    /// <param name="timestamp">The timestamp to encode.</param>
    /// <returns>The base64-encoded timestamp.</returns>
    public static string TimestampToBase64(long timestamp)
    {
        var bytes = new byte[8];
        for (var i = 0; i < 8; i++)
        {
            bytes[i] = (byte)(timestamp & 0xFF);
            timestamp >>= 8;
        }
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Converts a base64-encoded timestamp back to long format.
    /// </summary>
    /// <param name="base64Timestamp">The base64-encoded timestamp.</param>
    /// <returns>The decoded timestamp.</returns>
    public static long Base64ToTimestamp(string base64Timestamp)
    {
        var bytes = Convert.FromBase64String(base64Timestamp);
        long timestamp = 0;
        for (var i = 7; i >= 0; i--)
        {
            timestamp = (timestamp << 8) | bytes[i];
        }
        return timestamp;
    }
}

/// <summary>
/// Base class for all client messages.
/// </summary>
public abstract record ClientMessage
{
    /// <summary>
    /// Gets the message type.
    /// </summary>
    public abstract string Type { get; }
}

/// <summary>
/// Client message to establish WebSocket connection.
/// </summary>
public record ConnectMessage : ClientMessage
{
    public override string Type => "Connect";

    /// <summary>
    /// Gets the session identifier.
    /// </summary>
    public string SessionId { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets the connection count (number of times this client has connected).
    /// </summary>
    public int ConnectionCount { get; init; } = 1;

    /// <summary>
    /// Gets the reason for the last connection close, if any.
    /// </summary>
    public string? LastCloseReason { get; init; } = null;

    /// <summary>
    /// Gets the maximum observed timestamp for synchronization.
    /// </summary>
    public ConvexTimestamp? MaxObservedTimestamp { get; init; }

    /// <summary>
    /// Gets the client timestamp when sending this message.
    /// </summary>
    public long ClientTs { get; init; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    /// <summary>
    /// Gets the base version for query synchronization (not identity version).
    /// </summary>
    public int BaseVersion { get; init; } = 0;
}

/// <summary>
/// Client message to authenticate with the server.
/// Matches convex-js protocol: { type: "Authenticate", tokenType: "User", value: string, baseVersion: number }
/// </summary>
public record AuthenticateMessage : ClientMessage
{
    public override string Type => "Authenticate";

    /// <summary>
    /// The type of authentication token ("User", "Admin", or "None").
    /// </summary>
    public string TokenType { get; init; } = "User";

    /// <summary>
    /// The JWT authentication token value.
    /// </summary>
    public required string Value { get; init; }

    /// <summary>
    /// The base identity version before this authentication change.
    /// </summary>
    public int BaseVersion { get; init; } = 0;
}

/// <summary>
/// Client message to modify the query subscription set.
/// </summary>
public record ModifyQuerySetMessage : ClientMessage
{
    public override string Type => "ModifyQuerySet";

    /// <summary>
    /// Gets the base version for the query set.
    /// </summary>
    public int BaseVersion { get; init; } = 0;

    /// <summary>
    /// Gets the new version for the query set.
    /// </summary>
    public int NewVersion { get; init; } = 1;

    /// <summary>
    /// Gets the query set modifications.
    /// </summary>
    public QuerySetModification[] Modifications { get; init; } = [];
}

/// <summary>
/// Client message to execute a mutation.
/// </summary>
public record MutationMessage : ClientMessage
{
    public override string Type => "Mutation";

    /// <summary>
    /// Gets the request identifier.
    /// </summary>
    public required string RequestId { get; init; }

    /// <summary>
    /// Gets the UDF path.
    /// </summary>
    public required string UdfPath { get; init; }

    /// <summary>
    /// Gets the function arguments.
    /// </summary>
    public object?[] Args { get; init; } = [];

    /// <summary>
    /// Gets the component path for component isolation.
    /// Only admin auth is allowed to run mutations on non-root components.
    /// </summary>
    public string? ComponentPath { get; init; }
}

/// <summary>
/// Client message to execute an action.
/// </summary>
public record ActionMessage : ClientMessage
{
    public override string Type => "Action";

    /// <summary>
    /// Gets the request identifier.
    /// </summary>
    public required string RequestId { get; init; }

    /// <summary>
    /// Gets the UDF path.
    /// </summary>
    public required string UdfPath { get; init; }

    /// <summary>
    /// Gets the function arguments.
    /// </summary>
    public object?[] Args { get; init; } = [];

    /// <summary>
    /// Gets the component path for component isolation.
    /// Only admin auth is allowed to run actions on non-root components.
    /// </summary>
    public string? ComponentPath { get; init; }
}

/// <summary>
/// Base class for all server messages.
/// </summary>
public abstract record ServerMessage
{
    /// <summary>
    /// Gets the message type.
    /// </summary>
    public abstract string Type { get; }
}

/// <summary>
/// Server message indicating a fatal error.
/// </summary>
public record FatalErrorMessage : ServerMessage
{
    public override string Type => "FatalError";

    /// <summary>
    /// Gets the error message.
    /// </summary>
    public required string ErrorMessage { get; init; }

    /// <summary>
    /// Gets the error code.
    /// </summary>
    public string? ErrorCode { get; init; }
}

/// <summary>
/// Server message indicating an authentication error.
/// </summary>
public record AuthErrorMessage : ServerMessage
{
    public override string Type => "AuthError";

    /// <summary>
    /// Gets the error message.
    /// </summary>
    public required string ErrorMessage { get; init; }

    /// <summary>
    /// Gets the error code.
    /// </summary>
    public string? ErrorCode { get; init; }
}

/// <summary>
/// Server ping message for connection keepalive.
/// </summary>
public record PingMessage : ServerMessage
{
    public override string Type => "Ping";
}

/// <summary>
/// Server message containing query result transitions.
/// </summary>
public record TransitionMessage : ServerMessage
{
    public override string Type => "Transition";

    /// <summary>
    /// Gets the end version for this transition.
    /// </summary>
    public JsonElement EndVersion { get; init; }

    /// <summary>
    /// Gets the modifications in this transition.
    /// </summary>
    public JsonElement[] Modifications { get; init; } = [];
}

/// <summary>
/// Server response to a mutation request.
/// </summary>
public record MutationResponseMessage : ServerMessage
{
    public override string Type => "MutationResponse";

    /// <summary>
    /// Gets the request identifier.
    /// </summary>
    public required string RequestId { get; init; }

    /// <summary>
    /// Gets whether the mutation was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the mutation result (if successful).
    /// </summary>
    public JsonElement? Result { get; init; }

    /// <summary>
    /// Gets the error message (if failed).
    /// </summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Server response to an action request.
/// </summary>
public record ActionResponseMessage : ServerMessage
{
    public override string Type => "ActionResponse";

    /// <summary>
    /// Gets the request identifier.
    /// </summary>
    public required string RequestId { get; init; }

    /// <summary>
    /// Gets whether the action was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the action result (if successful).
    /// </summary>
    public JsonElement? Result { get; init; }

    /// <summary>
    /// Gets the error message (if failed).
    /// </summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Query set modification operation.
/// </summary>
public record QuerySetModification
{
    /// <summary>
    /// Gets the modification type ("Add" or "Remove").
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <summary>
    /// Gets the query identifier.
    /// </summary>
    [JsonPropertyName("queryId")]
    public required int QueryId { get; init; }

    /// <summary>
    /// Gets the UDF path (for Add operations).
    /// </summary>
    [JsonPropertyName("udfPath")]
    public string? UdfPath { get; init; }

    /// <summary>
    /// Gets the function arguments (for Add operations).
    /// </summary>
    [JsonPropertyName("args")]
    public object?[]? Args { get; init; }

    /// <summary>
    /// Gets the component path for component isolation (for Add operations).
    /// </summary>
    [JsonPropertyName("componentPath")]
    public string? ComponentPath { get; init; }
}

/// <summary>
/// Represents a Convex timestamp with base64 encoding support.
/// </summary>
/// <remarks>
/// Initializes a new instance of the ConvexTimestamp struct.
/// </remarks>
/// <param name="value">The timestamp value.</param>
public readonly struct ConvexTimestamp(long value) : IEquatable<ConvexTimestamp>
{
    /// <summary>
    /// Gets the timestamp value.
    /// </summary>
    public long Value { get; } = value;

    /// <summary>
    /// Converts the timestamp to base64-encoded format.
    /// </summary>
    /// <returns>The base64-encoded timestamp.</returns>
    public string ToBase64String() => ConvexWebSocketProtocol.TimestampToBase64(Value);

    /// <summary>
    /// Creates a timestamp from a base64-encoded string.
    /// </summary>
    /// <param name="base64String">The base64-encoded timestamp.</param>
    /// <returns>The decoded timestamp.</returns>
    public static ConvexTimestamp FromBase64String(string base64String) => new(ConvexWebSocketProtocol.Base64ToTimestamp(base64String));

    /// <summary>
    /// Creates a timestamp from the current time.
    /// </summary>
    /// <returns>A timestamp representing the current time.</returns>
    public static ConvexTimestamp Now() => new(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

    public bool Equals(ConvexTimestamp other) => Value == other.Value;

    public override bool Equals(object? obj) => obj is ConvexTimestamp other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public static bool operator ==(ConvexTimestamp left, ConvexTimestamp right) => left.Equals(right);

    public static bool operator !=(ConvexTimestamp left, ConvexTimestamp right) => !left.Equals(right);

    public override string ToString() => $"ConvexTimestamp({Value})";
}

/// <summary>
/// Server message acknowledging successful connection.
/// </summary>
public record ConnectedMessage : ServerMessage
{
    public override string Type => "Connected";
}
