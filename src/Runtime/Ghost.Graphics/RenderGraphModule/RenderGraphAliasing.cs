using Ghost.Core;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Ghost.Graphics.RenderGraphModule;

internal struct MemoryInfo
{
    public ulong offset;
    public ulong size;
    public bool isFree;
    public int firstUsePass;
    public int lastUsePass;
    public int logicalResourceIndex; // Which logical heap is currently using this block

    public MemoryInfo(ulong offset, ulong size)
    {
        this.offset = offset;
        this.size = size;
        isFree = true;
        firstUsePass = int.MaxValue;
        lastUsePass = -1;
        logicalResourceIndex = -1;
    }
}

internal struct PlacedResource : IDisposable
{
    public int index;
    public RGResourceType type;
    public ulong heapOffset;
    public ulong sizeInBytes;

    // Lifetime tracking
    public int firstUsePass = int.MaxValue;
    public int lastUsePass = -1;

    // Aliasing tracking
    public UnsafeList<int> aliasedLogicalResources;
    public MemoryInfo memoryInfo;

    public PlacedResource(AllocationHandle allocationHandle)
    {
        firstUsePass = int.MaxValue;
        lastUsePass = -1;
        aliasedLogicalResources = new UnsafeList<int>(4, allocationHandle);
    }

    public void UpdateLifetime(int passIndex)
    {
        firstUsePass = Math.Min(firstUsePass, passIndex);
        lastUsePass = Math.Max(lastUsePass, passIndex);
    }

    public void Dispose()
    {
        aliasedLogicalResources.Dispose();
    }
}

internal struct AliasingPlan : IDisposable
{
    public ulong totalHeapSize;
    public UnsafeList<PlacedResource> placedResources;
    public UnsafeArray<int> logicalToPlaced;

    public AliasingPlan(int resourceCount, AllocationHandle allocationHandle)
    {
        placedResources = new UnsafeList<PlacedResource>(32, allocationHandle);
        logicalToPlaced = resourceCount > 0
            ? new UnsafeArray<int>(resourceCount, allocationHandle)
            : default;

        if (logicalToPlaced.IsCreated)
        {
            logicalToPlaced.AsSpan().Fill(-1);
        }
    }

    public readonly int GetPlacedResourceIndex(int logicalIndex)
    {
        return logicalToPlaced.IsCreated && (uint)logicalIndex < (uint)logicalToPlaced.Length
            ? logicalToPlaced[logicalIndex]
            : -1;
    }

    public readonly Result<PlacedResource> GetPlacedResource(int placedIndex)
    {
        return placedIndex >= 0 && placedIndex < placedResources.Count
            ? placedResources[placedIndex]
            : Result.Failure();
    }

    public void StoreToCache(ref UnsafeArray<int> outLogicalToPlaced, ref UnsafeArray<PlacedResourceData> outPlacedData, ref UnsafeArray<int> outAliasedLogicalResources)
    {
        if (logicalToPlaced.IsCreated && logicalToPlaced.Length > 0 && outLogicalToPlaced.IsCreated)
        {
            outLogicalToPlaced.CopyFrom(logicalToPlaced);
        }

        if (placedResources.Count != 0)
        {
            outPlacedData.Clear();
            var aliasedLogicalResourceOffset = 0;
            for (var i = 0; i < placedResources.Count; i++)
            {
                var placed = placedResources[i];
                outPlacedData[i] = new PlacedResourceData
                {
                    index = placed.index,
                    type = placed.type,
                    heapOffset = placed.heapOffset,
                    sizeInBytes = placed.sizeInBytes,
                    firstUsePass = placed.firstUsePass,
                    lastUsePass = placed.lastUsePass,
                    aliasedLogicalResourceOffset = aliasedLogicalResourceOffset,
                    aliasedLogicalResourceCount = placed.aliasedLogicalResources.Count
                };

                for (var aliasIndex = 0; aliasIndex < placed.aliasedLogicalResources.Count; aliasIndex++)
                {
                    outAliasedLogicalResources[aliasedLogicalResourceOffset++] = placed.aliasedLogicalResources[aliasIndex];
                }
            }
        }
    }

    public void Dispose()
    {
        for (var i = 0; i < placedResources.Count; i++)
        {
            placedResources[i].Dispose();
        }

        placedResources.Dispose();
        if (logicalToPlaced.IsCreated)
        {
            logicalToPlaced.Dispose();
        }
    }
}

internal static class RenderGraphAliasingBuilder
{
    private readonly struct LogicalResourceEntry
    {
        public readonly int index;
        public readonly RenderGraphResource resource;
        public readonly ulong size;
        public readonly bool allowAliasing;

        public LogicalResourceEntry(int index, RenderGraphResource resource, ulong size, bool allowAliasing)
        {
            this.index = index;
            this.resource = resource;
            this.size = size;
            this.allowAliasing = allowAliasing;
        }
    }

