using DataStores.Abstractions;
using DataStores.Bootstrap;
using DataStores.Persistence;
using DataStores.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DataStores.Tests.Integration;

[Trait("Category", "Integration")]
/// <summary>
/// Integration-Tests für LiteDB ID-Handling und EntityBase-Funktionalität.
/// Prüft, dass neue Entitäten automatisch IDs von LiteDB erhalten.
/// </summary>
public class LiteDbDataStore_IdHandling_IntegrationTests : IDisposable
{
    private readonly string _testDbPath;

    public LiteDbDataStore_IdHandling_IntegrationTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"DataStoresIdTest_{Guid.NewGuid()}.db");
    }

    public void Dispose()
    {
        if (File.Exists(_testDbPath))
        {
            File.Delete(_testDbPath);
        }
    }

    /// <summary>
    /// Szenario: Neue Entitäten (Id = 0) werden eingefügt und erhalten automatisch IDs von LiteDB.
    /// </summary>
    [Fact]
    public async Task NewEntities_Should_GetIdFromLiteDb_AfterPersistence()
    {
        // Arrange - App Setup
        var services = new ServiceCollection();
        var module = new DataStoresServiceModule();
        module.Register(services);
        services.AddDataStoreRegistrar(new ProductDataStoreRegistrar(_testDbPath));

        var serviceProvider = services.BuildServiceProvider();
        await DataStoreBootstrap.RunAsync(serviceProvider);

        var dataStores = serviceProvider.GetRequiredService<IDataStores>();
        var productStore = dataStores.GetGlobal<Product>();

        // Act - Neue Produkte mit Id = 0 hinzufügen
        var product1 = new Product
        {
            Id = 0,
            Name = "Laptop",
            Price = 1299.99m
        };

        var product2 = new Product
        {
            Id = 0,
            Name = "Mouse",
            Price = 29.99m
        };

        productStore.Add(product1);
        productStore.AddRange(new[] { product2 });

        // Wait for auto-save
        await Task.Delay(200);

        // Assert - Produkte wurden gespeichert
        Assert.Equal(2, productStore.Items.Count);

        // Direkte Verifikation: IDs wurden von LiteDB vergeben
        var strategy = new LiteDbPersistenceStrategy<Product>(_testDbPath, "products");
        var savedProducts = await strategy.LoadAllAsync();

        Assert.Equal(2, savedProducts.Count);
        
        // Alle gespeicherten Produkte sollten IDs > 0 haben
        Assert.All(savedProducts, p => Assert.True(p.Id > 0, $"Product {p.Name} should have Id > 0, but has {p.Id}"));
        
        // IDs sollten eindeutig sein
        var ids = savedProducts.Select(p => p.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    /// <summary>
    /// Szenario: Entitäten mit Id != 0 werden beim Speichern still ignoriert.
    /// </summary>
    [Fact]
    public async Task EntitiesWithNonZeroId_Should_BeIgnored_DuringSave()
    {
        // Arrange
        var services = new ServiceCollection();
        var module = new DataStoresServiceModule();
        module.Register(services);
        services.AddDataStoreRegistrar(new ProductDataStoreRegistrar(_testDbPath));

        var serviceProvider = services.BuildServiceProvider();
        await DataStoreBootstrap.RunAsync(serviceProvider);

        var dataStores = serviceProvider.GetRequiredService<IDataStores>();
        var productStore = dataStores.GetGlobal<Product>();

        // Act - Gemischte IDs hinzufügen
        var newProduct = new Product { Id = 0, Name = "New Product", Price = 100m };
        var existingProduct = new Product { Id = 999, Name = "Existing Product", Price = 200m };

        productStore.Add(newProduct);
        productStore.Add(existingProduct);

        // Im Store sind beide vorhanden
        Assert.Equal(2, productStore.Items.Count);

        // Wait for auto-save
        await Task.Delay(200);

        // Assert - Nur das neue Produkt wurde gespeichert
        var strategy = new LiteDbPersistenceStrategy<Product>(_testDbPath, "products");
        var savedProducts = await strategy.LoadAllAsync();

        Assert.Single(savedProducts);
        Assert.Equal("New Product", savedProducts[0].Name);
        Assert.True(savedProducts[0].Id > 0);
    }

    /// <summary>
    /// Szenario: EntityBase ToString, Equals, GetHashCode werden korrekt implementiert.
    /// </summary>
    [Fact]
    public void Product_Should_ImplementEntityBase_Correctly()
    {
        // Arrange
        var product1 = new Product { Id = 1, Name = "Laptop", Price = 1000m };
        var product2 = new Product { Id = 1, Name = "Different", Price = 500m };
        var product3 = new Product { Id = 2, Name = "Laptop", Price = 1000m };
        var newProduct = new Product { Id = 0, Name = "New", Price = 100m };

        // Act & Assert - ToString
        var str = product1.ToString();
        Assert.Contains("Product", str);
        Assert.Contains("1", str);
        Assert.Contains("Laptop", str);

        // Act & Assert - Equals (basierend auf ID für Id > 0)
        Assert.True(product1.Equals(product2)); // Gleiche ID
        Assert.False(product1.Equals(product3)); // Verschiedene ID
        Assert.False(product1.Equals(newProduct)); // Eine ist neu (Id=0)

        // Act & Assert - GetHashCode (konsistent mit Equals)
        Assert.Equal(product1.GetHashCode(), product2.GetHashCode());
        Assert.NotEqual(product1.GetHashCode(), product3.GetHashCode());
    }

    /// <summary>
    /// Szenario: Nach dem Laden aus LiteDB haben alle Entities IDs > 0.
    /// Verwendet explizites Save statt autoSaveOnChange um Task.Delay zu vermeiden.
    /// </summary>
    [Fact]
    public async Task AfterLoadFromLiteDb_AllEntities_Should_HavePositiveIds()
    {
        // Arrange - Setup mit EXPLIZITEM Save (kein autoSaveOnChange!)
        var strategy = new LiteDbPersistenceStrategy<Product>(_testDbPath, "products");

        var products = new[]
        {
            new Product { Id = 0, Name = "A", Price = 10m },
            new Product { Id = 0, Name = "B", Price = 20m },
            new Product { Id = 0, Name = "C", Price = 30m }
        };

        // Act - Explizit speichern
        await strategy.SaveAllAsync(products);
        
        // Assert - IDs wurden zurückgeschrieben
        Assert.All(products, p => Assert.True(p.Id > 0));
        
        // Assert - Daten sind in DB
        var savedProducts = await strategy.LoadAllAsync();
        Assert.Equal(3, savedProducts.Count);

        // Act - Neue App-Instanz mit autoLoad
        var services = new ServiceCollection();
        var module = new DataStoresServiceModule();
        module.Register(services);
        services.AddDataStoreRegistrar(new ProductDataStoreRegistrar(_testDbPath));

        var serviceProvider = services.BuildServiceProvider();
        await DataStoreBootstrap.RunAsync(serviceProvider);

        var dataStores = serviceProvider.GetRequiredService<IDataStores>();
        var loadedStore = dataStores.GetGlobal<Product>();

        // Assert - Alle geladenen Produkte haben IDs > 0
        Assert.Equal(3, loadedStore.Items.Count);
        Assert.All(loadedStore.Items, p => Assert.True(p.Id > 0));
    }

    /// <summary>
    /// Szenario: Nur Id = 0 Items werden gespeichert - keine Exception bei Id != 0.
    /// </summary>
    [Fact]
    public async Task SaveWithNonZeroIds_Should_NotThrow_JustIgnoreThem()
    {
        // Arrange
        var strategy = new LiteDbPersistenceStrategy<Product>(_testDbPath, "products");

        var products = new[]
        {
            new Product { Id = 0, Name = "New 1", Price = 10m },
            new Product { Id = 99, Name = "Existing", Price = 20m },
            new Product { Id = 0, Name = "New 2", Price = 30m }
        };

        // Act - Sollte keine Exception werfen
        await strategy.SaveAllAsync(products);

        // Assert - Nur die 2 mit Id = 0 wurden gespeichert
        var loaded = await strategy.LoadAllAsync();
        Assert.Equal(2, loaded.Count);
        Assert.All(loaded, p => Assert.True(p.Id > 0));
        Assert.Contains(loaded, p => p.Name == "New 1");
        Assert.Contains(loaded, p => p.Name == "New 2");
        Assert.DoesNotContain(loaded, p => p.Name == "Existing");
    }

    // ====================================================================
    // Helper Classes
    // ====================================================================

    /// <summary>
    /// Beispiel-Produkt-Entität basierend auf EntityBase.
    /// </summary>
    private class Product : EntityBase
    {
        public string Name { get; set; } = "";
        public decimal Price { get; set; }

        public override string ToString() => $"Product #{Id}: {Name} ({Price:C})";

        public override bool Equals(object? obj)
        {
            if (obj is not Product other)
                return false;

            // Für persistierte Entitäten: Vergleich nach ID
            if (Id > 0 && other.Id > 0)
                return Id == other.Id;

            // Für neue Entitäten: Referenzvergleich
            return ReferenceEquals(this, other);
        }

        public override int GetHashCode()
        {
            // Für persistierte Entitäten: Hash der ID
            if (Id > 0)
                return Id.GetHashCode();

            // Für neue Entitäten: Hash aus Properties
            return HashCode.Combine(Name, Price);
        }
    }

    /// <summary>
    /// Registrar für Product-Store mit LiteDB-Persistierung.
    /// </summary>
    private class ProductDataStoreRegistrar : IDataStoreRegistrar
    {
        private readonly string _databasePath;

        public ProductDataStoreRegistrar(string databasePath)
        {
            _databasePath = databasePath;
        }

        public void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider)
        {
            var strategy = new LiteDbPersistenceStrategy<Product>(
                _databasePath,
                collectionName: "products");

            var innerStore = new InMemoryDataStore<Product>();
            var persistentStore = new PersistentStoreDecorator<Product>(
                innerStore,
                strategy,
                autoLoad: true,
                autoSaveOnChange: true);

            registry.RegisterGlobal(persistentStore);
        }
    }
}
