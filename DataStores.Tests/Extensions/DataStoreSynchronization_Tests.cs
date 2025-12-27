using DataStores.Abstractions;
using DataStores.Extensions;
using DataStores.Runtime;
using TestHelper.DataStores.Comparers;
using TestHelper.DataStores.Fakes;
using TestHelper.DataStores.Models;
using Xunit;

namespace DataStores.Tests.Extensions;

/// <summary>
/// Tests for bidirectional data store synchronization.
/// </summary>
[Trait("Category", "Unit")]
public class DataStoreSynchronization_Tests
{
    private readonly IEqualityComparerService _comparerService = new FakeEqualityComparerService();

    [Fact]
    public void SynchronizeWith_WithNullSource_Should_Throw()
    {
        // Arrange
        IDataStore<TestDto> source = null!;
        var target = new InMemoryDataStore<TestDto>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            source.SynchronizeWith(target, _comparerService));
    }

    [Fact]
    public void SynchronizeWith_WithNullTarget_Should_Throw()
    {
        // Arrange
        var source = new InMemoryDataStore<TestDto>();
        IDataStore<TestDto> target = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            source.SynchronizeWith(target, _comparerService));
    }

    [Fact]
    public void SynchronizeWith_WithNullComparerService_Should_Throw()
    {
        // Arrange
        var source = new InMemoryDataStore<TestDto>();
        var target = new InMemoryDataStore<TestDto>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            source.SynchronizeWith(target, null!));
    }

    [Fact]
    public void SynchronizeWith_Add_SourceToTarget_Should_Propagate()
    {
        // Arrange
        var source = new InMemoryDataStore<TestDto>();
        var target = new InMemoryDataStore<TestDto>();
        
        using var sync = source.SynchronizeWith(target, _comparerService, new SyncOptions
        {
            SyncSourceToTarget = true,
            SyncTargetToSource = false,
            InitialSync = false
        });

        // Act
        var item = new TestDto("Test", 25);
        source.Add(item);

        // Assert
        Assert.Single(target.Items);
        Assert.Equal("Test", target.Items[0].Name);
    }

    [Fact]
    public void SynchronizeWith_Add_TargetToSource_Should_Propagate()
    {
        // Arrange
        var source = new InMemoryDataStore<TestDto>();
        var target = new InMemoryDataStore<TestDto>();
        
        using var sync = source.SynchronizeWith(target, _comparerService, new SyncOptions
        {
            SyncSourceToTarget = false,
            SyncTargetToSource = true,
            InitialSync = false
        });

        // Act
        var item = new TestDto("Test", 25);
        target.Add(item);

        // Assert
        Assert.Single(source.Items);
        Assert.Equal("Test", source.Items[0].Name);
    }

    [Fact]
    public void SynchronizeWith_Bidirectional_Should_SyncBothWays()
    {
        // Arrange
        var source = new InMemoryDataStore<TestDto>();
        var target = new InMemoryDataStore<TestDto>();
        
        using var sync = source.SynchronizeWith(target, _comparerService, new SyncOptions
        {
            SyncSourceToTarget = true,
            SyncTargetToSource = true,
            InitialSync = false
        });

        // Act
        source.Add(new TestDto("FromSource", 25));
        target.Add(new TestDto("FromTarget", 30));

        // Assert
        Assert.Equal(2, source.Items.Count);
        Assert.Equal(2, target.Items.Count);
    }

    [Fact]
    public void SynchronizeWith_Remove_Should_Propagate()
    {
        // Arrange
        var source = new InMemoryDataStore<TestDto>();
        var target = new InMemoryDataStore<TestDto>();
        
        var item = new TestDto("Test", 25);
        source.Add(item);
        target.Add(item);

        using var sync = source.SynchronizeWith(target, _comparerService);

        // Act
        source.Remove(item);

        // Assert
        Assert.Empty(source.Items);
        Assert.Empty(target.Items);
    }

    [Fact]
    public void SynchronizeWith_Clear_Should_Propagate()
    {
        // Arrange
        var source = new InMemoryDataStore<TestDto>();
        var target = new InMemoryDataStore<TestDto>();
        
        source.AddRange(new[]
        {
            new TestDto("A", 20),
            new TestDto("B", 30)
        });
        target.AddRange(new[]
        {
            new TestDto("A", 20),
            new TestDto("B", 30)
        });

        using var sync = source.SynchronizeWith(target, _comparerService);

        // Act
        source.Clear();

        // Assert
        Assert.Empty(source.Items);
        Assert.Empty(target.Items);
    }

    [Fact]
    public void SynchronizeWith_InitialSync_Should_CopyMissingItems()
    {
        // Arrange
        var source = new InMemoryDataStore<TestDto>();
        var target = new InMemoryDataStore<TestDto>();
        
        source.AddRange(new[]
        {
            new TestDto("A", 20),
            new TestDto("B", 30),
            new TestDto("C", 40)
        });

        // Act
        using var sync = source.SynchronizeWith(target, _comparerService, new SyncOptions
        {
            InitialSync = true
        });

        // Assert
        Assert.Equal(3, target.Items.Count);
    }

    [Fact]
    public void SynchronizeWith_Dispose_Should_StopSync()
    {
        // Arrange
        var source = new InMemoryDataStore<TestDto>();
        var target = new InMemoryDataStore<TestDto>();
        
        var sync = source.SynchronizeWith(target, _comparerService);

        // Act
        sync.Dispose();
        source.Add(new TestDto("AfterDispose", 25));

        // Assert
        Assert.Single(source.Items);
        Assert.Empty(target.Items); // Not synchronized after dispose
    }

    [Fact]
    public void SynchronizeWith_WithCustomComparer_Should_PreventDuplicates()
    {
        // Arrange
        var comparer = new KeySelectorEqualityComparer<TestDto, string>(x => x.Name);
        var source = new InMemoryDataStore<TestDto>(comparer);
        var target = new InMemoryDataStore<TestDto>(comparer);
        
        var item = new TestDto("Test", 25);
        source.Add(item);

        using var sync = source.SynchronizeWith(target, _comparerService, new SyncOptions
        {
            Comparer = comparer,
            InitialSync = true
        });

        // Act - Try to add duplicate item with same Name
        // This should be prevented by InMemoryDataStore's duplicate prevention
        Assert.Throws<InvalidOperationException>(() => 
            source.Add(new TestDto("Test", 99)));

        // Assert - No duplicates created
        Assert.Single(source.Items);
        Assert.Single(target.Items);
        Assert.Equal(25, source.Items[0].Age); // Original value unchanged
        Assert.Equal(25, target.Items[0].Age); // Target also unchanged
    }

    [Fact]
    public void SynchronizeWith_BulkAdd_Should_SyncIndividually()
    {
        // Arrange
        var source = new InMemoryDataStore<TestDto>();
        var target = new InMemoryDataStore<TestDto>();
        
        using var sync = source.SynchronizeWith(target, _comparerService);

        // Act
        source.AddRange(new[]
        {
            new TestDto("A", 20),
            new TestDto("B", 30),
            new TestDto("C", 40)
        });

        // Assert
        Assert.Equal(3, target.Items.Count);
    }

    [Fact]
    public void SynchronizeWith_NoDuplicates_WhenItemAlreadyExists()
    {
        // Arrange
        var comparer = new KeySelectorEqualityComparer<TestDto, string>(x => x.Name);
        var source = new InMemoryDataStore<TestDto>(comparer);
        var target = new InMemoryDataStore<TestDto>(comparer);
        
        var item = new TestDto("Test", 25);
        target.Add(item);

        using var sync = source.SynchronizeWith(target, _comparerService, new SyncOptions
        {
            Comparer = comparer
        });

        // Act - Try to add same item to source
        source.Add(item);

        // Assert - Should not duplicate in target
        Assert.Single(source.Items);
        Assert.Single(target.Items);
    }

    [Fact]
    public void SynchronizeWith_ComplexScenario_MasterDetailPattern()
    {
        // Arrange
        var globalStore = new InMemoryDataStore<TestDto>();
        var localStore = new InMemoryDataStore<TestDto>();
        
        // Initial global data
        globalStore.AddRange(new[]
        {
            new TestDto("Global1", 10),
            new TestDto("Global2", 20)
        });

        // Act - Setup bidirectional sync
        using var sync = globalStore.SynchronizeWith(localStore, _comparerService, new SyncOptions
        {
            SyncSourceToTarget = true,
            SyncTargetToSource = true,
            InitialSync = true
        });

        // Local adds new item
        localStore.Add(new TestDto("Local1", 30));
        
        // Global adds new item
        globalStore.Add(new TestDto("Global3", 40));

        // Assert
        Assert.Equal(4, globalStore.Items.Count);
        Assert.Equal(4, localStore.Items.Count);
    }

    [Fact]
    public void SynchronizeWith_Performance_LargeDataset()
    {
        // Arrange
        var source = new InMemoryDataStore<TestDto>();
        var target = new InMemoryDataStore<TestDto>();
        
        using var sync = source.SynchronizeWith(target, _comparerService);

        // Act
        var startTime = DateTime.UtcNow;
        source.AddRange(Enumerable.Range(1, 1000)
            .Select(i => new TestDto($"Item{i}", i)));
        var duration = DateTime.UtcNow - startTime;

        // Assert
        Assert.Equal(1000, source.Items.Count);
        Assert.Equal(1000, target.Items.Count);
        Assert.True(duration.TotalSeconds < 2, $"Sync took {duration.TotalSeconds}s");
    }
}
