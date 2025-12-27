using DataStores.Abstractions;
using DataStores.Runtime;
using Microsoft.Extensions.DependencyInjection;
using TestHelper.DataStores.Models;
using Xunit;

namespace DataStores.Tests.Runtime;

/// <summary>
/// Unit-Tests für EqualityComparerService.
/// Tests ohne vollständigen DI-Container (siehe EqualityComparerService_IntegrationTests für DI-Tests).
/// </summary>
public class EqualityComparerService_UnitTests
{
    [Fact]
    public void Constructor_WithNullServiceProvider_Should_Throw()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => 
            new EqualityComparerService(null!));
        
        Assert.Equal("serviceProvider", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithValidServiceProvider_Should_Succeed()
    {
        // Arrange
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        // Act
        var service = new EqualityComparerService(provider);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void GetComparer_ForEntityBase_Should_ReturnEntityIdComparer()
    {
        // Arrange
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var service = new EqualityComparerService(provider);

        // Act
        var comparer = service.GetComparer<TestEntity>();

        // Assert
        Assert.NotNull(comparer);
        
        // Test EntityIdComparer-Verhalten: Vergleich nach ID
        var entity1 = new TestEntity { Id = 1, Name = "A" };
        var entity2 = new TestEntity { Id = 1, Name = "B" };
        var entity3 = new TestEntity { Id = 2, Name = "A" };
        
        Assert.True(comparer.Equals(entity1, entity2)); // Gleiche ID
        Assert.False(comparer.Equals(entity1, entity3)); // Verschiedene IDs
    }

    [Fact]
    public void GetComparer_ForEntityBase_WithIdZero_Should_UseReferenceEquality()
    {
        // Arrange
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var service = new EqualityComparerService(provider);

        // Act
        var comparer = service.GetComparer<TestEntity>();

        // Assert
        var newEntity1 = new TestEntity { Id = 0, Name = "A" };
        var newEntity2 = new TestEntity { Id = 0, Name = "A" };
        
        // Neue Entities (Id = 0) sind nur bei Referenzgleichheit gleich
        Assert.True(comparer.Equals(newEntity1, newEntity1)); // Same reference
        Assert.False(comparer.Equals(newEntity1, newEntity2)); // Different references
    }

    [Fact]
    public void GetComparer_ForEntityBase_HashCode_Should_BeConsistent()
    {
        // Arrange
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var service = new EqualityComparerService(provider);
        var comparer = service.GetComparer<TestEntity>();

        // Act & Assert - Persistierte Entity
        var entity1 = new TestEntity { Id = 42, Name = "Test" };
        var entity2 = new TestEntity { Id = 42, Name = "Different" };
        
        Assert.Equal(comparer.GetHashCode(entity1), comparer.GetHashCode(entity2));
        
        // Act & Assert - Neue Entity
        var newEntity = new TestEntity { Id = 0, Name = "New" };
        var hash1 = comparer.GetHashCode(newEntity);
        var hash2 = comparer.GetHashCode(newEntity);
        
        Assert.Equal(hash1, hash2); // Same instance → same hash
    }

    [Fact]
    public void GetComparer_ForNonEntityBase_Should_ReturnDefaultComparer()
    {
        // Arrange
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var service = new EqualityComparerService(provider);

        // Act
        var comparer = service.GetComparer<TestDto>();

        // Assert
        Assert.NotNull(comparer);
        Assert.Equal(EqualityComparer<TestDto>.Default, comparer);
    }

    [Fact]
    public void GetComparer_WithRegisteredCustomComparer_Should_ReturnCustomComparer()
    {
        // Arrange
        var customComparer = new TestDtoNameComparer();
        var services = new ServiceCollection();
        services.AddSingleton<IEqualityComparer<TestDto>>(customComparer);
        var provider = services.BuildServiceProvider();
        var service = new EqualityComparerService(provider);

        // Act
        var comparer = service.GetComparer<TestDto>();

        // Assert
        Assert.Same(customComparer, comparer);
        
        // Test Custom-Comparer-Verhalten
        var dto1 = new TestDto("John", 25);
        var dto2 = new TestDto("John", 30);
        var dto3 = new TestDto("Jane", 25);
        
        Assert.True(comparer.Equals(dto1, dto2)); // Gleicher Name
        Assert.False(comparer.Equals(dto1, dto3)); // Verschiedene Namen
    }

    [Fact]
    public void GetComparer_ForString_Should_ReturnStringDefaultComparer()
    {
        // Arrange
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var service = new EqualityComparerService(provider);

        // Act
        var comparer = service.GetComparer<string>();

        // Assert
        Assert.NotNull(comparer);
        Assert.Same(EqualityComparer<string>.Default, comparer);
    }

    [Fact]
    public void GetComparer_CalledMultipleTimes_Should_ReturnNewInstances_ForEntityBase()
    {
        // Arrange
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var service = new EqualityComparerService(provider);

        // Act
        var comparer1 = service.GetComparer<TestEntity>();
        var comparer2 = service.GetComparer<TestEntity>();

        // Assert - EntityIdComparer wird jedes Mal neu erstellt (nicht gecacht)
        Assert.NotSame(comparer1, comparer2);
        
        // Aber beide funktionieren identisch
        var entity = new TestEntity { Id = 1, Name = "Test" };
        Assert.Equal(comparer1.GetHashCode(entity), comparer2.GetHashCode(entity));
    }

    [Fact]
    public void GetComparer_WithNullHandling_Should_WorkCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var service = new EqualityComparerService(provider);
        var comparer = service.GetComparer<TestEntity>();

        // Act & Assert
        TestEntity? nullEntity = null;
        var entity = new TestEntity { Id = 1 };
        
        Assert.True(comparer.Equals(nullEntity, nullEntity));
        Assert.False(comparer.Equals(nullEntity, entity));
        Assert.False(comparer.Equals(entity, nullEntity));
    }

    [Fact]
    public void GetComparer_ForEntityBase_WithSameIdDifferentProperties_Should_BeEqual()
    {
        // Arrange
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var service = new EqualityComparerService(provider);
        var comparer = service.GetComparer<TestEntity>();

        // Act
        var entity1 = new TestEntity 
        { 
            Id = 100, 
            Name = "Original",
            Version = 1,
            Ratio = 1.5
        };
        
        var entity2 = new TestEntity 
        { 
            Id = 100, 
            Name = "Modified",
            Version = 2,
            Ratio = 2.5
        };

        // Assert - Nur ID zählt
        Assert.True(comparer.Equals(entity1, entity2));
        Assert.Equal(comparer.GetHashCode(entity1), comparer.GetHashCode(entity2));
    }

    // Test-Helper: Custom Comparer für TestDto (vergleicht nur Name)
    private class TestDtoNameComparer : IEqualityComparer<TestDto>
    {
        public bool Equals(TestDto? x, TestDto? y)
        {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;
            return x.Name == y.Name;
        }

        public int GetHashCode(TestDto obj)
        {
            return obj?.Name?.GetHashCode() ?? 0;
        }
    }
}
