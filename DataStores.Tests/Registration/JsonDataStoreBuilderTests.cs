using DataStores.Bootstrap;
using DataStores.Persistence;
using DataStores.Registration;
using DataStores.Runtime;
using Microsoft.Extensions.DependencyInjection;
using TestHelper.DataStores.PathProviders;

namespace DataStores.Tests.Registration;

[Trait("Category", "Unit")]
public class JsonDataStoreBuilderTests
{
    private class TestItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class TestRegistrar : DataStoreRegistrarBase
    {
        private readonly JsonDataStoreBuilder<TestItem> _builder;

        public TestRegistrar(JsonDataStoreBuilder<TestItem> builder)
        {
            _builder = builder;
        }

        protected override void ConfigureStores(IServiceProvider serviceProvider, IDataStorePathProvider pathProvider)
        {
            AddStore(_builder);
        }
    }

    [Fact]
    public void Register_Should_CreatePersistentStoreWithJsonStrategy()
    {
        var builder = new JsonDataStoreBuilder<TestItem>(
            filePath: "test.json");
        var registrar = new TestRegistrar(builder);
        var registry = new GlobalStoreRegistry();
        
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddSingleton<IDataStorePathProvider>(new NullDataStorePathProvider());
        var provider = services.BuildServiceProvider();

        registrar.Register(registry, provider);

        var store = registry.ResolveGlobal<TestItem>();
        Assert.NotNull(store);
        Assert.IsType<PersistentStoreDecorator<TestItem>>(store);
    }

    [Fact]
    public void Constructor_Should_ThrowWhenFilePathIsNull()
    {
        Assert.Throws<ArgumentException>(() =>
            new JsonDataStoreBuilder<TestItem>(filePath: null!));
    }

    [Fact]
    public void Constructor_Should_ThrowWhenFilePathIsEmpty()
    {
        Assert.Throws<ArgumentException>(() =>
            new JsonDataStoreBuilder<TestItem>(filePath: string.Empty));
    }

    [Fact]
    public void Constructor_Should_ThrowWhenFilePathIsWhitespace()
    {
        Assert.Throws<ArgumentException>(() =>
            new JsonDataStoreBuilder<TestItem>(filePath: "   "));
    }

    [Fact]
    public void Constructor_Should_AcceptValidFilePath()
    {
        var builder = new JsonDataStoreBuilder<TestItem>(
            filePath: "C:\\Data\\test.json");

        Assert.NotNull(builder);
    }

    [Fact]
    public void Constructor_Should_AcceptAllParameters()
    {
        var comparer = EqualityComparer<TestItem>.Default;
        var syncContext = new SynchronizationContext();

        var builder = new JsonDataStoreBuilder<TestItem>(
            filePath: "test.json",
            autoLoad: false,
            autoSave: false,
            comparer: comparer,
            synchronizationContext: syncContext);

        Assert.NotNull(builder);
    }

    [Fact]
    public void Register_Should_CreateStoreWithDefaultAutoLoadAndAutoSave()
    {
        var builder = new JsonDataStoreBuilder<TestItem>(
            filePath: "test.json");
        var registrar = new TestRegistrar(builder);
        var registry = new GlobalStoreRegistry();
        
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddSingleton<IDataStorePathProvider>(new NullDataStorePathProvider());
        var provider = services.BuildServiceProvider();

        registrar.Register(registry, provider);

        var store = registry.ResolveGlobal<TestItem>();
        Assert.NotNull(store);
    }
}
