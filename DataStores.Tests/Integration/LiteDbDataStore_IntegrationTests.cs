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
/// <remarks>
/// Tests folgen der One Assert Rule: Jeder Test prüft genau einen Aspekt.
/// Shared Setup reduziert Boilerplate-Code.
/// </remarks>
public class LiteDbDataStore_IntegrationTests : IAsyncLifetime
{
    private readonly string _testDbPath;
    private IServiceProvider _serviceProvider = null!;
    private IDataStores _dataStores = null!;
    private IDataStore<OrderDto> _orderStore = null!;

    public LiteDbDataStore_IntegrationTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"DataStoresTest_{Guid.NewGuid()}.db");
    }

    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();
        services.AddDataStoresCore();
        services.AddDataStoreRegistrar(new LiteDbOrderDataStoreRegistrar(_testDbPath));

        _serviceProvider = services.BuildServiceProvider();
        await DataStoreBootstrap.RunAsync(_serviceProvider);

        _dataStores = _serviceProvider.GetRequiredService<IDataStores>();
        _orderStore = _dataStores.GetGlobal<OrderDto>();
    }

    public Task DisposeAsync()
    {
        if (File.Exists(_testDbPath))
        {
            File.Delete(_testDbPath);
        }
        return Task.CompletedTask;
    }

    [Fact]
    public void Bootstrap_Should_CreateEmptyStore()
    {
        Assert.Empty(_orderStore.Items);
    }

    [Fact]
    public void Add_Should_AssignLiteDbId()
    {
        // Arrange
        var order = CreateTestOrder("ORD-001", 100, "Customer A", 1000m, OrderStatus.Pending);

        // Act
        _orderStore.Add(order);

        // Assert
        Assert.True(order.Id > 0);
    }

    [Fact]
    public void AddRange_Should_AddMultipleOrders()
    {
        // Arrange
        var orders = new[]
        {
            CreateTestOrder("ORD-001", 100, "Customer A", 1000m, OrderStatus.Pending),
            CreateTestOrder("ORD-002", 101, "Customer B", 2000m, OrderStatus.Processing)
        };

        // Act
        _orderStore.AddRange(orders);

        // Assert
        Assert.Equal(2, _orderStore.Items.Count);
    }

    [Fact]
    public void AddRange_Should_AssignIdsToAllItems()
    {
        // Arrange
        var orders = new[]
        {
            CreateTestOrder("ORD-001", 100, "Customer A", 1000m, OrderStatus.Pending),
            CreateTestOrder("ORD-002", 101, "Customer B", 2000m, OrderStatus.Processing),
            CreateTestOrder("ORD-003", 100, "Customer A", 500m, OrderStatus.Shipped)
        };

        // Act
        _orderStore.AddRange(orders);

        // Assert
        Assert.All(_orderStore.Items, o => Assert.True(o.Id > 0));
    }

    [Fact]
    public void Items_Should_SupportLinqFiltering()
    {
        // Arrange
        _orderStore.AddRange(new[]
        {
            CreateTestOrder("ORD-001", 100, "Customer A", 1000m, OrderStatus.Pending),
            CreateTestOrder("ORD-002", 101, "Customer B", 2000m, OrderStatus.Processing),
            CreateTestOrder("ORD-003", 100, "Customer A", 500m, OrderStatus.Shipped)
        });

        // Act
        var customerAOrders = _orderStore.Items.Where(o => o.CustomerId == 100).ToList();

        // Assert
        Assert.Equal(2, customerAOrders.Count);
    }

    [Fact]
    public void Items_Should_SupportLinqGrouping()
    {
        // Arrange
        _orderStore.AddRange(new[]
        {
            CreateTestOrder("ORD-001", 100, "Customer A", 1000m, OrderStatus.Pending),
            CreateTestOrder("ORD-002", 101, "Customer B", 2000m, OrderStatus.Processing),
            CreateTestOrder("ORD-003", 100, "Customer A", 500m, OrderStatus.Shipped)
        });

        // Act
        var ordersByStatus = _orderStore.Items
            .GroupBy(o => o.Status)
            .ToDictionary(g => g.Key, g => g.Count());

        // Assert
        Assert.Equal(1, ordersByStatus[OrderStatus.Pending]);
    }

    [Fact]
    public void Items_Should_SupportLinqAggregation()
    {
        // Arrange
        _orderStore.AddRange(new[]
        {
            CreateTestOrder("ORD-001", 100, "Customer A", 1599.99m, OrderStatus.Pending),
            CreateTestOrder("ORD-002", 101, "Customer B", 2999.99m, OrderStatus.Processing),
            CreateTestOrder("ORD-003", 100, "Customer A", 499.99m, OrderStatus.Shipped)
        });

        // Act
        var totalRevenue = _orderStore.Items.Sum(o => o.TotalAmount);

        // Assert
        Assert.Equal(5099.97m, totalRevenue);
    }

    [Fact]
    public void Changed_Event_Should_FireOnAdd()
    {
        // Arrange
        var eventFired = false;
        _orderStore.Changed += (sender, args) => eventFired = true;
        var order = CreateTestOrder("ORD-001", 100, "Customer A", 1000m, OrderStatus.Pending);

        // Act
        _orderStore.Add(order);

        // Assert
        Assert.True(eventFired);
    }

    [Fact]
    public void Changed_Event_Should_FireOnRemove()
    {
        // Arrange
        var order = CreateTestOrder("ORD-001", 100, "Customer A", 1000m, OrderStatus.Pending);
        _orderStore.Add(order);
        
        var eventFired = false;
        _orderStore.Changed += (sender, args) => eventFired = true;

        // Act
        _orderStore.Remove(order);

        // Assert
        Assert.True(eventFired);
    }

    [Fact]
    public void Remove_Should_DecreaseItemCount()
    {
        // Arrange
        var order1 = CreateTestOrder("ORD-001", 100, "Customer A", 1000m, OrderStatus.Pending);
        var order2 = CreateTestOrder("ORD-002", 101, "Customer B", 2000m, OrderStatus.Processing);
        _orderStore.AddRange(new[] { order1, order2 });

        // Act
        _orderStore.Remove(order1);

        // Assert
        Assert.Single(_orderStore.Items);
    }

    [Fact]
    public async Task Persistence_Should_CreatePhysicalDbFile()
    {
        // Arrange
        var order = CreateTestOrder("ORD-001", 100, "Customer A", 1000m, OrderStatus.Pending);
        _orderStore.Add(order);

        // Act
        await Task.Delay(200);

        // Assert
        Assert.True(File.Exists(_testDbPath));
    }

    [Fact]
    public async Task Persistence_Should_CreateNonEmptyDbFile()
    {
        // Arrange
        var order = CreateTestOrder("ORD-001", 100, "Customer A", 1000m, OrderStatus.Pending);
        _orderStore.Add(order);

        // Act
        await Task.Delay(200);

        // Assert
        Assert.True(new FileInfo(_testDbPath).Length > 0);
    }

    [Fact]
    public async Task Persistence_Should_SaveAddedOrders()
    {
        // Arrange
        _orderStore.AddRange(new[]
        {
            CreateTestOrder("ORD-001", 100, "Customer A", 1000m, OrderStatus.Pending),
            CreateTestOrder("ORD-002", 101, "Customer B", 2000m, OrderStatus.Processing)
        });

        // Act
        await Task.Delay(200);
        var strategy = new LiteDbPersistenceStrategy<OrderDto>(_testDbPath, "orders");
        var savedOrders = await strategy.LoadAllAsync();

        // Assert
        Assert.Equal(2, savedOrders.Count);
    }

    [Fact]
    public async Task Persistence_Should_NotSaveRemovedOrders()
    {
        // Arrange
        var order1 = CreateTestOrder("ORD-001", 100, "Customer A", 1000m, OrderStatus.Pending);
        var order2 = CreateTestOrder("ORD-002", 101, "Customer B", 2000m, OrderStatus.Processing);
        _orderStore.AddRange(new[] { order1, order2 });
        await Task.Delay(200);

        // Act
        _orderStore.Remove(order2);
        await Task.Delay(200);
        
        var strategy = new LiteDbPersistenceStrategy<OrderDto>(_testDbPath, "orders");
        var savedOrders = await strategy.LoadAllAsync();

        // Assert
        Assert.DoesNotContain(savedOrders, o => o.OrderNumber == "ORD-002");
    }

    [Fact]
    public async Task MultipleEntities_Should_UseIndependentCollections()
    {
        // Arrange - Neue Services mit Multi-Entity-Registrar
        var services = new ServiceCollection();
        services.AddDataStoresCore();
        services.AddDataStoreRegistrar(new MultiEntityLiteDbRegistrar(_testDbPath));

        var provider = services.BuildServiceProvider();
        await DataStoreBootstrap.RunAsync(provider);

        var dataStores = provider.GetRequiredService<IDataStores>();
        var orderStore = dataStores.GetGlobal<OrderDto>();
        var invoiceStore = dataStores.GetGlobal<InvoiceDto>();

        var order = CreateTestOrder("ORD-001", 100, "Customer A", 1000m, OrderStatus.Completed);
        orderStore.Add(order);

        // Act
        invoiceStore.AddRange(new[]
        {
            new InvoiceDto { Id = 0, InvoiceNumber = "INV-001", OrderId = order.Id, Amount = 1000m, IsPaid = true },
            new InvoiceDto { Id = 0, InvoiceNumber = "INV-002", OrderId = order.Id, Amount = 500m, IsPaid = false }
        });

        // Assert
        Assert.Equal(2, invoiceStore.Items.Count);
    }

    [Fact]
    public async Task MultipleEntities_Should_PersistIndependently()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDataStoresCore();
        services.AddDataStoreRegistrar(new MultiEntityLiteDbRegistrar(_testDbPath));

        var provider = services.BuildServiceProvider();
        await DataStoreBootstrap.RunAsync(provider);

        var dataStores = provider.GetRequiredService<IDataStores>();
        var orderStore = dataStores.GetGlobal<OrderDto>();
        var invoiceStore = dataStores.GetGlobal<InvoiceDto>();

        var order = CreateTestOrder("ORD-001", 100, "Customer A", 1000m, OrderStatus.Completed);
        orderStore.Add(order);
        invoiceStore.Add(new InvoiceDto { Id = 0, InvoiceNumber = "INV-001", OrderId = order.Id, Amount = 1000m, IsPaid = true });

        // Act
        await Task.Delay(200);
        var orderStrategy = new LiteDbPersistenceStrategy<OrderDto>(_testDbPath, "orders");
        var invoiceStrategy = new LiteDbPersistenceStrategy<InvoiceDto>(_testDbPath, "invoices");

        var savedOrders = await orderStrategy.LoadAllAsync();
        var savedInvoices = await invoiceStrategy.LoadAllAsync();

        // Assert
        Assert.Single(savedOrders);
        Assert.Single(savedInvoices);
    }

    // ====================================================================
    // Helper Classes - DTOs
    // ====================================================================

    private class OrderDto : EntityBase
    {
        public string OrderNumber { get; set; } = "";
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = "";
        public decimal TotalAmount { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime OrderDate { get; set; }
        public List<string> Items { get; set; } = new();

        public override string ToString() => 
            $"Order #{Id}: {OrderNumber} - {CustomerName} ({TotalAmount:C})";

        public override bool Equals(object? obj)
        {
            if (obj is not OrderDto other) return false;
            if (Id > 0 && other.Id > 0) return Id == other.Id;
            return ReferenceEquals(this, other);
        }

        public override int GetHashCode() => Id > 0 ? Id : HashCode.Combine(OrderNumber, CustomerId);
    }

    private class InvoiceDto : EntityBase
    {
        public string InvoiceNumber { get; set; } = "";
        public int OrderId { get; set; }
        public decimal Amount { get; set; }
        public bool IsPaid { get; set; }

        public override string ToString() => 
            $"Invoice #{Id}: {InvoiceNumber} - {Amount:C} (Paid: {IsPaid})";

        public override bool Equals(object? obj)
        {
            if (obj is not InvoiceDto other) return false;
            if (Id > 0 && other.Id > 0) return Id == other.Id;
            return ReferenceEquals(this, other);
        }

        public override int GetHashCode() => Id > 0 ? Id : HashCode.Combine(InvoiceNumber, OrderId);
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

    private static OrderDto CreateTestOrder(string orderNumber, int customerId, string customerName, 
        decimal totalAmount, OrderStatus status)
    {
        return new OrderDto
        {
            Id = 0,
            OrderNumber = orderNumber,
            CustomerId = customerId,
            CustomerName = customerName,
            TotalAmount = totalAmount,
            Status = status,
            OrderDate = DateTime.UtcNow,
            Items = new List<string> { "Item1", "Item2" }
        };
    }
}
