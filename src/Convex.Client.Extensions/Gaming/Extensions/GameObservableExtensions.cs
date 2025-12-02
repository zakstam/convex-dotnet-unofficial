using System.Reactive.Linq;
using Convex.Client.Extensions.Gaming.Sync;

namespace Convex.Client.Extensions.Gaming.Extensions;

/// <summary>
/// Extension methods for <see cref="IObservable{T}"/> that provide game-specific
/// state synchronization patterns.
/// </summary>
public static class GameObservableExtensions
{
    #region Interpolation Extensions
    /// <summary>
    /// Creates an <see cref="InterpolatedState{T}"/> that automatically receives updates from the observable.
    /// The observable is sampled at the specified rate and interpolation is applied for smooth rendering.
    /// </summary>
    /// <typeparam name="T">The state type that implements <see cref="IInterpolatable{T}"/>.</typeparam>
    /// <param name="source">The source observable (typically from client.Observe).</param>
    /// <param name="sampleInterval">The interval at which to sample updates from the server.</param>
    /// <param name="interpolationDelayMs">The interpolation delay in milliseconds. Default: 100ms.</param>
    /// <returns>A tuple containing the <see cref="InterpolatedState{T}"/> and the subscription disposable.</returns>
    /// <example>
    /// <code>
    /// var (interpolated, subscription) = client
    ///     .Observe&lt;PlayerPositions&gt;("game:positions")
    ///     .WithInterpolation(TimeSpan.FromMilliseconds(100));
    ///
    /// // In render loop
    /// var smooth = interpolated.GetRenderState();
    ///
    /// // Cleanup
    /// subscription.Dispose();
    /// </code>
    /// </example>
    public static (InterpolatedState<T> State, IDisposable Subscription) WithInterpolation<T>(
        this IObservable<T> source,
        TimeSpan sampleInterval,
        double interpolationDelayMs = 100)
        where T : class, IInterpolatable<T>
    {
        ArgumentNullException.ThrowIfNull(source);

        var interpolated = new InterpolatedState<T>
        {
            InterpolationDelayMs = interpolationDelayMs
        };

        var subscription = source
            .Sample(sampleInterval)
            .Subscribe(
                interpolated.PushState,
                static error => System.Diagnostics.Debug.WriteLine($"Interpolation subscription error: {error.Message}")
            );

        return (interpolated, subscription);
    }

    /// <summary>
    /// Creates an <see cref="InterpolatedState{T}"/> using settings from <see cref="GameSyncOptions"/>.
    /// </summary>
    /// <typeparam name="T">The state type that implements <see cref="IInterpolatable{T}"/>.</typeparam>
    /// <param name="source">The source observable.</param>
    /// <param name="options">The game sync options to use.</param>
    /// <returns>A tuple containing the <see cref="InterpolatedState{T}"/> and the subscription disposable.</returns>
    /// <example>
    /// <code>
    /// var options = GamePresets.ForActionGame();
    /// var (interpolated, subscription) = client
    ///     .Observe&lt;GameState&gt;("game:state")
    ///     .WithInterpolation(options);
    /// </code>
    /// </example>
    public static (InterpolatedState<T> State, IDisposable Subscription) WithInterpolation<T>(
        this IObservable<T> source,
        GameSyncOptions options)
        where T : class, IInterpolatable<T>
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(options);

        var interpolated = new InterpolatedState<T>
        {
            InterpolationDelayMs = options.InterpolationDelayMs,
            MaxExtrapolationMs = options.MaxExtrapolationMs
        };

        var sampledSource = options.SubscriptionThrottle.HasValue
            ? source.Sample(options.SubscriptionThrottle.Value)
            : source;

        var subscription = sampledSource.Subscribe(
            interpolated.PushState,
            static error => System.Diagnostics.Debug.WriteLine($"Interpolation subscription error: {error.Message}")
        );

