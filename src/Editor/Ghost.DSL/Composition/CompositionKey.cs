namespace Ghost.DSL.Composition;

public static class CompositionKey
{
    private const ulong OFFSET_BASIS = 14695981039346656037ul;
    private const ulong PRIME = 1099511628211ul;

    /// <summary>
    /// Computes a deterministic 64-bit composition key from a set of interface-to-implementation bindings.
    /// The bindings are canonicalized by sorting on InterfaceId before hashing, ensuring that order of
    /// definition or discovery does not alter the key.
    /// </summary>
    public static ulong Compute(ReadOnlySpan<(ulong InterfaceId, ulong ImplementationId)> bindings)
    {
        if (bindings.IsEmpty)
        {
            return 0;
        }

        // Sort bindings by InterfaceId ascending into a local array to guarantee canonical order
        var sorted = new (ulong InterfaceId, ulong ImplementationId)[bindings.Length];
        bindings.CopyTo(sorted);
        Array.Sort(sorted, static (a, b) => a.InterfaceId.CompareTo(b.InterfaceId));

        var hash = OFFSET_BASIS;

        for (var i = 0; i < sorted.Length; i++)
        {
            var (ifaceId, implId) = sorted[i];

            hash ^= ifaceId;
            hash *= PRIME;
            hash ^= implId;
            hash *= PRIME;
        }

        return hash;
    }
}
