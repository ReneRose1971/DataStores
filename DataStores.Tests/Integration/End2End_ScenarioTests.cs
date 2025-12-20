using DataStores.Abstractions;
using DataStores.Bootstrap;
using DataStores.Persistence;
using DataStores.Relations;
using DataStores.Runtime;
using Microsoft.Extensions.DependencyInjection;
using TestHelper.DataStores.Persistence;

namespace DataStores.Tests.Integration;

/// <summary>
/// End-to-end integration tests covering complete scenarios.
/// </summary>
public class End2End_ScenarioTests
{
    [Fact]
    public async Task CompleteScenario_WithPersistenceAndRelations()
    {
        // Arrange - Setup DI
        var services = new ServiceCollection();
        services.AddDataStoresCore();
        services.AddSingleton<IDataStoreRegistrar, TestDataRegistrar>();

        var provider = services.BuildServiceProvider();
        await DataStoreBootstrap.RunAsync(provider);

        var stores = provider.GetRequiredService<IDataStores>();

        // Act - Use global stores
        var customerStore = stores.GetGlobal<Customer>();
        var orderStore = stores.GetGlobal<Order>();

        customerStore.Add(new Customer { Id = 1, Name = "John Doe" });
        customerStore.Add(new Customer { Id = 2, Name = "Jane Smith" });

        orderStore.Add(new Order { Id = 101, CustomerId = 1, Total = 100.50m });
        orderStore.Add(new Order { Id = 102, CustomerId = 1, Total = 200.75m });
        orderStore.Add(new Order { Id = 103, CustomerId = 2, Total = 50.00m });

        // Create parent-child relationship
        var customer1 = customerStore.Items[0];
        var relationship = new ParentChildRelationship<Customer, Order>(
            stores,
            customer1,
            (c, o) => o.CustomerId == c.Id);

        relationship.UseGlobalDataSource();
        relationship.Refresh();

        // Assert
        Assert.Equal(2, customerStore.Items.Count);
        Assert.Equal(3, orderStore.Items.Count);
        Assert.Equal(2, relationship.Childs.Items.Count);
        Assert.All(relationship.Childs.Items, o => Assert.Equal(1, o.CustomerId));
    }

    [Fact]
    public async Task LocalSnapshot_Workflow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDataStoresCore();
        services.AddSingleton<IDataStoreRegistrar, TestDataRegistrar>();

        var provider = services.BuildServiceProvider();
        await DataStoreBootstrap.RunAsync(provider);

        var stores = provider.GetRequiredService<IDataStores>();
        var globalStore = stores.GetGlobal<Product>();

        // Add global products
        globalStore.AddRange(new[]
        {
            new Product { Id = 1, Name = "Laptop", Price = 999.99m, InStock = true },
            new Product { Id = 2, Name = "Mouse", Price = 29.99m, InStock = true },
            new Product { Id = 3, Name = "Keyboard", Price = 79.99m, InStock = false }
        });

        // Act - Create local snapshot with filter
        var inStockSnapshot = stores.CreateLocalSnapshotFromGlobal<Product>(
            p => p.InStock && p.Price > 50);

        // Modify snapshot locally
        inStockSnapshot.Add(new Product { Id = 99, Name = "Local Only", Price = 500m, InStock = true });

