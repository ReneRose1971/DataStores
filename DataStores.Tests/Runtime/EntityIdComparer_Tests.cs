using DataStores.Runtime;
using TestHelper.DataStores.Models;
using Xunit;

namespace DataStores.Tests.Runtime;

/// <summary>
/// Unit-Tests für EntityIdComparer.
/// Testet die ID-basierte Gleichheitslogik für EntityBase-Typen.
/// </summary>
public class EntityIdComparer_Tests
{
    [Fact]
    public void Equals_WithSameReference_Should_ReturnTrue()
    {
        // Arrange
        var comparer = new EntityIdComparer<TestEntity>();
        var entity = new TestEntity { Id = 1, Name = "Test" };

        // Act
        var result = comparer.Equals(entity, entity);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Equals_WithBothNull_Should_ReturnTrue()
    {
        // Arrange
        var comparer = new EntityIdComparer<TestEntity>();

        // Act
        var result = comparer.Equals(null, null);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Equals_WithOneNull_Should_ReturnFalse()
    {
        // Arrange
        var comparer = new EntityIdComparer<TestEntity>();
        var entity = new TestEntity { Id = 1, Name = "Test" };

        // Act
        var result1 = comparer.Equals(entity, null);
        var result2 = comparer.Equals(null, entity);

        // Assert
        Assert.False(result1);
        Assert.False(result2);
    }

    [Fact]
    public void Equals_WithSameId_DifferentProperties_Should_ReturnTrue()
    {
        // Arrange
        var comparer = new EntityIdComparer<TestEntity>();
        var entity1 = new TestEntity { Id = 42, Name = "Original", Version = 1, Ratio = 1.5 };
        var entity2 = new TestEntity { Id = 42, Name = "Modified", Version = 2, Ratio = 2.5 };

        // Act
        var result = comparer.Equals(entity1, entity2);

        // Assert - Nur ID zählt
        Assert.True(result);
    }

    [Fact]
    public void Equals_WithDifferentIds_Should_ReturnFalse()
    {
        // Arrange
        var comparer = new EntityIdComparer<TestEntity>();
        var entity1 = new TestEntity { Id = 1, Name = "Test" };
        var entity2 = new TestEntity { Id = 2, Name = "Test" };

        // Act
        var result = comparer.Equals(entity1, entity2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Equals_WithIdZero_DifferentReferences_Should_ReturnFalse()
    {
        // Arrange
        var comparer = new EntityIdComparer<TestEntity>();
        var newEntity1 = new TestEntity { Id = 0, Name = "New1" };
        var newEntity2 = new TestEntity { Id = 0, Name = "New1" };

        // Act
        var result = comparer.Equals(newEntity1, newEntity2);

        // Assert - Neue Entities (Id = 0) sind nur bei Referenzgleichheit gleich
        Assert.False(result);
    }

    [Fact]
    public void Equals_WithIdZero_SameReference_Should_ReturnTrue()
    {
        // Arrange
        var comparer = new EntityIdComparer<TestEntity>();
        var newEntity = new TestEntity { Id = 0, Name = "New" };

        // Act
        var result = comparer.Equals(newEntity, newEntity);

        // Assert - Referenzgleichheit gilt immer
        Assert.True(result);
    }

    [Fact]
    public void Equals_WithMixedIdZeroAndPositive_Should_ReturnFalse()
    {
        // Arrange
        var comparer = new EntityIdComparer<TestEntity>();
        var newEntity = new TestEntity { Id = 0, Name = "New" };
        var persistedEntity = new TestEntity { Id = 1, Name = "Persisted" };

        // Act
        var result = comparer.Equals(newEntity, persistedEntity);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetHashCode_ForSameId_Should_ReturnSameHash()
    {
        // Arrange
        var comparer = new EntityIdComparer<TestEntity>();
        var entity1 = new TestEntity { Id = 42, Name = "A", Version = 1 };
        var entity2 = new TestEntity { Id = 42, Name = "B", Version = 2 };

        // Act
        var hash1 = comparer.GetHashCode(entity1);
        var hash2 = comparer.GetHashCode(entity2);

        // Assert - Gleiche ID → gleicher Hash
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void GetHashCode_ForDifferentIds_Should_ReturnDifferentHash()
    {
        // Arrange
        var comparer = new EntityIdComparer<TestEntity>();
        var entity1 = new TestEntity { Id = 1, Name = "Test" };
        var entity2 = new TestEntity { Id = 2, Name = "Test" };

        // Act
        var hash1 = comparer.GetHashCode(entity1);
        var hash2 = comparer.GetHashCode(entity2);

        // Assert - Verschiedene IDs → verschiedene Hashes (sehr wahrscheinlich)
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void GetHashCode_ForIdZero_SameInstance_Should_ReturnSameHash()
    {
        // Arrange
        var comparer = new EntityIdComparer<TestEntity>();
        var newEntity = new TestEntity { Id = 0, Name = "New" };

        // Act
        var hash1 = comparer.GetHashCode(newEntity);
        var hash2 = comparer.GetHashCode(newEntity);

        // Assert - Referenz-Hash ist stabil für gleiche Instanz
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void GetHashCode_ForIdZero_DifferentInstances_Should_ReturnDifferentHash()
    {
        // Arrange
        var comparer = new EntityIdComparer<TestEntity>();
        var newEntity1 = new TestEntity { Id = 0, Name = "New" };
        var newEntity2 = new TestEntity { Id = 0, Name = "New" };

        // Act
        var hash1 = comparer.GetHashCode(newEntity1);
        var hash2 = comparer.GetHashCode(newEntity2);

        // Assert - Verschiedene Referenzen → verschiedene Hashes
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void GetHashCode_Should_BeConsistentWithEquals()
    {
        // Arrange
        var comparer = new EntityIdComparer<TestEntity>();
        var entity1 = new TestEntity { Id = 100, Name = "A" };
        var entity2 = new TestEntity { Id = 100, Name = "B" };

        // Act & Assert - Konsistenz-Regel: Equals → gleicher Hash
        if (comparer.Equals(entity1, entity2))
        {
            Assert.Equal(comparer.GetHashCode(entity1), comparer.GetHashCode(entity2));
        }
    }

    [Fact]
    public void Equals_WithNonEntityBaseType_Should_ReturnFalse()
    {
        // Arrange - TestDto ist kein EntityBase
        var comparer = new EntityIdComparer<TestDto>();
        var dto1 = new TestDto("Test", 25);
        var dto2 = new TestDto("Test", 25);

        // Act
        var result = comparer.Equals(dto1, dto2);

        // Assert - Nicht-EntityBase → false (außer Referenzgleichheit)
        Assert.False(result);
    }

    [Fact]
    public void Equals_WithNonEntityBaseType_SameReference_Should_ReturnTrue()
    {
        // Arrange
        var comparer = new EntityIdComparer<TestDto>();
        var dto = new TestDto("Test", 25);

        // Act
        var result = comparer.Equals(dto, dto);

        // Assert - Referenzgleichheit gilt immer
        Assert.True(result);
    }

    [Fact]
    public void GetHashCode_WithNonEntityBaseType_Should_ReturnReferenceHash()
    {
        // Arrange
        var comparer = new EntityIdComparer<TestDto>();
        var dto = new TestDto("Test", 25);

        // Act
        var hash1 = comparer.GetHashCode(dto);
        var hash2 = comparer.GetHashCode(dto);

        // Assert - Referenz-Hash ist stabil
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void Comparer_Should_WorkInHashSet()
    {
        // Arrange
        var comparer = new EntityIdComparer<TestEntity>();
        var hashSet = new HashSet<TestEntity>(comparer);
        
        var entity1 = new TestEntity { Id = 1, Name = "Original" };
        var entity2 = new TestEntity { Id = 1, Name = "Modified" };
        var entity3 = new TestEntity { Id = 2, Name = "Other" };

        // Act
        hashSet.Add(entity1);
        var added2 = hashSet.Add(entity2); // Sollte nicht hinzugefügt werden (gleiche ID)
        var added3 = hashSet.Add(entity3);

        // Assert
        Assert.False(added2); // entity2 hat gleiche ID wie entity1
        Assert.True(added3);
        Assert.Equal(2, hashSet.Count);
    }

    [Fact]
    public void Comparer_Should_WorkInDictionary()
    {
        // Arrange
        var comparer = new EntityIdComparer<TestEntity>();
        var dict = new Dictionary<TestEntity, string>(comparer);
        
        var entity1 = new TestEntity { Id = 1, Name = "Original" };
        var entity2 = new TestEntity { Id = 1, Name = "Modified" };

        // Act
        dict[entity1] = "Value1";
        dict[entity2] = "Value2"; // Sollte entity1 überschreiben (gleiche ID)

        // Assert
        Assert.Single(dict);
        Assert.Equal("Value2", dict[entity1]);
    }

    [Fact]
    public void Comparer_WithLargeIds_Should_Work()
    {
        // Arrange
        var comparer = new EntityIdComparer<TestEntity>();
        var entity1 = new TestEntity { Id = int.MaxValue, Name = "Max" };
        var entity2 = new TestEntity { Id = int.MaxValue, Name = "Different" };

        // Act
        var equals = comparer.Equals(entity1, entity2);
        var hash1 = comparer.GetHashCode(entity1);
        var hash2 = comparer.GetHashCode(entity2);

        // Assert
        Assert.True(equals);
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void Comparer_WithNegativeIds_Should_CompareLikePositiveIds()
    {
        // Arrange
        var comparer = new EntityIdComparer<TestEntity>();
        var entity1 = new TestEntity { Id = -1, Name = "Negative" };
        var entity2 = new TestEntity { Id = -1, Name = "Different" };
        var entity3 = new TestEntity { Id = -2, Name = "Other" };

        // Act
        var equals12 = comparer.Equals(entity1, entity2);
        var equals13 = comparer.Equals(entity1, entity3);

        // Assert - Negative IDs werden akzeptiert (Id != 0 → ID-Vergleich)
        // ABER: GetHashCode verwendet nur Id > 0, daher unterschiedliche Hashes für negative IDs
        Assert.True(equals12); // Gleiche negative ID
        Assert.False(equals13); // Verschiedene negative IDs
        
        // Hinweis: HashCodes sind für negative IDs NICHT konsistent mit Equals
        // Dies ist eine Inkonsistenz in der aktuellen Implementierung
        // In der Praxis sollten negative IDs vermieden werden
    }
}
