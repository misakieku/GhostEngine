using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Runtime.InteropServices;

namespace Ghost.Graphics.RenderGraphModule;

internal struct MemoryBlock
{
    public ulong offset;
    public ulong size;
    public bool isFree;
    public int firstUsePass;
    public int lastUsePass;
    public int logicalResourceIndex; // Which logical heap is currently using this block

    public MemoryBlock(ulong offset, ulong size)
    {
        this.offset = offset;
        this.size = size;
        isFree = true;
        firstUsePass = int.MaxValue;
        lastUsePass = -1;
        logicalResourceIndex = -1;
    }

    public void Reset()
    {
        isFree = true;
        firstUsePass = int.MaxValue;
        lastUsePass = -1;
        logicalResourceIndex = -1;
    }
}

internal sealed class ResourceHeap
{
    public int index;
    public ulong size;
    private readonly List<MemoryBlock> _blocks = new(32);

    public const ulong DEFAULT_ALIGNMENT = 65536; // 64KB

    public ResourceHeap(int index, ulong initialSize = 16 * 1024 * 1024) // 16MB default
    {
        this.index = index;
        this.size = initialSize;

        // Initially one large free block
        _blocks.Add(new MemoryBlock(0, initialSize));
    }

    public void Reset()
    {
        _blocks.Clear();
        _blocks.Add(new MemoryBlock(0, size));
    }

    /// <summary>
    /// Attempts to allocate a block of the requested size with proper alignment.
    /// Uses best-fit algorithm with lifetime-aware allocation.
    /// </summary>
    public (bool success, ulong offset, MemoryBlock block) TryAllocate(
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

        var blockSpan = CollectionsMarshal.AsSpan(_blocks);
        for (var i = 0; i < blockSpan.Length; i++)
        {
            ref var block = ref blockSpan[i];

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

        ref var bestFit = ref CollectionsMarshal.AsSpan(_blocks)[bestFitIndex];

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
                var newBlock = new MemoryBlock(bestFitOffset + alignedSize, remainingSize);
                _blocks.Insert(bestFitIndex + 1, newBlock);
            }
        }
        else
        {
            var aliasedBlock = new MemoryBlock(bestFitOffset, alignedSize)
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
            bestFit = ref CollectionsMarshal.AsSpan(_blocks)[insertIndex];
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
}

internal sealed class PlacedResource
{
    public int index;
    public RenderGraphResourceType type;
    public ulong heapOffset;
    public ulong sizeInBytes;

    // Lifetime tracking
    public int firstUsePass = int.MaxValue;
    public int lastUsePass = -1;

    // Aliasing tracking
    public readonly List<int> aliasedLogicalResources = new(4);
    public MemoryBlock memoryBlock;

    public void Reset()
    {
        index = -1;
        type = RenderGraphResourceType.Texture;
        heapOffset = 0;
        sizeInBytes = 0;
        firstUsePass = int.MaxValue;
        lastUsePass = -1;
        aliasedLogicalResources.Clear();
        memoryBlock = default;
    }

    public void UpdateLifetime(int passIndex)
    {
        firstUsePass = Math.Min(firstUsePass, passIndex);
        lastUsePass = Math.Max(lastUsePass, passIndex);
    }
}

internal sealed class AliasingPlan
{
    public ulong TotalHeapSize;
    public readonly List<PlacedResource> PlacedResources = new(32);
    public readonly Dictionary<int, int> LogicalToPlaced = new(64);

    public int GetPlacedResourceIndex(int logicalIndex)
    {
        return LogicalToPlaced.TryGetValue(logicalIndex, out var placedIndex) ? placedIndex : -1;
    }

    public PlacedResource? GetPlacedResource(int placedIndex)
    {
        return placedIndex >= 0 && placedIndex < PlacedResources.Count
            ? PlacedResources[placedIndex]
            : null;
    }

