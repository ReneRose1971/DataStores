using System.Collections.ObjectModel;

namespace DataStores.Relations;

/// <summary>
/// Service für die Verwaltung dynamischer Eltern-Kind-Beziehungen (1:n und 1:1).
/// Überwacht automatisch Änderungen in Child-Stores und PropertyChanged-Events in Child-Entities.
/// </summary>
/// <typeparam name="TParent">Der Parent-Entity-Typ (die "1"-Seite).</typeparam>
/// <typeparam name="TChild">Der Child-Entity-Typ (die "n"-Seite). Muss INotifyPropertyChanged implementieren für dynamisches Tracking.</typeparam>
/// <typeparam name="TKey">Der Schlüssel-Typ für das Matching.</typeparam>
/// <remarks>
/// <para>
/// Dieser Service verwaltet 1:n und 1:1 Beziehungen zwischen Parent- und Child-Entities.
/// Er reagiert automatisch auf:
/// </para>
/// <list type="bullet">
/// <item><description>Änderungen im Child-DataStore (Add, Remove, Clear)</description></item>
/// <item><description>PropertyChanged-Events in Child-Entities (z.B. Änderung des Foreign-Keys)</description></item>
/// </list>
/// <para>
/// Die View-Objekte (<see cref="OneToManyRelationView{TParent, TChild}"/> und 
/// <see cref="OneToOneRelationView{TParent, TChild}"/>) werden gecacht
/// und aktualisieren sich automatisch durch ObservableCollection.
/// </para>
/// </remarks>
public interface IRelationViewService<TParent, TChild, TKey> : IDisposable
    where TParent : class
    where TChild : class
    where TKey : notnull
{
    /// <summary>
    /// Ruft eine 1:n-Beziehungsansicht für den angegebenen Parent ab oder erstellt sie.
    /// </summary>
    /// <param name="parent">Die Parent-Entity.</param>
    /// <returns>Eine View der 1:n-Beziehung.</returns>
    /// <exception cref="ArgumentNullException">Wird ausgelöst, wenn <paramref name="parent"/> null ist.</exception>
    /// <remarks>
    /// <para>
    /// Diese Methode ist idempotent - wiederholte Aufrufe mit demselben Parent
    /// liefern dieselbe View-Instanz (gecacht).
    /// </para>
    /// <para>
    /// Die zurückgegebene View enthält eine <see cref="ReadOnlyObservableCollection{T}"/>,

    /// die sich automatisch aktualisiert, wenn sich die zugehörigen Children ändern.
    /// </para>
    /// </remarks>
    OneToManyRelationView<TParent, TChild> GetOneToManyRelation(TParent parent);

    /// <summary>
    /// Ruft eine 1:1-Beziehungsansicht für den angegebenen Parent ab oder erstellt sie.
    /// </summary>
    /// <param name="parent">Die Parent-Entity.</param>
    /// <param name="policy">
    /// Die Policy, die angibt, wie mit mehreren Children umgegangen werden soll.
    /// Standard ist <see cref="MultipleChildrenPolicy.ThrowIfMultiple"/>.
    /// </param>
    /// <returns>Eine View der 1:1-Beziehung.</returns>
    /// <exception cref="ArgumentNullException">Wird ausgelöst, wenn <paramref name="parent"/> null ist.</exception>
    /// <remarks>
    /// <para>
    /// Diese Methode erstellt eine 1:1-View über die zugrundeliegende 1:n-Beziehung.
    /// Die View aktualisiert sich automatisch, wenn sich Children ändern.
    /// </para>
    /// <para>
    /// <b>Wichtig:</b> Die View ist NICHT gecacht - jeder Aufruf erstellt eine neue Instanz.
    /// Dies ist bewusst so gewählt, da verschiedene Policies benötigt werden könnten.
    /// </para>
    /// <para>
    /// <b>Policies:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><description><see cref="MultipleChildrenPolicy.ThrowIfMultiple"/> - Wirft Exception bei mehreren Children</description></item>
    /// <item><description><see cref="MultipleChildrenPolicy.TakeFirst"/> - Nimmt das erste Child</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // 1:1 Beziehung mit Exception bei mehreren Children
    /// var oneToOne = service.GetOneToOneRelation(department);
    /// var manager = oneToOne.ChildOrNull; // Wirft Exception bei > 1 Children
    /// 
    /// // 1:1 Beziehung mit TakeFirst-Policy
    /// var oneToOne = service.GetOneToOneRelation(department, MultipleChildrenPolicy.TakeFirst);
    /// var primaryContact = oneToOne.ChildOrNull; // Nimmt erstes Child
    /// </code>
    /// </example>
    OneToOneRelationView<TParent, TChild> GetOneToOneRelation(
        TParent parent,
        MultipleChildrenPolicy policy = MultipleChildrenPolicy.ThrowIfMultiple);

    /// <summary>
    /// Ruft die schreibgeschützte Collection der Children für den angegebenen Parent ab.
    /// </summary>
    /// <param name="parent">Die Parent-Entity.</param>
    /// <returns>Eine schreibgeschützte Observable-Collection der Children.</returns>
    /// <exception cref="ArgumentNullException">Wird ausgelöst, wenn <paramref name="parent"/> null ist.</exception>
    /// <remarks>
    /// Dies ist eine Convenience-Methode, die <c>GetOneToManyRelation(parent).Children</c> zurückgibt.
    /// </remarks>
    ReadOnlyObservableCollection<TChild> GetChildren(TParent parent);
}
