using DataStores.Bootstrap;

namespace DataStores.Tests.Bootstrap;

/// <summary>
/// Unit tests for DataStorePathProvider.
/// </summary>
[Trait("Category", "Unit")]
public class DataStorePathProviderTests
{
    [Fact]
    public void Constructor_Should_ThrowWhenApplicationNameIsNull()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new DataStorePathProvider(null!));

        Assert.Contains("Application name cannot be null or empty", exception.Message);
    }

    [Fact]
    public void Constructor_Should_ThrowWhenApplicationNameIsEmpty()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new DataStorePathProvider(string.Empty));

        Assert.Contains("Application name cannot be null or empty", exception.Message);
    }

    [Fact]
    public void Constructor_Should_ThrowWhenApplicationNameIsWhitespace()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new DataStorePathProvider("   "));

        Assert.Contains("Application name cannot be null or empty", exception.Message);
    }

    [Fact]
    public void Constructor_Should_AcceptValidApplicationName()
    {
        // Act
        var provider = new DataStorePathProvider("TestApp");

        // Assert
        Assert.NotNull(provider);
    }

    [Fact]
    public void GetApplicationPath_Should_ContainApplicationName()
    {
        // Arrange
        var provider = new DataStorePathProvider("MyTestApp");

        // Act
        var path = provider.GetApplicationPath();

        // Assert
        Assert.Contains("MyTestApp", path);
    }

    [Fact]
    public void GetApplicationPath_WithRoaming_Should_UseAppData()
    {
        // Arrange
        var provider = new DataStorePathProvider("TestApp", useRoamingProfile: true);
        var expectedBase = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        // Act
        var path = provider.GetApplicationPath();

        // Assert
        Assert.StartsWith(expectedBase, path);
    }

    [Fact]
    public void GetApplicationPath_WithoutRoaming_Should_UseLocalAppData()
    {
        // Arrange
        var provider = new DataStorePathProvider("TestApp", useRoamingProfile: false);
        var expectedBase = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        // Act
        var path = provider.GetApplicationPath();

        // Assert
        Assert.StartsWith(expectedBase, path);
    }

    [Fact]
    public void GetDataPath_Should_ReturnDataSubdirectory()
    {
        // Arrange
        var provider = new DataStorePathProvider("TestApp");

        // Act
        var dataPath = provider.GetDataPath();

        // Assert
        Assert.EndsWith(Path.Combine("TestApp", "Data"), dataPath);
    }

    [Fact]
    public void GetSettingsPath_Should_ReturnSettingsSubdirectory()
    {
        // Arrange
        var provider = new DataStorePathProvider("TestApp");

        // Act
        var settingsPath = provider.GetSettingsPath();

        // Assert
        Assert.EndsWith(Path.Combine("TestApp", "Settings"), settingsPath);
    }

    [Fact]
    public void GetLogPath_Should_ReturnLogsSubdirectory()
    {
        // Arrange
        var provider = new DataStorePathProvider("TestApp");

        // Act
        var logPath = provider.GetLogPath();

        // Assert
        Assert.EndsWith(Path.Combine("TestApp", "Logs"), logPath);
    }

    [Fact]
    public void GetCachePath_Should_ReturnCacheSubdirectory()
    {
        // Arrange
        var provider = new DataStorePathProvider("TestApp");

        // Act
        var cachePath = provider.GetCachePath();

        // Assert
        Assert.EndsWith(Path.Combine("TestApp", "Cache"), cachePath);
    }

    [Fact]
    public void GetTempPath_Should_ReturnTempSubdirectory()
    {
        // Arrange
        var provider = new DataStorePathProvider("TestApp");

        // Act
        var tempPath = provider.GetTempPath();

        // Assert
        Assert.EndsWith(Path.Combine("TestApp", "Temp"), tempPath);
    }

    // ====================================================================
    // Format Methods Tests
    // ====================================================================

    [Fact]
    public void FormatJsonFileName_Should_AddJsonExtension()
    {
        // Arrange
        var provider = new DataStorePathProvider("TestApp");

        // Act
        var path = provider.FormatJsonFileName("customers");

        // Assert
        Assert.EndsWith("customers.json", path);
        Assert.Contains(Path.Combine("TestApp", "Data"), path);
    }

    [Fact]
    public void FormatJsonFileName_Should_NotDuplicateJsonExtension()
    {
        // Arrange
        var provider = new DataStorePathProvider("TestApp");

        // Act
        var path = provider.FormatJsonFileName("customers.json");

        // Assert
        Assert.EndsWith("customers.json", path);
        Assert.DoesNotContain("customers.json.json", path);
    }

    [Fact]
    public void FormatJsonFileName_Should_BeCaseInsensitive()
    {
        // Arrange
        var provider = new DataStorePathProvider("TestApp");

        // Act
        var path = provider.FormatJsonFileName("customers.JSON");

        // Assert
        Assert.EndsWith("customers.JSON", path);
        Assert.DoesNotContain(".json.JSON", path);
    }

    [Fact]
    public void FormatJsonFileName_Should_ThrowWhenNameIsNull()
    {
        // Arrange
        var provider = new DataStorePathProvider("TestApp");

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            provider.FormatJsonFileName(null!));
    }

    [Fact]
    public void FormatJsonFileName_Should_ThrowWhenNameIsEmpty()
    {
        // Arrange
        var provider = new DataStorePathProvider("TestApp");

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            provider.FormatJsonFileName(string.Empty));
    }

    [Fact]
    public void FormatLiteDbFileName_Should_AddDbExtension()
    {
        // Arrange
        var provider = new DataStorePathProvider("TestApp");

        // Act
        var path = provider.FormatLiteDbFileName("myapp");

        // Assert
        Assert.EndsWith("myapp.db", path);
        Assert.Contains(Path.Combine("TestApp", "Data"), path);
    }

    [Fact]
    public void FormatLiteDbFileName_Should_NotDuplicateDbExtension()
    {
        // Arrange
        var provider = new DataStorePathProvider("TestApp");

        // Act
        var path = provider.FormatLiteDbFileName("myapp.db");

        // Assert
        Assert.EndsWith("myapp.db", path);
        Assert.DoesNotContain("myapp.db.db", path);
    }

    [Fact]
    public void FormatLiteDbFileName_Should_BeCaseInsensitive()
    {
        // Arrange
        var provider = new DataStorePathProvider("TestApp");

        // Act
        var path = provider.FormatLiteDbFileName("myapp.DB");

        // Assert
        Assert.EndsWith("myapp.DB", path);
        Assert.DoesNotContain(".db.DB", path);
    }

    [Fact]
    public void FormatLiteDbFileName_Should_ThrowWhenDatabaseIsNull()
    {
        // Arrange
        var provider = new DataStorePathProvider("TestApp");

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            provider.FormatLiteDbFileName(null!));
    }

    [Fact]
    public void FormatLiteDbFileName_Should_ThrowWhenDatabaseIsEmpty()
    {
        // Arrange
        var provider = new DataStorePathProvider("TestApp");

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            provider.FormatLiteDbFileName(string.Empty));
    }

    [Fact]
    public void FormatSettingsFileName_Should_ReturnFullPath()
    {
        // Arrange
        var provider = new DataStorePathProvider("TestApp");

        // Act
        var path = provider.FormatSettingsFileName("appsettings.json");

        // Assert
        Assert.EndsWith("appsettings.json", path);
        Assert.Contains(Path.Combine("TestApp", "Settings"), path);
    }

    [Fact]
    public void FormatSettingsFileName_Should_ThrowWhenNameIsNull()
    {
        // Arrange
        var provider = new DataStorePathProvider("TestApp");

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            provider.FormatSettingsFileName(null!));
    }

    [Fact]
    public void FormatLogFileName_Should_AddLogExtension()
    {
        // Arrange
        var provider = new DataStorePathProvider("TestApp");

        // Act
        var path = provider.FormatLogFileName("application");

        // Assert
        Assert.EndsWith("application.log", path);
        Assert.Contains(Path.Combine("TestApp", "Logs"), path);
    }

    [Fact]
    public void FormatLogFileName_Should_NotDuplicateLogExtension()
    {
        // Arrange
        var provider = new DataStorePathProvider("TestApp");

        // Act
        var path = provider.FormatLogFileName("application.log");

        // Assert
        Assert.EndsWith("application.log", path);
        Assert.DoesNotContain("application.log.log", path);
    }

    [Fact]
    public void FormatLogFileName_Should_ThrowWhenNameIsNull()
    {
        // Arrange
        var provider = new DataStorePathProvider("TestApp");

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            provider.FormatLogFileName(null!));
    }

    // ====================================================================
    // Utility Methods Tests
    // ====================================================================

    [Fact]
    public void GetCustomPath_Should_ReturnCustomSubdirectory()
    {
        // Arrange
        var provider = new DataStorePathProvider("TestApp");

        // Act
        var path = provider.GetCustomPath("Reports");

        // Assert
        Assert.EndsWith(Path.Combine("TestApp", "Reports"), path);
    }

    [Fact]
    public void GetCustomPath_Should_SupportRelativePaths()
    {
        // Arrange
        var provider = new DataStorePathProvider("TestApp");

        // Act
        var path = provider.GetCustomPath(Path.Combine("Backups", "2025"));

        // Assert
        Assert.EndsWith(Path.Combine("TestApp", "Backups", "2025"), path);
    }

    [Fact]
    public void GetCustomPath_Should_ThrowWhenSubdirectoryIsNull()
    {
        // Arrange
        var provider = new DataStorePathProvider("TestApp");

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            provider.GetCustomPath(null!));
    }

    [Fact]
    public void GetCustomPath_Should_ThrowWhenSubdirectoryIsEmpty()
    {
        // Arrange
        var provider = new DataStorePathProvider("TestApp");

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            provider.GetCustomPath(string.Empty));
    }

    [Fact]
    public void EnsureDirectoriesExist_Should_CreateAllDirectories()
    {
        // Arrange
        var appName = $"TestApp_{Guid.NewGuid():N}";
        var provider = new DataStorePathProvider(appName);

        try
        {
            // Act
            provider.EnsureDirectoriesExist();

            // Assert
            Assert.True(Directory.Exists(provider.GetApplicationPath()));
            Assert.True(Directory.Exists(provider.GetDataPath()));
            Assert.True(Directory.Exists(provider.GetSettingsPath()));
            Assert.True(Directory.Exists(provider.GetLogPath()));
            Assert.True(Directory.Exists(provider.GetCachePath()));
            Assert.True(Directory.Exists(provider.GetTempPath()));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(provider.GetApplicationPath()))
            {
                try
                {
                    Directory.Delete(provider.GetApplicationPath(), recursive: true);
                }
                catch
                {
                    // Best effort cleanup
                }
            }
        }
    }

    [Fact]
    public void EnsureDirectoriesExist_Should_BeIdempotent()
    {
        // Arrange
        var appName = $"TestApp_{Guid.NewGuid():N}";
        var provider = new DataStorePathProvider(appName);

        try
        {
            // Act: Call twice
            provider.EnsureDirectoriesExist();
            provider.EnsureDirectoriesExist();

            // Assert: No exception, directories still exist
            Assert.True(Directory.Exists(provider.GetDataPath()));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(provider.GetApplicationPath()))
            {
                try
                {
                    Directory.Delete(provider.GetApplicationPath(), recursive: true);
                }
                catch
                {
                    // Best effort cleanup
                }
            }
        }
    }
}
