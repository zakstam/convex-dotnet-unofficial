using System.Reactive.Linq;
using Convex.Client;
using Convex.Client.Extensions.ExtensionMethods;
using Convex.Client.Infrastructure.ErrorHandling;
using TicTacToe.Shared.Models;

namespace TicTacToe.Shared.Services;

#region Argument Classes

/// <summary>
/// Arguments for the getGame query.
/// </summary>
public class GetGameArgs
{
    /// <summary>
    /// The ID of the game to retrieve.
    /// </summary>
    public required string GameId { get; set; }
}

/// <summary>
/// Arguments for the createGame mutation.
/// </summary>
public class CreateGameArgs
{
    /// <summary>
    /// The username of the player creating the game.
    /// </summary>
    public required string Username { get; set; }
}

/// <summary>
/// Arguments for the joinGame mutation.
/// </summary>
public class JoinGameArgs
{
    /// <summary>
    /// The ID of the game to join.
    /// </summary>
    public required string GameId { get; set; }

    /// <summary>
    /// The username of the player joining.
    /// </summary>
    public required string Username { get; set; }
}

/// <summary>
/// Arguments for the makeMove mutation.
/// </summary>
public class MakeMoveArgs
{
    /// <summary>
    /// The ID of the game.
    /// </summary>
    public required string GameId { get; set; }

    /// <summary>
    /// The username of the player making the move.
    /// </summary>
    public required string Username { get; set; }

    /// <summary>
    /// The board position (0-8).
    /// </summary>
    public required int Position { get; set; }
}

/// <summary>
/// Arguments for the forfeitGame mutation.
/// </summary>
public class ForfeitGameArgs
{
    /// <summary>
    /// The ID of the game to forfeit.
    /// </summary>
    public required string GameId { get; set; }

    /// <summary>
    /// The username of the player forfeiting.
    /// </summary>
    public required string Username { get; set; }
}

/// <summary>
/// Arguments for the updatePresence mutation.
/// </summary>
public class UpdatePresenceArgs
{
    /// <summary>
    /// The username of the player.
    /// </summary>
    public required string Username { get; set; }

    /// <summary>
    /// The status: "online", "in_game", or "offline".
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// Optional ID of the current game.
    /// </summary>
    public string? GameId { get; set; }
}

