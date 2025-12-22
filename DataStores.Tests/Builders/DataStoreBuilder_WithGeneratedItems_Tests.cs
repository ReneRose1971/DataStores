using DataStores.Abstractions;
using TestHelper.DataStores.Builders;
using TestHelper.DataStores.Comparers;
using TestHelper.DataStores.Models;
using TestHelper.DataStores.TestData;
using Xunit;

namespace DataStores.Tests.Builders;

/// <summary>
/// Tests für DataStoreBuilder mit Testdaten-Generierung.
/// </summary>
[Trait("Category", "Unit")]
public class DataStoreBuilder_WithGeneratedItems_Tests
{
    [Fact]
    public void WithGeneratedItems_Should_AddGeneratedItems()
    {
        // Arrange
        var factory = new ObjectFillerTestDataFactory<TestEntity>(seed: 42);

        // Act
        var store = new DataStoreBuilder<TestEntity>()
            .WithGeneratedItems(factory, count: 10)
            .Build();

        // Assert
        Assert.Equal(10, store.Items.Count);
    }

    [Fact]
    public void WithGeneratedItems_WithZeroCount_Should_CreateEmptyStore()
    {
        // Arrange
        var factory = new ObjectFillerTestDataFactory<TestEntity>(seed: 42);

        // Act
        var store = new DataStoreBuilder<TestEntity>()
            .WithGeneratedItems(factory, count: 0)
            .Build();

        // Assert
        Assert.Empty(store.Items);
    }

    [Fact]
    public void WithGeneratedItems_WithNullFactory_Should_ThrowArgumentNullException()
    {
        // Arrange
        var builder = new DataStoreBuilder<TestEntity>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            builder.WithGeneratedItems(null!, count: 10));
    }

    [Fact]
    public void WithGeneratedItems_WithNegativeCount_Should_ThrowArgumentOutOfRangeException()
    {
        // Arrange
        var factory = new ObjectFillerTestDataFactory<TestEntity>(seed: 42);
        var builder = new DataStoreBuilder<TestEntity>();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            builder.WithGeneratedItems(factory, count: -1));
    }

    [Fact]
    public void WithGeneratedItems_CombinedWithWithItems_Should_ContainBothItemSets()
    {
        // Arrange
        var factory = new ObjectFillerTestDataFactory<TestEntity>(seed: 42);
        var manualEntity = new TestEntity { Name = "Manual" };

        // Act
        var store = new DataStoreBuilder<TestEntity>()
            .WithItems(manualEntity)
            .WithGeneratedItems(factory, count: 5)
            .Build();

        // Assert
        Assert.Equal(6, store.Items.Count);
    }

    [Fact]
    public void WithGeneratedItems_CombinedWithWithItems_Should_PreserveManualItems()
    {
        // Arrange
        var factory = new ObjectFillerTestDataFactory<TestEntity>(seed: 42);
        var manualEntity = new TestEntity { Name = "Special" };

        // Act
        var store = new DataStoreBuilder<TestEntity>()
            .WithItems(manualEntity)
            .WithGeneratedItems(factory, count: 10)
            .Build();

        // Assert
        Assert.Contains(manualEntity, store.Items);
    }

    [Fact]
    public void WithGeneratedItems_MultipleInvocations_Should_AccumulateItems()
    {
        // Arrange
        var factory = new ObjectFillerTestDataFactory<TestEntity>(seed: 42);

        // Act
        var store = new DataStoreBuilder<TestEntity>()
            .WithGeneratedItems(factory, count: 5)
            .WithGeneratedItems(factory, count: 10)
            .Build();

        // Assert
        Assert.Equal(15, store.Items.Count);
    }

    [Fact]
    public void WithGeneratedItems_Should_RespectComparer()
    {
        // Arrange
        var factory = new ObjectFillerTestDataFactory<TestEntity>(seed: 42);
        var comparer = new KeySelectorEqualityComparer<TestEntity, int>(x => x.Id);

        // Act
        var store = new DataStoreBuilder<TestEntity>()
            .WithGeneratedItems(factory, count: 10)
            .WithComparer(comparer)
            .Build();
        var testEntity = new TestEntity { Id = store.Items.First().Id };

        // Assert
        Assert.True(store.Contains(testEntity));
    }

    [Fact]
    public void WithGeneratedItems_Should_FireChangedEvents()
    {
        // Arrange
        var factory = new ObjectFillerTestDataFactory<TestEntity>(seed: 42);
        var eventFired = false;

        // Act
        var store = new DataStoreBuilder<TestEntity>()
            .WithChangedHandler((s, e) => eventFired = true)
            .WithGeneratedItems(factory, count: 5)
            .Build();

        // Assert
        Assert.True(eventFired);
    }

    [Fact]
    public void WithGeneratedItems_Should_UseDeterministicDataWithSameSeed()
    {
        // Arrange
        var factory1 = new ObjectFillerTestDataFactory<TestEntity>(seed: 123);
        var factory2 = new ObjectFillerTestDataFactory<TestEntity>(seed: 123);

        // Act
        var store1 = new DataStoreBuilder<TestEntity>()
            .WithGeneratedItems(factory1, count: 5)
            .Build();
        var store2 = new DataStoreBuilder<TestEntity>()
            .WithGeneratedItems(factory2, count: 5)
            .Build();

        // Assert
        Assert.Equal(store1.Items.First().Name, store2.Items.First().Name);
    }

    [Fact]
    public void WithGeneratedItems_LargeCount_Should_HandleEfficiently()
    {
        // Arrange
        var factory = new ObjectFillerTestDataFactory<TestEntity>(seed: 42);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var store = new DataStoreBuilder<TestEntity>()
            .WithGeneratedItems(factory, count: 1000)
            .Build();
        stopwatch.Stop();

        // Assert (< 5 Sekunden für 1000 Entities + Store-Operationen)
        Assert.True(stopwatch.ElapsedMilliseconds < 5000);
    }

    [Fact]
    public void Build_Should_AddItemsInCorrectOrder()
    {
        // Arrange
        var factory = new ObjectFillerTestDataFactory<TestEntity>(seed: 42);
        var manualEntity1 = new TestEntity { Name = "First" };
        var manualEntity2 = new TestEntity { Name = "Last" };

        // Act
        var store = new DataStoreBuilder<TestEntity>()
            .WithItems(manualEntity1)
            .WithGeneratedItems(factory, count: 3)
            .WithItems(manualEntity2)
            .Build();

        // Assert
        Assert.Equal(manualEntity1, store.Items.First());
    }

    [Fact]
    public void Build_Should_AddLastManualItemLast()
    {
        // Arrange
        var factory = new ObjectFillerTestDataFactory<TestEntity>(seed: 42);
        var manualEntity1 = new TestEntity { Name = "First" };
        var manualEntity2 = new TestEntity { Name = "Last" };

        // Act
        var store = new DataStoreBuilder<TestEntity>()
            .WithItems(manualEntity1)
            .WithGeneratedItems(factory, count: 3)
            .WithItems(manualEntity2)
            .Build();

        // Assert
        Assert.Equal(manualEntity2, store.Items.Last());
    }
}
