using Ghost.Core;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Ghost.Entities.Query;

[Flags]
internal enum FilterMode
{
    All = 1 << 0,
    Any = 1 << 1,
    Absent = 1 << 2,
    Disabled = 1 << 3,
}

internal readonly struct FilterEntry(TypeHandle id, FilterMode mode)
{
    public readonly TypeHandle typeHandle = id;
    public readonly FilterMode mode = mode;
}

internal struct QueryFilter()
{
    internal List<TypeHandle> _all = new(6);
    internal List<TypeHandle> _any = new(6);
    internal List<TypeHandle> _absent = new(6);
    internal List<TypeHandle> _disabled = new(6);

    public readonly BitSet ComputeFilterBitMask(World world)
    {
        BitSet? allMask = null;
        BitSet? anyMask = null;
        BitSet? absentMask = null;

        var hasAll = false;
        var hasAny = false;
        var hasAbsent = false;

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

        var result = new BitSet(world.EntityManager.EntityCount);
        result.SetAll();

        if (hasAll)
        {
            result &= allMask!;
        }

        if (hasAny)
        {
            result &= anyMask!;
        }

        if (hasAbsent)
        {
            result &= ~absentMask!;
        }

        return result;
    }
}