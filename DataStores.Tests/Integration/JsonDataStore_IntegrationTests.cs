using DataStores.Abstractions;
using DataStores.Bootstrap;
using DataStores.Persistence;
using DataStores.Runtime;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Xunit;

namespace DataStores.Tests.Integration;

[Trait("Category", "Integration")]
/// <summary>
/// Integration-Tests für JSON-basierte DataStore-Szenarien aus User-Perspektive.
/// Demonstriert die komplette App-Initialisierung mit JSON-Persistierung
/// unter Verwendung der eingebauten JsonFilePersistenceStrategy.
/// </summary>
public class JsonDataStore_IntegrationTests : IDisposable
{
    private readonly string _testDataPath;

    public JsonDataStore_IntegrationTests()
    {
        _testDataPath = Path.Combine(Path.GetTempPath(), $"DataStoresTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDataPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDataPath))
        {
            Directory.Delete(_testDataPath, recursive: true);
        }
    }

    /// <summary>
    /// Szenario: Ein User möchte einen JSON-basierten DataStore für Kunden-DTOs einrichten,
    /// die App initialisieren und zur Laufzeit auf den Store zugreifen.
    /// Verwendet die eingebaute JsonFilePersistenceStrategy.
    /// </summary>
    [Fact]
    public async Task CompleteAppInitialization_WithJsonPersistence_UserScenario()
    {
        // ====================================================================
        // PHASE 1: App-Initialisierung (beim Start der Anwendung)
        // ====================================================================

        // 1. Dependency Injection Container einrichten
        var services = new ServiceCollection();

        // 2. DataStores Core Services registrieren
        services.AddDataStoresCore();

        // 3. JSON-Datei-Pfad definieren
        var jsonFilePath = Path.Combine(_testDataPath, "customers.json");

        // 4. Registrar mit Konfiguration direkt erstellen und registrieren
        // ✅ Keine separate Konfigurationsregistrierung nötig!
        services.AddDataStoreRegistrar(new JsonCustomerDataStoreRegistrar(jsonFilePath));

        // 5. Service Provider erstellen
        var serviceProvider = services.BuildServiceProvider();

        // 6. DataStore Bootstrap ausführen
        // Dies lädt automatisch alle gespeicherten Daten aus JSON-Dateien
        await DataStoreBootstrap.RunAsync(serviceProvider);

        // ====================================================================
        // PHASE 2: Laufzeit - Zugriff auf DataStore durch User/Service
        // ====================================================================

        // 7. IDataStores-Facade aus DI holen
        var dataStores = serviceProvider.GetRequiredService<IDataStores>();

        // 8. Globalen Store für CustomerDto abrufen
        var customerStore = dataStores.GetGlobal<CustomerDto>();

        // 9. Anfangs sollte der Store leer sein (keine persistierten Daten)
        Assert.Empty(customerStore.Items);

        // ====================================================================
        // PHASE 3: Daten hinzufügen und verwenden
        // ====================================================================

        // 10. Neue Kunden erstellen und hinzufügen
        var customer1 = new CustomerDto
        {
            Id = 1,
            FirstName = "Max",
            LastName = "Mustermann",
            Email = "max.mustermann@example.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var customer2 = new CustomerDto
        {
            Id = 2,
            FirstName = "Anna",
            LastName = "Schmidt",
            Email = "anna.schmidt@example.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var customer3 = new CustomerDto
        {
            Id = 3,
            FirstName = "Peter",
            LastName = "Weber",
            Email = "peter.weber@example.com",
            IsActive = false,
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };

        // 11. Einzelnen Kunden hinzufügen
        customerStore.Add(customer1);
        Assert.Single(customerStore.Items);

        // 12. Mehrere Kunden gleichzeitig hinzufügen
        customerStore.AddRange(new[] { customer2, customer3 });
        Assert.Equal(3, customerStore.Items.Count);

        // 13. Auf Changed-Events reagieren (z.B. für UI-Updates)
        var changeNotifications = new List<DataStoreChangeType>();
        customerStore.Changed += (sender, args) =>
        {
            changeNotifications.Add(args.ChangeType);
        };

        // 14. Weitere Operationen
        var customer4 = new CustomerDto
        {
            Id = 4,
            FirstName = "Lisa",
            LastName = "Müller",
            Email = "lisa.mueller@example.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        customerStore.Add(customer4);

        // 15. Kunden suchen/filtern
        var activeCustomers = customerStore.Items
            .Where(c => c.IsActive)
            .ToList();
        Assert.Equal(3, activeCustomers.Count);

        // 16. Kunden entfernen
        var removed = customerStore.Remove(customer3);
        Assert.True(removed);
        Assert.Equal(3, customerStore.Items.Count);

        // 17. Events wurden ausgelöst
        Assert.Equal(2, changeNotifications.Count);
        Assert.Equal(DataStoreChangeType.Add, changeNotifications[0]);
        Assert.Equal(DataStoreChangeType.Remove, changeNotifications[1]);

        // ====================================================================
        // PHASE 4: Persistierung überprüfen
        // ====================================================================

        // 18. Warten auf Auto-Save
        await Task.Delay(200);

        // 19. Prüfen, dass Daten auf Festplatte gespeichert wurden
        Assert.True(File.Exists(jsonFilePath));

        // 20. JSON-Datei manuell lesen und verifizieren
        var jsonContent = await File.ReadAllTextAsync(jsonFilePath);
        var deserializedCustomers = JsonSerializer.Deserialize<List<CustomerDto>>(jsonContent);
        Assert.NotNull(deserializedCustomers);
        Assert.Equal(3, deserializedCustomers.Count);

        // 21. Prüfen, dass die JSON-Datei die aktuellen Daten enthält
        Assert.Contains(deserializedCustomers, c => c.Email == "max.mustermann@example.com");
        Assert.Contains(deserializedCustomers, c => c.Email == "anna.schmidt@example.com");
        Assert.Contains(deserializedCustomers, c => c.Email == "lisa.mueller@example.com");
        
        // 22. Prüfen, dass der entfernte Kunde nicht mehr vorhanden ist
        Assert.DoesNotContain(deserializedCustomers, c => c.Email == "peter.weber@example.com");
    }

    /// <summary>
    /// Szenario: User möchte mit mehreren DTOs arbeiten, die jeweils
    /// in eigenen JSON-Dateien persistiert werden.
    /// Nutzt die vereinfachte Extension-Methode RegisterGlobalWithJsonFile.
    /// </summary>
    [Fact]
    public async Task MultipleDtos_WithSeparateJsonFiles_UsingExtensions_UserScenario()
    {
        // Arrange - App Setup mit Extension-Methoden
        var services = new ServiceCollection();
        services.AddDataStoresCore();
        
        var customerFile = Path.Combine(_testDataPath, "customers.json");
        var productFile = Path.Combine(_testDataPath, "products.json");

        // ✅ Registrar mit Konfiguration direkt erstellen
        services.AddDataStoreRegistrar(new MultiDtoJsonRegistrar(customerFile, productFile));

        var serviceProvider = services.BuildServiceProvider();
        await DataStoreBootstrap.RunAsync(serviceProvider);

        var dataStores = serviceProvider.GetRequiredService<IDataStores>();

        // Act - Verschiedene DTOs verwenden
        var customerStore = dataStores.GetGlobal<CustomerDto>();
        var productStore = dataStores.GetGlobal<ProductDto>();

        customerStore.Add(new CustomerDto
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });

        productStore.AddRange(new[]
        {
            new ProductDto { Id = 1, Name = "Laptop", Price = 999.99m, Stock = 10 },
            new ProductDto { Id = 2, Name = "Mouse", Price = 29.99m, Stock = 50 }
        });

        // Assert
        Assert.Single(customerStore.Items);
        Assert.Equal(2, productStore.Items.Count);

        // Wait for auto-save
        await Task.Delay(200);

        // Verify separate JSON files
        Assert.True(File.Exists(customerFile));
        Assert.True(File.Exists(productFile));
    }

    // ====================================================================
    // Helper Classes - DTOs
    // ====================================================================

    private class CustomerDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    private class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public int Stock { get; set; }
    }

    // ====================================================================
    // Helper Classes - Data Store Registrars
    // ====================================================================

    /// <summary>
    /// Registrar für JSON-persistierte Customer-Stores.
    /// Demonstriert manuelle Registrierung mit Konfiguration im Konstruktor.
    /// ✅ Keine externe Konfigurationsabhängigkeit!
    /// </summary>
    private class JsonCustomerDataStoreRegistrar : IDataStoreRegistrar
    {
        private readonly string _jsonFilePath;

        public JsonCustomerDataStoreRegistrar(string jsonFilePath)
        {
            _jsonFilePath = jsonFilePath;
        }

        public void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider)
        {
            // Variante 1: Manuelle Registrierung mit JsonFilePersistenceStrategy
            var strategy = new JsonFilePersistenceStrategy<CustomerDto>(_jsonFilePath);
            var innerStore = new InMemoryDataStore<CustomerDto>();
            var persistentStore = new PersistentStoreDecorator<CustomerDto>(
                innerStore,
                strategy,
                autoLoad: true,
                autoSaveOnChange: true);

            registry.RegisterGlobal(persistentStore);
        }
    }

    /// <summary>
    /// Registrar für mehrere DTOs mit jeweils eigener JSON-Datei.
    /// Demonstriert vereinfachte Registrierung mit Extension-Methoden.
    /// ✅ Keine externe Konfigurationsabhängigkeit!
    /// </summary>
    private class MultiDtoJsonRegistrar : IDataStoreRegistrar
    {
        private readonly string _customerFile;
        private readonly string _productFile;

        public MultiDtoJsonRegistrar(string customerFile, string productFile)
        {
            _customerFile = customerFile;
            _productFile = productFile;
        }

        public void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider)
        {
            // Variante 2: Vereinfachte Registrierung mit Extension-Methode
            registry
                .RegisterGlobalWithJsonFile<CustomerDto>(_customerFile)
                .RegisterGlobalWithJsonFile<ProductDto>(_productFile);
        }
    }
}