        return (interpolated, subscription);
    }

    /// <summary>
    /// Applies throttling to the observable based on <see cref="GameSyncOptions"/>.
    /// </summary>
    /// <typeparam name="T">The state type.</typeparam>
    /// <param name="source">The source observable.</param>
    /// <param name="options">The game sync options to use.</param>
    /// <returns>
    /// A throttled observable if <see cref="GameSyncOptions.SubscriptionThrottle"/> is set,
    /// otherwise the original observable.
    /// </returns>
    public static IObservable<T> WithThrottling<T>(this IObservable<T> source, GameSyncOptions options)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(options);

        return options.SubscriptionThrottle.HasValue
            ? source.Sample(options.SubscriptionThrottle.Value)
            : source;
    }

    /// <summary>
    /// Samples the observable at a fixed rate, reducing the number of updates received.
    /// This is a convenience wrapper around <see cref="Observable.Sample{TSource}(IObservable{TSource}, TimeSpan)"/>.
    /// </summary>
    /// <typeparam name="T">The state type.</typeparam>
    /// <param name="source">The source observable.</param>
    /// <param name="updatesPerSecond">The target number of updates per second.</param>
    /// <returns>A sampled observable.</returns>
    /// <example>
    /// <code>
    /// // Reduce to 10 updates per second
    /// client.Observe&lt;GameState&gt;("game:state")
    ///     .AtRate(10)
    ///     .Subscribe(state => Render(state));
    /// </code>
    /// </example>
    public static IObservable<T> AtRate<T>(this IObservable<T> source, int updatesPerSecond)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfLessThan(updatesPerSecond, 1);

        var interval = TimeSpan.FromMilliseconds(1000.0 / updatesPerSecond);
        return source.Sample(interval);
    }

    #endregion Interpolation Extensions

    #region Prediction Extensions

    /// <summary>
    /// Creates a <see cref="PredictedState{TState, TInput}"/> that receives server state updates from the observable.
    /// </summary>
    /// <typeparam name="TState">The state type that implements <see cref="IPredictable{TState, TInput}"/>.</typeparam>
    /// <typeparam name="TInput">The input type.</typeparam>
    /// <param name="source">The source observable providing server state updates.</param>
    /// <param name="initialState">The initial state to use before the first server update.</param>
    /// <param name="lastProcessedInputIdSelector">
    /// A function to extract the last processed input ID from each server state.
    /// This is used for reconciliation to know which inputs have been acknowledged.
    /// </param>
    /// <returns>A tuple containing the <see cref="PredictedState{TState, TInput}"/> and the subscription disposable.</returns>
    /// <example>
    /// <code>
    /// var (predicted, subscription) = client
    ///     .Observe&lt;ServerGameState&gt;("game:state")
    ///     .WithPrediction(
    ///         initialState,
    ///         state => state.LastProcessedInputId);
    ///
    /// // Apply local input
    /// var timestamped = predicted.ApplyInput(new MoveInput { X = 1 });
    /// await batcher.AddAsync(timestamped);
    ///
    /// // In render loop
    /// Render(predicted.CurrentState);
    /// </code>
    /// </example>
    public static (PredictedState<TState, TInput> State, IDisposable Subscription) WithPrediction<TState, TInput>(
        this IObservable<TState> source,
        TState initialState,
        Func<TState, long> lastProcessedInputIdSelector)
        where TState : class, IPredictable<TState, TInput>
        => WithPrediction<TState, TInput>(source, initialState, lastProcessedInputIdSelector, options: null);

    /// <summary>
    /// Creates a <see cref="PredictedState{TState, TInput}"/> that receives server state updates from the observable
    /// with custom game sync options.
    /// </summary>
    /// <typeparam name="TState">The state type that implements <see cref="IPredictable{TState, TInput}"/>.</typeparam>
    /// <typeparam name="TInput">The input type.</typeparam>
    /// <param name="source">The source observable providing server state updates.</param>
    /// <param name="initialState">The initial state to use before the first server update.</param>
    /// <param name="lastProcessedInputIdSelector">
    /// A function to extract the last processed input ID from each server state.
    /// This is used for reconciliation to know which inputs have been acknowledged.
    /// </param>
    /// <param name="options">The game sync options to use.</param>
    /// <returns>A tuple containing the <see cref="PredictedState{TState, TInput}"/> and the subscription disposable.</returns>
    /// <example>
    /// <code>
    /// var (predicted, subscription) = client
    ///     .Observe&lt;ServerGameState&gt;("game:state")
    ///     .WithPrediction(
    ///         initialState,
    ///         state => state.LastProcessedInputId,
    ///         GamePresets.ForActionGame());
    /// </code>
    /// </example>
    public static (PredictedState<TState, TInput> State, IDisposable Subscription) WithPrediction<TState, TInput>(
        this IObservable<TState> source,
        TState initialState,
        Func<TState, long> lastProcessedInputIdSelector,
        GameSyncOptions? options)
        where TState : class, IPredictable<TState, TInput>
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(initialState);
        ArgumentNullException.ThrowIfNull(lastProcessedInputIdSelector);

        var maxPendingInputs = options?.MaxPendingInputs ?? 60;
        var predicted = new PredictedState<TState, TInput>(initialState, maxPendingInputs)
        {
            IsEnabled = options?.EnablePrediction ?? true
        };

        var sampledSource = options?.SubscriptionThrottle is { } throttle
            ? source.Sample(throttle)
            : source;

        var subscription = sampledSource.Subscribe(
            serverState => predicted.OnServerState(serverState, lastProcessedInputIdSelector(serverState)),
            static error => System.Diagnostics.Debug.WriteLine($"Prediction subscription error: {error.Message}")
        );

        return (predicted, subscription);
    }

    /// <summary>
    /// Creates a <see cref="PredictedState{TState, TInput}"/> with a custom state extractor for server updates.
    /// Use this overload when the observable type differs from the state type (e.g., wrapper DTOs).
    /// </summary>
    /// <typeparam name="TServerMessage">The type of messages from the server observable.</typeparam>
    /// <typeparam name="TState">The state type that implements <see cref="IPredictable{TState, TInput}"/>.</typeparam>
    /// <typeparam name="TInput">The input type.</typeparam>
    /// <param name="source">The source observable providing server messages.</param>
    /// <param name="initialState">The initial state to use before the first server update.</param>
    /// <param name="stateSelector">A function to extract the state from each server message.</param>
    /// <param name="lastProcessedInputIdSelector">
    /// A function to extract the last processed input ID from each server message.
    /// </param>
    /// <returns>A tuple containing the <see cref="PredictedState{TState, TInput}"/> and the subscription disposable.</returns>
    public static (PredictedState<TState, TInput> State, IDisposable Subscription) WithPrediction<TServerMessage, TState, TInput>(
        this IObservable<TServerMessage> source,
        TState initialState,
        Func<TServerMessage, TState> stateSelector,
        Func<TServerMessage, long> lastProcessedInputIdSelector)
        where TState : class, IPredictable<TState, TInput>
        => WithPrediction<TServerMessage, TState, TInput>(source, initialState, stateSelector, lastProcessedInputIdSelector, options: null);

    /// <summary>
    /// Creates a <see cref="PredictedState{TState, TInput}"/> with a custom state extractor for server updates
    /// and custom game sync options.
    /// Use this overload when the observable type differs from the state type (e.g., wrapper DTOs).
    /// </summary>
    /// <typeparam name="TServerMessage">The type of messages from the server observable.</typeparam>
    /// <typeparam name="TState">The state type that implements <see cref="IPredictable{TState, TInput}"/>.</typeparam>
    /// <typeparam name="TInput">The input type.</typeparam>
    /// <param name="source">The source observable providing server messages.</param>
    /// <param name="initialState">The initial state to use before the first server update.</param>
    /// <param name="stateSelector">A function to extract the state from each server message.</param>
    /// <param name="lastProcessedInputIdSelector">
    /// A function to extract the last processed input ID from each server message.
    /// </param>
    /// <param name="options">The game sync options to use.</param>
    /// <returns>A tuple containing the <see cref="PredictedState{TState, TInput}"/> and the subscription disposable.</returns>
    public static (PredictedState<TState, TInput> State, IDisposable Subscription) WithPrediction<TServerMessage, TState, TInput>(
        this IObservable<TServerMessage> source,
        TState initialState,
        Func<TServerMessage, TState> stateSelector,
        Func<TServerMessage, long> lastProcessedInputIdSelector,
        GameSyncOptions? options)
        where TState : class, IPredictable<TState, TInput>
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(initialState);
        ArgumentNullException.ThrowIfNull(stateSelector);
        ArgumentNullException.ThrowIfNull(lastProcessedInputIdSelector);

        var maxPendingInputs = options?.MaxPendingInputs ?? 60;
        var predicted = new PredictedState<TState, TInput>(initialState, maxPendingInputs)
        {
            IsEnabled = options?.EnablePrediction ?? true
        };

        var sampledSource = options?.SubscriptionThrottle is { } throttle
            ? source.Sample(throttle)
            : source;

        var subscription = sampledSource.Subscribe(
            serverMessage => predicted.OnServerState(
                stateSelector(serverMessage),
                lastProcessedInputIdSelector(serverMessage)),
            static error => System.Diagnostics.Debug.WriteLine($"Prediction subscription error: {error.Message}")
        );

        return (predicted, subscription);
    }

    #endregion Prediction Extensions
}
