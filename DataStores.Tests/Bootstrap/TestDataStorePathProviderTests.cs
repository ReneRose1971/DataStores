using TestHelper.DataStores.PathProviders;

namespace DataStores.Tests.Bootstrap;

/// <summary>
/// Unit tests for TestDataStorePathProvider.
/// </summary>
[Trait("Category", "Unit")]
public class TestDataStorePathProviderTests
{
    [Fact]
    public void Constructor_Should_CreateUniqueTestRoot()
    {
        // Arrange & Act
        var provider1 = new TestDataStorePathProvider();
        var provider2 = new TestDataStorePathProvider();

        try
        {
            // Assert: Different instances have different roots
            Assert.NotEqual(provider1.TestRoot, provider2.TestRoot);
        }
        finally
        {
            provider1.Cleanup();
            provider2.Cleanup();
        }
    }

    [Fact]
    public void Constructor_Should_CreateTestRootDirectory()
    {
        // Arrange & Act
        var provider = new TestDataStorePathProvider();

        try
        {
            // Assert
            Assert.True(Directory.Exists(provider.TestRoot));
        }
        finally
        {
            provider.Cleanup();
        }
    }

    [Fact]
    public void Constructor_WithSubdirectory_Should_IncludeSubdirectory()
    {
        // Arrange & Act
        var provider = new TestDataStorePathProvider("MyTestCategory");

        try
        {
            // Assert
            Assert.Contains("MyTestCategory", provider.TestRoot);
        }
        finally
        {
            provider.Cleanup();
        }
    }

