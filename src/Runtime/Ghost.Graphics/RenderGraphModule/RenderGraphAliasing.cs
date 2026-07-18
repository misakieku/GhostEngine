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

internal sealed class ResourceHeap : IDisposable
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

    public void Reset()
    {
        _blocks.Clear();
        _blocks.Add(new MemoryInfo(0, Size));
    }

    /// <summary>
    /// Attempts to allocate a block of the requested size with proper alignment.
    /// Uses best-fit algorithm with lifetime-aware allocation.
    /// </summary>
    public (bool success, ulong offset, MemoryInfo block) TryAllocate(
        ulong requestedSize,
        int firstUsePass,
        int lastUsePass,
        int logicalResourceIndex,
        ulong alignment = DEFAULT_ALIGNMENT)
    {
        var alignedSize = AlignUp(requestedSize, alignment);

        var bestFitIndex = -1;
        ulong bestFitOffset = 0;
        var smallestWaste = ulong.MaxValue;

        for (var i = 0; i < _blocks.Count; i++)
        {
            ref var block = ref _blocks[i];

            var alignedOffset = AlignUp(block.offset, alignment);
            var endOffset = alignedOffset + alignedSize;

            if (endOffset <= block.offset + block.size)
            {
                var canUseOffset = CanPlaceAtOffset(alignedOffset, alignedSize, firstUsePass, lastUsePass);

                if (canUseOffset)
                {
                    var waste = block.size - alignedSize;

                    if (waste < smallestWaste)
                    {
                        smallestWaste = waste;
                        bestFitIndex = i;
                        bestFitOffset = alignedOffset;
                    }
                }
            }
        }

        if (bestFitIndex == -1)
        {
            return (false, 0, default);
        }

        ref var bestFit = ref _blocks[bestFitIndex];

        if (bestFit.isFree)
        {
            var remainingSize = (bestFit.offset + bestFit.size) - (bestFitOffset + alignedSize);

            bestFit.offset = bestFitOffset;
            bestFit.size = alignedSize;
            bestFit.isFree = false;
            bestFit.firstUsePass = firstUsePass;
            bestFit.lastUsePass = lastUsePass;
            bestFit.logicalResourceIndex = logicalResourceIndex;

            if (remainingSize > 0)
            {
                var newBlock = new MemoryInfo(bestFitOffset + alignedSize, remainingSize);
                _blocks.Insert(bestFitIndex + 1, newBlock);
            }
        }
        else
        {
            var aliasedBlock = new MemoryInfo(bestFitOffset, alignedSize)
            {
                isFree = false,
                firstUsePass = firstUsePass,
                lastUsePass = lastUsePass,
                logicalResourceIndex = logicalResourceIndex
            };

            var insertIndex = 0;
            for (var i = 0; i < _blocks.Count; i++)
            {
                if (_blocks[i].offset > bestFitOffset)
                {
                    break;
                }
                insertIndex = i + 1;
            }
            _blocks.Insert(insertIndex, aliasedBlock);
            bestFit = ref _blocks[insertIndex];
        }

        return (true, bestFitOffset, bestFit);
    }

    private bool CanPlaceAtOffset(ulong offset, ulong size, int firstUsePass, int lastUsePass)
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

    public ulong GetPeakUsage()
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
        throw new NotImplementedException();
    }
}

internal struct PlacedResource : IDisposable
{
    public int index;
    public RenderGraphResourceType type;
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

    public void StoreToCache(Dictionary<int, int> outLogicalToPlaced, List<PlacedResourceData> outPlacedData)
    {
        outLogicalToPlaced.Clear();
        foreach (var kvp in logicalToPlaced)
        {
            outLogicalToPlaced[kvp.Key] = kvp.Value;
        }

        outPlacedData.Clear();
        for (var i = 0; i < placedResources.Count; i++)
        {
            var placed = placedResources[i];
            outPlacedData.Add(new PlacedResourceData
            {
                index = placed.index,
                type = placed.type,
                heapOffset = placed.heapOffset,
                sizeInBytes = placed.sizeInBytes,
                firstUsePass = placed.firstUsePass,
                lastUsePass = placed.lastUsePass
            });
        }
    }

    public void Dispose()
    {
        placedResources.Dispose();
        logicalToPlaced.Dispose();
    }
}

internal static class RenderGraphAliasingBuilder
{
    private const ulong _DEFAULT_TEXTURE_ALIGNMENT = 65536; // 64KB
    private const ulong _DEFAULT_BUFFER_ALIGNMENT = 65536;  // 64KB

