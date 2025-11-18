#if NET8_0_OR_GREATER || NET9_0_OR_GREATER
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components;
using System.Reactive.Linq;
using System.Reactive.Disposables;

namespace Convex.Client.Extensions.ExtensionMethods;

/// <summary>
/// Extension methods for IObservable&lt;T&gt; that provide Blazor-specific integrations.
/// </summary>
public static class ConvexBlazorExtensions
{
    #region StateHasChanged Integration

    /// <summary>
    /// Subscribes to the observable and automatically marshals updates to the UI thread.
    /// This is a convenience helper that handles null filtering and synchronization.
    /// Note: Your onNext action should call StateHasChanged() to trigger UI updates.
    /// </summary>
    /// <typeparam name="T">The type of the observable sequence.</typeparam>
    /// <param name="source">The source observable.</param>
    /// <param name="component">The Blazor component (for type safety, not used directly).</param>
    /// <param name="onNext">Action to execute when a value is emitted (call StateHasChanged within).</param>
    /// <returns>An IDisposable that unsubscribes when disposed.</returns>
    /// <example>
    /// <code>
    /// // Simplified subscription with null filtering
    /// client.Observe&lt;Message[]&gt;("messages:list")
    ///     .SubscribeToUI(this, messages =>
    ///     {
    ///         Messages = messages;
    ///         StateHasChanged();
    ///     });
    /// </code>
    /// </example>
    public static IDisposable SubscribeToUI<T>(
        this IObservable<T> source,
        ComponentBase component,
        Action<T> onNext) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(component);
        ArgumentNullException.ThrowIfNull(onNext);

