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
    public UnsafeHashMap<int, int> logicalToPlaced;

    public AliasingPlan(AllocationHandle allocationHandle)
    {
        placedResources = new UnsafeList<PlacedResource>(32, allocationHandle);
        logicalToPlaced = new UnsafeHashMap<int, int>(64, allocationHandle);
    }

    public int GetPlacedResourceIndex(int logicalIndex)
    {
        return logicalToPlaced.TryGetValue(logicalIndex, out var placedIndex) ? placedIndex : -1;
    }

    public readonly Result<PlacedResource> GetPlacedResource(int placedIndex)
    {
        return placedIndex >= 0 && placedIndex < placedResources.Count
            ? placedResources[placedIndex]
            : Result.Failure();
    }

    public void StoreToCache(
        ref UnsafeHashMap<int, int> outLogicalToPlaced,
        ref UnsafeArray<PlacedResourceData> outPlacedData,
        ref UnsafeArray<int> outAliasedLogicalResources)
    {
        if (logicalToPlaced.Count != 0)
        {
            outLogicalToPlaced.Clear();
            foreach (var kvp in logicalToPlaced)
            {
                outLogicalToPlaced[kvp.Key] = kvp.Value;
            }
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
        logicalToPlaced.Dispose();
    }
}

internal static class RenderGraphAliasingBuilder
{
    private struct LogicalResourceEntry
    {
        public readonly int index;
        public readonly RenderGraphResource resource;
        public readonly ulong size;

        public LogicalResourceEntry(int index, RenderGraphResource resource, ulong size)
        {
            this.index = index;
            this.resource = resource;
            this.size = size;
        }
    }

    private readonly struct ResourceSizeDescendingComparer : IComparer<LogicalResourceEntry>
    {
        public int Compare(LogicalResourceEntry x, LogicalResourceEntry y)
        {
            return y.size.CompareTo(x.size); // Descending order
        }
    }

    internal struct ResourceHeap : IDisposable
    {
        private UnsafeList<MemoryInfo> _blocks;

        public int Index
        {
            get; set;
        }

        public ulong Size
        {
            get; set;
        }

        public const ulong DEFAULT_ALIGNMENT = 65536; // 64KB

        public ResourceHeap(int index, ulong size, AllocationHandle allocationHandle) // 16MB default
        {
            Index = index;
            Size = size;
            _blocks = new UnsafeList<MemoryInfo>(32, allocationHandle);

            // Initially one large free block
            _blocks.Add(new MemoryInfo(0, size));
        }

        public (bool success, ulong offset, MemoryInfo block) TryAllocate(
            ulong requestedSize,
            int firstUsePass,
            int lastUsePass,
            int logicalResourceIndex,
            ulong alignment = DEFAULT_ALIGNMENT)
        {
            var alignedSize = AlignUp(requestedSize, alignment);

            var fitIndex = -1;
            var fitOffset = 0UL;

            for (var i = 0; i < _blocks.Count; i++)
            {
                ref var block = ref _blocks[i];

                var alignedOffset = AlignUp(block.offset, alignment);
                var endOffset = alignedOffset + alignedSize;

                if (endOffset <= block.offset + block.size)
                {
                    if (CanPlaceAtOffset(alignedOffset, alignedSize, firstUsePass, lastUsePass))
                    {
                        fitIndex = i;
                        fitOffset = alignedOffset;
                        break;
                    }
                }
            }

            if (fitIndex == -1)
            {
                return (false, 0, default);
            }

            ref var targetBlock = ref _blocks[fitIndex];

            if (targetBlock.isFree)
            {
                var remainingSize = (targetBlock.offset + targetBlock.size) - (fitOffset + alignedSize);

                targetBlock.offset = fitOffset;
                targetBlock.size = alignedSize;
                targetBlock.isFree = false;
                targetBlock.firstUsePass = firstUsePass;
                targetBlock.lastUsePass = lastUsePass;
                targetBlock.logicalResourceIndex = logicalResourceIndex;

                if (remainingSize > 0)
                {
                    var newBlock = new MemoryInfo(fitOffset + alignedSize, remainingSize);
                    _blocks.Insert(fitIndex + 1, newBlock);
                }
            }
            else
            {
                var aliasedBlock = new MemoryInfo(fitOffset, alignedSize)
                {
                    isFree = false,
                    firstUsePass = firstUsePass,
                    lastUsePass = lastUsePass,
                    logicalResourceIndex = logicalResourceIndex
                };

                var insertIndex = 0;
                for (var i = 0; i < _blocks.Count; i++)
                {
                    if (_blocks[i].offset > fitOffset)
                    {
                        break;
                    }
                    insertIndex = i + 1;
                }

                _blocks.Insert(insertIndex, aliasedBlock);
                targetBlock = ref _blocks[insertIndex];
            }

            return (true, fitOffset, targetBlock);
        }

