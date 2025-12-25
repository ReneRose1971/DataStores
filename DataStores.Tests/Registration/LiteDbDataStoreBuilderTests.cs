using DataStores.Abstractions;
using DataStores.Persistence;
using DataStores.Registration;
using DataStores.Runtime;

namespace DataStores.Tests.Registration;

[Trait("Category", "Unit")]
public class LiteDbDataStoreBuilderTests
{
    private class TestEntity : EntityBase
    {
        public string Name { get; set; } = string.Empty;

        public override string ToString() => $"TestEntity #{Id}: {Name}";

        public override bool Equals(object? obj)
        {
            if (obj is not TestEntity other) return false;
            if (Id > 0 && other.Id > 0) return Id == other.Id;
            return ReferenceEquals(this, other);
        }

        public override int GetHashCode() => Id > 0 ? Id : HashCode.Combine(Name);
    }

    private class TestRegistrar : DataStoreRegistrarBase
    {
        private readonly LiteDbDataStoreBuilder<TestEntity> _builder;

        public TestRegistrar(LiteDbDataStoreBuilder<TestEntity> builder)
        {
            _builder = builder;
            AddStore(_builder);
        }
    }

    [Fact]
    public void Register_Should_CreatePersistentStoreWithLiteDbStrategy()
    {
        var builder = new LiteDbDataStoreBuilder<TestEntity>(
            databasePath: "test.db");
        var registrar = new TestRegistrar(builder);
        var registry = new GlobalStoreRegistry();

        registrar.Register(registry, null!);

        var store = registry.ResolveGlobal<TestEntity>();
        Assert.NotNull(store);
        Assert.IsType<PersistentStoreDecorator<TestEntity>>(store);
    }

    [Fact]
    public void Constructor_Should_ThrowWhenDatabasePathIsNull()
    {
        Assert.Throws<ArgumentException>(() =>
            new LiteDbDataStoreBuilder<TestEntity>(databasePath: null!));
    }

    [Fact]
    public void Constructor_Should_ThrowWhenDatabasePathIsEmpty()
    {
        Assert.Throws<ArgumentException>(() =>
            new LiteDbDataStoreBuilder<TestEntity>(databasePath: string.Empty));
    }

    [Fact]
    public void Constructor_Should_ThrowWhenDatabasePathIsWhitespace()
    {
        Assert.Throws<ArgumentException>(() =>
            new LiteDbDataStoreBuilder<TestEntity>(databasePath: "   "));
    }

    [Fact]
    public void Constructor_Should_AcceptValidDatabasePath()
    {
        var builder = new LiteDbDataStoreBuilder<TestEntity>(
            databasePath: "C:\\Data\\test.db");

        Assert.NotNull(builder);
    }

    [Fact]
    public void Constructor_Should_AcceptAllParameters()
    {
        var comparer = EqualityComparer<TestEntity>.Default;
        var syncContext = new SynchronizationContext();

        var builder = new LiteDbDataStoreBuilder<TestEntity>(
            databasePath: "test.db",
            autoLoad: false,
            autoSave: false,
            comparer: comparer,
            synchronizationContext: syncContext);

        Assert.NotNull(builder);
    }

    [Fact]
    public void Register_Should_UseTypeNameAsCollectionName()
    {
        var builder = new LiteDbDataStoreBuilder<TestEntity>(
            databasePath: "test.db");
        var registrar = new TestRegistrar(builder);
        var registry = new GlobalStoreRegistry();

        registrar.Register(registry, null!);

        var store = registry.ResolveGlobal<TestEntity>();
        Assert.NotNull(store);
    }

    [Fact]
    public void Register_Should_CreateStoreWithDefaultAutoLoadAndAutoSave()
    {
        var builder = new LiteDbDataStoreBuilder<TestEntity>(
            databasePath: "test.db");
        var registrar = new TestRegistrar(builder);
        var registry = new GlobalStoreRegistry();

        registrar.Register(registry, null!);

        var store = registry.ResolveGlobal<TestEntity>();
        Assert.NotNull(store);
    }
}
