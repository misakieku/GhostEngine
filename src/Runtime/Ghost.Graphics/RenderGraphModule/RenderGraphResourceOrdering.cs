using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Numerics;

namespace Ghost.Graphics.RenderGraphModule;

internal struct RenderGraphResourceOrdering : IDisposable
{
    private UnsafeArray<ulong> _useMasks;
    private UnsafeArray<ulong> _afterAllUsesMasks;
    private UnsafeArray<int> _firstUseScheduleIndices;
    private UnsafeArray<int> _lastUseScheduleIndices;

    public readonly int ResourceCount { get; }
    public readonly int PassCount { get; }
    public readonly int WordCount { get; }

    private RenderGraphResourceOrdering(int resourceCount, int passCount, AllocationHandle allocationHandle)
    {
        ResourceCount = resourceCount;
        PassCount = passCount;
        WordCount = (passCount + 63) / 64;
        _useMasks = WordCount > 0 && resourceCount > 0
            ? new UnsafeArray<ulong>(resourceCount * WordCount, allocationHandle, AllocationOption.Clear)
            : default;
        _afterAllUsesMasks = WordCount > 0 && resourceCount > 0
            ? new UnsafeArray<ulong>(resourceCount * WordCount, allocationHandle, AllocationOption.Clear)
            : default;
        _firstUseScheduleIndices = resourceCount > 0
            ? new UnsafeArray<int>(resourceCount, allocationHandle)
            : default;
        _lastUseScheduleIndices = resourceCount > 0
            ? new UnsafeArray<int>(resourceCount, allocationHandle)
            : default;

        if (resourceCount > 0)
        {
            _firstUseScheduleIndices.AsSpan().Fill(-1);
            _lastUseScheduleIndices.AsSpan().Fill(-1);
        }
    }

    public static RenderGraphResourceOrdering Build(RenderGraphResourceRegistry registry, ReadOnlySpan<int> scheduleIndexByPassIndex, in RenderGraphBitMatrix reachability, int passCount, AllocationHandle allocationHandle)
    {
        var ordering = new RenderGraphResourceOrdering(registry.ResourceCount, passCount, allocationHandle);
        if (ordering.ResourceCount == 0 || passCount == 0)
        {
            return ordering;
        }

        for (var resourceIndex = 0; resourceIndex < ordering.ResourceCount; resourceIndex++)
        {
            foreach (var producerPassIndex in registry.GetProducerPasses(resourceIndex))
            {
                ordering.AddUse(resourceIndex, scheduleIndexByPassIndex[producerPassIndex]);
            }

            foreach (var consumerPassIndex in registry.GetConsumerPasses(resourceIndex))
            {
                ordering.AddUse(resourceIndex, scheduleIndexByPassIndex[consumerPassIndex]);
            }
        }

        ordering.BuildAfterAllUsesMasks(in reachability);
        return ordering;
    }

    public readonly bool HasUses(int resourceIndex)
    {
        return _firstUseScheduleIndices[resourceIndex] >= 0;
    }

    public readonly int GetFirstUseScheduleIndex(int resourceIndex)
    {
        return _firstUseScheduleIndices[resourceIndex];
    }

    public readonly int GetLastUseScheduleIndex(int resourceIndex)
    {
        return _lastUseScheduleIndices[resourceIndex];
    }

    public readonly bool IsFirstUse(int resourceIndex, int scheduleIndex)
    {
        return _firstUseScheduleIndices[resourceIndex] == scheduleIndex;
    }

    public readonly bool AllUsesHappenBefore(int beforeResourceIndex, int afterResourceIndex)
    {
        if (!HasUses(beforeResourceIndex) || !HasUses(afterResourceIndex))
        {
            return false;
        }

        var beforeOffset = beforeResourceIndex * WordCount;
        var afterOffset = afterResourceIndex * WordCount;
        for (var wordIndex = 0; wordIndex < WordCount; wordIndex++)
        {
            var afterUses = _useMasks[afterOffset + wordIndex];
            var afterAllBeforeUses = _afterAllUsesMasks[beforeOffset + wordIndex];
            if ((afterUses & ~afterAllBeforeUses) != 0)
            {
                return false;
            }
        }

        return true;
    }

    public readonly bool CanAlias(int resourceA, int resourceB)
    {
        return AllUsesHappenBefore(resourceA, resourceB)
            || AllUsesHappenBefore(resourceB, resourceA);
    }

    public readonly ReadOnlySpan<int> GetFirstUseScheduleIndices()
    {
        return _firstUseScheduleIndices.IsCreated ? _firstUseScheduleIndices.AsSpan() : ReadOnlySpan<int>.Empty;
    }

    public readonly ReadOnlySpan<int> GetLastUseScheduleIndices()
    {
        return _lastUseScheduleIndices.IsCreated ? _lastUseScheduleIndices.AsSpan() : ReadOnlySpan<int>.Empty;
    }

    private void AddUse(int resourceIndex, int scheduleIndex)
    {
        if (scheduleIndex < 0)
        {
            return;
        }

        var maskIndex = (resourceIndex * WordCount) + (scheduleIndex >> 6);
        _useMasks[maskIndex] |= 1UL << (scheduleIndex & 63);

        ref var firstUse = ref _firstUseScheduleIndices[resourceIndex];
        ref var lastUse = ref _lastUseScheduleIndices[resourceIndex];
        firstUse = firstUse < 0 ? scheduleIndex : Math.Min(firstUse, scheduleIndex);
        lastUse = Math.Max(lastUse, scheduleIndex);
    }

    private readonly void BuildAfterAllUsesMasks(in RenderGraphBitMatrix reachability)
    {
        var afterAllUsesSpan = _afterAllUsesMasks.AsSpan();
        var tailBits = PassCount & 63;
        var lastWordMask = tailBits != 0 ? (1UL << tailBits) - 1UL : ulong.MaxValue;

        for (var resourceIndex = 0; resourceIndex < ResourceCount; resourceIndex++)
        {
            if (!HasUses(resourceIndex))
            {
                continue;
            }

            var resourceOffset = resourceIndex * WordCount;
            var afterRow = afterAllUsesSpan.Slice(resourceOffset, WordCount);
            for (var w = 0; w < WordCount - 1; w++)
            {
                afterRow[w] = ulong.MaxValue;
            }
            afterRow[WordCount - 1] = lastWordMask;

            for (var w = 0; w < WordCount; w++)
            {
                var useWord = _useMasks[resourceOffset + w];
                while (useWord != 0)
                {
                    var bit = BitOperations.TrailingZeroCount(useWord);
                    useWord &= useWord - 1;
                    var source = (w << 6) + bit;
                    reachability.AndRowInto(afterRow, source);
                }
            }
        }
    }

    public void Dispose()
    {
        if (_lastUseScheduleIndices.IsCreated)
        {
            _lastUseScheduleIndices.Dispose();
        }

        if (_firstUseScheduleIndices.IsCreated)
        {
            _firstUseScheduleIndices.Dispose();
        }

        if (_afterAllUsesMasks.IsCreated)
        {
            _afterAllUsesMasks.Dispose();
        }

        if (_useMasks.IsCreated)
        {
            _useMasks.Dispose();
        }
    }
}
