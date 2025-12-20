using DataStores.Abstractions;
using DataStores.Bootstrap;
using DataStores.Persistence;
using DataStores.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DataStores.Tests.Integration;

[Trait("Category", "Integration")]
/// <summary>
/// Integration-Tests für LiteDB-basierte DataStore-Szenarien aus User-Perspektive.
/// Demonstriert die komplette App-Initialisierung mit LiteDB-Persistierung
/// unter Verwendung der eingebauten LiteDbPersistenceStrategy.
/// </summary>
public class LiteDbDataStore_IntegrationTests : IDisposable
{
    private readonly string _testDbPath;

    public LiteDbDataStore_IntegrationTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"DataStoresTest_{Guid.NewGuid()}.db");
    }

    public void Dispose()
    {
        if (File.Exists(_testDbPath))
        {
            File.Delete(_testDbPath);
        }
    }

    /// <summary>
    /// Szenario: Ein User möchte einen LiteDB-basierten DataStore einrichten,
    /// die App initialisieren und zur Laufzeit auf den Store zugreifen.
    /// Verwendet die eingebaute LiteDbPersistenceStrategy.
    /// </summary>
    [Fact]
    public async Task CompleteAppInitialization_WithLiteDbPersistence_UserScenario()
    {
        // ====================================================================
        // PHASE 1: App-Initialisierung (beim Start der Anwendung)
        // ====================================================================

        // 1. Dependency Injection Container einrichten
        var services = new ServiceCollection();

        // 2. DataStores Core Services registrieren
        services.AddDataStoresCore();

        // 3. Registrar mit Konfiguration direkt erstellen und registrieren
        // ✅ Keine separate Konfigurationsregistrierung nötig!
        services.AddDataStoreRegistrar(new LiteDbOrderDataStoreRegistrar(_testDbPath));

        // 4. Service Provider erstellen
        var serviceProvider = services.BuildServiceProvider();

        // 5. DataStore Bootstrap ausführen
        await DataStoreBootstrap.RunAsync(serviceProvider);

        // ====================================================================
        // PHASE 2: Laufzeit - Zugriff auf DataStore durch User/Service
        // ====================================================================

        // 6. IDataStores-Facade aus DI holen
        var dataStores = serviceProvider.GetRequiredService<IDataStores>();

        // 7. Globalen Store für OrderDto abrufen
        var orderStore = dataStores.GetGlobal<OrderDto>();

        // 8. Anfangs sollte der Store leer sein
        Assert.Empty(orderStore.Items);

        // ====================================================================
        // PHASE 3: Business Logic - Bestellungen verwalten
        // ====================================================================

        // 9. Neue Bestellungen erstellen
        var order1 = new OrderDto
        {
            Id = 1,
            OrderNumber = "ORD-2024-001",
            CustomerId = 100,
            CustomerName = "Tech GmbH",
            TotalAmount = 1599.99m,
            Status = OrderStatus.Pending,
            OrderDate = DateTime.UtcNow,
            Items = new List<string> { "Laptop", "Mouse", "Keyboard" }
        };

        var order2 = new OrderDto
        {
            Id = 2,
            OrderNumber = "ORD-2024-002",
            CustomerId = 101,
            CustomerName = "Software AG",
            TotalAmount = 2999.99m,
            Status = OrderStatus.Processing,
            OrderDate = DateTime.UtcNow.AddHours(-2),
            Items = new List<string> { "Server", "Monitor x2" }
        };

        var order3 = new OrderDto
        {
            Id = 3,
            OrderNumber = "ORD-2024-003",
            CustomerId = 100,
            CustomerName = "Tech GmbH",
            TotalAmount = 499.99m,
            Status = OrderStatus.Shipped,
            OrderDate = DateTime.UtcNow.AddDays(-1),
            Items = new List<string> { "USB-C Cable", "Adapter" }
        };

        // 10. Bestellungen zum Store hinzufügen
        orderStore.Add(order1);
        orderStore.AddRange(new[] { order2, order3 });

        Assert.Equal(3, orderStore.Items.Count);

        // 11. Geschäftslogik: Bestellungen nach Kunde filtern
        var techGmbHOrders = orderStore.Items
            .Where(o => o.CustomerId == 100)
            .ToList();
        Assert.Equal(2, techGmbHOrders.Count);

        // 12. Geschäftslogik: Bestellungen nach Status gruppieren
        var ordersByStatus = orderStore.Items
            .GroupBy(o => o.Status)
            .ToDictionary(g => g.Key, g => g.ToList());

        Assert.Single(ordersByStatus[OrderStatus.Pending]);
        Assert.Single(ordersByStatus[OrderStatus.Processing]);
        Assert.Single(ordersByStatus[OrderStatus.Shipped]);

        // 13. Geschäftslogik: Gesamtumsatz berechnen
        var totalRevenue = orderStore.Items.Sum(o => o.TotalAmount);
        Assert.Equal(5099.97m, totalRevenue);

        // ====================================================================
        // PHASE 4: Datenänderungen
        // ====================================================================

        // 14. Event-Tracking für Änderungen
        var changeLog = new List<string>();
        orderStore.Changed += (sender, args) =>
        {
            changeLog.Add($"{args.ChangeType}: {args.AffectedItems.Count} items");
        };

        // 15. Bestellstatus aktualisieren (simuliert durch Entfernen/Hinzufügen)
        var orderToUpdate = orderStore.Items.First(o => o.Id == 1);
        orderStore.Remove(orderToUpdate);
        
        var updatedOrder = new OrderDto
        {
            Id = orderToUpdate.Id,
            OrderNumber = orderToUpdate.OrderNumber,
            CustomerId = orderToUpdate.CustomerId,
            CustomerName = orderToUpdate.CustomerName,
            TotalAmount = orderToUpdate.TotalAmount,
            Status = OrderStatus.Processing,
            OrderDate = orderToUpdate.OrderDate,
            Items = orderToUpdate.Items
        };
        orderStore.Add(updatedOrder);

        // 16. Bestellung stornieren (entfernen)
        var orderToCancel = orderStore.Items.First(o => o.Id == 3);
        orderStore.Remove(orderToCancel);

        Assert.Equal(2, orderStore.Items.Count);
        Assert.Equal(3, changeLog.Count);

        // ====================================================================
        // PHASE 5: Persistierung überprüfen
        // ====================================================================

        // 17. Warten auf Auto-Save
        await Task.Delay(200);

        // 18. Prüfen, dass LiteDB-Datei erstellt wurde
        Assert.True(File.Exists(_testDbPath));
        Assert.True(new FileInfo(_testDbPath).Length > 0);

        // 19. Direkte Verifikation der gespeicherten Daten
        var strategy = new LiteDbPersistenceStrategy<OrderDto>(_testDbPath, "orders");
        var savedOrders = await strategy.LoadAllAsync();
        
        Assert.Equal(2, savedOrders.Count);
        
        var savedOrder1 = savedOrders.FirstOrDefault(o => o.Id == 1);
        if (savedOrder1 != null)
        {
            Assert.Equal(OrderStatus.Processing, savedOrder1.Status);
        }
        
        Assert.DoesNotContain(savedOrders, o => o.Id == 3);
    }

    /// <summary>
    /// Szenario: Mehrere Entity-Typen in derselben LiteDB-Datenbank.
    /// Verwendet die vereinfachte Extension-Methode RegisterGlobalWithLiteDb.
    /// </summary>
    [Fact]
    public async Task MultipleEntities_InSameLiteDb_UsingExtensions_UserScenario()
    {
        // Arrange - App Setup mit Extension-Methoden
        var services = new ServiceCollection();
        services.AddDataStoresCore();
        
        // ✅ Registrar mit Konfiguration direkt erstellen
        services.AddDataStoreRegistrar(new MultiEntityLiteDbRegistrar(_testDbPath));

        var serviceProvider = services.BuildServiceProvider();
        await DataStoreBootstrap.RunAsync(serviceProvider);

        var dataStores = serviceProvider.GetRequiredService<IDataStores>();

        // Act - Mit verschiedenen Entities arbeiten
        var orderStore = dataStores.GetGlobal<OrderDto>();
        var invoiceStore = dataStores.GetGlobal<InvoiceDto>();

        orderStore.Add(new OrderDto
        {
            Id = 1,
            OrderNumber = "ORD-001",
            CustomerId = 100,
            CustomerName = "Kunde A",
            TotalAmount = 1000m,
            Status = OrderStatus.Completed,
            OrderDate = DateTime.UtcNow,
            Items = new List<string> { "Produkt 1" }
        });

        invoiceStore.AddRange(new[]
        {
            new InvoiceDto { Id = 1, InvoiceNumber = "INV-001", OrderId = 1, Amount = 1000m, IsPaid = true },
            new InvoiceDto { Id = 2, InvoiceNumber = "INV-002", OrderId = 1, Amount = 500m, IsPaid = false }
        });

        // Assert
        Assert.Single(orderStore.Items);
        Assert.Equal(2, invoiceStore.Items.Count);

        // Wait for auto-save
        await Task.Delay(200);

        // Verify data was saved
        var orderStrategy = new LiteDbPersistenceStrategy<OrderDto>(_testDbPath, "orders");
        var invoiceStrategy = new LiteDbPersistenceStrategy<InvoiceDto>(_testDbPath, "invoices");

        var savedOrders = await orderStrategy.LoadAllAsync();
        var savedInvoices = await invoiceStrategy.LoadAllAsync();

        Assert.Single(savedOrders);
        Assert.Equal(2, savedInvoices.Count);
    }

    // ====================================================================
    // Helper Classes - DTOs
    // ====================================================================

    private class OrderDto
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = "";
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = "";
        public decimal TotalAmount { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime OrderDate { get; set; }
        public List<string> Items { get; set; } = new();
    }

    private class InvoiceDto
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = "";
        public int OrderId { get; set; }
        public decimal Amount { get; set; }
        public bool IsPaid { get; set; }
    }

    private enum OrderStatus
    {
        Pending,
        Processing,
        Shipped,
        Completed,
        Cancelled
    }

    // ====================================================================
    // Helper Classes - Data Store Registrars
    // ====================================================================

    /// <summary>
    /// Registrar für LiteDB-persistierte Order-Stores.
    /// Demonstriert manuelle Registrierung mit Konfiguration im Konstruktor.
    /// ✅ Keine externe Konfigurationsabhängigkeit!
    /// </summary>
    private class LiteDbOrderDataStoreRegistrar : IDataStoreRegistrar
    {
        private readonly string _databasePath;

        public LiteDbOrderDataStoreRegistrar(string databasePath)
        {
            _databasePath = databasePath;
        }

        public void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider)
        {
            // Variante 1: Manuelle Registrierung mit LiteDbPersistenceStrategy
            var strategy = new LiteDbPersistenceStrategy<OrderDto>(
                _databasePath,
                collectionName: "orders");

            var innerStore = new InMemoryDataStore<OrderDto>();
            var persistentStore = new PersistentStoreDecorator<OrderDto>(
                innerStore,
                strategy,
                autoLoad: true,
                autoSaveOnChange: true);

            registry.RegisterGlobal(persistentStore);
        }
    }

    /// <summary>
    /// Registrar für mehrere Entity-Typen in derselben LiteDB.
    /// Demonstriert vereinfachte Registrierung mit Extension-Methoden.
    /// ✅ Keine externe Konfigurationsabhängigkeit!
    /// </summary>
    private class MultiEntityLiteDbRegistrar : IDataStoreRegistrar
    {
        private readonly string _databasePath;

        public MultiEntityLiteDbRegistrar(string databasePath)
        {
            _databasePath = databasePath;
        }

        public void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider)
        {
            // Variante 2: Vereinfachte Registrierung mit Extension-Methode
            registry
                .RegisterGlobalWithLiteDb<OrderDto>(_databasePath, "orders")
                .RegisterGlobalWithLiteDb<InvoiceDto>(_databasePath, "invoices");
        }
    }
}
