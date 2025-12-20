using System;
using System.Linq;
using DataStores.Abstractions;
using DataStores.Bootstrap;
using DataStores.Relations;
using DataStores.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DataStores.Tests.Integration;

/// <summary>
/// Integration tests demonstrating the new service-centric relation architecture.
/// Shows how ParentChildRelationService, Views, and OneToOne work together.
/// </summary>
[Trait("Category", "Integration")]
public class ParentChildRelationService_Integration_Tests
{
    #region Test Entities

    private class Organization
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
    }

    private class Department : System.ComponentModel.INotifyPropertyChanged
    {
        private Guid _organizationId;
        private string _name = "";

        public Guid Id { get; set; }
        
        public Guid OrganizationId
        {
            get => _organizationId;
            set
            {
                if (_organizationId != value)
                {
                    _organizationId = value;
                    PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(OrganizationId)));
                }
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Name)));
                }
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    }

    private class Employee : System.ComponentModel.INotifyPropertyChanged
    {
        private Guid _departmentId;
        private string _name = "";

        public Guid Id { get; set; }
        
        public Guid DepartmentId
        {
            get => _departmentId;
            set
            {
                if (_departmentId != value)
                {
                    _departmentId = value;
                    PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(DepartmentId)));
                }
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Name)));
                }
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    }

    #endregion

    [Fact]
    public async Task CompleteScenario_OrganizationDepartmentEmployee_WithDynamicTracking()
    {
        // ====================================================================
        // PHASE 1: Setup DI and Bootstrap
        // ====================================================================
        var services = new ServiceCollection();
        services.AddDataStoresCore();
        services.AddDataStoreRegistrar<TestDataStoreRegistrar>();

        var serviceProvider = services.BuildServiceProvider();
        await DataStoreBootstrap.RunAsync(serviceProvider);

        var dataStores = serviceProvider.GetRequiredService<IDataStores>();

        // ====================================================================
        // PHASE 2: Get global stores
        // ====================================================================
        var orgStore = dataStores.GetGlobal<Organization>();
        var deptStore = dataStores.GetGlobal<Department>();
        var empStore = dataStores.GetGlobal<Employee>();

        // ====================================================================
        // PHASE 3: Create sample data
        // ====================================================================
        var acmeId = Guid.NewGuid();
        var globexId = Guid.NewGuid();

        orgStore.AddRange(new[]
        {
            new Organization { Id = acmeId, Name = "ACME Corp" },
            new Organization { Id = globexId, Name = "Globex Inc" }
        });

        var acmeItId = Guid.NewGuid();
        var acmeHrId = Guid.NewGuid();
        var globexItId = Guid.NewGuid();

        deptStore.AddRange(new[]
        {
            new Department { Id = acmeItId, OrganizationId = acmeId, Name = "IT" },
            new Department { Id = acmeHrId, OrganizationId = acmeId, Name = "HR" },
            new Department { Id = globexItId, OrganizationId = globexId, Name = "IT" }
        });

        empStore.AddRange(new[]
        {
            new Employee { Id = Guid.NewGuid(), DepartmentId = acmeItId, Name = "Alice" },
            new Employee { Id = Guid.NewGuid(), DepartmentId = acmeItId, Name = "Bob" },
            new Employee { Id = Guid.NewGuid(), DepartmentId = acmeHrId, Name = "Carol" },
            new Employee { Id = Guid.NewGuid(), DepartmentId = globexItId, Name = "David" }
        });

        // ====================================================================
        // PHASE 4: Setup Relations (Service-centric architecture)
        // ====================================================================

        // Organization -> Departments (1:n)
        var orgDeptDefinition = new RelationDefinition<Organization, Department, Guid>(
            parent => parent.Id,
            child => child.OrganizationId);

        var orgDeptService = new ParentChildRelationService<Organization, Department, Guid>(
            orgStore, deptStore, orgDeptDefinition);

        // Department -> Employees (1:n)
        var deptEmpDefinition = new RelationDefinition<Department, Employee, Guid>(
            parent => parent.Id,
            child => child.DepartmentId);

        var deptEmpService = new ParentChildRelationService<Department, Employee, Guid>(
            deptStore, empStore, deptEmpDefinition);

        // ====================================================================
        // PHASE 5: Access Relations
        // ====================================================================
        var acme = orgStore.Items.First(o => o.Id == acmeId);
        var acmeRelation = orgDeptService.GetRelation(acme);

        Assert.Equal(2, acmeRelation.Childs.Count);
        Assert.All(acmeRelation.Childs, d => Assert.Equal(acmeId, d.OrganizationId));

        var acmeIt = deptStore.Items.First(d => d.Id == acmeItId);
        var acmeItEmployees = deptEmpService.GetChildren(acmeIt);

        Assert.Equal(2, acmeItEmployees.Count);
        Assert.Contains(acmeItEmployees, e => e.Name == "Alice");
        Assert.Contains(acmeItEmployees, e => e.Name == "Bob");

        // ====================================================================
        // PHASE 6: Dynamic Tracking - Add new employee
        // ====================================================================
        var newEmployee = new Employee 
        { 
            Id = Guid.NewGuid(), 
            DepartmentId = acmeItId, 
            Name = "Eve" 
        };
        empStore.Add(newEmployee);

        // Should automatically appear in relation
        Assert.Equal(3, acmeItEmployees.Count);
        Assert.Contains(acmeItEmployees, e => e.Name == "Eve");

        // ====================================================================
        // PHASE 7: Dynamic Tracking - Move employee between departments
        // ====================================================================
        var alice = empStore.Items.First(e => e.Name == "Alice");
        var acmeHr = deptStore.Items.First(d => d.Id == acmeHrId);
        var acmeHrEmployees = deptEmpService.GetChildren(acmeHr);

        // Before: Alice in IT, Carol in HR
        Assert.Equal(3, acmeItEmployees.Count);
        Assert.Single(acmeHrEmployees);

        // Move Alice from IT to HR
        alice.DepartmentId = acmeHrId;

        // After: Alice moved to HR automatically
        Assert.Equal(2, acmeItEmployees.Count);
        Assert.DoesNotContain(acmeItEmployees, e => e.Name == "Alice");
        
        Assert.Equal(2, acmeHrEmployees.Count);
        Assert.Contains(acmeHrEmployees, e => e.Name == "Alice");
        Assert.Contains(acmeHrEmployees, e => e.Name == "Carol");

        // ====================================================================
        // PHASE 8: Dynamic Tracking - Remove department
        // ====================================================================
        deptStore.Remove(acmeHr);

        // Departments relation should update
        Assert.Single(acmeRelation.Childs);
        Assert.DoesNotContain(acmeRelation.Childs, d => d.Name == "HR");

        // ====================================================================
        // PHASE 9: OneToOne View
        // ====================================================================
        // Organization might have one "main" department (using 1:1 view)
        var globex = orgStore.Items.First(o => o.Id == globexId);
        var globexRelation = orgDeptService.GetRelation(globex);
        var globexMainDept = new OneToOneRelationView<Organization, Department>(
            globexRelation, 
            MultipleChildrenPolicy.TakeFirst);

        Assert.True(globexMainDept.HasChild);
        Assert.Equal("IT", globexMainDept.ChildOrNull?.Name);

        // Add another department - TakeFirst policy still returns first
        deptStore.Add(new Department 
        { 
            Id = Guid.NewGuid(), 
            OrganizationId = globexId, 
            Name = "Sales" 
        });

        Assert.Equal("IT", globexMainDept.ChildOrNull?.Name); // Still first

        // ====================================================================
        // PHASE 10: Clean up
        // ====================================================================
        orgDeptService.Dispose();
        deptEmpService.Dispose();

        // After dispose, adding new employees should not update relations
        var initialCount = acmeItEmployees.Count;
        empStore.Add(new Employee 
        { 
            Id = Guid.NewGuid(), 
            DepartmentId = acmeItId, 
            Name = "Frank" 
        });

        Assert.Equal(initialCount, acmeItEmployees.Count); // No update
    }

    [Fact]
    public void MultipleLevelsHierarchy_WithSorting()
    {
        // Arrange
        var orgStore = new InMemoryDataStore<Organization>();
        var deptStore = new InMemoryDataStore<Department>();
        var empStore = new InMemoryDataStore<Employee>();

        var orgId = Guid.NewGuid();
        orgStore.Add(new Organization { Id = orgId, Name = "TechCorp" });

        var dept1Id = Guid.NewGuid();
        var dept2Id = Guid.NewGuid();
        deptStore.AddRange(new[]
        {
            new Department { Id = dept1Id, OrganizationId = orgId, Name = "Development" },
            new Department { Id = dept2Id, OrganizationId = orgId, Name = "QA" }
        });

        empStore.AddRange(new[]
        {
            new Employee { Id = Guid.NewGuid(), DepartmentId = dept1Id, Name = "Zoe" },
            new Employee { Id = Guid.NewGuid(), DepartmentId = dept1Id, Name = "Alice" },
            new Employee { Id = Guid.NewGuid(), DepartmentId = dept2Id, Name = "Mike" },
            new Employee { Id = Guid.NewGuid(), DepartmentId = dept2Id, Name = "Anna" }
        });

        // Setup with sorting by name
        var deptEmpDefinition = new RelationDefinition<Department, Employee, Guid>(
            parent => parent.Id,
            child => child.DepartmentId,
            childComparer: Comparer<Employee>.Create((x, y) => 
                string.Compare(x.Name, y.Name, StringComparison.Ordinal)));

        var deptEmpService = new ParentChildRelationService<Department, Employee, Guid>(
            deptStore, empStore, deptEmpDefinition);

        // Act
        var dev = deptStore.Items.First(d => d.Name == "Development");
        var devEmployees = deptEmpService.GetChildren(dev);

        // Assert - Should be sorted
        Assert.Equal(2, devEmployees.Count);
        Assert.Equal("Alice", devEmployees[0].Name);
        Assert.Equal("Zoe", devEmployees[1].Name);

        // Add new employee - should maintain sort
        empStore.Add(new Employee { Id = Guid.NewGuid(), DepartmentId = dept1Id, Name = "Charlie" });

        Assert.Equal(3, devEmployees.Count);
        Assert.Equal("Alice", devEmployees[0].Name);
        Assert.Equal("Charlie", devEmployees[1].Name);
        Assert.Equal("Zoe", devEmployees[2].Name);
    }

    private class TestDataStoreRegistrar : IDataStoreRegistrar
    {
        public void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider)
        {
            registry.RegisterGlobal(new InMemoryDataStore<Organization>());
            registry.RegisterGlobal(new InMemoryDataStore<Department>());
            registry.RegisterGlobal(new InMemoryDataStore<Employee>());
        }
    }
}
