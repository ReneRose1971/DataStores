using DataStores.Abstractions;
using Common.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace DataStores.Runtime;

/// <summary>
/// Implementation von IEqualityComparerService mit automatischer Auflösung und EntityBase-Unterstützung.
/// </summary>
/// <remarks>
/// <para>
/// Verwendet die Common.Extensions <see cref="ServiceProviderExtensions.GetEqualityComparer{T}"/>
/// für registrierte Comparer und liefert automatisch einen <see cref="EntityIdComparer{T}"/> für EntityBase-Typen.
/// </para>
/// <para>
/// <b>Thread-Sicherheit:</b>
/// Diese Klasse ist thread-safe. GetComparer() kann von mehreren Threads gleichzeitig aufgerufen werden.
/// </para>
/// </remarks>
public sealed class EqualityComparerService : IEqualityComparerService
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initialisiert eine neue Instanz der <see cref="EqualityComparerService"/> Klasse.
    /// </summary>
    /// <param name="serviceProvider">Service-Provider für DI-Auflösung.</param>
    /// <exception cref="ArgumentNullException">Wird ausgelöst, wenn serviceProvider null ist.</exception>
    public EqualityComparerService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <inheritdoc/>
    public IEqualityComparer<T> GetComparer<T>() where T : class
    {
        // 1. Falls T von EntityBase erbt: Verwende ID-Comparer
        if (typeof(EntityBase).IsAssignableFrom(typeof(T)))
        {
            return new EntityIdComparer<T>();
        }

        // 2. Versuche registrierten Comparer zu finden (via Common.Extensions)
        // Die Extension liefert bereits Default-Comparer als Fallback wenn nichts registriert
        var registeredComparer = _serviceProvider.GetEqualityComparer<T>();
        
        return registeredComparer;
    }
}