        private readonly bool CanPlaceAtOffset(ulong offset, ulong size, int firstUsePass, int lastUsePass)
        {
            var endOffset = offset + size;

            foreach (var block in _blocks)
            {
                if (block.isFree)
                {
                    continue;
                }

                var blockEnd = block.offset + block.size;
                var memoryOverlap = !(offset >= blockEnd || endOffset <= block.offset);

                if (memoryOverlap)
                {
                    var lifetimeOverlap = !(firstUsePass > block.lastUsePass || lastUsePass < block.firstUsePass);

                    if (lifetimeOverlap)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public readonly ulong GetPeakUsage()
        {
            var peak = 0ul;
            foreach (var block in _blocks)
            {
                if (!block.isFree)
                {
                    peak = Math.Max(peak, block.offset + block.size);
                }
            }

            return peak;
        }

        private static ulong AlignUp(ulong value, ulong alignment)
        {
            return (value + alignment - 1) & ~(alignment - 1);
        }

        public void Dispose()
        {
            _blocks.Dispose();
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

    /// <summary>
    /// Builds a memory aliasing plan for all transient resources in the registry.
    /// </summary>
    public static AliasingPlan Build(RenderGraphResourceRegistry registry, IResourceAllocator allocator, AllocationHandle allocationHandle)
    {
        using var scope = AllocationManager.CreateStackScope();
        // Build list of all logical resources with their lifetimes
        using var logicalResources = new UnsafeList<LogicalResourceEntry>(registry.ResourceCount, scope.AllocationHandle);

        for (var i = 0; i < registry.ResourceCount; i++)
        {
            var resource = registry.GetResourceByIndex(i);

            // Don't memory-alias imported OR extracted resources
            if (!resource.isImported && !resource.isExtracted)
            {
                var size = GetResourceSize(resource, allocator);
                logicalResources.Add(new LogicalResourceEntry(resource.index, resource, size));
            }
        }

        // Sort by size descending
        // TODO: Avoid closure.
        logicalResources.AsSpan().Sort(default(ResourceSizeDescendingComparer));

        var plan = new AliasingPlan(allocationHandle);

        // Simulate allocation to find peak memory usage
        using var simulationHeap = new ResourceHeap(0, ulong.MaxValue, scope.AllocationHandle);
        for (var i = 0; i < logicalResources.Count; i++)
        {
            ref readonly var item = ref logicalResources[i];
            var alignment = item.resource.type == RGResourceType.Texture
                ? DEFAULT_TEXTURE_ALIGNMENT
                : DEFAULT_BUFFER_ALIGNMENT;

            var (success, offset, memInfo) = simulationHeap.TryAllocate(
                item.size,
                item.resource.firstUsePass,
                item.resource.lastUsePass,
                item.index,
                alignment);

            Logger.DebugAssert(success, "Simulation allocation failed - heap should be unlimited in size");

            var assignedPlaced = new PlacedResource(allocationHandle)
            {
                index = plan.placedResources.Count,
                type = item.resource.type,
                heapOffset = offset,
                sizeInBytes = item.size,
                firstUsePass = item.resource.firstUsePass,
                lastUsePass = item.resource.lastUsePass,
                memoryInfo = memInfo
            };
            assignedPlaced.aliasedLogicalResources.Add(item.index);

            plan.placedResources.Add(assignedPlaced);
            plan.logicalToPlaced[item.index] = assignedPlaced.index;
        }

        var peakMemoryUsage = AlignUp(simulationHeap.GetPeakUsage(), DEFAULT_TEXTURE_ALIGNMENT);
        plan.totalHeapSize = peakMemoryUsage;

        // Populate aliasedLogicalResources lists
        for (var i = 0; i < plan.placedResources.Count; i++)
        {
            ref var placed = ref plan.placedResources[i];

            for (var j = 0; j < plan.placedResources.Count; j++)
            {
                if (i == j) continue;

                var other = plan.placedResources[j];
                if (other.heapOffset == placed.heapOffset)
                {
                    var otherLogicalIndex = other.aliasedLogicalResources[0];
                    if (!placed.aliasedLogicalResources.Contains(otherLogicalIndex))
                    {
                        placed.aliasedLogicalResources.Add(otherLogicalIndex);
                    }
                }
            }
        }

        return plan;
    }

    public static AliasingPlan RestoreFromCache(
        UnsafeHashMap<int, int> logicalToPlaced,
        ReadOnlySpan<PlacedResourceData> placedData,
        ReadOnlySpan<int> aliasedLogicalResources,
        ulong totalHeapSize,
        AllocationHandle allocationHandle)
    {
        var plan = new AliasingPlan(allocationHandle)
        {
            totalHeapSize = totalHeapSize
        };
        foreach (var kvp in logicalToPlaced)
        {
            plan.logicalToPlaced[kvp.Key] = kvp.Value;
        }

        for (var i = 0; i < placedData.Length; i++)
        {
            var data = placedData[i];
            var placed = new PlacedResource(allocationHandle)
            {
                index = data.index,
                type = data.type,
                heapOffset = data.heapOffset,
                sizeInBytes = data.sizeInBytes,
                firstUsePass = data.firstUsePass,
                lastUsePass = data.lastUsePass
            };

            var aliasEnd = data.aliasedLogicalResourceOffset + data.aliasedLogicalResourceCount;
            for (var aliasIndex = data.aliasedLogicalResourceOffset; aliasIndex < aliasEnd; aliasIndex++)
            {
                placed.aliasedLogicalResources.Add(aliasedLogicalResources[aliasIndex]);
            }

            plan.placedResources.Add(placed);
        }

        return plan;
    }
}
