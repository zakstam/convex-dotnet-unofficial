namespace Convex.Client.Shared.Http;

/// <summary>
/// Helper class for handling timeout-related cancellation tokens.
/// </summary>
public static class TimeoutHelper
{
    /// <summary>
    /// Creates a cancellation token that combines a user-provided cancellation token with a timeout.
    /// </summary>
    /// <param name="timeout">Optional timeout duration. If null, only the user token is used.</param>
    /// <param name="userToken">The user-provided cancellation token.</param>
    /// <returns>A disposable wrapper containing the effective cancellation token and cleanup logic.</returns>
    public static TimeoutTokenWrapper CreateTimeoutToken(TimeSpan? timeout, CancellationToken userToken)
    {
        if (!timeout.HasValue)
        {
            return new TimeoutTokenWrapper(userToken, null, null);
        }

        var timeoutCts = new CancellationTokenSource(timeout.Value);
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            userToken,
            timeoutCts.Token);

        return new TimeoutTokenWrapper(linkedCts.Token, timeoutCts, linkedCts);
    }

    /// <summary>
    /// Wraps a cancellation token and associated disposable resources for cleanup.
    /// </summary>
    public sealed class TimeoutTokenWrapper : IDisposable
    {
        private readonly CancellationTokenSource? _timeoutCts;
        private readonly CancellationTokenSource? _linkedCts;

        /// <summary>
        /// Gets the effective cancellation token that combines timeout and user token.
        /// </summary>
        public CancellationToken Token { get; }

        /// <summary>
        /// Gets a value indicating whether the timeout was triggered.
        /// </summary>
        public bool WasTimeout => _timeoutCts?.IsCancellationRequested == true;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1068:CancellationToken parameters must come last", Justification = "This is a constructor")]
        internal TimeoutTokenWrapper(
            CancellationToken token,
            CancellationTokenSource? timeoutCts,
            CancellationTokenSource? linkedCts)
        {
            Token = token;
            _timeoutCts = timeoutCts;
            _linkedCts = linkedCts;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _timeoutCts?.Dispose();
            _linkedCts?.Dispose();
        }
    }
}

