using DataStores.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace DataStores.Relations;

/// <summary>
/// Service-Modul für die Registrierung von Relation-View-Services.
/// Implementiert das IServiceModule-Pattern für strukturierte Service-Registrierung.
/// </summary>
/// <remarks>
/// <para>
/// Dieses Modul ermöglicht die typsichere Registrierung von <see cref="IRelationViewService{TParent, TChild, TKey}"/>
/// Services im Dependency Injection Container.
/// </para>
/// <para>
/// <b>Verwendung:</b>
/// </para>
/// <code>
/// services.AddRelationViewService&lt;Group, Member, Guid&gt;(
///     parentStore =&gt; parentStore.Id,
///     childStore =&gt; childStore.GroupId);
/// </code>
/// </remarks>
public static class RelationServiceModule
{
    /// <summary>
    /// Registriert einen RelationViewService für eine 1:n-Beziehung.
    /// </summary>
    /// <typeparam name="TParent">Der Parent-Entity-Typ.</typeparam>
    /// <typeparam name="TChild">Der Child-Entity-Typ.</typeparam>
    /// <typeparam name="TKey">Der Schlüssel-Typ.</typeparam>
    /// <param name="services">Die Service-Collection.</param>
    /// <param name="getParentKey">Funktion zur Extraktion des Parent-Schlüssels.</param>
    /// <param name="getChildKey">Funktion zur Extraktion des Child-Schlüssels.</param>
    /// <param name="childComparer">Optionaler Comparer für die Sortierung der Children.</param>
    /// <returns>Die Service-Collection für Fluent-API.</returns>
    public static IServiceCollection AddRelationViewService<TParent, TChild, TKey>(
        this IServiceCollection services,
        Func<TParent, TKey> getParentKey,
        Func<TChild, TKey> getChildKey,
        IComparer<TChild>? childComparer = null)
        where TParent : class
        where TChild : class
        where TKey : notnull
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }
        
        if (getParentKey == null)
        {
            throw new ArgumentNullException(nameof(getParentKey));
        }
        
        if (getChildKey == null)
        {
            throw new ArgumentNullException(nameof(getChildKey));
        }

        services.AddSingleton<IRelationViewService<TParent, TChild, TKey>>(provider =>
        {
            var parentStore = provider.GetRequiredService<IDataStores>().GetGlobal<TParent>();
            var childStore = provider.GetRequiredService<IDataStores>().GetGlobal<TChild>();
            
            var definition = new RelationDefinition<TParent, TChild, TKey>(
                getParentKey,
                getChildKey,
                childComparer);

            return new RelationViewService<TParent, TChild, TKey>(
                parentStore,
                childStore,
                definition);
        });

        return services;
    }

    /// <summary>
    /// Registriert einen RelationViewService mit expliziter RelationDefinition.
    /// </summary>
    /// <typeparam name="TParent">Der Parent-Entity-Typ.</typeparam>
    /// <typeparam name="TChild">Der Child-Entity-Typ.</typeparam>
    /// <typeparam name="TKey">Der Schlüssel-Typ.</typeparam>
    /// <param name="services">Die Service-Collection.</param>
    /// <param name="definition">Die Relation-Definition.</param>
    /// <returns>Die Service-Collection für Fluent-API.</returns>
    public static IServiceCollection AddRelationViewService<TParent, TChild, TKey>(
        this IServiceCollection services,
        RelationDefinition<TParent, TChild, TKey> definition)
        where TParent : class
        where TChild : class
        where TKey : notnull
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }
        
        if (definition == null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        services.AddSingleton<IRelationViewService<TParent, TChild, TKey>>(provider =>
        {
            var parentStore = provider.GetRequiredService<IDataStores>().GetGlobal<TParent>();
            var childStore = provider.GetRequiredService<IDataStores>().GetGlobal<TChild>();

            return new RelationViewService<TParent, TChild, TKey>(
                parentStore,
                childStore,
                definition);
        });

        return services;
    }
}
