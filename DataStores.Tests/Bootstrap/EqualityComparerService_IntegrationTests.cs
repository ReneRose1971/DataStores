using Common.Bootstrap;
using DataStores.Abstractions;
using DataStores.Bootstrap;
using DataStores.Runtime;
using Microsoft.Extensions.DependencyInjection;
using TestHelper.DataStores.Models;
using Xunit;

namespace DataStores.Tests.Bootstrap;

/// <summary>
/// Integration-Tests für EqualityComparerService mit vollständigem DI-Container.
/// Testet die automatische Registrierung via ServiceModule und Bootstrap-Prozess.
/// </summary>
public class EqualityComparerService_IntegrationTests
{
    [Fact]
    public void DataStoresServiceModule_Should_RegisterEqualityComparerService()
    {
        // Arrange
        var services = new ServiceCollection();
        var module = new DataStoresServiceModule();

        // Act
        module.Register(services);
        var provider = services.BuildServiceProvider();

        // Assert - Service wurde registriert
        var service = provider.GetService<IEqualityComparerService>();
        Assert.NotNull(service);
        Assert.IsType<EqualityComparerService>(service);
    }

    [Fact]
    public void FullBootstrap_Should_ResolveEqualityComparerService()
    {
        // Arrange - Vollständiger Bootstrap mit Common.Bootstrap
        var services = new ServiceCollection();
        
        // Verwende DefaultBootstrapWrapper für automatisches Scannen
        var bootstrap = new DefaultBootstrapWrapper();
        bootstrap.RegisterServices(
            services,
            typeof(DataStoresServiceModule).Assembly);

        var provider = services.BuildServiceProvider();

        // Act
        var service = provider.GetRequiredService<IEqualityComparerService>();

        // Assert
        Assert.NotNull(service);
        Assert.IsType<EqualityComparerService>(service);
    }

    [Fact]
    public void EqualityComparerService_Should_BeRegisteredAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        var module = new DataStoresServiceModule();
        module.Register(services);
        var provider = services.BuildServiceProvider();

        // Act
        var service1 = provider.GetRequiredService<IEqualityComparerService>();
        var service2 = provider.GetRequiredService<IEqualityComparerService>();

