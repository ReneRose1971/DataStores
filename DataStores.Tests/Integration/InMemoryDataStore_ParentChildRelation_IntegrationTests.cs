using DataStores.Abstractions;
using DataStores.Bootstrap;
using DataStores.Relations;
using DataStores.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DataStores.Tests.Integration;

/// <summary>
/// Integration-Tests für InMemoryDataStore mit ParentChildRelationship aus User-Perspektive.
/// Demonstriert wie zur Laufzeit hierarchische Beziehungen zwischen Entities aufgebaut werden.
/// </summary>
public class InMemoryDataStore_ParentChildRelation_IntegrationTests
{
    /// <summary>
    /// Szenario: User möchte zur Laufzeit eine ParentChildRelationship zwischen
    /// Kategorien und Produkten erstellen und verwenden.
    /// </summary>
    [Fact]
    public async Task RuntimeCreation_CategoryProductRelationship_UserScenario()
    {
        // ====================================================================
        // PHASE 1: App-Initialisierung
        // ====================================================================

        // 1. DI Container einrichten
        var services = new ServiceCollection();
        services.AddDataStoresCore();
        services.AddDataStoreRegistrar<CategoryProductRegistrar>();

        var serviceProvider = services.BuildServiceProvider();

        // 2. Bootstrap ausführen
        await DataStoreBootstrap.RunAsync(serviceProvider);

        // 3. DataStores-Facade abrufen
        var dataStores = serviceProvider.GetRequiredService<IDataStores>();

        // ====================================================================
        // PHASE 2: Stammdaten anlegen
        // ====================================================================

        // 4. Kategorien im globalen Store anlegen
        var categoryStore = dataStores.GetGlobal<Category>();
        
        var electronics = new Category
        {
            Id = 1,
            Name = "Elektronik",
            Description = "Elektronische Geräte und Zubehör"
        };

        var computers = new Category
        {
            Id = 2,
            Name = "Computer",
            Description = "Desktop-PCs, Laptops und Zubehör",
            ParentCategoryId = 1 // Unterkategorie von Elektronik
        };

        var accessories = new Category
        {
            Id = 3,
            Name = "Zubehör",
            Description = "Diverses Computer-Zubehör",
            ParentCategoryId = 2
        };

        var furniture = new Category
        {
            Id = 4,
            Name = "Möbel",
            Description = "Büromöbel und Einrichtung"
        };

        categoryStore.AddRange(new[] { electronics, computers, accessories, furniture });

        // 5. Produkte im globalen Store anlegen
        var productStore = dataStores.GetGlobal<Product>();

        var products = new[]
        {
            // Computer-Produkte
            new Product { Id = 101, CategoryId = 2, Name = "Gaming Laptop", Price = 1899.99m, Stock = 5 },
            new Product { Id = 102, CategoryId = 2, Name = "Business Laptop", Price = 1299.99m, Stock = 10 },
            new Product { Id = 103, CategoryId = 2, Name = "Desktop PC", Price = 999.99m, Stock = 8 },
            
            // Zubehör-Produkte
            new Product { Id = 201, CategoryId = 3, Name = "Wireless Mouse", Price = 29.99m, Stock = 50 },
            new Product { Id = 202, CategoryId = 3, Name = "Mechanical Keyboard", Price = 89.99m, Stock = 30 },
            new Product { Id = 203, CategoryId = 3, Name = "USB-C Hub", Price = 49.99m, Stock = 25 },
            new Product { Id = 204, CategoryId = 3, Name = "Webcam", Price = 79.99m, Stock = 15 },
            
            // Möbel-Produkte
            new Product { Id = 301, CategoryId = 4, Name = "Schreibtisch", Price = 299.99m, Stock = 12 },
            new Product { Id = 302, CategoryId = 4, Name = "Bürostuhl", Price = 199.99m, Stock = 20 }
        };

        productStore.AddRange(products);

        Assert.Equal(4, categoryStore.Items.Count);
        Assert.Equal(9, productStore.Items.Count);

        // ====================================================================
        // PHASE 3: Zur Laufzeit ParentChildRelationship erstellen
        // ====================================================================

        // 6. Beziehung für Computer-Kategorie erstellen
        var computerCategory = categoryStore.Items.First(c => c.Id == 2);

        var computerProductsRelation = new ParentChildRelationship<Category, Product>(
            dataStores,
            parent: computerCategory,
            filter: (category, product) => product.CategoryId == category.Id
        );

        // 7. Globale Datenquelle verwenden
        computerProductsRelation.UseGlobalDataSource();

        // 8. Beziehung aktualisieren (lädt gefilterte Produkte)
        computerProductsRelation.Refresh();

        // ====================================================================
        // PHASE 4: Mit der Beziehung arbeiten
        // ====================================================================

        // 9. Alle Produkte der Computer-Kategorie abrufen
        var computerProducts = computerProductsRelation.Childs.Items;

        Assert.Equal(3, computerProducts.Count);
        Assert.All(computerProducts, p => Assert.Equal(2, p.CategoryId));
        Assert.Contains(computerProducts, p => p.Name == "Gaming Laptop");
        Assert.Contains(computerProducts, p => p.Name == "Business Laptop");
        Assert.Contains(computerProducts, p => p.Name == "Desktop PC");

        // 10. Geschäftslogik: Gesamtwert des Lagerbestands berechnen
        var totalInventoryValue = computerProducts.Sum(p => p.Price * p.Stock);
        // Gaming Laptop: 1899.99 * 5 = 9499.95
        // Business Laptop: 1299.99 * 10 = 12999.90
        // Desktop PC: 999.99 * 8 = 7999.92
        // Total: 30499.77
        Assert.Equal(30499.77m, totalInventoryValue, precision: 2);

        // 11. Geschäftslogik: Durchschnittspreis ermitteln
        var avgPrice = computerProducts.Average(p => p.Price);
        Assert.Equal(1399.99m, avgPrice, precision: 2);

        // ====================================================================
        // PHASE 5: Zweite Beziehung für andere Kategorie
        // ====================================================================

        // 12. Beziehung für Zubehör-Kategorie erstellen
        var accessoriesCategory = categoryStore.Items.First(c => c.Id == 3);

        var accessoryProductsRelation = new ParentChildRelationship<Category, Product>(
            dataStores,
            parent: accessoriesCategory,
            filter: (category, product) => product.CategoryId == category.Id
        );

        accessoryProductsRelation.UseGlobalDataSource();
        accessoryProductsRelation.Refresh();

        // 13. Zubehör-Produkte abrufen
        var accessoryProducts = accessoryProductsRelation.Childs.Items;

        Assert.Equal(4, accessoryProducts.Count);
        Assert.All(accessoryProducts, p => Assert.Equal(3, p.CategoryId));

        // ====================================================================
        // PHASE 6: Mit Snapshot arbeiten (gefilterte Datenquelle)
        // ====================================================================

        // 14. Beziehung mit Snapshot erstellen (nur Produkte auf Lager)
        var furnitureCategory = categoryStore.Items.First(c => c.Id == 4);

        var inStockFurnitureRelation = new ParentChildRelationship<Category, Product>(
            dataStores,
            parent: furnitureCategory,
            filter: (category, product) => product.CategoryId == category.Id
        );

        // 15. Snapshot mit Vorfilterung erstellen (nur Artikel mit Stock > 0)
        inStockFurnitureRelation.UseSnapshotFromGlobal(predicate: p => p.Stock > 0);
        inStockFurnitureRelation.Refresh();

        var inStockFurniture = inStockFurnitureRelation.Childs.Items;

        Assert.Equal(2, inStockFurniture.Count); // Alle Möbel sind auf Lager
        Assert.All(inStockFurniture, p => Assert.True(p.Stock > 0));

        // ====================================================================
        // PHASE 7: Dynamische Updates - Neue Produkte hinzufügen
        // ====================================================================

        // 16. Neues Produkt zur Computer-Kategorie hinzufügen
        var newProduct = new Product
        {
            Id = 104,
            CategoryId = 2,
            Name = "Mini PC",
            Price = 599.99m,
            Stock = 15
        };

        productStore.Add(newProduct);

        // 17. Beziehung aktualisieren
        computerProductsRelation.Refresh();

        // 18. Neues Produkt sollte jetzt in der Beziehung erscheinen
        Assert.Equal(4, computerProductsRelation.Childs.Items.Count);
        Assert.Contains(computerProductsRelation.Childs.Items, p => p.Name == "Mini PC");

        // ====================================================================
        // PHASE 8: Event-Handling auf Child-Collection
        // ====================================================================

        // 19. Events auf der Child-Collection abonnieren
        var childChangeLog = new List<string>();
        accessoryProductsRelation.Childs.Changed += (sender, args) =>
        {
            childChangeLog.Add($"{args.ChangeType}: {args.AffectedItems.Count} items");
        };

        // 20. Child-Collection direkt manipulieren (lokale Änderung)
        var tempProduct = new Product
        {
            Id = 999,
            CategoryId = 3,
            Name = "Temporary Item",
            Price = 9.99m,
            Stock = 1
        };
        accessoryProductsRelation.Childs.Add(tempProduct);

        Assert.Single(childChangeLog);
        Assert.Equal("Add: 1 items", childChangeLog[0]);

        // 21. Refresh überschreibt lokale Änderungen
        accessoryProductsRelation.Refresh();
        Assert.DoesNotContain(accessoryProductsRelation.Childs.Items, p => p.Id == 999);

        // ====================================================================
        // PHASE 9: Komplexere Filter-Logik
        // ====================================================================

        // 22. Beziehung mit komplexem Filter (Kategorie UND Preis)
        var premiumComputersRelation = new ParentChildRelationship<Category, Product>(
            dataStores,
            parent: computerCategory,
            filter: (category, product) =>
                product.CategoryId == category.Id &&
                product.Price > 1000m && // Nur teure Produkte
                product.Stock > 0        // Nur auf Lager
        );

        premiumComputersRelation.UseGlobalDataSource();
        premiumComputersRelation.Refresh();

        var premiumComputers = premiumComputersRelation.Childs.Items;

        Assert.Equal(2, premiumComputers.Count); // Gaming Laptop und Business Laptop
        Assert.All(premiumComputers, p =>
        {
            Assert.True(p.Price > 1000m);
            Assert.True(p.Stock > 0);
        });

        // ====================================================================
        // PHASE 10: Mehrere Hierarchie-Ebenen (Parent-Child-Grandchild)
        // ====================================================================

        // 23. Kategorien-Hierarchie erstellen
        var mainCategoryRelation = new ParentChildRelationship<Category, Category>(
            dataStores,
            parent: electronics,
            filter: (parentCat, childCat) => childCat.ParentCategoryId == parentCat.Id
        );

        mainCategoryRelation.UseGlobalDataSource();
        mainCategoryRelation.Refresh();

        // 24. Unterkategorien von Elektronik
        var subCategories = mainCategoryRelation.Childs.Items;
        Assert.Single(subCategories); // Nur "Computer" ist Unterkategorie von "Elektronik"
        Assert.Equal("Computer", subCategories[0].Name);

        // 25. Zweite Ebene: Unterkategorien von Computer
        var computerSubCatRelation = new ParentChildRelationship<Category, Category>(
            dataStores,
            parent: computers,
            filter: (parentCat, childCat) => childCat.ParentCategoryId == parentCat.Id
        );

        computerSubCatRelation.UseGlobalDataSource();
        computerSubCatRelation.Refresh();

        var computerSubCategories = computerSubCatRelation.Childs.Items;
        Assert.Single(computerSubCategories); // "Zubehör" ist Unterkategorie von "Computer"
        Assert.Equal("Zubehör", computerSubCategories[0].Name);
    }

