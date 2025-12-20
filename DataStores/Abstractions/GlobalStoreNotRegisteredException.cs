namespace DataStores.Abstractions;

/// <summary>
/// Exception thrown when attempting to access a global store that has not been registered.
/// </summary>
public class GlobalStoreNotRegisteredException : InvalidOperationException
{
    /// <summary>
    /// Gets the type of the store that was not registered.
    /// </summary>
    public Type StoreType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalStoreNotRegisteredException"/> class.
    /// </summary>
    /// <param name="storeType">The type of the store.</param>
    public GlobalStoreNotRegisteredException(Type storeType)
        : base($"No global store has been registered for type '{storeType.FullName}'.")
    {
        StoreType = storeType;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalStoreNotRegisteredException"/> class.
    /// </summary>
    /// <param name="storeType">The type of the store.</param>
    /// <param name="message">The error message.</param>
    public GlobalStoreNotRegisteredException(Type storeType, string message)
        : base(message)
    {
        StoreType = storeType;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalStoreNotRegisteredException"/> class.
    /// </summary>
    /// <param name="storeType">The type of the store.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public GlobalStoreNotRegisteredException(Type storeType, string message, Exception innerException)
        : base(message, innerException)
    {
        StoreType = storeType;
    }
}
