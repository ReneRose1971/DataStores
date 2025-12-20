using System.Runtime.CompilerServices;

namespace TestHelper.DataStores.Comparers;

/// <summary>
/// Vergleicht TestEntity-Objekte nur nach ihrer Id (int).
/// </summary>
public sealed class TestEntityIdComparer : IEqualityComparer<Models.TestEntity>
{
    public bool Equals(Models.TestEntity? x, Models.TestEntity? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;

        // Für neue Entities (Id == 0): Referenzvergleich
        if (x.Id == 0 && y.Id == 0)
            return ReferenceEquals(x, y);

        return x.Id == y.Id;
    }

    public int GetHashCode(Models.TestEntity obj)
    {
        if (obj is null) throw new ArgumentNullException(nameof(obj));

        // Für neue Entities: Object-Hash
        if (obj.Id == 0)
            return RuntimeHelpers.GetHashCode(obj);

        return obj.Id.GetHashCode();
    }
}

/// <summary>
/// Vergleicht TestEntity-Objekte nach allen Properties (Deep Equality).
/// </summary>
public sealed class TestEntityDeepComparer : IEqualityComparer<Models.TestEntity>
{
    public bool Equals(Models.TestEntity? x, Models.TestEntity? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;

        return x.Id == y.Id &&
               x.Name == y.Name &&
               x.Version == y.Version &&
               x.UpdatedUtc == y.UpdatedUtc &&
               x.IsDeleted == y.IsDeleted &&
               Math.Abs(x.Ratio - y.Ratio) < 0.0001 && // Double-Vergleich mit Toleranz
               x.Tag == y.Tag;
    }

    public int GetHashCode(Models.TestEntity obj)
    {
        if (obj is null) throw new ArgumentNullException(nameof(obj));

        return HashCode.Combine(
            obj.Id,
            obj.Name,
            obj.Version,
            obj.UpdatedUtc,
            obj.IsDeleted,
            obj.Ratio,
            obj.Tag);
    }
}
