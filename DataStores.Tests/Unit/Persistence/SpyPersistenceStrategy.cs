using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DataStores.Persistence;

namespace DataStores.Tests.Unit.Persistence;

/// <summary>
/// Spy-Implementation von IPersistenceStrategy für Unit-Tests.
/// Zählt Save-Aufrufe ohne echtes Dateisystem.
/// </summary>
public class SpyPersistenceStrategy<T> : IPersistenceStrategy<T> where T : class
{
    private readonly object _lock = new();
    private IReadOnlyList<T> _data;
    private int _saveCallCount;
    private int _loadCallCount;
    private List<IReadOnlyList<T>> _savedSnapshots = new();

    public int SaveCallCount
    {
        get { lock (_lock)
            {
                return _saveCallCount;
            }
        }
    }

    public int LoadCallCount
    {
        get { lock (_lock)
            {
                return _loadCallCount;
            }
        }
    }

    public IReadOnlyList<IReadOnlyList<T>> SavedSnapshots
    {
        get { lock (_lock)
            {
                return _savedSnapshots.AsReadOnly();
            }
        }
    }

    public IReadOnlyList<T>? LastSavedSnapshot
    {
        get
        {
            lock (_lock)
            {
                return _savedSnapshots.Count > 0 ? _savedSnapshots[^1] : null;
            }
        }
    }

    public int? LastSavedSnapshotCount => LastSavedSnapshot?.Count;

    public SpyPersistenceStrategy(IReadOnlyList<T>? initialData = null)
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
            _data = items;
            _savedSnapshots.Add(items.ToList()); // Snapshot speichern
            return Task.CompletedTask;
        }
    }

    public Task UpdateSingleAsync(T item, CancellationToken cancellationToken = default)
    {
        // Spy: No-Op (Tests tracken SaveCallCount)
        return Task.CompletedTask;
    }

    public void SetItemsProvider(Func<IReadOnlyList<T>>? itemsProvider)
    {
        // Spy: No-Op
    }

    public void Reset()
    {
        lock (_lock)
        {
            _saveCallCount = 0;
            _loadCallCount = 0;
            _savedSnapshots.Clear();
        }
    }
}