    [Fact]
    public void Constructor_WithSubdirectory_Should_ThrowWhenNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new TestDataStorePathProvider(null!));
    }

    [Fact]
    public void Constructor_WithSubdirectory_Should_ThrowWhenEmpty()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new TestDataStorePathProvider(string.Empty));
    }

    [Fact]
    public void GetApplicationPath_Should_ReturnTestRoot()
    {
        // Arrange
        var provider = new TestDataStorePathProvider();

        try
        {
            // Act
            var path = provider.GetApplicationPath();

            // Assert
            Assert.Equal(provider.TestRoot, path);
        }
        finally
        {
            provider.Cleanup();
        }
    }

    [Fact]
    public void GetDataPath_Should_ReturnDataSubdirectory()
    {
        // Arrange
        var provider = new TestDataStorePathProvider();

        try
        {
            // Act
            var dataPath = provider.GetDataPath();

            // Assert
            Assert.EndsWith("Data", dataPath);
            Assert.StartsWith(provider.TestRoot, dataPath);
        }
        finally
        {
            provider.Cleanup();
        }
    }

    [Fact]
    public void FormatJsonFileName_Should_AddJsonExtension()
    {
        // Arrange
        var provider = new TestDataStorePathProvider();

        try
        {
            // Act
            var path = provider.FormatJsonFileName("test");

            // Assert
            Assert.EndsWith("test.json", path);
            Assert.Contains(Path.Combine(provider.TestRoot, "Data"), path);
        }
        finally
        {
            provider.Cleanup();
        }
    }

    [Fact]
    public void FormatJsonFileName_Should_NotDuplicateExtension()
    {
        // Arrange
        var provider = new TestDataStorePathProvider();

        try
        {
            // Act
            var path = provider.FormatJsonFileName("test.json");

            // Assert
            Assert.EndsWith("test.json", path);
            Assert.DoesNotContain("test.json.json", path);
        }
        finally
        {
            provider.Cleanup();
        }
    }

    [Fact]
    public void FormatLiteDbFileName_Should_AddDbExtension()
    {
        // Arrange
        var provider = new TestDataStorePathProvider();

        try
        {
            // Act
            var path = provider.FormatLiteDbFileName("test");

            // Assert
            Assert.EndsWith("test.db", path);
            Assert.Contains(Path.Combine(provider.TestRoot, "Data"), path);
        }
        finally
        {
            provider.Cleanup();
        }
    }

    [Fact]
    public void FormatLiteDbFileName_Should_NotDuplicateExtension()
    {
        // Arrange
        var provider = new TestDataStorePathProvider();

        try
        {
            // Act
            var path = provider.FormatLiteDbFileName("test.db");

            // Assert
            Assert.EndsWith("test.db", path);
            Assert.DoesNotContain("test.db.db", path);
        }
        finally
        {
            provider.Cleanup();
        }
    }

    [Fact]
    public void FormatSettingsFileName_Should_ReturnFullPath()
    {
        // Arrange
        var provider = new TestDataStorePathProvider();

        try
        {
            // Act
            var path = provider.FormatSettingsFileName("test.json");

            // Assert
            Assert.EndsWith("test.json", path);
            Assert.Contains(Path.Combine(provider.TestRoot, "Settings"), path);
        }
        finally
        {
            provider.Cleanup();
        }
    }

    [Fact]
    public void FormatLogFileName_Should_AddLogExtension()
    {
        // Arrange
        var provider = new TestDataStorePathProvider();

        try
        {
            // Act
            var path = provider.FormatLogFileName("test");

            // Assert
            Assert.EndsWith("test.log", path);
            Assert.Contains(Path.Combine(provider.TestRoot, "Logs"), path);
        }
        finally
        {
            provider.Cleanup();
        }
    }

    [Fact]
    public void EnsureDirectoriesExist_Should_CreateAllDirectories()
    {
        // Arrange
        var provider = new TestDataStorePathProvider();

        try
        {
            // Act
            provider.EnsureDirectoriesExist();

            // Assert
            Assert.True(Directory.Exists(provider.GetDataPath()));
            Assert.True(Directory.Exists(provider.GetSettingsPath()));
            Assert.True(Directory.Exists(provider.GetLogPath()));
            Assert.True(Directory.Exists(provider.GetCachePath()));
            Assert.True(Directory.Exists(provider.GetTempPath()));
        }
        finally
        {
            provider.Cleanup();
        }
    }

    [Fact]
    public void GetCustomPath_Should_ReturnCustomSubdirectory()
    {
        // Arrange
        var provider = new TestDataStorePathProvider();

        try
        {
            // Act
            var path = provider.GetCustomPath("Custom");

            // Assert
            Assert.EndsWith("Custom", path);
            Assert.StartsWith(provider.TestRoot, path);
        }
        finally
        {
            provider.Cleanup();
        }
    }

    [Fact]
    public void Cleanup_Should_DeleteTestRoot()
    {
        // Arrange
        var provider = new TestDataStorePathProvider();
        provider.EnsureDirectoriesExist();
        var rootPath = provider.TestRoot;

        // Act
        provider.Cleanup();

        // Assert
        Assert.False(Directory.Exists(rootPath));
    }

    [Fact]
    public void Cleanup_Should_BeIdempotent()
    {
        // Arrange
        var provider = new TestDataStorePathProvider();

        // Act: Call cleanup twice
        provider.Cleanup();
        provider.Cleanup();

        // Assert: No exception thrown
        Assert.False(Directory.Exists(provider.TestRoot));
    }

    [Fact]
    public void Cleanup_Should_HandleNonExistentDirectory()
    {
        // Arrange
        var provider = new TestDataStorePathProvider();
        provider.Cleanup(); // First cleanup

        // Act & Assert: Second cleanup should not throw
        provider.Cleanup();
    }

    // ====================================================================
    // Integration Test: Full Path Provider Lifecycle
    // ====================================================================

    [Fact]
    public void FullLifecycle_Should_WorkCorrectly()
    {
        // Arrange
        var provider = new TestDataStorePathProvider("LifecycleTest");
        
        try
        {
            // Act: Create directories
            provider.EnsureDirectoriesExist();

            // Create a test file
            var jsonPath = provider.FormatJsonFileName("test");
            File.WriteAllText(jsonPath, "{}");

            var dbPath = provider.FormatLiteDbFileName("test");
            File.WriteAllText(dbPath, "test data");

            // Assert: Files exist
            Assert.True(File.Exists(jsonPath));
            Assert.True(File.Exists(dbPath));

            // Cleanup
            provider.Cleanup();

            // Assert: Everything deleted
            Assert.False(Directory.Exists(provider.TestRoot));
            Assert.False(File.Exists(jsonPath));
            Assert.False(File.Exists(dbPath));
        }
        finally
        {
            // Safety cleanup
            provider.Cleanup();
        }
    }
}
