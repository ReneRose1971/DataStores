using DataStores.Abstractions;

namespace TestHelper.DataStores.Fakes;

/// <summary>
/// Fake implementation of IDataStore for testing purposes.
/// Tracks all operations and provides controllable behavior.
/// </summary>
public class FakeDataStore<T> : IDataStore<T> where T : class
{
    private readonly List<T> _items = new();
    private readonly List<DataStoreChangedEventArgs<T>> _changedEvents = new();

    public IReadOnlyList<T> Items => _items.AsReadOnly();
    
    public event EventHandler<DataStoreChangedEventArgs<T>>? Changed;

    // Test Properties
    public int AddCallCount { get; private set; }
    public int AddOrReplaceCallCount { get; private set; }
    public int RemoveCallCount { get; private set; }
    public int ClearCallCount { get; private set; }
    public int AddRangeCallCount { get; private set; }
    public bool ThrowOnAdd { get; set; }
    public bool ThrowOnRemove { get; set; }
    public IReadOnlyList<DataStoreChangedEventArgs<T>> ChangedEvents => _changedEvents.AsReadOnly();

    public void Add(T item)
    {
        AddCallCount++;
        
        if (ThrowOnAdd)
        {
            throw new InvalidOperationException("Simulated add failure");
        }

        _items.Add(item);
        var args = new DataStoreChangedEventArgs<T>(DataStoreChangeType.Add, item);
        _changedEvents.Add(args);
        Changed?.Invoke(this, args);
    }

    public void AddOrReplace(T item)
    {
        AddOrReplaceCallCount++;
        
        var existingIndex = _items.FindIndex(x => EqualityComparer<T>.Default.Equals(x, item));
        bool wasReplaced;
        
        if (existingIndex >= 0)
        {
            _items[existingIndex] = item;
            wasReplaced = true;
        }
        else
        {
            _items.Add(item);
            wasReplaced = false;
        }

        var args = new DataStoreChangedEventArgs<T>(
            wasReplaced ? DataStoreChangeType.Update : DataStoreChangeType.Add, 
            item);
        _changedEvents.Add(args);
        Changed?.Invoke(this, args);
    }

    public void AddRange(IEnumerable<T> items)
    {
        AddRangeCallCount++;
        
        var itemList = items.ToList();
        _items.AddRange(itemList);
        
        var args = new DataStoreChangedEventArgs<T>(DataStoreChangeType.BulkAdd, itemList);
        _changedEvents.Add(args);
        Changed?.Invoke(this, args);
    }

    public bool Remove(T item)
    {
        RemoveCallCount++;
        
        if (ThrowOnRemove)
        {
            throw new InvalidOperationException("Simulated remove failure");
        }

        var removed = _items.Remove(item);
        if (removed)
        {
            var args = new DataStoreChangedEventArgs<T>(DataStoreChangeType.Remove, item);
            _changedEvents.Add(args);
            Changed?.Invoke(this, args);
        }
        return removed;
    }

    public void Clear()
    {
        ClearCallCount++;
        _items.Clear();
        
        var args = new DataStoreChangedEventArgs<T>(DataStoreChangeType.Clear);
        _changedEvents.Add(args);
        Changed?.Invoke(this, args);
    }

    public bool Contains(T item) => _items.Contains(item);

    public void Reset()
    {
        _items.Clear();
        _changedEvents.Clear();
        AddCallCount = 0;
        AddOrReplaceCallCount = 0;
        RemoveCallCount = 0;
        ClearCallCount = 0;
        AddRangeCallCount = 0;
        ThrowOnAdd = false;
        ThrowOnRemove = false;
    }
}