        // Assert - Same instance (Singleton)
        Assert.Same(service1, service2);
    }

    [Fact]
    public void EqualityComparerService_WithAutoRegisteredComparer_Should_ReturnCustomComparer()
    {
        // Arrange - Simuliere automatisches Scannen von IEqualityComparer<T>
        var services = new ServiceCollection();
        
        // Common.Bootstrap würde dies automatisch tun:
        services.AddSingleton<IEqualityComparer<TestDto>, TestDtoAgeComparer>();
        
        // DataStoresServiceModule registriert EqualityComparerService
        var module = new DataStoresServiceModule();
        module.Register(services);
        
        var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<IEqualityComparerService>();

        // Act
        var comparer = service.GetComparer<TestDto>();

        // Assert - Sollte den registrierten AgeComparer bekommen
        Assert.IsType<TestDtoAgeComparer>(comparer);
        
        // Test Comparer-Verhalten
        var dto1 = new TestDto("John", 25);
        var dto2 = new TestDto("Jane", 25);
        var dto3 = new TestDto("Bob", 30);
        
        Assert.True(comparer.Equals(dto1, dto2)); // Gleiches Alter
        Assert.False(comparer.Equals(dto1, dto3)); // Verschiedenes Alter
    }

    [Fact]
    public void FullBootstrapWithComparerRegistration_Should_WorkEndToEnd()
    {
        // Arrange - Vollständiger realistischer Bootstrap
        var services = new ServiceCollection();
        
        // 1. Registriere Custom-Comparer (würde normalerweise durch Assembly-Scan passieren)
        services.AddSingleton<IEqualityComparer<TestDto>, TestDtoNameComparer>();
        
        // 2. Bootstrap mit DataStoresServiceModule
        var bootstrap = new DefaultBootstrapWrapper();
        bootstrap.RegisterServices(
            services,
            typeof(DataStoresServiceModule).Assembly);
        
        var provider = services.BuildServiceProvider();

        // Act - Service über DI auflösen
        var comparerService = provider.GetRequiredService<IEqualityComparerService>();
        
        // TestDto: Sollte Custom-Comparer bekommen
        var testDtoComparer = comparerService.GetComparer<TestDto>();
        
        // TestEntity: Sollte EntityIdComparer bekommen
        var entityComparer = comparerService.GetComparer<TestEntity>();

        // Assert
        Assert.IsType<TestDtoNameComparer>(testDtoComparer);
        
        // TestEntity-Comparer sollte ID-basiert sein
        var entity1 = new TestEntity { Id = 1, Name = "A" };
        var entity2 = new TestEntity { Id = 1, Name = "B" };
        Assert.True(entityComparer.Equals(entity1, entity2));
    }

    [Fact]
    public void ServiceProvider_Should_BeInjectedCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var module = new DataStoresServiceModule();
        module.Register(services);
        var provider = services.BuildServiceProvider();

        // Act - Service wird mit IServiceProvider erstellt
        var service = provider.GetRequiredService<IEqualityComparerService>();

        // Assert - Service sollte funktionieren (beweist, dass IServiceProvider injiziert wurde)
        var comparer = service.GetComparer<TestEntity>();
        Assert.NotNull(comparer);
    }

    [Fact]
    public void MultipleComparerTypes_Should_AllBeResolved()
    {
        // Arrange - Mehrere Custom-Comparer registrieren
        var services = new ServiceCollection();
        services.AddSingleton<IEqualityComparer<TestDto>, TestDtoNameComparer>();
        services.AddSingleton<IEqualityComparer<string>, StringLengthComparer>();
        
        var module = new DataStoresServiceModule();
        module.Register(services);
        var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<IEqualityComparerService>();

        // Act
        var dtoComparer = service.GetComparer<TestDto>();
        var stringComparer = service.GetComparer<string>();
        var entityComparer = service.GetComparer<TestEntity>();

        // Assert
        Assert.IsType<TestDtoNameComparer>(dtoComparer);
        Assert.IsType<StringLengthComparer>(stringComparer);
        
        // TestEntity sollte automatisch EntityIdComparer bekommen
        var entity = new TestEntity { Id = 1 };
        Assert.NotEqual(0, entityComparer.GetHashCode(entity));
    }

    [Fact]
    public void ComparerService_InRealisticScenario_Should_WorkWithDataStores()
    {
        // Arrange - Vollständiges Setup wie in echter Anwendung
        var services = new ServiceCollection();
        
        // Bootstrap mit allen Modulen
        var bootstrap = new DefaultBootstrapWrapper();
        bootstrap.RegisterServices(
            services,
            typeof(DataStoresServiceModule).Assembly);
        
        var provider = services.BuildServiceProvider();
        
        // Act - Verwende Service zusammen mit DataStores
        var comparerService = provider.GetRequiredService<IEqualityComparerService>();
        var stores = provider.GetRequiredService<IDataStores>();
        
        // Erstelle Store mit automatischem Comparer
        var entityComparer = comparerService.GetComparer<TestEntity>();
        var store = new InMemoryDataStore<TestEntity>(entityComparer);
        
        var entity1 = new TestEntity { Id = 1, Name = "Test" };
        var entity2 = new TestEntity { Id = 1, Name = "Different" };
        
        store.Add(entity1);

        // Assert - Store sollte entity2 finden (gleiche ID)
        Assert.True(store.Contains(entity2));
    }

    [Fact]
    public void GetComparer_WithoutRegistration_Should_ReturnDefault()
    {
        // Arrange - Keine Custom-Comparer registriert
        var services = new ServiceCollection();
        var module = new DataStoresServiceModule();
        module.Register(services);
        var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<IEqualityComparerService>();

        // Act
        var comparer = service.GetComparer<TestDto>();

        // Assert - Sollte Default-Comparer sein
        Assert.Same(EqualityComparer<TestDto>.Default, comparer);
    }

    [Fact]
    public void ServiceLifetime_Should_AllowThreadSafeUsage()
    {
        // Arrange
        var services = new ServiceCollection();
        var module = new DataStoresServiceModule();
        module.Register(services);
        var provider = services.BuildServiceProvider();

        // Act - Concurrent access von mehreren Threads
        var tasks = Enumerable.Range(0, 10).Select(_ => Task.Run(() =>
        {
            var service = provider.GetRequiredService<IEqualityComparerService>();
            var comparer = service.GetComparer<TestEntity>();
            
            var entity = new TestEntity { Id = 1 };
            return comparer.GetHashCode(entity);
        })).ToArray();

        Task.WaitAll(tasks);

        // Assert - Alle Tasks sollten erfolgreich sein (keine Exceptions)
        Assert.All(tasks, task => Assert.True(task.IsCompletedSuccessfully));
        
        // Alle Hash-Codes sollten gleich sein (gleiche ID)
        var hashes = tasks.Select(t => t.Result).Distinct().ToList();
        Assert.Single(hashes);
    }

    // ─────────────────────────────────────────────────────────────
    // Test-Helper: Custom Comparers
    // ─────────────────────────────────────────────────────────────

    private class TestDtoAgeComparer : IEqualityComparer<TestDto>
    {
        public bool Equals(TestDto? x, TestDto? y)
        {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;
            return x.Age == y.Age;
        }

        public int GetHashCode(TestDto obj) => obj?.Age ?? 0;
    }

    private class TestDtoNameComparer : IEqualityComparer<TestDto>
    {
        public bool Equals(TestDto? x, TestDto? y)
        {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;
            return x.Name == y.Name;
        }

        public int GetHashCode(TestDto obj) => obj?.Name?.GetHashCode() ?? 0;
    }

    private class StringLengthComparer : IEqualityComparer<string>
    {
        public bool Equals(string? x, string? y)
        {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;
            return x.Length == y.Length;
        }

        public int GetHashCode(string obj) => obj?.Length ?? 0;
    }
}