    private readonly struct PlacedInterval
    {
        public readonly ulong offset;
        public readonly ulong alignedSize;
        public readonly int logicalResourceIndex;
        public readonly bool allowAliasing;

        public ulong End => offset + alignedSize;

        public PlacedInterval(ulong offset, ulong alignedSize, int logicalResourceIndex, bool allowAliasing)
        {
            this.offset = offset;
            this.alignedSize = alignedSize;
            this.logicalResourceIndex = logicalResourceIndex;
            this.allowAliasing = allowAliasing;
        }
    }

    private readonly struct ForbiddenByteRange
    {
        public readonly ulong start;
        public readonly ulong end;

        public ForbiddenByteRange(ulong start, ulong end)
        {
            this.start = start;
            this.end = end;
        }
    }

    private const ulong DEFAULT_TEXTURE_ALIGNMENT = 65536; // 64KB
    private const ulong DEFAULT_BUFFER_ALIGNMENT = 65536;  // 64KB

    private static ulong GetResourceSize(RenderGraphResource resource, IResourceAllocator allocator)
    {
        if (resource.type == RGResourceType.Texture)
        {
            var textureDesc = resource.rgTextureDesc.ToTextureDesc(resource.resolvedWidth, resource.resolvedHeight);
            return allocator.GetSizeInfo(ResourceDesc.Texture(textureDesc)).Size;
        }
        else // Buffer
        {
            return allocator.GetSizeInfo(ResourceDesc.Buffer(resource.bufferDesc)).Size;
        }
    }

    private static ulong AlignUp(ulong value, ulong alignment)
    {
        return (value + alignment - 1) & ~(alignment - 1);
    }

    private static ulong FindFirstFitOffset(ulong alignedSize, ulong alignment, ReadOnlySpan<PlacedInterval> placedIntervals, int logicalResourceIndex, bool allowAliasing,
        in RenderGraphResourceOrdering ordering, ref UnsafeList<ForbiddenByteRange> forbiddenRanges)
    {
        if (alignedSize == 0 || placedIntervals.IsEmpty)
        {
            return 0;
        }

        forbiddenRanges.Clear();
        for (var i = 0; i < placedIntervals.Length; i++)
        {
            ref readonly var other = ref placedIntervals[i];
            if (other.alignedSize == 0)
            {
                continue;
            }

            if (!allowAliasing || !other.allowAliasing || !ordering.CanAlias(logicalResourceIndex, other.logicalResourceIndex))
            {
                forbiddenRanges.Add(new ForbiddenByteRange(other.offset, other.End));
            }
        }

        if (forbiddenRanges.Count == 0)
        {
            return 0;
        }

        forbiddenRanges.AsSpan().Sort(static (a, b) =>
        {
            var cmp = a.start.CompareTo(b.start);
            return cmp != 0 ? cmp : a.end.CompareTo(b.end);
        });

        var candidateOffset = 0UL;
        for (var i = 0; i < forbiddenRanges.Count; i++)
        {
            ref readonly var forbidden = ref forbiddenRanges[i];
            candidateOffset = AlignUp(candidateOffset, alignment);
            if (candidateOffset + alignedSize <= forbidden.start)
            {
                return candidateOffset;
            }

            if (candidateOffset < forbidden.end)
            {
                candidateOffset = forbidden.end;
            }
        }

        return AlignUp(candidateOffset, alignment);
    }

