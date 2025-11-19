using System.Reactive.Linq;
using Convex.Client;
using Convex.Client.Shared.ErrorHandling;
using TicTacToe.Shared.Models;

namespace TicTacToe.Shared.Services;

/// <summary>
/// Business logic service for Tic-Tac-Toe game operations.
/// Handles all Convex client interactions, game management, and subscriptions.
/// </summary>
public class TicTacToeService : IDisposable
{
    #region Events

    /// <summary>
    /// Raised when game state is updated (from subscription).
    /// </summary>
    public event EventHandler<Game>? GameUpdated;

    /// <summary>
    /// Raised when games list is updated (from subscription).
    /// </summary>
    public event EventHandler<List<Game>>? GamesListUpdated;

    /// <summary>
    /// Raised when an error occurs.
    /// </summary>
    public event EventHandler<string>? ErrorOccurred;

    #endregion

    #region State

    private readonly IConvexClient _client;
    private IDisposable? _gameSubscription;
    private IDisposable? _gamesListSubscription;

    #endregion

    /// <summary>
    /// Creates a new TicTacToeService instance.
    /// </summary>
    /// <param name="client">The Convex client instance.</param>
    public TicTacToeService(IConvexClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    #region Subscriptions

    /// <summary>
    /// Subscribe to real-time updates for a specific game.
    /// </summary>
    /// <param name="gameId">The ID of the game to subscribe to.</param>
    public void SubscribeToGame(string gameId)
    {
        _gameSubscription?.Dispose();

        try
        {
            _gameSubscription = _client
                .Observe<Game, object>("functions/getGame", new { gameId })
                .Subscribe(
                    game =>
                    {
                        if (game != null)
                        {
                            GameUpdated?.Invoke(this, game);
                        }
                    },
                    error =>
                    {
                        var errorMessage = $"Error subscribing to game: {error.Message}";
                        ErrorOccurred?.Invoke(this, errorMessage);
                    }
                );
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Failed to subscribe to game: {ex.Message}");
        }
    }

    /// <summary>
    /// Subscribe to real-time updates for the active games list.
    /// </summary>
    public void SubscribeToGamesList()
    {
        _gamesListSubscription?.Dispose();

        try
        {
            _gamesListSubscription = _client
                .Observe<List<Game>>("functions/getGames")
                .Subscribe(
                    games =>
                    {
                        GamesListUpdated?.Invoke(this, games ?? new List<Game>());
                    },
                    error =>
                    {
                        var errorMessage = $"Error subscribing to games list: {error.Message}";
                        ErrorOccurred?.Invoke(this, errorMessage);
                    }
                );
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Failed to subscribe to games list: {ex.Message}");
        }
    }

    /// <summary>
    /// Subscribe to real-time updates for joinable games (waiting games with no second player).
    /// </summary>
    public void SubscribeToJoinableGames()
    {
        _gamesListSubscription?.Dispose();

        try
        {
            _gamesListSubscription = _client
                .Observe<List<Game>>("functions/getJoinableGames")
                .Subscribe(
                    games =>
                    {
                        GamesListUpdated?.Invoke(this, games ?? new List<Game>());
                    },
                    error =>
                    {
                        var errorMessage = $"Error subscribing to joinable games: {error.Message}";
                        ErrorOccurred?.Invoke(this, errorMessage);
                    }
                );
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Failed to subscribe to joinable games: {ex.Message}");
        }
    }

    /// <summary>
    /// Unsubscribe from game updates.
    /// </summary>
    public void UnsubscribeFromGame()
    {
        _gameSubscription?.Dispose();
        _gameSubscription = null;
    }

    /// <summary>
    /// Unsubscribe from games list updates.
    /// </summary>
    public void UnsubscribeFromGamesList()
    {
        _gamesListSubscription?.Dispose();
        _gamesListSubscription = null;
    }

    #endregion

    #region Mutations

    /// <summary>
    /// Create a new game waiting for opponent.
    /// </summary>
    /// <param name="username">The username of the player creating the game.</param>
    /// <returns>The ID of the created game.</returns>
    public async Task<string> CreateGameAsync(string username)
    {
        try
        {
            var result = await _client
                .Mutate<string>("functions/createGame")
                .WithArgs(new { username })
                .ExecuteAsync();

            return result;
        }
        catch (ConvexException ex)
        {
            ErrorOccurred?.Invoke(this, $"Failed to create game: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Join an existing game as player O.
    /// </summary>
    /// <param name="gameId">The ID of the game to join.</param>
    /// <param name="username">The username of the player joining.</param>
    public async Task JoinGameAsync(string gameId, string username)
    {
        try
        {
            await _client
                .Mutate<object>("functions/joinGame")
                .WithArgs(new { gameId, username })
                .ExecuteAsync();
        }
        catch (ConvexException ex)
        {
            ErrorOccurred?.Invoke(this, $"Failed to join game: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Make a move on the board.
    /// </summary>
    /// <param name="gameId">The ID of the game.</param>
    /// <param name="username">The username of the player making the move.</param>
    /// <param name="position">The board position (0-8).</param>
    /// <returns>The result containing success status and winner if game ended.</returns>
    public async Task<MakeMoveResult> MakeMoveAsync(string gameId, string username, int position)
    {
        try
        {
            var result = await _client
                .Mutate<MakeMoveResult>("functions/makeMove")
                .WithArgs(new { gameId, username, position })
                .ExecuteAsync();

            return result;
        }
        catch (ConvexException ex)
        {
            ErrorOccurred?.Invoke(this, $"Failed to make move: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Forfeit/leave a game.
    /// </summary>
    /// <param name="gameId">The ID of the game.</param>
    /// <param name="username">The username of the player forfeiting.</param>
    public async Task ForfeitGameAsync(string gameId, string username)
    {
        try
        {
            await _client
                .Mutate<object>("functions/forfeitGame")
                .WithArgs(new { gameId, username })
                .ExecuteAsync();
        }
        catch (ConvexException ex)
        {
            ErrorOccurred?.Invoke(this, $"Failed to forfeit game: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Update player presence/online status.
    /// </summary>
    /// <param name="username">The username of the player.</param>
    /// <param name="status">The status: "online", "in_game", or "offline".</param>
    /// <param name="gameId">Optional ID of the current game.</param>
    public async Task UpdatePresenceAsync(string username, string status, string? gameId = null)
    {
        try
        {
            if (gameId != null)
            {
                await _client
                    .Mutate<object>("functions/updatePresence")
                    .WithArgs(new { username, status, gameId })
                    .ExecuteAsync();
            }
            else
            {
                await _client
                    .Mutate<object>("functions/updatePresence")
                    .WithArgs(new { username, status })
                    .ExecuteAsync();
            }
        }
        catch (ConvexException ex)
        {
            ErrorOccurred?.Invoke(this, $"Failed to update presence: {ex.Message}");
            throw;
        }
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Dispose of subscriptions and clean up resources.
    /// </summary>
    public void Dispose()
    {
        _gameSubscription?.Dispose();
        _gamesListSubscription?.Dispose();
        GC.SuppressFinalize(this);
    }

    #endregion
}

/// <summary>
/// Result of a make move operation.
/// </summary>
public class MakeMoveResult
{
    /// <summary>
    /// Whether the move was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Winner if game ended: "X", "O", "draw", or null.
    /// </summary>
    public string? Winner { get; set; }
}

