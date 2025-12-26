using DataStores.Bootstrap;

namespace TestHelper.DataStores.PathProviders;

/// <summary>
/// Null-Pattern PathProvider for tests that don't use paths.
/// Returns empty strings for all path requests.
/// </summary>
/// <remarks>
/// Use this when tests register InMemory stores only and don't need actual file paths.
/// For tests with persistence, use <see cref="TestDataStorePathProvider"/> instead.
/// </remarks>
public class NullDataStorePathProvider : IDataStorePathProvider
{
    public string GetApplicationPath() => string.Empty;
    public string GetDataPath() => string.Empty;
    public string GetSettingsPath() => string.Empty;
    public string GetLogPath() => string.Empty;
    public string GetCachePath() => string.Empty;
    public string GetTempPath() => string.Empty;
    public string FormatJsonFileName(string name) => string.Empty;
    public string FormatLiteDbFileName(string database) => string.Empty;
    public string FormatSettingsFileName(string name) => string.Empty;
    public string FormatLogFileName(string name) => string.Empty;
    public void EnsureDirectoriesExist() { }
    public string GetCustomPath(string subdirectory) => string.Empty;
}