#endregion

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
    private readonly string _getGameFunctionName;
    private readonly string _getGamesFunctionName;
    private readonly string _getJoinableGamesFunctionName;
    private readonly string _createGameFunctionName;
    private readonly string _joinGameFunctionName;
    private readonly string _makeMoveFunctionName;
    private readonly string _forfeitGameFunctionName;
    private readonly string _updatePresenceFunctionName;
    private IDisposable? _gameSubscription;
    private IDisposable? _gamesListSubscription;

    #endregion

    /// <summary>
    /// Creates a new TicTacToeService instance.
    /// </summary>
    /// <param name="client">The Convex client instance.</param>
    /// <param name="getGameFunctionName">The function name for getting a game (e.g., "functions/getGame" or from ConvexFunctions.Queries.GetGame).</param>
    /// <param name="getGamesFunctionName">The function name for getting all games (e.g., "functions/getGames" or from ConvexFunctions.Queries.GetGames).</param>
    /// <param name="getJoinableGamesFunctionName">The function name for getting joinable games (e.g., "functions/getJoinableGames" or from ConvexFunctions.Queries.GetJoinableGames).</param>
    /// <param name="createGameFunctionName">The function name for creating a game (e.g., "functions/createGame" or from ConvexFunctions.Mutations.CreateGame).</param>
    /// <param name="joinGameFunctionName">The function name for joining a game (e.g., "functions/joinGame" or from ConvexFunctions.Mutations.JoinGame).</param>
    /// <param name="makeMoveFunctionName">The function name for making a move (e.g., "functions/makeMove" or from ConvexFunctions.Mutations.MakeMove).</param>
    /// <param name="forfeitGameFunctionName">The function name for forfeiting a game (e.g., "functions/forfeitGame" or from ConvexFunctions.Mutations.ForfeitGame).</param>
    /// <param name="updatePresenceFunctionName">The function name for updating presence (e.g., "functions/updatePresence" or from ConvexFunctions.Mutations.UpdatePresence).</param>
    public TicTacToeService(
        IConvexClient client,
        string getGameFunctionName = "functions/getGame",
        string getGamesFunctionName = "functions/getGames",
        string getJoinableGamesFunctionName = "functions/getJoinableGames",
        string createGameFunctionName = "functions/createGame",
        string joinGameFunctionName = "functions/joinGame",
        string makeMoveFunctionName = "functions/makeMove",
        string forfeitGameFunctionName = "functions/forfeitGame",
        string updatePresenceFunctionName = "functions/updatePresence")
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _getGameFunctionName = getGameFunctionName ?? throw new ArgumentNullException(nameof(getGameFunctionName));
        _getGamesFunctionName = getGamesFunctionName ?? throw new ArgumentNullException(nameof(getGamesFunctionName));
        _getJoinableGamesFunctionName = getJoinableGamesFunctionName ?? throw new ArgumentNullException(nameof(getJoinableGamesFunctionName));
        _createGameFunctionName = createGameFunctionName ?? throw new ArgumentNullException(nameof(createGameFunctionName));
        _joinGameFunctionName = joinGameFunctionName ?? throw new ArgumentNullException(nameof(joinGameFunctionName));
        _makeMoveFunctionName = makeMoveFunctionName ?? throw new ArgumentNullException(nameof(makeMoveFunctionName));
        _forfeitGameFunctionName = forfeitGameFunctionName ?? throw new ArgumentNullException(nameof(forfeitGameFunctionName));
        _updatePresenceFunctionName = updatePresenceFunctionName ?? throw new ArgumentNullException(nameof(updatePresenceFunctionName));
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
                .CreateResilientSubscription<Game>(_getGameFunctionName, new GetGameArgs { GameId = gameId })
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
        catch (ConvexException ex)
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
                .CreateResilientSubscription<List<Game>>(_getGamesFunctionName)
                .Subscribe(
                    games =>
                    {
                        GamesListUpdated?.Invoke(this, games ?? []);
                    },
                    error =>
                    {
                        var errorMessage = $"Error subscribing to games list: {error.Message}";
                        ErrorOccurred?.Invoke(this, errorMessage);
                    }
                );
        }
        catch (ConvexException ex)
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
                .CreateResilientSubscription<List<Game>>(_getJoinableGamesFunctionName)
                .Subscribe(
                    games =>
                    {
                        GamesListUpdated?.Invoke(this, games ?? []);
                    },
                    error =>
                    {
                        var errorMessage = $"Error subscribing to joinable games: {error.Message}";
                        ErrorOccurred?.Invoke(this, errorMessage);
                    }
                );
        }
        catch (ConvexException ex)
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
                .Mutate<string>(_createGameFunctionName)
                .WithArgs(new CreateGameArgs { Username = username })
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
            _ = await _client
                .Mutate<object>(_joinGameFunctionName)
                .WithArgs(new JoinGameArgs { GameId = gameId, Username = username })
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
                .Mutate<MakeMoveResult>(_makeMoveFunctionName)
                .WithArgs(new MakeMoveArgs { GameId = gameId, Username = username, Position = position })
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
            _ = await _client
                .Mutate<object>(_forfeitGameFunctionName)
                .WithArgs(new ForfeitGameArgs { GameId = gameId, Username = username })
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
            _ = await _client
                .Mutate<object>(_updatePresenceFunctionName)
                .WithArgs(new UpdatePresenceArgs { Username = username, Status = status, GameId = gameId })
                .ExecuteAsync();
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