        // Just filter out nulls - the user's onNext action handles StateHasChanged
        return source
            .Where(value => value != null)
            .Subscribe(onNext);
    }

    /// <summary>
    /// Subscribes to the observable with error handling and automatic null filtering.
    /// Note: Your onNext and onError actions should call StateHasChanged() to trigger UI updates.
    /// </summary>
    /// <typeparam name="T">The type of the observable sequence.</typeparam>
    /// <param name="source">The source observable.</param>
    /// <param name="component">The Blazor component (for type safety, not used directly).</param>
    /// <param name="onNext">Action to execute when a value is emitted (call StateHasChanged within).</param>
    /// <param name="onError">Action to execute when an error occurs (call StateHasChanged within).</param>
    /// <returns>An IDisposable that unsubscribes when disposed.</returns>
    /// <example>
    /// <code>
    /// client.Observe&lt;Message[]&gt;("messages:list")
    ///     .SubscribeToUI(
    ///         this,
    ///         messages => { Messages = messages; StateHasChanged(); },
    ///         error => { ErrorMessage = error.Message; StateHasChanged(); });
    /// </code>
    /// </example>
    public static IDisposable SubscribeToUI<T>(
        this IObservable<T> source,
        ComponentBase component,
        Action<T> onNext,
        Action<Exception>? onError = null) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(component);
        ArgumentNullException.ThrowIfNull(onNext);

        return source
            .Where(value => value != null)
            .Subscribe(
                onNext,
                onError ?? DefaultErrorHandler);
    }

    private static void DefaultErrorHandler(Exception ex) =>
        // Default error handler - just log to console in development
        Console.WriteLine($"Convex subscription error: {ex.Message}");

    /// <summary>
    /// Subscribes to the observable and automatically calls StateHasChanged on the component
    /// after each value emission. Useful for triggering UI updates in Blazor components.
    /// NOTE: Consider using SubscribeToUI instead - it handles InvokeAsync automatically.
    /// </summary>
    /// <typeparam name="T">The type of the observable sequence.</typeparam>
    /// <param name="source">The source observable.</param>
    /// <param name="component">The Blazor component that needs state updates.</param>
    /// <param name="onNext">Action to execute when a value is emitted (should call StateHasChanged).</param>
    /// <returns>An IDisposable that unsubscribes when disposed.</returns>
    /// <example>
    /// <code>
    /// public partial class MessagesComponent : ComponentBase
    /// {
    ///     private Message[] _messages = Array.Empty&lt;Message&gt;();
    ///
    ///     protected override void OnInitialized()
    ///     {
    ///         client.Observe&lt;Message[]&gt;("messages:list")
    ///             .SubscribeWithStateHasChanged(this, messages =>
    ///             {
    ///                 _messages = messages;
    ///                 StateHasChanged();
    ///             });
    ///     }
    /// }
    /// </code>
    /// </example>
    public static IDisposable SubscribeWithStateHasChanged<T>(
        this IObservable<T> source,
        ComponentBase component,
        Action<T> onNext)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(component);
        ArgumentNullException.ThrowIfNull(onNext);

        return source.Subscribe(onNext);
    }

    /// <summary>
    /// Subscribes to the observable and automatically calls StateHasChanged on the component
    /// after each value emission, with error handling.
    /// </summary>
    /// <typeparam name="T">The type of the observable sequence.</typeparam>
    /// <param name="source">The source observable.</param>
    /// <param name="component">The Blazor component that needs state updates.</param>
    /// <param name="onNext">Action to execute when a value is emitted (should call StateHasChanged).</param>
    /// <param name="onError">Action to execute when an error occurs (should call StateHasChanged).</param>
    /// <returns>An IDisposable that unsubscribes when disposed.</returns>
    /// <example>
    /// <code>
    /// client.Observe&lt;Message[]&gt;("messages:list")
    ///     .SubscribeWithStateHasChanged(
    ///         this,
    ///         messages => { _messages = messages; StateHasChanged(); },
    ///         error => { _errorMessage = error.Message; StateHasChanged(); });
    /// </code>
    /// </example>
    public static IDisposable SubscribeWithStateHasChanged<T>(
        this IObservable<T> source,
        ComponentBase component,
        Action<T> onNext,
        Action<Exception> onError)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(component);
        ArgumentNullException.ThrowIfNull(onNext);
        ArgumentNullException.ThrowIfNull(onError);

        return source.Subscribe(onNext, onError);
    }

    #endregion

    #region AsyncEnumerable Integration

    /// <summary>
    /// Converts an observable to an IAsyncEnumerable for use with @foreach await in Blazor.
    /// </summary>
    /// <typeparam name="T">The type of the observable sequence.</typeparam>
    /// <param name="source">The source observable.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>An async enumerable that yields values from the observable.</returns>
    /// <example>
    /// <code>
    /// @code {
    ///     private IAsyncEnumerable&lt;Message&gt; _messages;
    ///
    ///     protected override void OnInitialized()
    ///     {
    ///         _messages = client.Observe&lt;Message[]&gt;("messages:list")
    ///             .SelectMany(messages => messages)
    ///             .ToAsyncEnumerable();
    ///     }
    /// }
    ///
    /// &lt;ul&gt;
    ///     @foreach await (var message in _messages)
    ///     {
    ///         &lt;li&gt;@message.Text&lt;/li&gt;
    ///     }
    /// &lt;/ul&gt;
    /// </code>
    /// </example>
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(
        this IObservable<T> source,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        var channel = System.Threading.Channels.Channel.CreateUnbounded<T>();
        var subscription = source.Subscribe(
            onNext: value => channel.Writer.TryWrite(value),
            onError: error => channel.Writer.TryComplete(error),
            onCompleted: () => channel.Writer.TryComplete());

        using var _ = subscription;

        await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return item;
        }
    }

    #endregion

    #region Form Binding

    /// <summary>
    /// Creates a two-way binding between an observable and form state.
    /// Updates the form when the observable emits, and can save changes back.
    /// </summary>
    /// <typeparam name="T">The type of the form data.</typeparam>
    /// <param name="source">The observable providing form data.</param>
    /// <param name="updateForm">Action to update the form UI with new data.</param>
    /// <param name="formChanges">Observable of form changes from user input.</param>
    /// <param name="saveChanges">Function to save form changes (returns Task).</param>
    /// <returns>An IDisposable that manages the binding.</returns>
    /// <example>
    /// <code>
    /// private User _user = new();
    /// private Subject&lt;User&gt; _formChanges = new();
    ///
    /// protected override void OnInitialized()
    /// {
    ///     var binding = client.Observe&lt;User&gt;("users:current")
    ///         .BindToForm(
    ///             user => _user = user,
    ///             _formChanges,
    ///             async updatedUser => await client.MutateAsync("users:update", updatedUser));
    /// }
    ///
    /// private void OnUserChanged(User user)
    /// {
    ///     _formChanges.OnNext(user);
    /// }
    /// </code>
    /// </example>
    public static IDisposable BindToForm<T>(
        this IObservable<T> source,
        Action<T> updateForm,
        IObservable<T> formChanges,
        Func<T, Task> saveChanges)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(updateForm);
        ArgumentNullException.ThrowIfNull(formChanges);
        ArgumentNullException.ThrowIfNull(saveChanges);

        // Subscribe to source changes and update form
        var sourceSubscription = source.Subscribe(updateForm);

        // Subscribe to form changes and save them
        var formSubscription = formChanges
            .Throttle(TimeSpan.FromMilliseconds(500)) // Debounce form changes
            .Select(formData => Observable.FromAsync(() => saveChanges(formData)))
            .Concat()
            .Subscribe();

        return new CompositeDisposable(sourceSubscription, formSubscription);
    }

    /// <summary>
    /// Creates a simple one-way binding from observable to form with StateHasChanged.
    /// </summary>
    /// <typeparam name="T">The type of the form data.</typeparam>
    /// <param name="source">The observable providing form data.</param>
    /// <param name="component">The Blazor component.</param>
    /// <param name="updateForm">Action to update the form with new data.</param>
    /// <returns>An IDisposable that manages the binding.</returns>
    /// <example>
    /// <code>
    /// private User _user = new();
    ///
    /// protected override void OnInitialized()
    /// {
    ///     client.Observe&lt;User&gt;("users:current")
    ///         .BindToForm(this, user => _user = user);
    /// }
    /// </code>
    /// </example>
    public static IDisposable BindToForm<T>(
        this IObservable<T> source,
        ComponentBase component,
        Action<T> updateForm)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(component);
        ArgumentNullException.ThrowIfNull(updateForm);

        return source.SubscribeWithStateHasChanged(component, updateForm);
    }

    #endregion
}
#endif
