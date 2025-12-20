using System;

namespace DataStores.Tests.TestEntities;

/// <summary>
/// Test-Entity für Parent in ParentChildRelationship-Tests.
/// </summary>
public class Group
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
