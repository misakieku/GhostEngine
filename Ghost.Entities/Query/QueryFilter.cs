using Ghost.Core;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Runtime.CompilerServices;

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
        UnsafeBitSet allMask = default;
        UnsafeBitSet anyMask = default;
        UnsafeBitSet absentMask = default;

        using var scope = AllocationManager.CreateStackScope();

        foreach (var typeHandle in _all)
        {
            var mask = world.ComponentStorage.GetOrCreateMask(typeHandle);

            if (!allMask.IsCreated)
            {
                allMask = new UnsafeBitSet(mask.Length, Allocator.Stack, AllocationOption.Clear);
                allMask.SetAll();
            }

            allMask.AndOperation(mask);
        }

        foreach (var typeHandle in _any)
        {
            var mask = world.ComponentStorage.GetOrCreateMask(typeHandle);

            if (!anyMask.IsCreated)
            {
                anyMask = new UnsafeBitSet(mask.Length, Allocator.Stack, AllocationOption.Clear);
            }

            anyMask.OrOperation(mask);
        }

        foreach (var typeHandle in _absent)
        {
            var mask = world.ComponentStorage.GetOrCreateMask(typeHandle);

            if (!absentMask.IsCreated)
            {
                absentMask = new UnsafeBitSet(mask.Length, Allocator.Stack, AllocationOption.Clear);
            }

            absentMask.OrOperation(mask);
        }

        var result = new UnsafeBitSet(world.EntityManager.EntityCount, Allocator.Persistent);
        result.SetAll();

        if (allMask.IsCreated)
        {
            result.AndOperation(allMask);
            allMask.Dispose();
        }

        if (anyMask.IsCreated)
        {
            result.AndOperation(anyMask);
            anyMask.Dispose();
        }

        if (absentMask.IsCreated)
        {
            result.AndOperation(~absentMask);
            absentMask.Dispose();
        }

        return result;
    }
}