using Convex.Client;
using Convex.Client.Extensions.Gaming.Presets;
using Convex.Client.Extensions.Gaming.Sync;
using DrawingGame.Shared.Models;

namespace DrawingGame.Shared.Services;

/// <summary>
/// Service for interacting with the Drawing Game backend.
/// Provides game sync options for smooth drawing with reduced server calls.
/// </summary>
public class DrawingGameService : IDisposable
{
    private readonly IConvexClient _client;
    private readonly List<IDisposable> _subscriptions = new();

    // Events
    public event EventHandler<Room?>? RoomUpdated;
    public event EventHandler<List<Room>>? RoomsListUpdated;
    public event EventHandler<List<Guess>>? GuessesUpdated;

    public DrawingGameService(IConvexClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    /// <summary>
    /// Gets the Convex client for direct batch subscriptions.
    /// </summary>
    public IConvexClient Client => _client;

    /// <summary>
    /// Gets the game sync options for drawing applications.
    /// Use these settings for consistent behavior across input batching and subscription throttling.
    /// </summary>
    /// <remarks>
    /// These options provide:
    /// <list type="bullet">
    /// <item>33ms input sampling (~30fps - sufficient for drawing)</item>
    /// <item>500ms batch interval (2 batches/sec - 94% cost reduction)</item>
    /// <item>2px spatial filtering (reduces micro-jitter)</item>
    /// <item>50ms interpolation delay for smooth rendering</item>
    /// <item>100ms max extrapolation for brief network gaps</item>
    /// </list>
    /// </remarks>
    public static GameSyncOptions DrawingSyncOptions => GamePresets.ForDrawing();

    /// <summary>
    /// Creates an interpolated state for smooth stroke point rendering.
    /// Use this when you need silky-smooth stroke animations between network updates.
    /// </summary>
    /// <param name="options">Optional sync options. Defaults to <see cref="DrawingSyncOptions"/>.</param>
    /// <returns>An <see cref="InterpolatedState{T}"/> for smooth stroke rendering.</returns>
    /// <example>
    /// <code>
    /// // Create interpolated stroke state
    /// var interpolated = gameService.CreateInterpolatedStrokeState();
    ///
    /// // In your render loop (e.g., animation frame)
    /// var smoothPoint = interpolated.GetRenderState();
    /// if (smoothPoint != null)
    /// {
    ///     RenderStrokePoint(smoothPoint.X, smoothPoint.Y);
    /// }
    /// </code>
    /// </example>
    public InterpolatedState<StrokePointData> CreateInterpolatedStrokeState(GameSyncOptions? options = null)
    {
        options ??= DrawingSyncOptions;

        return new InterpolatedState<StrokePointData>
        {
            InterpolationDelayMs = options.InterpolationDelayMs,
            MaxExtrapolationMs = options.MaxExtrapolationMs
        };
    }

    // Subscribe to rooms list updates
    public void SubscribeToRoomsList()
    {
        var subscription = _client
            .Observe<List<Room>>("functions/getRooms")
            .Subscribe(
                rooms => RoomsListUpdated?.Invoke(this, rooms ?? new List<Room>()),
                error => Console.WriteLine($"Rooms list subscription error: {error.Message}")
            );

        _subscriptions.Add(subscription);
    }

    // Subscribe to room updates
    public void SubscribeToRoom(string roomId)
    {
        var subscription = _client
            .Observe<Room?, object>("functions/getRoom", new { roomId })
            .Subscribe(
                room => RoomUpdated?.Invoke(this, room),
                error => Console.WriteLine($"Room subscription error: {error.Message}")
            );

        _subscriptions.Add(subscription);
    }


    // Subscribe to guesses (for chat)
    public void SubscribeToGuesses(string roomId, double round)
    {
        var subscription = _client
            .Observe<List<Guess>, object>("functions/getGuesses", new { roomId, round })
            .Subscribe(
                guesses => GuessesUpdated?.Invoke(this, guesses ?? new List<Guess>()),
                error => Console.WriteLine($"Guesses subscription error: {error.Message}")
            );

        _subscriptions.Add(subscription);
    }

    // Get list of active rooms
    public async Task<List<Room>> GetRoomsAsync()
    {
        var rooms = await _client
            .Query<List<Room>>("functions/getRooms")
            .ExecuteAsync();

        return rooms ?? new List<Room>();
    }

    // Create room
    public async Task<CreateRoomResult> CreateRoomAsync(
        string name,
        string username,
        double maxPlayers = 6,
        double roundDuration = 90,
        string difficulty = "mixed")
    {
        var result = await _client
            .Mutate<CreateRoomResult>("functions/createRoom")
            .WithArgs(new
            {
                name,
                hostUsername = username,
                maxPlayers,
                roundDuration,
                difficulty
            })
            .ExecuteAsync();

        return result;
    }

    // Join room
    public async Task<string> JoinRoomAsync(string code, string username)
    {
        var roomId = await _client
            .Mutate<string>("functions/joinRoom")
            .WithArgs(new { code, username })
            .ExecuteAsync();

        return roomId;
    }

    // Start game
    public async Task StartGameAsync(string roomId, string username)
    {
        await _client
            .Mutate<object>("functions/startGame")
            .WithArgs(new { roomId, username })
            .ExecuteAsync();
    }

    // Select word
    public async Task SelectWordAsync(string roomId, string username, string word)
    {
        await _client
            .Mutate<object>("functions/selectWord")
            .WithArgs(new { roomId, username, word })
            .ExecuteAsync();
    }


    // Submit guess
    public async Task<GuessResult> SubmitGuessAsync(string roomId, string username, string guess)
    {
        var result = await _client
            .Mutate<GuessResult>("functions/submitGuess")
            .WithArgs(new { roomId, username, guess })
            .ExecuteAsync();

        return result;
    }

    // End round
    public async Task EndRoundAsync(string roomId)
    {
        await _client
            .Mutate<object>("functions/endRound")
            .WithArgs(new { roomId })
            .ExecuteAsync();
    }

    // Clear canvas
    public async Task ClearCanvasAsync(string roomId, string username)
    {
        await _client
            .Mutate<object>("functions/clearCanvas")
            .WithArgs(new { roomId, username })
            .ExecuteAsync();
    }

    // Dispose subscriptions
    public void Dispose()
    {
        foreach (var sub in _subscriptions)
        {
            sub.Dispose();
        }
        _subscriptions.Clear();
        GC.SuppressFinalize(this);
    }
}
