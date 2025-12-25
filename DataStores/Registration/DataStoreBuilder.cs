using DataStores.Abstractions;

namespace DataStores.Registration;

/// <summary>
/// Abstract base class for data store builders.
/// Builders encapsulate the creation and registration logic for different store types.
/// </summary>
/// <typeparam name="T">The type of items in the store. Must be a reference type.</typeparam>
/// <remarks>
/// <para>
/// This class is the foundation of the builder pattern for data store registration.
/// Concrete builders (InMemory, JSON, LiteDB) inherit from this class and implement
/// the actual registration logic.
/// </para>
/// <para>
/// INTERNAL INFRASTRUCTURE: Application code does NOT interact with builders directly.
/// Builders are created and used within <see cref="DataStoreRegistrarBase"/> implementations.
/// </para>
/// </remarks>
public abstract class DataStoreBuilder<T> where T : class
{
    /// <summary>
    /// Gets the optional equality comparer for items in the store.
    /// </summary>
    /// <remarks>
    /// Used by InMemoryDataStore for Contains and Remove operations.
    /// If null, EqualityComparer&lt;T&gt;.Default is used.
    /// </remarks>
    protected IEqualityComparer<T>? Comparer { get; init; }

    /// <summary>
    /// Gets the optional synchronization context for event marshalling.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Used by InMemoryDataStore to marshal Changed events to a specific thread (typically UI thread).
    /// </para>
    /// <para>
    /// Common scenarios:
    /// </para>
    /// <list type="bullet">
    /// <item><description>WPF: SynchronizationContext.Current from UI thread</description></item>
    /// <item><description>WinForms: WindowsFormsSynchronizationContext</description></item>
    /// <item><description>Console/Backend: null (synchronous event invocation)</description></item>
    /// </list>
    /// </remarks>
    protected SynchronizationContext? SynchronizationContext { get; init; }

    /// <summary>
    /// Registers the data store with the global registry.
    /// </summary>
    /// <param name="registry">The global store registry.</param>
    /// <remarks>
    /// This method is called internally by <see cref="DataStoreRegistrarBase"/> during startup.
    /// Each builder implements its own registration logic (InMemory, JSON, LiteDB, etc.).
    /// </remarks>
    internal abstract void Register(IGlobalStoreRegistry registry);
}
