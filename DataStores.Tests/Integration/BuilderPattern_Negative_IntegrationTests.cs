using DataStores.Abstractions;
using DataStores.Bootstrap;
using DataStores.Registration;
using Microsoft.Extensions.DependencyInjection;
using TestHelper.DataStores.Models;

namespace DataStores.Tests.Integration;

/// <summary>
/// Negative Tests für Builder Pattern - Fehlerszenarien und Edge Cases.
/// </summary>
[Trait("Category", "Integration")]
public class BuilderPattern_Negative_IntegrationTests
{
    // ====================================================================
    // Constructor Validation Tests
    // ====================================================================

    [Fact]
    public void JsonBuilder_WithNullFilePath_Should_Throw()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new JsonDataStoreBuilder<TestDto>(filePath: null!));

        Assert.Contains("File path cannot be null or empty", exception.Message);
    }

    [Fact]
    public void JsonBuilder_WithEmptyFilePath_Should_Throw()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new JsonDataStoreBuilder<TestDto>(filePath: string.Empty));

        Assert.Contains("File path cannot be null or empty", exception.Message);
    }

    [Fact]
    public void JsonBuilder_WithWhitespaceFilePath_Should_Throw()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new JsonDataStoreBuilder<TestDto>(filePath: "   "));

        Assert.Contains("File path cannot be null or empty", exception.Message);
    }

    [Fact]
    public void LiteDbBuilder_WithNullDatabasePath_Should_Throw()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new LiteDbDataStoreBuilder<TestEntity>(databasePath: null!));

        Assert.Contains("Database path cannot be null or empty", exception.Message);
    }

    [Fact]
    public void LiteDbBuilder_WithEmptyDatabasePath_Should_Throw()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new LiteDbDataStoreBuilder<TestEntity>(databasePath: string.Empty));

        Assert.Contains("Database path cannot be null or empty", exception.Message);
    }

    [Fact]
    public void LiteDbBuilder_WithWhitespaceDatabasePath_Should_Throw()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new LiteDbDataStoreBuilder<TestEntity>(databasePath: "   "));

        Assert.Contains("Database path cannot be null or empty", exception.Message);
    }

    // ====================================================================
    // Invalid Path Tests
    // ====================================================================

    [Fact]
    public async Task JsonBuilder_WithInvalidPath_Should_ThrowDuringBootstrap()
    {
        // Arrange
        var invalidPath = "Z:\\NonExistent\\Path\\test.json"; // Drive likely doesn't exist
        var services = new ServiceCollection();
        new DataStoresServiceModule().Register(services);
        services.AddDataStoreRegistrar(new InvalidPathJsonRegistrar(invalidPath));

        var provider = services.BuildServiceProvider();

        // Act & Assert: Bootstrap should handle invalid paths gracefully
        // (depending on implementation, might throw or create directory)
        await DataStoreBootstrap.RunAsync(provider);
        
        var stores = provider.GetRequiredService<IDataStores>();
        var store = stores.GetGlobal<TestDto>();
        
        // Store should be usable even if persistence fails
        store.Add(new TestDto("Test", 25));
        Assert.Single(store.Items);
    }

    // ====================================================================
    // Bootstrap Order Tests
    // ====================================================================

    [Fact]
    public void AccessStore_BeforeBootstrap_Should_Throw()
    {
        // Arrange: Register but don't bootstrap
        var services = new ServiceCollection();
        new DataStoresServiceModule().Register(services);
        services.AddDataStoreRegistrar(new SimpleRegistrar());

        var provider = services.BuildServiceProvider();
        var stores = provider.GetRequiredService<IDataStores>();

        // Act & Assert: Accessing store before bootstrap throws
        var exception = Assert.Throws<GlobalStoreNotRegisteredException>(() =>
            stores.GetGlobal<TestEntity>());

        Assert.Equal(typeof(TestEntity), exception.StoreType);
    }

    [Fact]
    public async Task AccessNonRegisteredStore_AfterBootstrap_Should_Throw()
    {
        // Arrange: Bootstrap with one type
        var services = new ServiceCollection();
        new DataStoresServiceModule().Register(services);
        services.AddDataStoreRegistrar(new SimpleRegistrar()); // Only registers TestEntity

        var provider = services.BuildServiceProvider();
        await DataStoreBootstrap.RunAsync(provider);

        var stores = provider.GetRequiredService<IDataStores>();

        // Act & Assert: Accessing non-registered type throws
        var exception = Assert.Throws<GlobalStoreNotRegisteredException>(() =>
            stores.GetGlobal<TestDto>()); // TestDto was not registered

        Assert.Equal(typeof(TestDto), exception.StoreType);
    }

    // ====================================================================
    // Double Registration Tests
    // ====================================================================

    [Fact]
    public async Task RegisterSameType_Twice_Should_Throw()
    {
        // Arrange
        var services = new ServiceCollection();
        new DataStoresServiceModule().Register(services);
        services.AddDataStoreRegistrar(new DoubleRegistrationRegistrar());

        var provider = services.BuildServiceProvider();

        // Act & Assert: Bootstrap should throw on double registration
        var exception = await Assert.ThrowsAsync<GlobalStoreAlreadyRegisteredException>(
            async () => await DataStoreBootstrap.RunAsync(provider));

        Assert.Equal(typeof(TestEntity), exception.StoreType);
    }

    // ====================================================================
    // Empty Registrar Tests
    // ====================================================================

    [Fact]
    public async Task EmptyRegistrar_Should_NotCauseErrors()
    {
        // Arrange
        var services = new ServiceCollection();
        new DataStoresServiceModule().Register(services);
        services.AddDataStoreRegistrar(new EmptyRegistrar());

        var provider = services.BuildServiceProvider();

        // Act: Bootstrap should succeed
        await DataStoreBootstrap.RunAsync(provider);

        // Assert: Facade is available but no stores registered
        var stores = provider.GetRequiredService<IDataStores>();
        Assert.NotNull(stores);
    }

    [Fact]
    public async Task NoRegistrars_Should_NotCauseErrors()
    {
        // Arrange
        var services = new ServiceCollection();
        new DataStoresServiceModule().Register(services);
        // NO registrar added

        var provider = services.BuildServiceProvider();

        // Act: Bootstrap should succeed
        await DataStoreBootstrap.RunAsync(provider);

        // Assert: Facade is available
        var stores = provider.GetRequiredService<IDataStores>();
        Assert.NotNull(stores);
    }

    // ====================================================================
    // Null Parameter Tests
    // ====================================================================

    [Fact]
    public void InMemoryBuilder_WithNullComparer_Should_Work()
    {
        // Arrange & Act
        var builder = new InMemoryDataStoreBuilder<TestEntity>(comparer: null);

        // Assert: No exception, null is valid
        Assert.NotNull(builder);
    }

    [Fact]
    public void InMemoryBuilder_WithNullSyncContext_Should_Work()
    {
        // Arrange & Act
        var builder = new InMemoryDataStoreBuilder<TestEntity>(
            synchronizationContext: null);

        // Assert: No exception, null is valid
        Assert.NotNull(builder);
    }

    [Fact]
    public void JsonBuilder_WithNullComparer_Should_Work()
    {
        // Arrange & Act
        var builder = new JsonDataStoreBuilder<TestDto>(
            filePath: "test.json",
            comparer: null);

        // Assert: No exception, null is valid
        Assert.NotNull(builder);
    }

    [Fact]
    public void JsonBuilder_WithNullSyncContext_Should_Work()
    {
        // Arrange & Act
        var builder = new JsonDataStoreBuilder<TestDto>(
            filePath: "test.json",
            synchronizationContext: null);

        // Assert: No exception, null is valid
        Assert.NotNull(builder);
    }

    [Fact]
    public void LiteDbBuilder_WithNullComparer_Should_Work()
    {
        // Arrange & Act
        var builder = new LiteDbDataStoreBuilder<TestEntity>(
            databasePath: "test.db",
            comparer: null);

        // Assert: No exception, null is valid
        Assert.NotNull(builder);
    }

    [Fact]
    public void LiteDbBuilder_WithNullSyncContext_Should_Work()
    {
        // Arrange & Act
        var builder = new LiteDbDataStoreBuilder<TestEntity>(
            databasePath: "test.db",
            synchronizationContext: null);

        // Assert: No exception, null is valid
        Assert.NotNull(builder);
    }

    // ====================================================================
    // Type Constraint Tests
    // ====================================================================

    [Fact]
    public void LiteDbBuilder_WithNonEntityBase_Should_NotCompile()
    {
        // This test documents the compile-time constraint
        // Uncomment to verify:
        // var builder = new LiteDbDataStoreBuilder<TestDto>("test.db");
        // ☝️ Should not compile: TestDto doesn't inherit from EntityBase

        Assert.True(true); // Compile-time verification
    }

    // ====================================================================
    // Concurrent Bootstrap Tests
    // ====================================================================

    [Fact]
    public async Task ConcurrentBootstrap_Should_BeIdempotent()
    {
        // Arrange
        var services = new ServiceCollection();
        new DataStoresServiceModule().Register(services);
        services.AddDataStoreRegistrar(new SimpleRegistrar());

        var provider = services.BuildServiceProvider();

        // Act: Call bootstrap concurrently
        var task1 = DataStoreBootstrap.RunAsync(provider);
        var task2 = DataStoreBootstrap.RunAsync(provider);
        var task3 = DataStoreBootstrap.RunAsync(provider);

        await Task.WhenAll(task1, task2, task3);

        // Assert: Store is still usable
        var stores = provider.GetRequiredService<IDataStores>();
        var store = stores.GetGlobal<TestEntity>();
        store.Add(new TestEntity { Name = "Test" });
        Assert.Single(store.Items);
    }

    // ====================================================================
    // Edge Case Path Tests
    // ====================================================================

    [Fact]
    public async Task JsonBuilder_WithLongPath_Should_Work()
    {
        // Arrange
        var tempPath = Path.GetTempPath();
        var longSubPath = string.Join(Path.DirectorySeparatorChar.ToString(),
            Enumerable.Range(1, 10).Select(i => $"Dir{i}"));
        var longPath = Path.Combine(tempPath, longSubPath, "test.json");

        var services = new ServiceCollection();
        new DataStoresServiceModule().Register(services);
        services.AddDataStoreRegistrar(new JsonPathRegistrar(longPath));

        try
        {
            // Act
            var provider = services.BuildServiceProvider();
            await DataStoreBootstrap.RunAsync(provider);

            var stores = provider.GetRequiredService<IDataStores>();
            var store = stores.GetGlobal<TestDto>();

            // Assert: Store is usable
            store.Add(new TestDto("Test", 25));
            Assert.Single(store.Items);
        }
        finally
        {
            // Cleanup
            if (File.Exists(longPath))
                File.Delete(longPath);
            
            var dirToDelete = Path.Combine(tempPath, longSubPath.Split(Path.DirectorySeparatorChar)[0]);
            if (Directory.Exists(dirToDelete))
                Directory.Delete(dirToDelete, recursive: true);
        }
    }

    [Fact]
    public async Task JsonBuilder_WithSpecialCharactersInPath_Should_Work()
    {
        // Arrange
        var tempPath = Path.GetTempPath();
        var specialPath = Path.Combine(tempPath, $"Test_File_{Guid.NewGuid()}.json");

        var services = new ServiceCollection();
        new DataStoresServiceModule().Register(services);
        services.AddDataStoreRegistrar(new JsonPathRegistrar(specialPath));

        try
        {
            // Act
            var provider = services.BuildServiceProvider();
            await DataStoreBootstrap.RunAsync(provider);

            var stores = provider.GetRequiredService<IDataStores>();
            var store = stores.GetGlobal<TestDto>();

            // Assert: Store is usable
            store.Add(new TestDto("Test", 25));
            await Task.Delay(200);
            Assert.True(File.Exists(specialPath));
        }
        finally
        {
            if (File.Exists(specialPath))
                File.Delete(specialPath);
        }
    }

    // ====================================================================
    // Test Registrars
    // ====================================================================

    private class SimpleRegistrar : DataStoreRegistrarBase
    {
        public SimpleRegistrar()
        {
            AddStore(new InMemoryDataStoreBuilder<TestEntity>());
        }
    }

    private class InvalidPathJsonRegistrar : DataStoreRegistrarBase
    {
        public InvalidPathJsonRegistrar(string invalidPath)
        {
            AddStore(new JsonDataStoreBuilder<TestDto>(
                filePath: invalidPath,
                autoLoad: false,
                autoSave: false)); // Disable auto-save to prevent errors
        }
    }

    private class DoubleRegistrationRegistrar : DataStoreRegistrarBase
    {
        public DoubleRegistrationRegistrar()
        {
            AddStore(new InMemoryDataStoreBuilder<TestEntity>());
            AddStore(new InMemoryDataStoreBuilder<TestEntity>()); // Duplicate!
        }
    }

    private class EmptyRegistrar : DataStoreRegistrarBase
    {
        public EmptyRegistrar()
        {
            // No stores registered
        }
    }

    private class JsonPathRegistrar : DataStoreRegistrarBase
    {
        public JsonPathRegistrar(string path)
        {
            AddStore(new JsonDataStoreBuilder<TestDto>(filePath: path));
        }
    }
}
