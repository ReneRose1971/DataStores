namespace DataStores.Bootstrap;

/// <summary>
/// Standard implementation of <see cref="IDataStorePathProvider"/> for desktop applications.
/// </summary>
/// <remarks>
/// <para>
/// This implementation uses platform-specific standard paths:
/// </para>
/// <list type="bullet">
/// <item><description>Windows: %LOCALAPPDATA%\AppName or %APPDATA%\AppName (roaming)</description></item>
/// <item><description>macOS: ~/Library/Application Support/AppName</description></item>
/// <item><description>Linux: ~/.local/share/AppName</description></item>
/// </list>
/// </remarks>
public class DataStorePathProvider : IDataStorePathProvider
{
    private readonly string _applicationName;
    private readonly string _rootPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataStorePathProvider"/> class.
    /// </summary>
    /// <param name="applicationName">The application name (e.g., "MyApp").</param>
    /// <param name="useRoamingProfile">
    /// If true, uses roaming profile (%APPDATA%).
    /// If false, uses local profile (%LOCALAPPDATA%). Default is false.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="applicationName"/> is null or whitespace.
    /// </exception>
    public DataStorePathProvider(
        string applicationName,
        bool useRoamingProfile = false)
    {
        if (string.IsNullOrWhiteSpace(applicationName))
            throw new ArgumentException("Application name cannot be null or empty.", nameof(applicationName));

        _applicationName = applicationName;

        var baseFolder = useRoamingProfile 
            ? Environment.SpecialFolder.ApplicationData 
            : Environment.SpecialFolder.LocalApplicationData;

        _rootPath = Path.Combine(
            Environment.GetFolderPath(baseFolder),
            _applicationName);
    }

    // ====================================================================
    // Core Paths
    // ====================================================================

    /// <inheritdoc/>
    public string GetApplicationPath() => _rootPath;

    /// <inheritdoc/>
    public string GetDataPath() => Path.Combine(_rootPath, "Data");

    /// <inheritdoc/>
    public string GetSettingsPath() => Path.Combine(_rootPath, "Settings");

    /// <inheritdoc/>
    public string GetLogPath() => Path.Combine(_rootPath, "Logs");

    /// <inheritdoc/>
    public string GetCachePath() => Path.Combine(_rootPath, "Cache");

    /// <inheritdoc/>
    public string GetTempPath() => Path.Combine(_rootPath, "Temp");

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
        var directories = new[]
        {
            GetApplicationPath(),
            GetDataPath(),
            GetSettingsPath(),
            GetLogPath(),
            GetCachePath(),
            GetTempPath()
        };

        foreach (var directory in directories)
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }

    /// <inheritdoc/>
    public string GetCustomPath(string subdirectory)
    {
        if (string.IsNullOrWhiteSpace(subdirectory))
            throw new ArgumentException("Subdirectory cannot be null or empty.", nameof(subdirectory));

        return Path.Combine(_rootPath, subdirectory);
    }
}
