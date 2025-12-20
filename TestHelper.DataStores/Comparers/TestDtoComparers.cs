namespace TestHelper.DataStores.Comparers;

/// <summary>
/// Vergleicht TestDto-Objekte nur nach ihrer Id (Guid).
/// </summary>
public sealed class TestDtoIdComparer : IEqualityComparer<Models.TestDto>
{
    public bool Equals(Models.TestDto? x, Models.TestDto? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;
        return x.Id == y.Id;
    }

    public int GetHashCode(Models.TestDto obj)
    {
        if (obj is null) throw new ArgumentNullException(nameof(obj));
        return obj.Id.GetHashCode();
    }
}

/// <summary>
/// Vergleicht TestDto-Objekte nach allen Properties (Deep Equality).
/// </summary>
public sealed class TestDtoDeepComparer : IEqualityComparer<Models.TestDto>
{
    public bool Equals(Models.TestDto? x, Models.TestDto? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;

        return x.Id == y.Id &&
               x.Name == y.Name &&
               x.Age == y.Age &&
               x.CreatedUtc == y.CreatedUtc &&
               x.IsActive == y.IsActive &&
               x.Score == y.Score &&
               x.Notes == y.Notes;
    }

    public int GetHashCode(Models.TestDto obj)
    {
        if (obj is null) throw new ArgumentNullException(nameof(obj));

        return HashCode.Combine(
            obj.Id,
            obj.Name,
            obj.Age,
            obj.CreatedUtc,
            obj.IsActive,
            obj.Score,
            obj.Notes);
    }
}