    /// <summary>
    /// Szenario: User erstellt Beziehungen zwischen verschiedenen Entity-Typen
    /// in einem E-Commerce-Kontext (Kunde -> Bestellungen -> Bestellpositionen).
    /// </summary>
    [Fact]
    public async Task RuntimeCreation_CustomerOrderLineItemHierarchy_UserScenario()
    {
        // Arrange - App Setup
        var services = new ServiceCollection();
        services.AddDataStoresCore();
        services.AddDataStoreRegistrar<ECommerceRegistrar>();

        var serviceProvider = services.BuildServiceProvider();
        await DataStoreBootstrap.RunAsync(serviceProvider);

        var dataStores = serviceProvider.GetRequiredService<IDataStores>();

        // Setup data
        var customerStore = dataStores.GetGlobal<Customer>();
        var orderStore = dataStores.GetGlobal<Order>();
        var lineItemStore = dataStores.GetGlobal<OrderLineItem>();

        // Kunden anlegen
        var customer1 = new Customer { Id = 1, Name = "Max Mustermann", Email = "max@example.com" };
        var customer2 = new Customer { Id = 2, Name = "Anna Schmidt", Email = "anna@example.com" };
        customerStore.AddRange(new[] { customer1, customer2 });

        // Bestellungen anlegen
        var order1 = new Order { Id = 101, CustomerId = 1, OrderDate = DateTime.UtcNow.AddDays(-5), Total = 0 };
        var order2 = new Order { Id = 102, CustomerId = 1, OrderDate = DateTime.UtcNow.AddDays(-2), Total = 0 };
        var order3 = new Order { Id = 103, CustomerId = 2, OrderDate = DateTime.UtcNow.AddDays(-1), Total = 0 };
        orderStore.AddRange(new[] { order1, order2, order3 });

        // Bestellpositionen anlegen
        var lineItems = new[]
        {
            // Bestellung 101
            new OrderLineItem { Id = 1, OrderId = 101, ProductName = "Laptop", Quantity = 1, Price = 999.99m },
            new OrderLineItem { Id = 2, OrderId = 101, ProductName = "Mouse", Quantity = 2, Price = 29.99m },
            
            // Bestellung 102
            new OrderLineItem { Id = 3, OrderId = 102, ProductName = "Keyboard", Quantity = 1, Price = 89.99m },
            new OrderLineItem { Id = 4, OrderId = 102, ProductName = "Monitor", Quantity = 1, Price = 299.99m },
            new OrderLineItem { Id = 5, OrderId = 102, ProductName = "USB Cable", Quantity = 3, Price = 9.99m },
            
            // Bestellung 103
            new OrderLineItem { Id = 6, OrderId = 103, ProductName = "Webcam", Quantity = 1, Price = 79.99m }
        };
        lineItemStore.AddRange(lineItems);

        // Act - Hierarchie aufbauen: Customer -> Orders
        var customerOrdersRelation = new ParentChildRelationship<Customer, Order>(
            dataStores,
            parent: customer1,
            filter: (customer, order) => order.CustomerId == customer.Id
        );

        customerOrdersRelation.UseGlobalDataSource();
        customerOrdersRelation.Refresh();

        // Assert - Bestellungen des ersten Kunden
        var customer1Orders = customerOrdersRelation.Childs.Items;
        Assert.Equal(2, customer1Orders.Count);
        Assert.All(customer1Orders, o => Assert.Equal(1, o.CustomerId));

        // Act - Tiefere Hierarchie: Order -> LineItems
        var firstOrder = customer1Orders.First();
        var orderLineItemsRelation = new ParentChildRelationship<Order, OrderLineItem>(
            dataStores,
            parent: firstOrder,
            filter: (order, lineItem) => lineItem.OrderId == order.Id
        );

        orderLineItemsRelation.UseGlobalDataSource();
        orderLineItemsRelation.Refresh();

        // Assert - Positionen der ersten Bestellung
        var firstOrderLineItems = orderLineItemsRelation.Childs.Items;
        Assert.Equal(2, firstOrderLineItems.Count);

        // Geschäftslogik - Bestellsumme berechnen
        var orderTotal = firstOrderLineItems.Sum(li => li.Quantity * li.Price);
        Assert.Equal(1059.97m, orderTotal, precision: 2);

        // Complete scenario - alle Bestellungen eines Kunden mit allen Positionen
        var allCustomerLineItems = new List<OrderLineItem>();
        foreach (var order in customer1Orders)
        {
            var relation = new ParentChildRelationship<Order, OrderLineItem>(
                dataStores,
                parent: order,
                filter: (o, li) => li.OrderId == o.Id
            );
            relation.UseGlobalDataSource();
            relation.Refresh();
            allCustomerLineItems.AddRange(relation.Childs.Items);
        }

        // Kunde 1 hat insgesamt 5 Bestellpositionen über 2 Bestellungen
        Assert.Equal(5, allCustomerLineItems.Count);
    }

    // ====================================================================
    // Helper Classes - Entity Models
    // ====================================================================

    private class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public int? ParentCategoryId { get; set; }
    }

    private class Product
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public int Stock { get; set; }
    }

    private class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
    }

    private class Order
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal Total { get; set; }
    }

    private class OrderLineItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string ProductName { get; set; } = "";
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

    // ====================================================================
    // Helper Classes - Registrars
    // ====================================================================

    private class CategoryProductRegistrar : IDataStoreRegistrar
    {
        public void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider)
        {
            registry.RegisterGlobal(new InMemoryDataStore<Category>());
            registry.RegisterGlobal(new InMemoryDataStore<Product>());
        }
    }

    private class ECommerceRegistrar : IDataStoreRegistrar
    {
        public void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider)
        {
            registry.RegisterGlobal(new InMemoryDataStore<Customer>());
            registry.RegisterGlobal(new InMemoryDataStore<Order>());
            registry.RegisterGlobal(new InMemoryDataStore<OrderLineItem>());
        }
    }
}
