using DataStores.Bootstrap;

namespace TestHelper.DataStores.PathProviders;

/// <summary>
/// Test implementation of <see cref="IDataStorePathProvider"/> with isolated temporary directories.
/// </summary>
/// <remarks>
/// <para>
/// Creates a unique temporary directory for each test instance.
/// Automatically isolated to prevent test interference.
/// </para>
/// <para>
/// <b>Usage:</b>
/// </para>
/// <code>
/// var pathProvider = new TestDataStorePathProvider();
/// pathProvider.EnsureDirectoriesExist();
/// 
/// // Use in tests
/// var dbPath = pathProvider.FormatLiteDbFileName("test");
/// 
/// // Cleanup after test
/// pathProvider.Cleanup();
/// </code>
/// </remarks>
public class TestDataStorePathProvider : IDataStorePathProvider
{
    private readonly string _testRoot;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestDataStorePathProvider"/> class.
    /// </summary>
    /// <remarks>
    /// Creates a unique temporary directory under:
    /// %TEMP%\DataStoresTests\{GUID}
    /// </remarks>
    public TestDataStorePathProvider()
    {
        _testRoot = Path.Combine(
            Path.GetTempPath(),
            "DataStoresTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_testRoot);
    }

    /// <summary>
    /// Initializes a new instance with a custom subdirectory name.
    /// </summary>
    /// <param name="subdirectory">Subdirectory name for test organization.</param>
    /// <remarks>
    /// Useful for organizing tests by category:
    /// %TEMP%\DataStoresTests\{subdirectory}\{GUID}
    /// </remarks>
    public TestDataStorePathProvider(string subdirectory)
    {
        if (string.IsNullOrWhiteSpace(subdirectory))
            throw new ArgumentException("Subdirectory cannot be null or empty.", nameof(subdirectory));

        _testRoot = Path.Combine(
            Path.GetTempPath(),
            "DataStoresTests",
            subdirectory,
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_testRoot);
    }

    // ====================================================================
    // Core Paths
    // ====================================================================

    /// <inheritdoc/>
    public string GetApplicationPath() => _testRoot;

    /// <inheritdoc/>
    public string GetDataPath() => Path.Combine(_testRoot, "Data");

    /// <inheritdoc/>
    public string GetSettingsPath() => Path.Combine(_testRoot, "Settings");

    /// <inheritdoc/>
    public string GetLogPath() => Path.Combine(_testRoot, "Logs");

    /// <inheritdoc/>
    public string GetCachePath() => Path.Combine(_testRoot, "Cache");

    /// <inheritdoc/>
    public string GetTempPath() => Path.Combine(_testRoot, "Temp");

    // ====================================================================
    // File Path Formatters
    // ====================================================================

    /// <inheritdoc/>
    public string FormatJsonFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be null or empty.", nameof(name));

        var fileName = name.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
            ? name
            : $"{name}.json";

        return Path.Combine(GetDataPath(), fileName);
    }

    /// <inheritdoc/>
    public string FormatLiteDbFileName(string database)
    {
        if (string.IsNullOrWhiteSpace(database))
            throw new ArgumentException("Database name cannot be null or empty.", nameof(database));

        var fileName = database.EndsWith(".db", StringComparison.OrdinalIgnoreCase)
            ? database
            : $"{database}.db";

        return Path.Combine(GetDataPath(), fileName);
    }

    /// <inheritdoc/>
    public string FormatSettingsFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be null or empty.", nameof(name));

        return Path.Combine(GetSettingsPath(), name);
    }

    /// <inheritdoc/>
    public string FormatLogFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be null or empty.", nameof(name));

        var fileName = name.EndsWith(".log", StringComparison.OrdinalIgnoreCase)
            ? name
            : $"{name}.log";

        return Path.Combine(GetLogPath(), fileName);
    }

    // ====================================================================
    // Utilities
    // ====================================================================

    /// <inheritdoc/>
    public void EnsureDirectoriesExist()
    {
        Directory.CreateDirectory(GetDataPath());
        Directory.CreateDirectory(GetSettingsPath());
        Directory.CreateDirectory(GetLogPath());
        Directory.CreateDirectory(GetCachePath());
        Directory.CreateDirectory(GetTempPath());
    }

    /// <inheritdoc/>
    public string GetCustomPath(string subdirectory)
    {
        if (string.IsNullOrWhiteSpace(subdirectory))
            throw new ArgumentException("Subdirectory cannot be null or empty.", nameof(subdirectory));

        return Path.Combine(_testRoot, subdirectory);
    }

    // ====================================================================
    // Test-Specific Methods
    // ====================================================================

    /// <summary>
    /// Cleans up the test directory and all files.
    /// </summary>
    /// <remarks>
    /// Best effort cleanup - errors are ignored.
    /// Call this in test teardown or disposal.
    /// </remarks>
    public void Cleanup()
    {
        if (Directory.Exists(_testRoot))
        {
            try
            {
                Directory.Delete(_testRoot, recursive: true);
            }
            catch
            {
                // Best effort - ignore errors
            }
        }
    }

    /// <summary>
    /// Gets the root test directory path.
    /// </summary>
    /// <remarks>
    /// Useful for debugging or manual file inspection.
    /// </remarks>
    public string TestRoot => _testRoot;
}