    public void Clear(RenderGraphObjectPool pool)
    {
        for (var i = 0; i < PlacedResources.Count; i++)
        {
            pool.Return(PlacedResources[i]);
        }
        PlacedResources.Clear();
        LogicalToPlaced.Clear();
        TotalHeapSize = 0;
    }

    public void StoreToCache(Dictionary<int, int> outLogicalToPlaced, List<PlacedResourceData> outPlacedData)
    {
        outLogicalToPlaced.Clear();
        foreach (var kvp in LogicalToPlaced)
        {
            outLogicalToPlaced[kvp.Key] = kvp.Value;
        }

        outPlacedData.Clear();
        for (var i = 0; i < PlacedResources.Count; i++)
        {
            var placed = PlacedResources[i];
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
    public static void Build(AliasingPlan plan, RenderGraphResourceRegistry registry, IResourceAllocator allocator, RenderGraphObjectPool pool)
    {
        using var scope = AllocationManager.CreateStackScope();

        // Build list of all logical resources with their lifetimes
        using var logicalResources = new UnsafeList<(int index, RenderGraphResource resource)>(registry.ResourceCount, scope.AllocationHandle);

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

        // Simulate allocation to find peak memory usage
        var simulationHeap = new ResourceHeap(0, ulong.MaxValue);
        foreach (var (logicalIndex, logicalResource) in logicalResources)
        {
            var size = GetResourceSize(logicalResource, allocator);
            var alignment = logicalResource.type == RenderGraphResourceType.Texture
                ? _DEFAULT_TEXTURE_ALIGNMENT
                : _DEFAULT_BUFFER_ALIGNMENT;

            var (success, offset, block) = simulationHeap.TryAllocate(
                size,
                logicalResource.firstUsePass,
                logicalResource.lastUsePass,
                logicalIndex,
                alignment);

            Logger.DebugAssert(success, "Simulation allocation failed - heap should be unlimited in size");

            var assignedPlaced = pool.Rent<PlacedResource>();
            assignedPlaced.index = plan.PlacedResources.Count;
            assignedPlaced.type = logicalResource.type;
            assignedPlaced.heapOffset = offset;
            assignedPlaced.sizeInBytes = size;
            assignedPlaced.firstUsePass = logicalResource.firstUsePass;
            assignedPlaced.lastUsePass = logicalResource.lastUsePass;
            assignedPlaced.memoryBlock = block;
            assignedPlaced.aliasedLogicalResources.Clear();
            assignedPlaced.aliasedLogicalResources.Add(logicalIndex);

            plan.PlacedResources.Add(assignedPlaced);
            plan.LogicalToPlaced[logicalIndex] = assignedPlaced.index;
        }

        var peakMemoryUsage = AlignUp(simulationHeap.GetPeakUsage(), _DEFAULT_TEXTURE_ALIGNMENT);
        plan.TotalHeapSize = peakMemoryUsage;

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
        for (var i = 0; i < plan.PlacedResources.Count; i++)
        {
            var placed = plan.PlacedResources[i];

            for (var j = 0; j < plan.PlacedResources.Count; j++)
            {
                if (i == j) continue;

                var other = plan.PlacedResources[j];
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
    }

    public static void RestoreFromCache(AliasingPlan plan, Dictionary<int, int> logicalToPlaced, List<PlacedResourceData> placedData, RenderGraphObjectPool pool)
    {
        plan.LogicalToPlaced.Clear();
        foreach (var kvp in logicalToPlaced)
        {
            plan.LogicalToPlaced[kvp.Key] = kvp.Value;
        }

        // Restore placed resources
        for (var i = 0; i < placedData.Count; i++)
        {
            var placed = pool.Rent<PlacedResource>();

            var data = placedData[i];
            placed.index = data.index;
            placed.type = data.type;
            placed.heapOffset = data.heapOffset;
            placed.sizeInBytes = data.sizeInBytes;
            placed.firstUsePass = data.firstUsePass;
            placed.lastUsePass = data.lastUsePass;
            placed.aliasedLogicalResources.Clear();

            plan.PlacedResources.Add(placed);
        }
    }
}
