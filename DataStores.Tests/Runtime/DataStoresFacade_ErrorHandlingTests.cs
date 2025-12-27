using DataStores.Abstractions;
using DataStores.Runtime;
using TestHelper.DataStores.Fakes;
using Xunit;

namespace DataStores.Tests.Runtime;

/// <summary>
/// Error handling and edge case tests for DataStoresFacade.
/// </summary>
public class DataStoresFacade_ErrorHandlingTests
{
    private static DataStoresFacade CreateFacade(IGlobalStoreRegistry registry, ILocalDataStoreFactory factory)
    {
        return new DataStoresFacade(registry, factory, new FakeEqualityComparerService());
    }

    [Fact]
    public void GetGlobal_MissingStore_Should_ThrowWithTypeName()
    {
        // Arrange
        var registry = new GlobalStoreRegistry();
        var factory = new LocalDataStoreFactory();
        var facade = CreateFacade(registry, factory);

        // Act & Assert
        var ex = Assert.Throws<GlobalStoreNotRegisteredException>(() =>
            facade.GetGlobal<TestItem>());

        Assert.Contains(nameof(TestItem), ex.Message);
        Assert.Equal(typeof(TestItem), ex.StoreType);
    }

    [Fact]
    public void CreateLocal_WithNullComparer_Should_UseDefault()
    {
        // Arrange
        var registry = new GlobalStoreRegistry();
        var factory = new LocalDataStoreFactory();
        var facade = CreateFacade(registry, factory);

        // Act
        var local = facade.CreateLocal<TestItem>(comparer: null);
        
        var item1 = new TestItem { Id = 1, Name = "A" };
        var item2 = new TestItem { Id = 1, Name = "A" }; // Different instance

        local.Add(item1);

        // Assert - Default comparer uses reference equality
        Assert.False(local.Contains(item2));
    }

    [Fact]
    public void CreateLocalSnapshotFromGlobal_MissingStore_Should_Throw()
    {
        // Arrange
        var registry = new GlobalStoreRegistry();
        var factory = new LocalDataStoreFactory();
        var facade = CreateFacade(registry, factory);

        // Act & Assert
        Assert.Throws<GlobalStoreNotRegisteredException>(() =>
            facade.CreateLocalSnapshotFromGlobal<TestItem>());
    }

    [Fact]
    public void CreateLocalSnapshotFromGlobal_WithNullPredicate_Should_CopyAll()
    {
        // Arrange
        var registry = new GlobalStoreRegistry();
        var factory = new LocalDataStoreFactory();
        var facade = CreateFacade(registry, factory);
        
        var globalStore = new InMemoryDataStore<TestItem>();
        globalStore.AddRange(new[]
        {
            new TestItem { Id = 1, Name = "Item1" },
            new TestItem { Id = 2, Name = "Item2" },
            new TestItem { Id = 3, Name = "Item3" }
        });
        registry.RegisterGlobal(globalStore);

        // Act
        var snapshot = facade.CreateLocalSnapshotFromGlobal<TestItem>(predicate: null);

        // Assert - Should copy all items
        Assert.Equal(3, snapshot.Items.Count);
    }

