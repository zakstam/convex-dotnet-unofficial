using System.Text.Json.Serialization;

namespace TicTacToe.Shared.Models;

/// <summary>
/// Represents a Tic-Tac-Toe game state.
/// Maps directly from Convex backend response.
/// </summary>
public class Game
{
    /// <summary>
    /// Unique game ID (assigned by Convex).
    /// </summary>
    [JsonPropertyName("_id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Creation time timestamp (assigned by Convex).
    /// Stored as double to handle Convex's floating-point timestamp format.
    /// </summary>
    [JsonPropertyName("_creationTime")]
    public double CreationTime { get; set; }

    /// <summary>
    /// Username of player X.
    /// </summary>
    [JsonPropertyName("playerX")]
    public string PlayerX { get; set; } = string.Empty;

    /// <summary>
    /// Username of player O (null if waiting for opponent).
    /// </summary>
    [JsonPropertyName("playerO")]
    public string? PlayerO { get; set; }

    /// <summary>
    /// Game board as 9-element array (positions 0-8).
    /// Empty strings represent empty cells, "X" or "O" represent marks.
    /// </summary>
    [JsonPropertyName("board")]
    public List<string> Board { get; set; } = new();

    /// <summary>
    /// Current player's turn ("X" or "O").
    /// </summary>
    [JsonPropertyName("currentTurn")]
    public string CurrentTurn { get; set; } = "X";

    /// <summary>
    /// Game status: "waiting", "playing", or "finished".
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = "waiting";

    /// <summary>
    /// Winner: "X", "O", "draw", or null if game not finished.
    /// </summary>
    [JsonPropertyName("winner")]
    public string? Winner { get; set; }

    /// <summary>
    /// Timestamp when game was created (milliseconds since epoch).
    /// Stored as double to handle Convex's floating-point timestamp format.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public double CreatedAt { get; set; }

    /// <summary>
    /// Timestamp when game finished (milliseconds since epoch), null if not finished.
    /// Stored as double? to handle Convex's floating-point timestamp format.
    /// </summary>
    [JsonPropertyName("finishedAt")]
    public double? FinishedAt { get; set; }
}

