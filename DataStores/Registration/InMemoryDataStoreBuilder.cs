using DataStores.Abstractions;
using DataStores.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace DataStores.Registration;

/// <summary>
/// Builder for registering in-memory data stores without persistence.
/// </summary>
/// <typeparam name="T">The type of items in the store. Must be a reference type.</typeparam>
/// <remarks>
/// <para>
/// InMemory stores are thread-safe, transient data stores that do NOT persist data.
/// All data is lost when the application stops.
/// </para>
/// <para>
/// Use this builder when:
/// </para>
/// <list type="bullet">
/// <item><description>Data does not need to be persisted (e.g., temporary UI state, cache)</description></item>
/// <item><description>Data is loaded from external sources at runtime</description></item>
/// <item><description>Performance is critical and persistence is handled separately</description></item>
/// </list>
/// <para>
/// <b>Automatic Comparer Resolution:</b>
/// When no explicit comparer is provided, the builder automatically resolves an appropriate
/// comparer via <see cref="IEqualityComparerService"/>:
/// </para>
/// <list type="bullet">
/// <item><description>EntityBase types → EntityIdComparer (ID-based comparison)</description></item>
/// <item><description>Types with registered custom comparer → From DI container</description></item>
/// <item><description>All other types → EqualityComparer&lt;T&gt;.Default</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Simple in-memory store with automatic comparer
/// AddStore(new InMemoryDataStoreBuilder&lt;Product&gt;());
/// 
/// // With custom comparer (overrides automatic resolution)
/// AddStore(new InMemoryDataStoreBuilder&lt;Category&gt;(
///     comparer: new CategoryIdComparer()));
/// 
/// // With UI-thread event marshalling (WPF)
/// AddStore(new InMemoryDataStoreBuilder&lt;OrderViewModel&gt;(
///     synchronizationContext: SynchronizationContext.Current));
/// </code>
/// </example>
public sealed class InMemoryDataStoreBuilder<T> : DataStoreBuilder<T> where T : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryDataStoreBuilder{T}"/> class.
    /// </summary>
    /// <param name="comparer">
    /// Optional equality comparer for items.
    /// Used for Contains, Remove operations, and duplicate detection.
    /// If null, comparer is automatically resolved via IEqualityComparerService.
    /// </param>
    /// <param name="synchronizationContext">
    /// Optional synchronization context for event marshalling.
    /// If provided, Changed events are posted to this context (e.g., UI thread).
    /// If null, events are raised synchronously on the calling thread.
    /// </param>
    /// <remarks>
    /// This constructor captures the configuration for the store.
    /// The actual store is created during the Register phase with automatic comparer resolution.
    /// </remarks>
    public InMemoryDataStoreBuilder(
        IEqualityComparer<T>? comparer = null,
        SynchronizationContext? synchronizationContext = null)
    {
        Comparer = comparer;
        SynchronizationContext = synchronizationContext;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Creates and registers a new <see cref="InMemoryDataStore{T}"/> with the configured parameters.
    /// If no explicit comparer was provided, automatically resolves an appropriate comparer via
    /// <see cref="IEqualityComparerService"/>.
    /// </remarks>
    internal override void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider)
    {
        // Resolve comparer automatically if not explicitly provided
        var effectiveComparer = Comparer;
        if (effectiveComparer == null)
        {
            var comparerService = serviceProvider.GetRequiredService<IEqualityComparerService>();
            effectiveComparer = comparerService.GetComparer<T>();
        }

        var store = new InMemoryDataStore<T>(effectiveComparer, SynchronizationContext);
        registry.RegisterGlobal(store);
    }
}
