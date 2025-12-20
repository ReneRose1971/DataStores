using DataStores.Persistence;

namespace TestHelper.DataStores.Persistence;

/// <summary>
/// Persistence strategy that introduces artificial delays for testing race conditions and timing issues.
/// </summary>
public class SlowLoadStrategy<T> : IPersistenceStrategy<T> where T : class
{
    private readonly TimeSpan _delay;
    private readonly IReadOnlyList<T> _data;

    public int LoadCallCount { get; private set; }
    public int SaveCallCount { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SlowLoadStrategy{T}"/> class.
    /// </summary>
    /// <param name="delay">The artificial delay to introduce during load operations.</param>
    /// <param name="data">The data to return after the delay.</param>
    public SlowLoadStrategy(TimeSpan delay, IReadOnlyList<T> data)
    {
        _delay = delay;
        _data = data;
    }

    public async Task<IReadOnlyList<T>> LoadAllAsync(CancellationToken cancellationToken = default)
    {
        LoadCallCount++;
        await Task.Delay(_delay, cancellationToken);
        return _data;
    }

    public Task SaveAllAsync(IReadOnlyList<T> items, CancellationToken cancellationToken = default)
    {
        SaveCallCount++;
        return Task.CompletedTask;
    }
}
