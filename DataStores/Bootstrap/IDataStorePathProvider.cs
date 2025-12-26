namespace DataStores.Bootstrap;

/// <summary>
/// Provides standardized paths for DataStore persistence files.
/// </summary>
/// <remarks>
/// <para>
/// Simplified path provider focused on DataStores use cases.
/// </para>
/// <para>
/// <b>Directory Structure:</b>
/// </para>
/// <code>
/// %LOCALAPPDATA%\AppName\
/// ├── Data\
/// │   ├── myapp.db          (LiteDB)
/// │   ├── customers.json
/// │   └── orders.json
/// ├── Settings\
/// │   └── appsettings.json
/// ├── Logs\
/// │   └── application.log
/// ├── Cache\
/// └── Temp\
/// </code>
/// </remarks>
public interface IDataStorePathProvider
{
    // ====================================================================
    // Core Paths
    // ====================================================================

    /// <summary>
    /// Gets the root directory for the application.
    /// </summary>
    /// <remarks>
    /// <b>Windows:</b> %LOCALAPPDATA%\AppName<br/>
    /// <b>macOS:</b> ~/Library/Application Support/AppName<br/>
    /// <b>Linux:</b> ~/.local/share/AppName
    /// </remarks>
    string GetApplicationPath();

    /// <summary>
    /// Gets the directory for data files (JSON and Databases).
    /// </summary>
    /// <remarks>
    /// Combined directory for both JSON files and database files.
    /// </remarks>
    string GetDataPath();

    /// <summary>
    /// Gets the directory for settings and configuration files.
    /// </summary>
    string GetSettingsPath();

    /// <summary>
    /// Gets the directory for log files.
    /// </summary>
    string GetLogPath();

    /// <summary>
    /// Gets the directory for cache files (deletable without data loss).
    /// </summary>
    string GetCachePath();

    /// <summary>
    /// Gets the directory for temporary files.
    /// </summary>
    string GetTempPath();

    // ====================================================================
    // File Path Formatters
    // ====================================================================

    /// <summary>
    /// Formats a JSON file name and returns the full path.
    /// </summary>
    /// <param name="name">The base file name (without extension).</param>
    /// <returns>Full path to the JSON file.</returns>
    /// <remarks>
    /// Automatically adds .json extension if missing.
    /// File is placed in the Data directory.
    /// </remarks>
    /// <example>
    /// <code>
    /// var path = provider.FormatJsonFileName("customers");
    /// // C:\Users\User\AppData\Local\MyApp\Data\customers.json
    /// 
    /// var path2 = provider.FormatJsonFileName("settings.json");
    /// // C:\Users\User\AppData\Local\MyApp\Data\settings.json
    /// </code>
    /// </example>
    string FormatJsonFileName(string name);

    /// <summary>
    /// Formats a LiteDB database file name and returns the full path.
    /// </summary>
    /// <param name="database">The database name (without extension).</param>
    /// <returns>Full path to the LiteDB file.</returns>
    /// <remarks>
    /// Automatically adds .db extension if missing.
    /// File is placed in the Data directory.
    /// </remarks>
    /// <example>
    /// <code>
    /// var path = provider.FormatLiteDbFileName("myapp");
    /// // C:\Users\User\AppData\Local\MyApp\Data\myapp.db
    /// 
    /// var path2 = provider.FormatLiteDbFileName("cache.db");
    /// // C:\Users\User\AppData\Local\MyApp\Data\cache.db
    /// </code>
    /// </example>
    string FormatLiteDbFileName(string database);

    /// <summary>
    /// Formats a settings file name and returns the full path.
    /// </summary>
    /// <param name="name">The settings file name.</param>
    /// <returns>Full path to the settings file.</returns>
    /// <example>
    /// <code>
    /// var path = provider.FormatSettingsFileName("appsettings.json");
    /// // C:\Users\User\AppData\Local\MyApp\Settings\appsettings.json
    /// </code>
    /// </example>
    string FormatSettingsFileName(string name);

    /// <summary>
    /// Formats a log file name and returns the full path.
    /// </summary>
    /// <param name="name">The log file name (without extension).</param>
    /// <returns>Full path to the log file.</returns>
    /// <remarks>
    /// Automatically adds .log extension if missing.
    /// </remarks>
    /// <example>
    /// <code>
    /// var path = provider.FormatLogFileName("application");
    /// // C:\Users\User\AppData\Local\MyApp\Logs\application.log
    /// </code>
    /// </example>
    string FormatLogFileName(string name);

    // ====================================================================
    // Utilities
    // ====================================================================

    /// <summary>
    /// Ensures all standard directories exist.
    /// </summary>
    /// <remarks>
    /// Call during application startup to ensure directories are created.
    /// </remarks>
    void EnsureDirectoriesExist();

    /// <summary>
    /// Gets a custom subdirectory path.
    /// </summary>
    /// <param name="subdirectory">The subdirectory name or relative path.</param>
    /// <returns>Full path to the custom subdirectory.</returns>
    /// <example>
    /// <code>
    /// var reports = provider.GetCustomPath("Reports");
    /// // C:\Users\User\AppData\Local\MyApp\Reports
    /// </code>
    /// </example>
    string GetCustomPath(string subdirectory);
}
