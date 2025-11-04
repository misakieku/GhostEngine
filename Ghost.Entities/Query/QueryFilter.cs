using Ghost.Core;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Ghost.Entities.Query;

public struct QueryFilter : IDisposable
{
    private readonly Stack.Scope _scope;

    internal UnsafeList<TypeHandle> _all;
    internal UnsafeList<TypeHandle> _any;
    internal UnsafeList<TypeHandle> _absent;
    internal UnsafeList<TypeHandle> _disabled;

    public QueryFilter()
    {
        _scope = AllocationManager.CreateStackScope();

        _all = new UnsafeList<TypeHandle>(4, Allocator.Stack);
        _any = new UnsafeList<TypeHandle>(4, Allocator.Stack);
        _absent = new UnsafeList<TypeHandle>(4, Allocator.Stack);
        _disabled = new UnsafeList<TypeHandle>(4, Allocator.Stack);
    }

    public readonly UnsafeBitSet ComputeFilterBitMask(World world, Allocator allocator)
    {
        UnsafeBitSet allMask = default;
        UnsafeBitSet anyMask = default;
        UnsafeBitSet absentMask = default;

        var result = new UnsafeBitSet(world.EntityManager.EntityCount, allocator);
        result.SetAll();

        using (AllocationManager.CreateStackScope())
        {
            foreach (var typeHandle in _all)
            {
                var mask = world.ComponentStorage.GetOrCreateMask(typeHandle);

                if (!allMask.IsCreated)
                {
                    allMask = new UnsafeBitSet(mask.Length, Allocator.Stack, AllocationOption.None);
                    allMask.SetAll();
                }

                allMask.AndOperation(mask);
            }

            foreach (var typeHandle in _any)
            {
                var mask = world.ComponentStorage.GetOrCreateMask(typeHandle);

                if (!anyMask.IsCreated)
                {
                    anyMask = new UnsafeBitSet(mask.Length, Allocator.Stack);
                }

                anyMask.OrOperation(mask);
            }

            foreach (var typeHandle in _absent)
            {
                var mask = world.ComponentStorage.GetOrCreateMask(typeHandle);

                if (!absentMask.IsCreated)
                {
                    absentMask = new UnsafeBitSet(mask.Length, Allocator.Stack);
                }

                absentMask.OrOperation(mask);
            }

            if (allMask.IsCreated)
            {
                result.AndOperation(allMask);
            }

            if (anyMask.IsCreated)
            {
                result.AndOperation(anyMask);
            }

            if (absentMask.IsCreated)
            {
                result.AndOperation(~absentMask);
            }
        }

        return result;
    }

    public readonly void Dispose()
    {
        _scope.Dispose();
    }
}