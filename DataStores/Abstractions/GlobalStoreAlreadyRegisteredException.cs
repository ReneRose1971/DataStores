namespace DataStores.Abstractions;

/// <summary>
/// Exception thrown when attempting to register a global store for a type that already has a registration.
/// </summary>
public class GlobalStoreAlreadyRegisteredException : InvalidOperationException
{
    /// <summary>
    /// Gets the type of the store that was already registered.
    /// </summary>
    public Type StoreType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalStoreAlreadyRegisteredException"/> class.
    /// </summary>
    /// <param name="storeType">The type of the store.</param>
    public GlobalStoreAlreadyRegisteredException(Type storeType)
        : base($"A global store for type '{storeType.FullName}' has already been registered.")
    {
        StoreType = storeType;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalStoreAlreadyRegisteredException"/> class.
    /// </summary>
    /// <param name="storeType">The type of the store.</param>
    /// <param name="message">The error message.</param>
    public GlobalStoreAlreadyRegisteredException(Type storeType, string message)
        : base(message)
    {
        StoreType = storeType;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalStoreAlreadyRegisteredException"/> class.
    /// </summary>
    /// <param name="storeType">The type of the store.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public GlobalStoreAlreadyRegisteredException(Type storeType, string message, Exception innerException)
        : base(message, innerException)
    {
        StoreType = storeType;
    }
}
