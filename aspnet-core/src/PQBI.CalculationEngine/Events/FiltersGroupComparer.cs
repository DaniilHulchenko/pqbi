using PQS.Data.Events;

public sealed class FiltersGroupComparer : IEqualityComparer<FiltersGroup>
{
    public bool Equals(FiltersGroup? x, FiltersGroup? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;

        // Quick check: same number of filters
        if (x.FiltersCount != y.FiltersCount) return false;

        // Compare all filter types & values
        foreach (var type in x.GetAllFilterTypes())
        {
            var xf = x.GetFilter(type);
            var yf = y.GetFilter(type);
            if (yf is null) return false;
            if (!xf.Equals(yf)) return false;   // relies on FilterBase.Equals being correct
        }
        return true;
    }

    public int GetHashCode(FiltersGroup obj)
    {
        // Order-independent hash from (FilterType, Filter) pairs
        unchecked
        {
            int hash = 17;
            // To ensure stability, iterate in a deterministic order
            foreach (var type in obj.GetAllFilterTypes().OrderBy(t => (int)t))
            {
                var f = obj.GetFilter(type);
                hash = hash * 31 + type.GetHashCode();
                hash = hash * 31 + (f?.GetHashCode() ?? 0);
            }
            return hash;
        }
    }
}