    /// <summary>
    /// Builds a memory aliasing plan for all transient resources in the registry.
    /// </summary>
    public static AliasingPlan Build(RenderGraphResourceRegistry registry, IResourceAllocator allocator, RenderGraphResourceOrdering ordering, bool noAliasing,
        AllocationHandle allocationHandle, ReadOnlySpan<CommandQueueType> effectiveQueues = default)
    {
        var plan = new AliasingPlan(registry.ResourceCount, allocationHandle);

        if (noAliasing)
        {
            for (var i = 0; i < registry.ResourceCount; i++)
            {
                var resource = registry.GetResourceByIndex(i);
                if (!resource.isImported && !resource.isExtracted)
                {
                    var size = GetResourceSize(resource, allocator);
                    var placed = new PlacedResource(allocationHandle)
                    {
                        index = plan.placedResources.Count,
                        type = resource.type,
                        heapOffset = plan.totalHeapSize,
                        sizeInBytes = size,
                        firstUsePass = ordering.GetFirstUseScheduleIndex(resource.index),
                        lastUsePass = ordering.GetLastUseScheduleIndex(resource.index)
                    };

                    placed.aliasedLogicalResources.Add(resource.index);
                    plan.placedResources.Add(placed);
                    plan.logicalToPlaced[resource.index] = placed.index;
                    plan.totalHeapSize += size;
                }
            }

            return plan;
        }

        using var scope = AllocationManager.CreateStackScope();
        using var logicalResources = new UnsafeList<LogicalResourceEntry>(registry.ResourceCount, scope.AllocationHandle);

        for (var i = 0; i < registry.ResourceCount; i++)
        {
            var resource = registry.GetResourceByIndex(i);

            // Don't memory-alias imported, extracted, or upload resources
            if (resource.CanBeAliased)
            {
                var size = GetResourceSize(resource, allocator);
                var allowAliasing = true;
                if (resource.type == RGResourceType.Texture && !effectiveQueues.IsEmpty)
                {
                    var firstUse = ordering.GetFirstUseScheduleIndex(resource.index);
                    if (firstUse >= 0 && effectiveQueues[firstUse] == CommandQueueType.Copy)
                    {
                        // Copy queues do not support layout transition / discard aliasing barriers.
                        allowAliasing = false;
                    }
                }

                logicalResources.Add(new LogicalResourceEntry(resource.index, resource, size, allowAliasing));
            }
        }

        // Sort by size descending (with index tie-breaker for deterministic placement)
        logicalResources.AsSpan().Sort(static (x, y) =>
        {
            var cmp = y.size.CompareTo(x.size);
            return cmp != 0 ? cmp : x.index.CompareTo(y.index);
        });

        using var placedIntervals = new UnsafeList<PlacedInterval>(logicalResources.Count, scope.AllocationHandle);
        var forbiddenRanges = new UnsafeList<ForbiddenByteRange>(logicalResources.Count, scope.AllocationHandle);
        var peakExtent = 0UL;

        try
        {
            for (var i = 0; i < logicalResources.Count; i++)
            {
                ref readonly var item = ref logicalResources[i];
                var alignment = item.resource.type == RGResourceType.Texture
                    ? DEFAULT_TEXTURE_ALIGNMENT
                    : DEFAULT_BUFFER_ALIGNMENT;
                var alignedSize = AlignUp(item.size, alignment);

                var firstUseScheduleIndex = ordering.GetFirstUseScheduleIndex(item.index);
                var lastUseScheduleIndex = ordering.GetLastUseScheduleIndex(item.index);
                var offset = FindFirstFitOffset(alignedSize, alignment, placedIntervals.AsSpan(), item.index, item.allowAliasing, in ordering, ref forbiddenRanges);

                var memInfo = new MemoryInfo(offset, alignedSize)
                {
                    isFree = false,
                    firstUsePass = firstUseScheduleIndex,
                    lastUsePass = lastUseScheduleIndex,
                    logicalResourceIndex = item.index
                };

                var assignedPlaced = new PlacedResource(allocationHandle)
                {
                    index = plan.placedResources.Count,
                    type = item.resource.type,
                    heapOffset = offset,
                    sizeInBytes = item.size,
                    firstUsePass = firstUseScheduleIndex,
                    lastUsePass = lastUseScheduleIndex,
                    memoryInfo = memInfo
                };

                assignedPlaced.aliasedLogicalResources.Add(item.index);

                placedIntervals.Add(new PlacedInterval(offset, alignedSize, item.index, item.allowAliasing));
                peakExtent = Math.Max(peakExtent, offset + alignedSize);
                plan.placedResources.Add(assignedPlaced);
                plan.logicalToPlaced[item.index] = assignedPlaced.index;
            }
        }
        finally
        {
            forbiddenRanges.Dispose();
        }

        plan.totalHeapSize = AlignUp(peakExtent, DEFAULT_TEXTURE_ALIGNMENT);

        // Populate aliasedLogicalResources from overlapping placed byte intervals
        var placedSpan = placedIntervals.AsSpan();
        for (var i = 0; i < placedSpan.Length; i++)
        {
            ref readonly var intervalA = ref placedSpan[i];
            if (intervalA.alignedSize == 0)
            {
                continue;
            }

            ref var placedA = ref plan.placedResources[i];
            for (var j = i + 1; j < placedSpan.Length; j++)
            {
                ref readonly var intervalB = ref placedSpan[j];
                if (intervalB.alignedSize == 0)
                {
                    continue;
                }

                if (intervalA.offset < intervalB.End && intervalB.offset < intervalA.End)
                {
                    placedA.aliasedLogicalResources.Add(intervalB.logicalResourceIndex);
                    plan.placedResources[j].aliasedLogicalResources.Add(intervalA.logicalResourceIndex);
                }
            }
        }

        return plan;
    }
}
