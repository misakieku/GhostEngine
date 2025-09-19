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

    public readonly UnsafeBitSet ComputeFilterBitMask(World world)
    {
        UnsafeBitSet? allMask = null;
        UnsafeBitSet? anyMask = null;
        UnsafeBitSet? absentMask = null;

        foreach (var typeHandle in _all)
        {
            var mask = world.ComponentStorage.GetOrCreateMask(typeHandle);

            if (!allMask.HasValue)
            {
                allMask = new UnsafeBitSet(mask.Length);
                allMask.Value.SetAll();
            }

            allMask &= mask;
        }

        foreach (var typeHandle in _any)
        {
            var mask = world.ComponentStorage.GetOrCreateMask(typeHandle);

            if (!anyMask.HasValue)
            {
                anyMask = new UnsafeBitSet(mask.Length);
            }

            anyMask |= mask;
        }

        foreach (var typeHandle in _absent)
        {
            var mask = world.ComponentStorage.GetOrCreateMask(typeHandle);

            if (!absentMask.HasValue)
            {
                absentMask = new UnsafeBitSet(mask.Length);
            }

            absentMask |= mask;
        }

        var result = new UnsafeBitSet(world.EntityManager.EntityCount);
        result.SetAll();

        if (allMask.HasValue)
        {
            result &= allMask.Value;
        }

        if (anyMask.HasValue)
        {
            result &= anyMask.Value;
        }

        if (absentMask.HasValue)
        {
            result &= ~absentMask.Value;
        }

        return result;
    }
}