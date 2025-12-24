using DataStores.Persistence;

namespace TestHelper.DataStores.Persistence;

/// <summary>
/// Fake persistence strategy for testing purposes.
/// Allows testing persistence logic without actual I/O operations.
/// </summary>
public class FakePersistenceStrategy<T> : IPersistenceStrategy<T> where T : class
{
    private readonly object _lock = new();
    private IReadOnlyList<T> _data;
    private int _loadCallCount;
    private int _saveCallCount;

    public int LoadCallCount
    {
        get { lock (_lock)
            {
                return _loadCallCount;
            }
        }
    }

    public int SaveCallCount
    {
        get { lock (_lock)
            {
                return _saveCallCount;
            }
        }
    }

    public IReadOnlyList<T>? LastSavedItems { get; private set; }

    public FakePersistenceStrategy(IReadOnlyList<T>? initialData = null)
    {
        _data = initialData ?? Array.Empty<T>();
    }

    public Task<IReadOnlyList<T>> LoadAllAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _loadCallCount++;
            return Task.FromResult(_data);
        }
    }

    public Task SaveAllAsync(IReadOnlyList<T> items, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _saveCallCount++;
            LastSavedItems = items;
            _data = items;
            return Task.CompletedTask;
        }
    }

    public Task UpdateSingleAsync(T item, CancellationToken cancellationToken = default)
    {
        // Fake: No-Op (Tests können SaveCallCount tracken)
        return Task.CompletedTask;
    }

    public void SetItemsProvider(Func<IReadOnlyList<T>>? itemsProvider)
    {
        // Fake: No-Op
    }

    public void SetData(IReadOnlyList<T> data)
    {
        lock (_lock)
        {
            _data = data;
        }
    }
}
