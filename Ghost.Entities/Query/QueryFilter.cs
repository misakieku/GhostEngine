using Misaki.HighPerformance.Unsafe.Collections;

namespace Ghost.Entities.Query;

[Flags]
internal enum FilterMode
{
    All = 1 << 0,
    Any = 1 << 1,
    Absent = 1 << 2,
    Disabled = 1 << 3,
}

internal readonly struct FilterEntry(nint id, FilterMode mode)
{
    public readonly nint typeHandle = id;
    public readonly FilterMode mode = mode;
}

internal struct QueryFilter()
{
    internal List<nint> _all = new(6);
    internal List<nint> _any = new(6);
    internal List<nint> _absent = new(6);
    internal List<nint> _disabled = new(6);

    public readonly void ComputeFilterBitMask(World world, BitSet result)
    {
        BitSet allMask = new();
        BitSet anyMask = new();
        BitSet absentMask = new();

        var hasAll = false;
        var hasAny = false;
        var hasAbsent = false;

        // Compute All mask (intersection)
        foreach (var typeHandle in _all)
        {
            var mask = world.ComponentStorage.GetOrCreateMask(typeHandle);

            if (!hasAll)
            {
                allMask = new BitSet(mask.Length);
                allMask.SetAll();
                hasAll = true;
            }

            allMask &= mask;
        }

        // Compute Any mask (union)
        foreach (var typeHandle in _any)
        {
            var mask = world.ComponentStorage.GetOrCreateMask(typeHandle);

            if (!hasAny)
            {
                anyMask = new BitSet(mask.Length);
                hasAny = true;
            }

            anyMask |= mask;
        }

        // Compute Absent mask (union for exclusion)
        foreach (var typeHandle in _absent)
        {
            var mask = world.ComponentStorage.GetOrCreateMask(typeHandle);

            if (!hasAbsent)
            {
                absentMask = new BitSet(mask.Length);
                hasAbsent = true;
            }

            absentMask |= mask;
        }

        result.SetAll();

        if (hasAll)
        {
            result &= allMask;
        }

        if (hasAny)
        {
            result &= anyMask;
        }

        if (hasAbsent)
        {
            result &= ~absentMask;
        }
    }
}