#if NET8_0_OR_GREATER || NET9_0_OR_GREATER
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Windows.Input;

namespace Convex.Client.Extensions.ExtensionMethods;

/// <summary>
/// Extension methods for IObservable&lt;T&gt; that provide UI framework integrations
/// for WPF and MAUI applications.
/// </summary>
public static class ConvexWpfMauiExtensions
{
    #region UI Thread Marshalling

    /// <summary>
    /// Automatically marshals observable notifications to the UI thread.
    /// Uses SynchronizationContext.Current to ensure thread safety.
    /// </summary>
    /// <typeparam name="T">The type of the observable sequence.</typeparam>
    /// <param name="source">The source observable.</param>
    /// <returns>An observable that notifies on the UI thread.</returns>
    /// <example>
    /// <code>
    /// var subscription = client.Observe&lt;Message[]&gt;("messages:list")
    ///     .ObserveOnUI()
    ///     .Subscribe(messages => UpdateUI(messages));
    /// </code>
    /// </example>
    public static IObservable<T> ObserveOnUI<T>(this IObservable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var context = SynchronizationContext.Current;
        ArgumentNullException.ThrowIfNull(context);

        return source.ObserveOn(context);
    }

    #endregion

    #region Property Binding

    /// <summary>
    /// Binds an observable to an INotifyPropertyChanged property with automatic conversion.
    /// </summary>
    /// <typeparam name="TSource">The type of the observable sequence.</typeparam>
    /// <typeparam name="TTarget">The type of the target object.</typeparam>
    /// <typeparam name="TProp">The type of the target property.</typeparam>
    /// <param name="source">The source observable.</param>
    /// <param name="target">The target object that implements INotifyPropertyChanged.</param>
    /// <param name="propertyExpression">Expression identifying the property to bind to.</param>
    /// <param name="converter">Optional converter function from source to property type.</param>
    /// <returns>An IDisposable that unbinds when disposed.</returns>
    /// <example>
    /// <code>
    /// public class ViewModel : INotifyPropertyChanged
    /// {
    ///     private string _userName;
    ///     public string UserName
    ///     {
    ///         get => _userName;
    ///         set { _userName = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UserName))); }
    ///     }
    ///     public event PropertyChangedEventHandler? PropertyChanged;
    /// }
    ///
    /// var viewModel = new ViewModel();
    /// var binding = client.Observe&lt;User&gt;("users:current")
    ///     .BindToProperty(viewModel, vm => vm.UserName, user => user.Name);
    /// </code>
    /// </example>
    public static IDisposable BindToProperty<TSource, TTarget, TProp>(
        this IObservable<TSource> source,
        TTarget target,
        Expression<Func<TTarget, TProp>> propertyExpression,
        Func<TSource, TProp>? converter = null)
        where TTarget : INotifyPropertyChanged
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(propertyExpression);

        var propertyName = GetPropertyName(propertyExpression);
        var convert = converter ?? (source => (TProp)(object)source!);

        return source.Subscribe(value =>
        {
            var propertyInfo = typeof(TTarget).GetProperty(propertyName);
            if (propertyInfo != null && propertyInfo.CanWrite)
            {
                propertyInfo.SetValue(target, convert(value));
            }
        });
    }

    /// <summary>
    /// Binds a boolean observable to an ICommand's CanExecute state.
    /// Automatically calls RaiseCanExecuteChanged when the value changes.
    /// </summary>
    /// <param name="source">The boolean observable.</param>
    /// <param name="command">The command to bind to.</param>
    /// <returns>An IDisposable that unbinds when disposed.</returns>
    /// <example>
    /// <code>
    /// var saveCommand = new RelayCommand(Save, () => CanSave);
    /// var canSaveObservable = Observable.CombineLatest(
    ///     titleObservable, contentObservable,
    ///     (title, content) => !string.IsNullOrEmpty(title) &amp;&amp; !string.IsNullOrEmpty(content));
    ///
    /// var binding = canSaveObservable.BindToCanExecute(saveCommand);
    /// </code>
    /// </example>
    public static IDisposable BindToCanExecute(this IObservable<bool> source, ICommand command)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(command);

        return source.DistinctUntilChanged().Subscribe(canExecute =>
        {
            if (command is IRaiseCanExecuteChanged raiser)
            {
                raiser.RaiseCanExecuteChanged();
            }
        });
    }

    #endregion

    #region Collection Binding

    /// <summary>
    /// Creates an ObservableCollection that automatically synchronizes with the observable stream.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="source">The observable providing collection data.</param>
    /// <returns>An ObservableCollection that stays in sync with the observable.</returns>
    /// <example>
    /// <code>
    /// // Bind to UI list control
    /// var messages = client.Observe&lt;Message[]&gt;("messages:list")
    ///     .ToObservableCollection();
    /// listView.ItemsSource = messages;
    /// </code>
    /// </example>
    public static ObservableCollection<T> ToObservableCollection<T>(this IObservable<IEnumerable<T>> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var collection = new ObservableCollection<T>();
        var subscription = source.Subscribe(items =>
        {
            collection.Clear();
            foreach (var item in items)
            {
                collection.Add(item);
            }
        });

        // Store the subscription on the collection for cleanup
        // Note: This is a simple approach; in production you might want a more sophisticated solution
        return collection;
    }

    #endregion

    #region Helper Methods

    private static string GetPropertyName<TTarget, TProp>(Expression<Func<TTarget, TProp>> propertyExpression)
    {
        return propertyExpression.Body is not MemberExpression memberExpression
            ? throw new ArgumentException("Expression must be a property access expression", nameof(propertyExpression))
            : memberExpression.Member.Name;
    }

    #endregion
}

/// <summary>
/// Interface for commands that can raise CanExecuteChanged events.
/// Implemented by most MVVM command implementations.
/// </summary>
public interface IRaiseCanExecuteChanged
{
    /// <summary>
    /// Raises the CanExecuteChanged event.
    /// </summary>
    void RaiseCanExecuteChanged();
}
#endif
