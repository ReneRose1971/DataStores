using DataStores.Abstractions;
using DataStores.Bootstrap;
using DataStores.Persistence;
using DataStores.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace TestHelper.DataStores.Fixtures;

/// <summary>
/// Shared Fixture für LiteDB-basierte Integration-Tests.
/// Erstellt einen vollständig initialisierten DataStore-Kontext mit LiteDB-Persistierung.
/// </summary>
/// <remarks>
/// Verwendung mit xUnit IClassFixture oder IAsyncLifetime für async Setup.
/// Stellt einen konfigurierten ServiceProvider und IDataStores-Facade bereit.
/// </remarks>
public sealed class LiteDbIntegrationFixture : IAsyncDisposable
{
    private bool _isInitialized;

    /// <summary>
    /// Pfad zur LiteDB-Datenbankdatei.
    /// </summary>
    public string DbPath { get; private set; } = "";

    /// <summary>
    /// Konfigurierter Service Provider mit allen DataStore-Services.
    /// </summary>
    public IServiceProvider ServiceProvider { get; private set; } = null!;

    /// <summary>
    /// DataStores-Facade für Zugriff auf globale und lokale Stores.
    /// </summary>
    public IDataStores DataStores { get; private set; } = null!;

    /// <summary>
    /// Initialisiert die LiteDB-Fixture mit einem spezifischen Registrar.
    /// </summary>
    /// <typeparam name="TRegistrar">Typ des zu verwendenden DataStore-Registrars.</typeparam>
    /// <param name="registrar">Instanz des Registrars mit Konfiguration.</param>
    public async Task InitializeAsync<TRegistrar>(TRegistrar registrar)
        where TRegistrar : IDataStoreRegistrar
    {
        if (_isInitialized)
            throw new InvalidOperationException("Fixture bereits initialisiert.");

        DbPath = Path.Combine(Path.GetTempPath(), $"DataStoresTest_{Guid.NewGuid()}.db");

        var services = new ServiceCollection();
        services.AddDataStoresCore();
        services.AddDataStoreRegistrar(registrar);

        ServiceProvider = services.BuildServiceProvider();
        await DataStoreBootstrap.RunAsync(ServiceProvider);

        DataStores = ServiceProvider.GetRequiredService<IDataStores>();
        _isInitialized = true;
    }

    /// <summary>
    /// Bereinigt Ressourcen und löscht die Testdatenbank.
    /// </summary>
    public ValueTask DisposeAsync()
    {
        if (File.Exists(DbPath))
        {
            try
            {
                File.Delete(DbPath);
            }
            catch
            {
                // Best effort cleanup
            }
        }

        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        return ValueTask.CompletedTask;
    }
}