        // Assert
        Assert.Single(inStockSnapshot.Items.Where(p => p.Id < 99)); // Only Laptop
        Assert.Equal(2, inStockSnapshot.Items.Count); // Laptop + Local Only
        Assert.Equal(3, globalStore.Items.Count); // Global unchanged
    }

    [Fact]
    public async Task MultipleRegistrars_Integration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDataStoresCore();
        services.AddSingleton<IDataStoreRegistrar, CustomerRegistrar>();
        services.AddSingleton<IDataStoreRegistrar, ProductRegistrar>();

        var provider = services.BuildServiceProvider();
        await DataStoreBootstrap.RunAsync(provider);

        var stores = provider.GetRequiredService<IDataStores>();

        // Act
        var customerStore = stores.GetGlobal<Customer>();
        var productStore = stores.GetGlobal<Product>();

        // Assert - Both registrars executed
        Assert.NotNull(customerStore);
        Assert.NotNull(productStore);
    }

    [Fact]
    public async Task PersistentStore_LoadSaveCycle()
    {
        // Arrange
        var strategy = new FakePersistenceStrategy<Customer>(new[]
        {
            new Customer { Id = 1, Name = "Persisted Customer" }
        });

        var innerStore = new InMemoryDataStore<Customer>();
        var decorator = new PersistentStoreDecorator<Customer>(
            innerStore, strategy, autoLoad: true, autoSaveOnChange: true);

        // Act - Initialize (loads data)
        await decorator.InitializeAsync();

        // Modify
        decorator.Add(new Customer { Id = 2, Name = "New Customer" });
        await Task.Delay(100); // Wait for async save

        // Assert
        Assert.Equal(2, decorator.Items.Count);
        Assert.Equal(1, strategy.LoadCallCount);
        Assert.True(strategy.SaveCallCount > 0);
        Assert.Equal(2, strategy.LastSavedItems?.Count);
    }

    [Fact]
    public void MultipleLocalStores_Independence()
    {
        // Arrange
        var registry = new GlobalStoreRegistry();
        var factory = new LocalDataStoreFactory();
        var stores = new DataStoresFacade(registry, factory);

        // Act - Create multiple local stores
        var local1 = stores.CreateLocal<Customer>();
        var local2 = stores.CreateLocal<Customer>();
        var local3 = stores.CreateLocal<Customer>();

        local1.Add(new Customer { Id = 1, Name = "Local1" });
        local2.Add(new Customer { Id = 2, Name = "Local2" });
        local3.Add(new Customer { Id = 3, Name = "Local3" });

        // Assert - Completely independent
        Assert.Single(local1.Items);
        Assert.Single(local2.Items);
        Assert.Single(local3.Items);
        Assert.NotEqual(local1.Items[0].Id, local2.Items[0].Id);
    }

    [Fact]
    public async Task ComplexHierarchy_ParentChildGrandchild()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDataStoresCore();
        services.AddSingleton<IDataStoreRegistrar, HierarchyRegistrar>();

        var provider = services.BuildServiceProvider();
        await DataStoreBootstrap.RunAsync(provider);

        var stores = provider.GetRequiredService<IDataStores>();

        // Setup data
        var customerStore = stores.GetGlobal<Customer>();
        var orderStore = stores.GetGlobal<Order>();
        var itemStore = stores.GetGlobal<OrderItem>();

        customerStore.Add(new Customer { Id = 1, Name = "Customer1" });
        
        orderStore.AddRange(new[]
        {
            new Order { Id = 101, CustomerId = 1, Total = 0 },
            new Order { Id = 102, CustomerId = 1, Total = 0 }
        });

        itemStore.AddRange(new[]
        {
            new OrderItem { Id = 1001, OrderId = 101, ProductName = "Item1", Price = 50 },
            new OrderItem { Id = 1002, OrderId = 101, ProductName = "Item2", Price = 75 },
            new OrderItem { Id = 1003, OrderId = 102, ProductName = "Item3", Price = 100 }
        });

        // Act - Build hierarchy
        var customer = customerStore.Items[0];
        var customerOrders = new ParentChildRelationship<Customer, Order>(
            stores, customer, (c, o) => o.CustomerId == c.Id);
        
        customerOrders.UseGlobalDataSource();
        customerOrders.Refresh();

        var firstOrder = customerOrders.Childs.Items[0];
        var orderItems = new ParentChildRelationship<Order, OrderItem>(
            stores, firstOrder, (o, i) => i.OrderId == o.Id);
        
        orderItems.UseGlobalDataSource();
        orderItems.Refresh();

        // Assert
        Assert.Equal(2, customerOrders.Childs.Items.Count); // 2 orders for customer
        Assert.Equal(2, orderItems.Childs.Items.Count); // 2 items for first order
    }

    [Fact]
    public void StressTest_1000Items_Performance()
    {
        // Arrange
        var registry = new GlobalStoreRegistry();
        var factory = new LocalDataStoreFactory();
        var stores = new DataStoresFacade(registry, factory);
        var globalStore = new InMemoryDataStore<Product>();
        registry.RegisterGlobal(globalStore);

        // Act - Add 1000 items
        var products = Enumerable.Range(1, 1000)
            .Select(i => new Product 
            { 
                Id = i, 
                Name = $"Product{i}", 
                Price = i * 10.0m, 
                InStock = i % 2 == 0 
            })
            .ToList();

        var startTime = DateTime.UtcNow;
        globalStore.AddRange(products);
        var addDuration = DateTime.UtcNow - startTime;

        // Create snapshot
        startTime = DateTime.UtcNow;
        var snapshot = stores.CreateLocalSnapshotFromGlobal<Product>(p => p.InStock);
        var snapshotDuration = DateTime.UtcNow - startTime;

        // Assert
        Assert.Equal(1000, globalStore.Items.Count);
        Assert.Equal(500, snapshot.Items.Count); // Half are in stock
        Assert.True(addDuration.TotalSeconds < 1, "AddRange should be fast");
        Assert.True(snapshotDuration.TotalSeconds < 1, "Snapshot should be fast");
    }

    [Fact]
    public async Task ConcurrentAccess_MultipleThreads()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDataStoresCore();
        services.AddSingleton<IDataStoreRegistrar, TestDataRegistrar>();

        var provider = services.BuildServiceProvider();
        await DataStoreBootstrap.RunAsync(provider);

        var stores = provider.GetRequiredService<IDataStores>();
        var globalStore = stores.GetGlobal<Customer>();

        // Act - Concurrent operations
        var tasks = Enumerable.Range(1, 100)
            .Select(i => Task.Run(() =>
            {
                globalStore.Add(new Customer { Id = i, Name = $"Customer{i}" });
            }))
            .ToArray();

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(100, globalStore.Items.Count);
    }

    [Fact]
    public void EventPropagation_ThroughHierarchy()
    {
        // Arrange
        var registry = new GlobalStoreRegistry();
        var factory = new LocalDataStoreFactory();
        var stores = new DataStoresFacade(registry, factory);
        var globalStore = new InMemoryDataStore<Order>();
        registry.RegisterGlobal(globalStore);

        var eventCount = 0;
        globalStore.Changed += (s, e) => eventCount++;

        // Act
        globalStore.Add(new Order { Id = 1, CustomerId = 1, Total = 100 });
        globalStore.AddRange(new[]
        {
            new Order { Id = 2, CustomerId = 1, Total = 200 },
            new Order { Id = 3, CustomerId = 1, Total = 300 }
        });
        globalStore.Remove(globalStore.Items[0]);
        globalStore.Clear();

        // Assert - 4 events (Add, BulkAdd, Remove, Clear)
        Assert.Equal(4, eventCount);
    }

    // Helper Classes & Registrars

    private class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    private class Order
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public decimal Total { get; set; }
    }

    private class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string ProductName { get; set; } = "";
        public decimal Price { get; set; }
    }

    private class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public bool InStock { get; set; }
    }

    private class TestDataRegistrar : IDataStoreRegistrar
    {
        public void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider)
        {
            registry.RegisterGlobal(new InMemoryDataStore<Customer>());
            registry.RegisterGlobal(new InMemoryDataStore<Order>());
            registry.RegisterGlobal(new InMemoryDataStore<Product>());
        }
    }

    private class CustomerRegistrar : IDataStoreRegistrar
    {
        public void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider)
        {
            registry.RegisterGlobal(new InMemoryDataStore<Customer>());
        }
    }

    private class ProductRegistrar : IDataStoreRegistrar
    {
        public void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider)
        {
            registry.RegisterGlobal(new InMemoryDataStore<Product>());
        }
    }

    private class HierarchyRegistrar : IDataStoreRegistrar
    {
        public void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider)
        {
            registry.RegisterGlobal(new InMemoryDataStore<Customer>());
            registry.RegisterGlobal(new InMemoryDataStore<Order>());
            registry.RegisterGlobal(new InMemoryDataStore<OrderItem>());
        }
    }
}
