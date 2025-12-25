using DataStores.Abstractions;
using DataStores.Runtime;
using TestHelper.DataStores.Models;

namespace DataStores.Tests;

[Trait("Category", "Unit")]
public class InMemoryDataStoreTests
{
    [Fact]
    public void Add_Should_AddItem()
    {
        var store = new InMemoryDataStore<TestDto>();
        var item = new TestDto("Test", 25);

        store.Add(item);

        Assert.Single(store.Items);
    }

    [Fact]
    public void Add_Should_ContainAddedItem()
    {
        var store = new InMemoryDataStore<TestDto>();
        var item = new TestDto("Test", 25);

        store.Add(item);

        Assert.Equal(item, store.Items[0]);
    }

    [Fact]
    public void Remove_Should_RemoveItem_WhenExists()
    {
        var store = new InMemoryDataStore<TestDto>();
        var item = new TestDto("Test", 25);
        store.Add(item);

        var result = store.Remove(item);

        Assert.True(result);
    }

    [Fact]
    public void Remove_Should_LeaveStoreEmpty_WhenLastItemRemoved()
    {
        var store = new InMemoryDataStore<TestDto>();
        var item = new TestDto("Test", 25);
        store.Add(item);

        store.Remove(item);

        Assert.Empty(store.Items);
    }

    [Fact]
    public void Remove_Should_ReturnFalse_WhenNotExists()
    {
        var store = new InMemoryDataStore<TestDto>();
        var item = new TestDto("Test", 25);

        var result = store.Remove(item);

        Assert.False(result);
    }

    [Fact]
    public void Clear_Should_RemoveAllItems()
    {
        var store = new InMemoryDataStore<TestDto>();
        store.Add(new TestDto("A", 20));
        store.Add(new TestDto("B", 30));

        store.Clear();

        Assert.Empty(store.Items);
    }

    [Fact]
    public void AddRange_Should_AddAllItems()
    {
        var store = new InMemoryDataStore<TestDto>();
        var items = new[]
        {
            new TestDto("A", 20),
            new TestDto("B", 30),
            new TestDto("C", 40)
        };

        store.AddRange(items);

        Assert.Equal(3, store.Items.Count);
    }

    [Fact]
    public void AddRange_Should_RaiseSingleBulkChangedEvent()
    {
        var store = new InMemoryDataStore<TestDto>();
        var eventCount = 0;
        DataStoreChangeType? changeType = null;

        store.Changed += (s, e) =>
        {
            eventCount++;
            changeType = e.ChangeType;
        };

        store.AddRange(new[] { new TestDto("A", 20), new TestDto("B", 30) });

        Assert.Equal(1, eventCount);
    }

    [Fact]
    public void AddRange_Should_RaiseBulkAddChangeType()
    {
        var store = new InMemoryDataStore<TestDto>();
        DataStoreChangeType? changeType = null;

        store.Changed += (s, e) => changeType = e.ChangeType;

        store.AddRange(new[] { new TestDto("A", 20), new TestDto("B", 30) });

        Assert.Equal(DataStoreChangeType.BulkAdd, changeType);
    }

    [Fact]
    public void Changed_Should_FireOnAdd()
    {
        var store = new InMemoryDataStore<TestDto>();
        var fired = false;

        store.Changed += (s, e) => fired = true;

        store.Add(new TestDto("Test", 25));

        Assert.True(fired);
    }

    [Fact]
    public void Changed_Should_FireOnRemove()
    {
        var store = new InMemoryDataStore<TestDto>();
        var item = new TestDto("Test", 25);
        store.Add(item);
        var fired = false;

        store.Changed += (s, e) => fired = true;

        store.Remove(item);

        Assert.True(fired);
    }

    [Fact]
    public void Changed_Should_FireOnClear()
    {
        var store = new InMemoryDataStore<TestDto>();
        store.Add(new TestDto("Test", 25));
        var fired = false;

        store.Changed += (s, e) => fired = true;

        store.Clear();

        Assert.True(fired);
    }

    [Fact]
    public void Contains_Should_ReturnTrue_WhenItemExists()
    {
        var store = new InMemoryDataStore<TestDto>();
        var item = new TestDto("Test", 25);
        store.Add(item);

        var result = store.Contains(item);

        Assert.True(result);
    }

    [Fact]
    public void Contains_Should_ReturnFalse_WhenItemDoesNotExist()
    {
        var store = new InMemoryDataStore<TestDto>();
        var item = new TestDto("Test", 25);

        var result = store.Contains(item);

        Assert.False(result);
    }

    [Fact]
    public void Items_Should_ReturnSnapshot()
    {
        var store = new InMemoryDataStore<TestDto>();
        var item1 = new TestDto("A", 20);
        store.Add(item1);

        var snapshot = store.Items;
        store.Add(new TestDto("B", 30));

        Assert.Single(snapshot);
    }
}