    private static ulong GetResourceSize(RenderGraphResource resource, IResourceAllocator allocator)
    {
        if (resource.type == RenderGraphResourceType.Texture)
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
        using var logicalResources = new UnsafeList<(int index, RenderGraphResource resource)>(registry.ResourceCount, scope.AllocationHandle);

        try
        {
            for (var i = 0; i < registry.ResourceCount; i++)
            {
                var resource = registry.GetResourceByIndex(i);
                if (!resource.isImported) // Don't alias imported resources
                {
                    logicalResources.Add((resource.index, resource));
                }
            }

            // Sort by size descending
            logicalResources.AsSpan().Sort((a, b) =>
            {
                var sizeA = GetResourceSize(a.resource, allocator);
                var sizeB = GetResourceSize(b.resource, allocator);
                return sizeB.CompareTo(sizeA);
            });

            var plan = new AliasingPlan(allocationHandle);

            // Simulate allocation to find peak memory usage
            using var simulationHeap = new ResourceHeap(0, ulong.MaxValue, scope.AllocationHandle);
            foreach (var (logicalIndex, logicalResource) in logicalResources)
            {
                var size = GetResourceSize(logicalResource, allocator);
                var alignment = logicalResource.type == RenderGraphResourceType.Texture
                    ? _DEFAULT_TEXTURE_ALIGNMENT
                    : _DEFAULT_BUFFER_ALIGNMENT;

                var (success, offset, memInfo) = simulationHeap.TryAllocate(
                    size,
                    logicalResource.firstUsePass,
                    logicalResource.lastUsePass,
                    logicalIndex,
                    alignment);

                Logger.DebugAssert(success, "Simulation allocation failed - heap should be unlimited in size");

                var assignedPlaced = new PlacedResource(allocationHandle)
                {
                    index = plan.placedResources.Count,
                    type = logicalResource.type,
                    heapOffset = offset,
                    sizeInBytes = size,
                    firstUsePass = logicalResource.firstUsePass,
                    lastUsePass = logicalResource.lastUsePass,
                    memoryInfo = memInfo
                };
                assignedPlaced.aliasedLogicalResources.Add(logicalIndex);

                plan.placedResources.Add(assignedPlaced);
                plan.logicalToPlaced[logicalIndex] = assignedPlaced.index;
            }

            var peakMemoryUsage = AlignUp(simulationHeap.GetPeakUsage(), _DEFAULT_TEXTURE_ALIGNMENT);
            plan.totalHeapSize = peakMemoryUsage;

            // Real allocation
            // var realHeap = new ResourceHeap(0, peakMemoryUsage);
            //
            // foreach (var (logicalIndex, logicalResource) in logicalResources)
            // {
            //     var size = GetResourceSize(logicalResource, allocator);
            //     var alignment = logicalResource.type == RenderGraphResourceType.Texture
            //         ? _DEFAULT_TEXTURE_ALIGNMENT
            //         : _DEFAULT_BUFFER_ALIGNMENT;
            //
            //     var (success, offset, block) = realHeap.TryAllocate(
            //         size,
            //         logicalResource.firstUsePass,
            //         logicalResource.lastUsePass,
            //         logicalIndex,
            //         alignment);
            //
            //     if (!success)
            //     {
            //         throw new InvalidOperationException("Real allocation failed - this should match simulation");
            //     }
            //
            //     var assignedPlaced = pool.Rent<PlacedResource>();
            //     assignedPlaced.index = plan.PlacedResources.Count;
            //     assignedPlaced.type = logicalResource.type;
            //     assignedPlaced.heapOffset = offset;
            //     assignedPlaced.sizeInBytes = size;
            //     assignedPlaced.firstUsePass = logicalResource.firstUsePass;
            //     assignedPlaced.lastUsePass = logicalResource.lastUsePass;
            //     assignedPlaced.memoryBlock = block;
            //     assignedPlaced.aliasedLogicalResources.Clear();
            //     assignedPlaced.aliasedLogicalResources.Add(logicalIndex);
            //
            //     plan.PlacedResources.Add(assignedPlaced);
            //     plan.LogicalToPlaced[logicalIndex] = assignedPlaced.index;
            // }

            // Populate aliasedLogicalResources lists
            for (var i = 0; i < plan.placedResources.Count; i++)
            {
                var placed = plan.placedResources[i];

                for (var j = 0; j < plan.placedResources.Count; j++)
                {
                    if (i == j) continue;

                    var other = plan.placedResources[j];
                    if (other.heapOffset == placed.heapOffset)
                    {
                        var otherLogicalIndex = other.aliasedLogicalResources[0];
                        if (!placed.aliasedLogicalResources.AsSpan().Contains(otherLogicalIndex))
                        {
                            placed.aliasedLogicalResources.Add(otherLogicalIndex);
                        }
                    }
                }
            }

            return plan;
        }
        finally
        {
            for (int i = 0; i < logicalResources.Count; i++)
            {
                logicalResources[i].resource.Dispose();
            }
        }
    }

    public static AliasingPlan RestoreFromCache(Dictionary<int, int> logicalToPlaced, List<PlacedResourceData> placedData, AllocationHandle allocationHandle)
    {
        var plan =  new AliasingPlan(allocationHandle);
        foreach (var kvp in logicalToPlaced)
        {
            plan.logicalToPlaced[kvp.Key] = kvp.Value;
        }

        // Restore placed resources
        for (var i = 0; i < placedData.Count; i++)
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
            plan.placedResources.Add(placed);
        }

        return plan;
    }
}
