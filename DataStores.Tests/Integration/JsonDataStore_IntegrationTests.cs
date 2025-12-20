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
/// <remarks>
/// Tests folgen der One Assert Rule: Jeder Test prüft genau einen Aspekt.
/// Shared Setup reduziert Boilerplate-Code.
/// </remarks>
public class JsonDataStore_IntegrationTests : IAsyncLifetime
{
    private readonly string _testDataPath;
    private IServiceProvider _serviceProvider = null!;
    private IDataStores _dataStores = null!;
    private IDataStore<CustomerDto> _customerStore = null!;
    private string _customerJsonPath = "";

    public JsonDataStore_IntegrationTests()
    {
        _testDataPath = Path.Combine(Path.GetTempPath(), $"DataStoresTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDataPath);
    }

    public async Task InitializeAsync()
    {
        _customerJsonPath = Path.Combine(_testDataPath, "customers.json");
        
        var services = new ServiceCollection();
        services.AddDataStoresCore();
        services.AddDataStoreRegistrar(new JsonCustomerDataStoreRegistrar(_customerJsonPath));

        _serviceProvider = services.BuildServiceProvider();
        await DataStoreBootstrap.RunAsync(_serviceProvider);

        _dataStores = _serviceProvider.GetRequiredService<IDataStores>();
        _customerStore = _dataStores.GetGlobal<CustomerDto>();
    }

    public Task DisposeAsync()
    {
        if (Directory.Exists(_testDataPath))
        {
            Directory.Delete(_testDataPath, recursive: true);
        }
        return Task.CompletedTask;
    }

    [Fact]
    public void Bootstrap_Should_CreateEmptyStore()
    {
        Assert.Empty(_customerStore.Items);
    }

    [Fact]
    public void Add_Should_AddSingleCustomer()
    {
        // Arrange
        var customer = CreateTestCustomer(1, "Max", "Mustermann", "max@example.com");

        // Act
        _customerStore.Add(customer);

        // Assert
        Assert.Single(_customerStore.Items);
    }

    [Fact]
    public void AddRange_Should_AddMultipleCustomers()
    {
        // Arrange
        var customers = new[]
        {
            CreateTestCustomer(1, "Max", "Mustermann", "max@example.com"),
            CreateTestCustomer(2, "Anna", "Schmidt", "anna@example.com")
        };

        // Act
        _customerStore.AddRange(customers);

        // Assert
        Assert.Equal(2, _customerStore.Items.Count);
    }

    [Fact]
    public void Items_Should_SupportLinqFiltering()
    {
        // Arrange
        _customerStore.AddRange(new[]
        {
            CreateTestCustomer(1, "Max", "Mustermann", "max@example.com", isActive: true),
            CreateTestCustomer(2, "Anna", "Schmidt", "anna@example.com", isActive: true),
            CreateTestCustomer(3, "Peter", "Weber", "peter@example.com", isActive: false)
        });

        // Act
        var activeCustomers = _customerStore.Items.Where(c => c.IsActive).ToList();

        // Assert
        Assert.Equal(2, activeCustomers.Count);
    }

    [Fact]
    public void Changed_Event_Should_FireOnAdd()
    {
        // Arrange
        var eventFired = false;
        _customerStore.Changed += (sender, args) => eventFired = true;
        var customer = CreateTestCustomer(1, "Max", "Mustermann", "max@example.com");

        // Act
        _customerStore.Add(customer);

        // Assert
        Assert.True(eventFired);
    }

    [Fact]
    public void Changed_Event_Should_ReportCorrectChangeType()
    {
        // Arrange
        DataStoreChangeType? capturedChangeType = null;
        _customerStore.Changed += (sender, args) => capturedChangeType = args.ChangeType;
        var customer = CreateTestCustomer(1, "Max", "Mustermann", "max@example.com");

        // Act
        _customerStore.Add(customer);

        // Assert
        Assert.Equal(DataStoreChangeType.Add, capturedChangeType);
    }

    [Fact]
    public void Remove_Should_DecreaseItemCount()
    {
        // Arrange
        var customer1 = CreateTestCustomer(1, "Max", "Mustermann", "max@example.com");
        var customer2 = CreateTestCustomer(2, "Anna", "Schmidt", "anna@example.com");
        _customerStore.AddRange(new[] { customer1, customer2 });

        // Act
        _customerStore.Remove(customer1);

        // Assert
        Assert.Single(_customerStore.Items);
    }

    [Fact]
    public void Remove_Should_FireChangedEvent()
    {
        // Arrange
        var customer = CreateTestCustomer(1, "Max", "Mustermann", "max@example.com");
        _customerStore.Add(customer);
        
        DataStoreChangeType? capturedChangeType = null;
        _customerStore.Changed += (sender, args) => capturedChangeType = args.ChangeType;

        // Act
        _customerStore.Remove(customer);

        // Assert
        Assert.Equal(DataStoreChangeType.Remove, capturedChangeType);
    }

    [Fact]
    public async Task Persistence_Should_CreateJsonFile()
    {
        // Arrange
        var customer = CreateTestCustomer(1, "Max", "Mustermann", "max@example.com");
        _customerStore.Add(customer);

        // Act
        await Task.Delay(200);

        // Assert
        Assert.True(File.Exists(_customerJsonPath));
    }

    [Fact]
    public async Task Persistence_Should_SaveAddedCustomers()
    {
        // Arrange
        _customerStore.AddRange(new[]
        {
            CreateTestCustomer(1, "Max", "Mustermann", "max@example.com"),
            CreateTestCustomer(2, "Anna", "Schmidt", "anna@example.com"),
            CreateTestCustomer(3, "Lisa", "Müller", "lisa@example.com")
        });

        // Act
        await Task.Delay(200);
        var jsonContent = await File.ReadAllTextAsync(_customerJsonPath);
        var deserializedCustomers = JsonSerializer.Deserialize<List<CustomerDto>>(jsonContent);

        // Assert
        Assert.Equal(3, deserializedCustomers?.Count);
    }

    [Fact]
    public async Task Persistence_Should_ContainCorrectData()
    {
        // Arrange
        _customerStore.Add(CreateTestCustomer(1, "Max", "Mustermann", "max@example.com"));

        // Act
        await Task.Delay(200);
        var jsonContent = await File.ReadAllTextAsync(_customerJsonPath);
        var deserializedCustomers = JsonSerializer.Deserialize<List<CustomerDto>>(jsonContent);

        // Assert
        Assert.Contains(deserializedCustomers!, c => c.Email == "max@example.com");
    }

    [Fact]
    public async Task Persistence_Should_NotContainRemovedCustomers()
    {
        // Arrange
        var customer1 = CreateTestCustomer(1, "Max", "Mustermann", "max@example.com");
        var customer2 = CreateTestCustomer(2, "Peter", "Weber", "peter@example.com");
        _customerStore.AddRange(new[] { customer1, customer2 });
        await Task.Delay(200);

        // Act
        _customerStore.Remove(customer2);
        await Task.Delay(200);

        var jsonContent = await File.ReadAllTextAsync(_customerJsonPath);
        var deserializedCustomers = JsonSerializer.Deserialize<List<CustomerDto>>(jsonContent);

        // Assert
        Assert.DoesNotContain(deserializedCustomers!, c => c.Email == "peter@example.com");
    }

    [Fact]
    public async Task MultipleEntityTypes_Should_UseSeparateFiles()
    {
        // Arrange
        var customerFile = Path.Combine(_testDataPath, "customers.json");
        var productFile = Path.Combine(_testDataPath, "products.json");

        var services = new ServiceCollection();
        services.AddDataStoresCore();
        services.AddDataStoreRegistrar(new MultiDtoJsonRegistrar(customerFile, productFile));

        var provider = services.BuildServiceProvider();
        await DataStoreBootstrap.RunAsync(provider);

        var dataStores = provider.GetRequiredService<IDataStores>();
        var customerStore = dataStores.GetGlobal<CustomerDto>();
        var productStore = dataStores.GetGlobal<ProductDto>();

        customerStore.Add(CreateTestCustomer(1, "John", "Doe", "john@example.com"));
        productStore.Add(new ProductDto { Id = 1, Name = "Laptop", Price = 999.99m, Stock = 10 });

        // Act
        await Task.Delay(200);

        // Assert
        Assert.True(File.Exists(customerFile));
        Assert.True(File.Exists(productFile));
    }

    [Fact]
    public async Task MultipleEntityTypes_Should_PersistIndependently()
    {
        // Arrange
        var customerFile = Path.Combine(_testDataPath, "customers.json");
        var productFile = Path.Combine(_testDataPath, "products.json");

        var services = new ServiceCollection();
        services.AddDataStoresCore();
        services.AddDataStoreRegistrar(new MultiDtoJsonRegistrar(customerFile, productFile));

        var provider = services.BuildServiceProvider();
        await DataStoreBootstrap.RunAsync(provider);

        var dataStores = provider.GetRequiredService<IDataStores>();
        var customerStore = dataStores.GetGlobal<CustomerDto>();
        var productStore = dataStores.GetGlobal<ProductDto>();

        customerStore.Add(CreateTestCustomer(1, "John", "Doe", "john@example.com"));
        productStore.AddRange(new[]
        {
            new ProductDto { Id = 1, Name = "Laptop", Price = 999.99m, Stock = 10 },
            new ProductDto { Id = 2, Name = "Mouse", Price = 29.99m, Stock = 50 }
        });

        // Act
        await Task.Delay(200);

        // Assert
        Assert.Single(customerStore.Items);
        Assert.Equal(2, productStore.Items.Count);
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

    private static CustomerDto CreateTestCustomer(int id, string firstName, string lastName, 
        string email, bool isActive = true)
    {
        return new CustomerDto
        {
            Id = id,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };
    }
}
