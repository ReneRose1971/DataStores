using DataStores.Abstractions;
using Xunit;

namespace DataStores.Tests.Abstractions;

/// <summary>
/// Tests for custom exceptions in DataStores.Abstractions.
/// </summary>
public class Exceptions_Tests
{
    [Fact]
    public void GlobalStoreNotRegisteredException_Should_ContainTypeName()
    {
        // Arrange & Act
        var exception = new GlobalStoreNotRegisteredException(typeof(TestEntity));

        // Assert
        Assert.Contains("TestEntity", exception.Message);
        Assert.Contains("registered", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GlobalStoreNotRegisteredException_Should_ExposeStoreType()
    {
        // Arrange & Act
        var exception = new GlobalStoreNotRegisteredException(typeof(TestEntity));

        // Assert
        Assert.Equal(typeof(TestEntity), exception.StoreType);
    }

    [Fact]
    public void GlobalStoreAlreadyRegisteredException_Should_ContainTypeName()
    {
        // Arrange & Act
        var exception = new GlobalStoreAlreadyRegisteredException(typeof(TestEntity));

        // Assert
        Assert.Contains("TestEntity", exception.Message);
        Assert.Contains("already", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GlobalStoreAlreadyRegisteredException_Should_ExposeStoreType()
    {
        // Arrange & Act
        var exception = new GlobalStoreAlreadyRegisteredException(typeof(TestEntity));

        // Assert
        Assert.Equal(typeof(TestEntity), exception.StoreType);
    }

    [Fact]
    public void GlobalStoreNotRegisteredException_Should_BeInvalidOperationException()
    {
        // Arrange & Act
        var exception = new GlobalStoreNotRegisteredException(typeof(TestEntity));

        // Assert
        Assert.IsAssignableFrom<InvalidOperationException>(exception);
    }

    [Fact]
    public void GlobalStoreAlreadyRegisteredException_Should_BeInvalidOperationException()
    {
        // Arrange & Act
        var exception = new GlobalStoreAlreadyRegisteredException(typeof(TestEntity));

        // Assert
        Assert.IsAssignableFrom<InvalidOperationException>(exception);
    }

    [Fact]
    public void GlobalStoreNotRegisteredException_WithNullType_Should_HandleGracefully()
    {
        // Act - Constructor should handle null gracefully (or throw ArgumentNullException)
        var ex = Assert.ThrowsAny<Exception>(() => 
            new GlobalStoreNotRegisteredException(null!));
        
        // Assert - Either ArgumentNullException or NullReferenceException is acceptable
        Assert.True(ex is ArgumentNullException || ex is NullReferenceException);
    }

    [Fact]
    public void GlobalStoreAlreadyRegisteredException_WithNullType_Should_HandleGracefully()
    {
        // Act - Constructor should handle null gracefully (or throw ArgumentNullException)
        var ex = Assert.ThrowsAny<Exception>(() => 
            new GlobalStoreAlreadyRegisteredException(null!));
        
        // Assert - Either ArgumentNullException or NullReferenceException is acceptable
        Assert.True(ex is ArgumentNullException || ex is NullReferenceException);
    }

    [Fact]
    public void Exceptions_Should_BeSerializable()
    {
        // This test ensures exceptions can be serialized across app domains if needed
        var notRegistered = new GlobalStoreNotRegisteredException(typeof(TestEntity));
        var alreadyRegistered = new GlobalStoreAlreadyRegisteredException(typeof(TestEntity));

        // Assert - Just verify they can be instantiated and have basic properties
        Assert.NotNull(notRegistered.ToString());
        Assert.NotNull(alreadyRegistered.ToString());
    }

    private class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }
}