    [Fact]
    public void Constructor_WithNullRegistry_Should_Throw()
    {
        // Arrange
        var factory = new LocalDataStoreFactory();
        var comparerService = new FakeEqualityComparerService();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new DataStoresFacade(null!, factory, comparerService));
    }

    [Fact]
    public void Constructor_WithNullFactory_Should_Throw()
    {
        // Arrange
        var registry = new GlobalStoreRegistry();
        var comparerService = new FakeEqualityComparerService();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new DataStoresFacade(registry, null!, comparerService));
    }

    [Fact]
    public void GetGlobal_MultipleTypes_Should_ReturnCorrectStore()
    {
        // Arrange
        var registry = new GlobalStoreRegistry();
        var factory = new LocalDataStoreFactory();
        var facade = CreateFacade(registry, factory);
        
        var store1 = new InMemoryDataStore<TestItem>();
        var store2 = new InMemoryDataStore<OtherTestItem>();
        
        registry.RegisterGlobal(store1);
        registry.RegisterGlobal(store2);

        // Act
        var resolved1 = facade.GetGlobal<TestItem>();
        var resolved2 = facade.GetGlobal<OtherTestItem>();

        // Assert
        Assert.Same(store1, resolved1);
        Assert.Same(store2, resolved2);
        Assert.NotSame(resolved1, resolved2);
    }

    [Fact]
    public void CreateLocal_Should_NotShareState()
    {
        // Arrange
        var registry = new GlobalStoreRegistry();
        var factory = new LocalDataStoreFactory();
        var facade = CreateFacade(registry, factory);

        // Act
        var local1 = facade.CreateLocal<TestItem>();
        var local2 = facade.CreateLocal<TestItem>();
        
        local1.Add(new TestItem { Id = 1, Name = "Item1" });
        local2.Add(new TestItem { Id = 2, Name = "Item2" });

        // Assert - Completely independent
        Assert.Single(local1.Items);
        Assert.Single(local2.Items);
        Assert.NotEqual(local1.Items[0].Id, local2.Items[0].Id);
    }

    [Fact]
    public void Snapshot_Should_NotBeAffectedByGlobalChanges()
    {
        // Arrange
        var registry = new GlobalStoreRegistry();
        var factory = new LocalDataStoreFactory();
        var facade = CreateFacade(registry, factory);
        
        var globalStore = new InMemoryDataStore<TestItem>();
        globalStore.Add(new TestItem { Id = 1, Name = "Item1" });
        registry.RegisterGlobal(globalStore);

        // Act
        var snapshot = facade.CreateLocalSnapshotFromGlobal<TestItem>();
        
        // Modify global after snapshot
        globalStore.Add(new TestItem { Id = 2, Name = "Item2" });
        globalStore.Add(new TestItem { Id = 3, Name = "Item3" });

        // Assert - Snapshot unchanged
        Assert.Single(snapshot.Items);
        Assert.Equal(3, globalStore.Items.Count);
    }

    [Fact]
    public void MultipleSnapshots_Should_BeIndependent()
    {
        // Arrange
        var registry = new GlobalStoreRegistry();
        var factory = new LocalDataStoreFactory();
        var facade = CreateFacade(registry, factory);
        
        var globalStore = new InMemoryDataStore<TestItem>();
        globalStore.AddRange(new[]
        {
            new TestItem { Id = 1, Name = "Item1" },
            new TestItem { Id = 2, Name = "Item2" },
            new TestItem { Id = 3, Name = "Item3" }
        });
        registry.RegisterGlobal(globalStore);

        // Act
        var snapshot1 = facade.CreateLocalSnapshotFromGlobal<TestItem>(x => x.Id > 1);
        var snapshot2 = facade.CreateLocalSnapshotFromGlobal<TestItem>(x => x.Id < 3);
        
        snapshot1.Add(new TestItem { Id = 99, Name = "Snapshot1Only" });
        snapshot2.Add(new TestItem { Id = 88, Name = "Snapshot2Only" });

        // Assert
        Assert.Equal(3, snapshot1.Items.Count); // 2 filtered + 1 added
        Assert.Equal(3, snapshot2.Items.Count); // 2 filtered + 1 added
        Assert.Equal(3, globalStore.Items.Count); // Unchanged
    }

    [Fact]
    public void CreateLocalSnapshotFromGlobal_WithComplexPredicate_Should_FilterCorrectly()
    {
        // Arrange
        var registry = new GlobalStoreRegistry();
        var factory = new LocalDataStoreFactory();
        var facade = CreateFacade(registry, factory);
        
        var globalStore = new InMemoryDataStore<TestItem>();
        globalStore.AddRange(new[]
        {
            new TestItem { Id = 1, Name = "ActiveItem" },
            new TestItem { Id = 2, Name = "InactiveItem" },
            new TestItem { Id = 3, Name = "ActiveItem" },
            new TestItem { Id = 4, Name = "ArchivedItem" }
        });
        registry.RegisterGlobal(globalStore);

        // Act
        var snapshot = facade.CreateLocalSnapshotFromGlobal<TestItem>(
            x => x.Name.Contains("Active") && x.Id > 1);

        // Assert - Only Id=3 matches both conditions
        Assert.Single(snapshot.Items);
        Assert.Equal(3, snapshot.Items[0].Id);
    }

    [Fact]
    public void CreateLocal_WithCustomComparer_Should_UseComparer()
    {
        // Arrange
        var registry = new GlobalStoreRegistry();
        var factory = new LocalDataStoreFactory();
        var facade = CreateFacade(registry, factory);
        
        var comparer = new IdOnlyComparer();

        // Act
        var local = facade.CreateLocal<TestItem>(comparer);
        local.Add(new TestItem { Id = 1, Name = "Original" });

        // Assert - Should find by Id only
        Assert.True(local.Contains(new TestItem { Id = 1, Name = "Different" }));
    }

    [Fact]
    public void GetGlobal_CalledMultipleTimes_Should_ReturnSameInstance()
    {
        // Arrange
        var registry = new GlobalStoreRegistry();
        var factory = new LocalDataStoreFactory();
        var facade = CreateFacade(registry, factory);
        
        var globalStore = new InMemoryDataStore<TestItem>();
        registry.RegisterGlobal(globalStore);

        // Act
        var resolved1 = facade.GetGlobal<TestItem>();
        var resolved2 = facade.GetGlobal<TestItem>();
        var resolved3 = facade.GetGlobal<TestItem>();

        // Assert
        Assert.Same(resolved1, resolved2);
        Assert.Same(resolved2, resolved3);
        Assert.Same(globalStore, resolved1);
    }

    [Fact]
    public void CreateLocalSnapshotFromGlobal_EmptyGlobalStore_Should_CreateEmptySnapshot()
    {
        // Arrange
        var registry = new GlobalStoreRegistry();
        var factory = new LocalDataStoreFactory();
        var facade = CreateFacade(registry, factory);
        
        var globalStore = new InMemoryDataStore<TestItem>();
        registry.RegisterGlobal(globalStore);

        // Act
        var snapshot = facade.CreateLocalSnapshotFromGlobal<TestItem>();

        // Assert
        Assert.Empty(snapshot.Items);
        Assert.NotSame(globalStore, snapshot);
    }

    private class TestItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    private class OtherTestItem
    {
        public int Id { get; set; }
        public string Value { get; set; } = "";
    }

    private class IdOnlyComparer : IEqualityComparer<TestItem>
    {
        public bool Equals(TestItem? x, TestItem? y)
        {
            if (x == null && y == null)
            {
                return true;
            }

            if (x == null || y == null)
            {
                return false;
            }

            return x.Id == y.Id;
        }

        public int GetHashCode(TestItem obj)
        {
            return obj.Id.GetHashCode();
        }
    }
}
