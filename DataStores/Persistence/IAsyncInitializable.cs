namespace DataStores.Persistence;

/// <summary>
/// Marker interface for types that require asynchronous initialization.
/// </summary>
public interface IAsyncInitializable
{
    /// <summary>
    /// Initializes the instance asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
