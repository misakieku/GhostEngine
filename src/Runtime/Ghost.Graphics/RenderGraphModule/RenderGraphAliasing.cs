using Ghost.Core.Utilities;
using Ghost.Graphics.RHI;
using System.Diagnostics;
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

        // Find the best fit block that doesn't overlap with lifetime
        // TODO: Is first-fit better? Since we already sort by size beforehand.
        var blockSpan = CollectionsMarshal.AsSpan(_blocks);
        for (var i = 0; i < blockSpan.Length; i++)
        {
            ref var block = ref blockSpan[i];

            // Try to find space within this block
            var alignedOffset = AlignUp(block.offset, alignment);
            var endOffset = alignedOffset + alignedSize;

            if (endOffset <= block.offset + block.size)
            {
                // Check if this offset range conflicts with ANY existing allocations
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

        // If the block is free, we need to split it
        if (bestFit.isFree)
        {
            var remainingSize = (bestFit.offset + bestFit.size) - (bestFitOffset + alignedSize);

            // Update the current block to be allocated
            bestFit.offset = bestFitOffset;
            bestFit.size = alignedSize;
            bestFit.isFree = false;
            bestFit.firstUsePass = firstUsePass;
            bestFit.lastUsePass = lastUsePass;
            bestFit.logicalResourceIndex = logicalResourceIndex;

            // Create a new free block for the remaining space if there is any
            if (remainingSize > 0)
            {
                var newBlock = new MemoryBlock(bestFitOffset + alignedSize, remainingSize);
                _blocks.Insert(bestFitIndex + 1, newBlock);
            }
        }
        else
        {
            // Block is already allocated but lifetime doesn't overlap, we can alias it
            // Create a new aliased block at the same location
            var aliasedBlock = new MemoryBlock(bestFitOffset, alignedSize)
            {
                isFree = false,
                firstUsePass = firstUsePass,
                lastUsePass = lastUsePass,
                logicalResourceIndex = logicalResourceIndex
            };

            // Insert in sorted order by offset
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
            // Update bestFit to point to the newly inserted block
            bestFit = ref CollectionsMarshal.AsSpan(_blocks)[insertIndex];
        }

        return (true, bestFitOffset, bestFit);
    }

    private bool CanPlaceAtOffset(ulong offset, ulong size, int firstUsePass, int lastUsePass)
    {
        var endOffset = offset + size;

        foreach (var block in _blocks)
        {
            // Skip free blocks - they don't have lifetime constraints
            if (block.isFree)
            {
                continue;
            }

            // Check if this block's memory range overlaps with our target range
            var blockEnd = block.offset + block.size;
            var memoryOverlap = !(offset >= blockEnd || endOffset <= block.offset);

            if (memoryOverlap)
            {
                // Memory ranges overlap, check if lifetimes also overlap
                var lifetimeOverlap = !(firstUsePass > block.lastUsePass || lastUsePass < block.firstUsePass);

                if (lifetimeOverlap)
                {
                    // Both memory AND lifetime overlap - cannot place here!
                    return false;
                }
            }
        }

        return true;
    }

    public ulong GetTotalAllocatedWithoutAliasing()
    {
        var total = 0ul;
        foreach (var block in _blocks)
        {
            if (!block.isFree)
            {
                total += block.size;
            }
        }

        return total;
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

internal sealed class ResourceAliasingManager
{
    private readonly IResourceAllocator _allocator;
    private readonly RenderGraphObjectPool _pool;

    private readonly ResourceHeap _heap;
    private readonly List<PlacedResource> _placedResources;
    // Mapping from logical heap index to placed heap index
    private readonly Dictionary<int, int> _logicalToPlaced;

    private const ulong _DEFAULT_TEXTURE_ALIGNMENT = 65536; // 64KB
    private const ulong _DEFAULT_BUFFER_ALIGNMENT = 65536;  // 64KB

    public ResourceHeap Heap => _heap;

    private ulong GetResourceSize(RenderGraphResource resource)
    {
        if (resource.type == RenderGraphResourceType.Texture)
        {
            var textureDesc = resource.rgTextureDesc.ToTextureDesc(resource.resolvedWidth, resource.resolvedHeight);
            return _allocator.GetSizeInfo(ResourceDesc.Texture(textureDesc)).Size;
        }
        else // Buffer
        {
            return _allocator.GetSizeInfo(ResourceDesc.Buffer(resource.bufferDesc)).Size;
        }
    }

    public ResourceAliasingManager(IResourceAllocator allocator, RenderGraphObjectPool pool)
    {
        _allocator = allocator;
        _pool = pool;

        _heap = new ResourceHeap(0);
        _placedResources = new List<PlacedResource>(32);
        _logicalToPlaced = new Dictionary<int, int>(64);
    }

    public void Clear()
    {
        for (var i = 0; i < _placedResources.Count; i++)
        {
            _pool.Return(_placedResources[i]);
        }

        _placedResources.Clear();
        _logicalToPlaced.Clear();

        _heap.Reset();
    }

    public void AssignPhysicalResources(RenderGraphResourceRegistry registry, int passCount)
    {
        // Build list of all logical resources (both textures and buffers) with their lifetimes
        var logicalResources = ListPool<(int index, RenderGraphResource resource)>.Rent();

        // Iterate through all resources in unified list
        for (var i = 0; i < registry.ResourceCount; i++)
        {
            var resource = registry.GetResourceByIndex(i);
            if (!resource.isImported) // Don't alias imported resources
            {
                logicalResources.Add((resource.index, resource));
            }
        }

        // Sort by size descending (larger resources first for better packing)
        logicalResources.Sort((a, b) =>
        {
            var sizeA = GetResourceSize(a.resource);
            var sizeB = GetResourceSize(b.resource);
            return sizeB.CompareTo(sizeA); // Descending
        });

        // NOTE: We assume we are at least D3D12 heap tier 2 (the engine won't even start if the hardware does not supports)
        // so buffers and textures can alias as long as lifetimes don't overlap.


        // TODO: Handle non-aliased resources like history buffers that need to persist across frames.
        // They will be placed in the same heap but we can mark them as non-aliased and skip them when looking for aliasing candidates.
        // We do not place them in a separate heap because our buffers are cached and reused across frames as long as the graph topology doesn't change.

        // ===== PASS 1: Simulate allocation to determine peak memory usage =====

        var simulationHeap = new ResourceHeap(0, ulong.MaxValue); // Unlimited size for simulation
        foreach (var (logicalIndex, logicalResource) in logicalResources)
        {
            var size = GetResourceSize(logicalResource);
            var alignment = logicalResource.type == RenderGraphResourceType.Texture
                ? _DEFAULT_TEXTURE_ALIGNMENT
                : _DEFAULT_BUFFER_ALIGNMENT;

            var (success, offset, block) = simulationHeap.TryAllocate(
                size,
                logicalResource.firstUsePass,
                logicalResource.lastUsePass,
                logicalIndex,
                alignment);

            Debug.Assert(success, "Simulation allocation failed - heap should be unlimited in size");
        }

        // Get peak usage from simulation
        var peakMemoryUsage = simulationHeap.GetPeakUsage();

        // Align peak usage to 64KB (D3D12 requirement)
        peakMemoryUsage = AlignUp(peakMemoryUsage, _DEFAULT_TEXTURE_ALIGNMENT);

        // ===== PASS 2: Create a single heap of the peak size and do the real allocation =====

        _heap.size = peakMemoryUsage;
        _heap.Reset();

        // Allocate each logical heap in the heap
        foreach (var (logicalIndex, logicalResource) in logicalResources)
        {
            // TODO: Currently we are recalculating the aliasing candidates in the real allocation pass.
            // We can optimize this by caching the candidates from the simulation pass since the heap layout should be the same.
            var size = GetResourceSize(logicalResource);
            var alignment = logicalResource.type == RenderGraphResourceType.Texture
                ? _DEFAULT_TEXTURE_ALIGNMENT
                : _DEFAULT_BUFFER_ALIGNMENT;

            var (success, offset, block) = _heap.TryAllocate(
                size,
                logicalResource.firstUsePass,
                logicalResource.lastUsePass,
                logicalIndex,
                alignment);

            if (!success)
            {
                throw new InvalidOperationException("Real allocation failed - this should match simulation");
            }

            var assignedOffset = offset;
            var assignedBlock = block;

            var assignedPlaced = _pool.Rent<PlacedResource>();
            assignedPlaced.index = _placedResources.Count;
            assignedPlaced.type = logicalResource.type;
            assignedPlaced.heapOffset = assignedOffset;
            assignedPlaced.sizeInBytes = size;
            assignedPlaced.firstUsePass = logicalResource.firstUsePass;
            assignedPlaced.lastUsePass = logicalResource.lastUsePass;
            assignedPlaced.memoryBlock = assignedBlock;
            assignedPlaced.aliasedLogicalResources.Clear();
            assignedPlaced.aliasedLogicalResources.Add(logicalIndex);

            _placedResources.Add(assignedPlaced);
            _logicalToPlaced[logicalIndex] = assignedPlaced.index;
        }

        // Second pass: Populate aliasedLogicalResources lists
        // For each placed heap, find all OTHER placed resources at the same offset
        for (var i = 0; i < _placedResources.Count; i++)
        {
            var placed = _placedResources[i];

            // Find all logical resources that share the same heap location
            for (var j = 0; j < _placedResources.Count; j++)
            {
                if (i == j)
                {
                    continue; // Skip self
                }

                var other = _placedResources[j];

                // Check if they're at the same offset
                if (other.heapOffset == placed.heapOffset)
                {
                    // Add the other's logical heap to this one's aliased list
                    var otherLogicalIndex = other.aliasedLogicalResources[0]; // Each has exactly one at this point
                    if (!placed.aliasedLogicalResources.Contains(otherLogicalIndex))
                    {
                        placed.aliasedLogicalResources.Add(otherLogicalIndex);
                    }
                }
            }
        }

        ListPool<(int index, RenderGraphResource resource)>.Return(logicalResources);
    }

    public int GetPlacedResourceIndex(int logicalIndex)
    {
        return _logicalToPlaced.TryGetValue(logicalIndex, out var placedIndex) ? placedIndex : -1;
    }

    public PlacedResource? GetPlacedResource(int placedIndex)
    {
        return placedIndex >= 0 && placedIndex < _placedResources.Count
            ? _placedResources[placedIndex]
            : null;
    }

    private static ulong AlignUp(ulong value, ulong alignment)
    {
        return (value + alignment - 1) & ~(alignment - 1);
    }

    public void RestoreFromCache(Dictionary<int, int> logicalToPlaced, List<PlacedResourceData> placedData)
    {
        _logicalToPlaced.Clear();
        foreach (var kvp in logicalToPlaced)
        {
            _logicalToPlaced[kvp.Key] = kvp.Value;
        }

        // Restore placed resources
        for (var i = 0; i < placedData.Count; i++)
        {
            var placed = _pool.Rent<PlacedResource>();

            var data = placedData[i];
            placed.index = data.index;
            placed.type = data.type;
            placed.heapOffset = data.heapOffset;
            placed.sizeInBytes = data.sizeInBytes;
            placed.firstUsePass = data.firstUsePass;
            placed.lastUsePass = data.lastUsePass;

            placed.aliasedLogicalResources.Clear();

            _placedResources.Add(placed);
        }
    }

    public void StoreToCache(Dictionary<int, int> outLogicalToPlaced, List<PlacedResourceData> outPlacedData)
    {
        outLogicalToPlaced.Clear();
        foreach (var kvp in _logicalToPlaced)
        {
            outLogicalToPlaced[kvp.Key] = kvp.Value;
        }

        outPlacedData.Clear();
        for (var i = 0; i < _placedResources.Count; i++)
        {
            var placed = _placedResources[i];
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
