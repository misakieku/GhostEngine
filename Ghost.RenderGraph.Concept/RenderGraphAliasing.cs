using Ghost.Core.Utilities;
using System.Runtime.InteropServices;

namespace Ghost.RenderGraph.Concept;

/// <summary>
/// Represents a memory block within a heap.
/// </summary>
internal struct MemoryBlock
{
    public ulong offset;
    public ulong size;
    public bool isFree;
    public int firstUsePass;
    public int lastUsePass;
    public int logicalResourceIndex; // Which logical resource is currently using this block

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

/// <summary>
/// Represents a GPU memory heap for placed resources.
/// Supports D3D12-style heap tier 2 (buffers and textures can alias).
/// </summary>
internal sealed class ResourceHeap
{
    public int index;
    public ulong size;
    private readonly List<MemoryBlock> _blocks = new(32);

    // D3D12 heap alignment requirement (64KB for MSAA textures, 4KB for others)
    private const ulong DefaultAlignment = 65536; // 64KB

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
        ulong alignment = DefaultAlignment)
    {
        var alignedSize = AlignUp(requestedSize, alignment);

        var bestFitIndex = -1;
        ulong bestFitOffset = 0;
        var smallestWaste = ulong.MaxValue;

        // Find the best fit block that doesn't overlap with lifetime
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

    /// <summary>
    /// Checks if a resource can be placed at the given offset without lifetime conflicts.
    /// Must check ALL blocks that overlap with this offset range.
    /// </summary>
    private bool CanPlaceAtOffset(ulong offset, ulong size, int firstUsePass, int lastUsePass)
    {
        var endOffset = offset + size;
        
        foreach (var block in _blocks)
        {
            // Skip free blocks - they don't have lifetime constraints
            if (block.isFree)
                continue;
            
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

    /// <summary>
    /// Gets the total memory that would be used if no aliasing occurred.
    /// </summary>
    public ulong GetTotalAllocatedWithoutAliasing()
    {
        ulong total = 0;
        foreach (var block in _blocks)
        {
            if (!block.isFree)
            {
                total += block.size;
            }
        }
        return total;
    }

    /// <summary>
    /// Gets the peak memory usage considering aliasing (max offset + size).
    /// </summary>
    public ulong GetPeakUsage()
    {
        ulong peak = 0;
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

/// <summary>
/// Represents a placed resource within a heap.
/// </summary>
internal sealed class PlacedResource
{
    public int index;
    public RenderGraphResourceType type;
    public int heapIndex;
    public ulong heapOffset;
    public ulong sizeInBytes;

    // Original descriptor
    public TextureDescriptor textureDesc;
    public BufferDescriptor bufferDesc;

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
        heapIndex = -1;
        heapOffset = 0;
        sizeInBytes = 0;
        textureDesc = default;
        bufferDesc = default;
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

/// <summary>
/// Manages physical resource allocation and aliasing using heap-based allocation.
/// Supports D3D12 heap tier 2: buffers and textures can alias as long as lifetimes don't overlap.
/// </summary>
internal sealed class ResourceAliasingManager
{
    private readonly List<ResourceHeap> _heaps = new(4);
    private readonly List<PlacedResource> _placedResources = new(32);
    private readonly RenderGraphObjectPool _pool = new();

    // Mapping from logical resource index to placed resource index
    private readonly Dictionary<int, int> _logicalToPlaced = new(64);

    // D3D12 alignment constants
    private const ulong DefaultTextureAlignment = 65536; // 64KB
    private const ulong DefaultBufferAlignment = 65536;  // 64KB for D3D12

    public void BeginFrame()
    {
        for (var i = 0; i < _placedResources.Count; i++)
        {
            _pool.Return(_placedResources[i]);
        }

        _placedResources.Clear();
        _logicalToPlaced.Clear();

        // Reset heaps
        for (var i = 0; i < _heaps.Count; i++)
        {
            _heaps[i].Reset();
        }
    }

    /// <summary>
    /// Assigns physical resources (placed resources) to logical resources using heap-based allocation.
    /// This is the modern D3D12 approach: check if resource fits in a hole, not if it matches size/format.
    /// Uses a two-pass algorithm:
    /// 1. First pass: Simulate allocation to determine peak memory usage
    /// 2. Second pass: Create a single heap of the peak size and do the real allocation
    /// </summary>
    public void AssignPhysicalResources(RenderGraphResourceRegistry registry, int passCount)
    {
#if DEBUG
        Console.WriteLine("\n=== Heap-Based Resource Aliasing Analysis ===");
        ulong totalLogicalSize = 0;
#endif

        // Build list of all logical resources (both textures and buffers) with their lifetimes
        var logicalResources = ListPool<(int index, RenderGraphResource resource)>.Rent();

        // Iterate through all resources in unified list
        for (var i = 0; i < registry.ResourceCount; i++)
        {
            var resource = registry.GetResourceByIndex(i);
            if (!resource.isImported) // Don't alias imported resources
            {
                logicalResources.Add((resource.index, resource));
#if DEBUG
                var size = resource.type == RenderGraphResourceType.Texture
                    ? CalculateTextureSize(resource.textureDescriptor)
                    : resource.bufferDescriptor.sizeInBytes;
                totalLogicalSize += size;

                var typeName = resource.type == RenderGraphResourceType.Texture ? "Texture" : "Buffer";
                var name = resource.type == RenderGraphResourceType.Texture
                    ? resource.textureDescriptor.name
                    : resource.bufferDescriptor.name;

                Console.WriteLine($"Logical {typeName} {i}: {name}");
                Console.WriteLine($"  Lifetime: Pass {resource.firstUsePass} -> {resource.lastUsePass}");
                Console.WriteLine($"  Size: {size / 1024.0:F2} KB");
#endif
            }
        }

        // Sort by size descending (larger resources first for better packing)
        logicalResources.Sort(static (a, b) =>
        {
            var sizeA = a.resource.type == RenderGraphResourceType.Texture
                ? CalculateTextureSize(a.resource.textureDescriptor)
                : a.resource.bufferDescriptor.sizeInBytes;
            var sizeB = b.resource.type == RenderGraphResourceType.Texture
                ? CalculateTextureSize(b.resource.textureDescriptor)
                : b.resource.bufferDescriptor.sizeInBytes;
            return sizeB.CompareTo(sizeA); // Descending
        });

        // ===== PASS 1: Simulate allocation to determine peak memory usage =====
        var simulationHeap = new ResourceHeap(0, ulong.MaxValue); // Unlimited size for simulation
        
        foreach (var (logicalIndex, logicalResource) in logicalResources)
        {
            ulong size;
            ulong alignment;

            if (logicalResource.type == RenderGraphResourceType.Texture)
            {
                size = CalculateTextureSize(logicalResource.textureDescriptor);
                alignment = DefaultTextureAlignment;
            }
            else // Buffer
            {
                size = logicalResource.bufferDescriptor.sizeInBytes;
                alignment = DefaultBufferAlignment;
            }

            var (success, offset, block) = simulationHeap.TryAllocate(
                size,
                logicalResource.firstUsePass,
                logicalResource.lastUsePass,
                logicalIndex,
                alignment);

            if (!success)
            {
                throw new InvalidOperationException("Simulation allocation failed - this should never happen with unlimited heap");
            }
        }

        // Get peak usage from simulation
        var peakMemoryUsage = simulationHeap.GetPeakUsage();
        
        // Align peak usage to 64KB (D3D12 requirement)
        peakMemoryUsage = AlignUp(peakMemoryUsage, DefaultTextureAlignment);

#if DEBUG
        Console.WriteLine($"\nPeak Memory Usage: {peakMemoryUsage / (1024.0 * 1024.0):F2} MB");
#endif

        // ===== PASS 2: Create a single heap of the peak size and do the real allocation =====
        var mainHeap = new ResourceHeap(0, peakMemoryUsage);
        _heaps.Add(mainHeap);

#if DEBUG
        Console.WriteLine($"Created Single Heap:");
        Console.WriteLine($"  Size: {peakMemoryUsage / (1024.0 * 1024.0):F2} MB\n");
#endif

        // Allocate each logical resource in the heap
        foreach (var (logicalIndex, logicalResource) in logicalResources)
        {
            ulong size;
            ulong alignment;

            if (logicalResource.type == RenderGraphResourceType.Texture)
            {
                size = CalculateTextureSize(logicalResource.textureDescriptor);
                alignment = DefaultTextureAlignment;
            }
            else // Buffer
            {
                size = logicalResource.bufferDescriptor.sizeInBytes;
                alignment = DefaultBufferAlignment;
            }

            var (success, offset, block) = mainHeap.TryAllocate(
                size,
                logicalResource.firstUsePass,
                logicalResource.lastUsePass,
                logicalIndex,
                alignment);

            if (!success)
            {
                throw new InvalidOperationException("Real allocation failed - this should match simulation");
            }

            var assignedHeapIndex = 0;
            var assignedOffset = offset;
            var assignedBlock = block;

            var assignedPlaced = _pool.Rent<PlacedResource>();
            assignedPlaced.index = _placedResources.Count;
            assignedPlaced.type = logicalResource.type;
            assignedPlaced.heapIndex = assignedHeapIndex;
            assignedPlaced.heapOffset = assignedOffset;
            assignedPlaced.sizeInBytes = size;
            assignedPlaced.firstUsePass = logicalResource.firstUsePass;
            assignedPlaced.lastUsePass = logicalResource.lastUsePass;
            assignedPlaced.memoryBlock = assignedBlock;

            if (logicalResource.type == RenderGraphResourceType.Texture)
            {
                assignedPlaced.textureDesc = logicalResource.textureDescriptor;
            }
            else
            {
                assignedPlaced.bufferDesc = logicalResource.bufferDescriptor;
            }

            assignedPlaced.aliasedLogicalResources.Clear();
            assignedPlaced.aliasedLogicalResources.Add(logicalIndex);

            _placedResources.Add(assignedPlaced);

#if DEBUG
            var isAliased = assignedBlock.logicalResourceIndex != logicalIndex && assignedBlock.logicalResourceIndex != -1;
            var name = logicalResource.type == RenderGraphResourceType.Texture
                ? logicalResource.textureDescriptor.name
                : logicalResource.bufferDescriptor.name;
            var typeName = logicalResource.type == RenderGraphResourceType.Texture ? "Texture" : "Buffer";

            if (isAliased)
            {
                Console.WriteLine($"\nALIASING {typeName}: {name}");
                Console.WriteLine($"  Placed in Heap {assignedHeapIndex} at offset {assignedOffset} ({assignedOffset / 1024.0:F2} KB)");
                Console.WriteLine($"  Size: {size / 1024.0:F2} KB");
            }
            else
            {
                Console.WriteLine($"\nAllocated {typeName}: {name}");
                Console.WriteLine($"  Heap {assignedHeapIndex}, Offset {assignedOffset} ({assignedOffset / 1024.0:F2} KB)");
                Console.WriteLine($"  Size: {size / 1024.0:F2} KB");
            }
#endif

            // Record the mapping
            _logicalToPlaced[logicalIndex] = assignedPlaced.index;
        }

        // Second pass: Populate aliasedLogicalResources lists
        // For each placed resource, find all OTHER placed resources at the same heap+offset
        for (var i = 0; i < _placedResources.Count; i++)
        {
            var placed = _placedResources[i];
            
            // Find all logical resources that share the same heap location
            for (var j = 0; j < _placedResources.Count; j++)
            {
                if (i == j) continue; // Skip self
                
                var other = _placedResources[j];
                
                // Check if they're at the same heap+offset
                if (other.heapIndex == placed.heapIndex && other.heapOffset == placed.heapOffset)
                {
                    // Add the other's logical resource to this one's aliased list
                    var otherLogicalIndex = other.aliasedLogicalResources[0]; // Each has exactly one at this point
                    if (!placed.aliasedLogicalResources.Contains(otherLogicalIndex))
                    {
                        placed.aliasedLogicalResources.Add(otherLogicalIndex);
                    }
                }
            }
        }

#if DEBUG
        // Debug output: Show which resources alias with each other
        Console.WriteLine("\n=== Aliasing Groups ===");
        var processedOffsets = new HashSet<(int heapIndex, ulong offset)>();
        for (var i = 0; i < _placedResources.Count; i++)
        {
            var placed = _placedResources[i];
            var key = (placed.heapIndex, placed.heapOffset);
            
            if (!processedOffsets.Contains(key) && placed.aliasedLogicalResources.Count > 1)
            {
                processedOffsets.Add(key);
                Console.WriteLine($"Heap {placed.heapIndex} @ Offset {placed.heapOffset / 1024.0:F2} KB ({placed.aliasedLogicalResources.Count} resources):");
                
                foreach (var logicalIdx in placed.aliasedLogicalResources)
                {
                    var res = registry.GetResourceByIndex(logicalIdx);
                    var name = res.type == RenderGraphResourceType.Texture
                        ? res.textureDescriptor.name
                        : res.bufferDescriptor.name;
                    Console.WriteLine($"  - {name} (Pass {res.firstUsePass}-{res.lastUsePass})");
                }
            }
        }
        Console.WriteLine("=======================\n");
#endif

#if DEBUG
        ulong totalPhysicalSize = 0;
        for (var i = 0; i < _heaps.Count; i++)
        {
            totalPhysicalSize += _heaps[i].GetPeakUsage();
        }

        Console.WriteLine($"\n=== Heap-Based Aliasing Summary ===");
        Console.WriteLine($"Logical Resources: {logicalResources.Count}");
        Console.WriteLine($"Placed Resources: {_placedResources.Count}");
        Console.WriteLine($"Heaps: {_heaps.Count}");
        Console.WriteLine($"Total Logical Memory: {totalLogicalSize / 1024.0:F2} KB");
        Console.WriteLine($"Total Physical Memory: {totalPhysicalSize / 1024.0:F2} KB");
        Console.WriteLine($"Memory Saved: {(totalLogicalSize - totalPhysicalSize) / 1024.0:F2} KB ({(1.0 - (double)totalPhysicalSize / totalLogicalSize) * 100.0:F1}%)");
        Console.WriteLine("================================\n");
#endif

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

    private static ulong CalculateTextureSize(TextureDescriptor descriptor)
    {
        var bytesPerPixel = descriptor.format switch
        {
            TextureFormat.RGBA8 => 4,
            TextureFormat.RGBA16F => 8,
            TextureFormat.RGBA32F => 16,
            TextureFormat.Depth32F => 4,
            TextureFormat.Depth24Stencil8 => 4,
            _ => 4
        };

        // Add alignment padding (D3D12 requires 64KB alignment)
        var size = (ulong)(descriptor.width * descriptor.height * bytesPerPixel);
        return AlignUp(size, DefaultTextureAlignment);
    }

    private static ulong AlignUp(ulong value, ulong alignment)
    {
        return (value + alignment - 1) & ~(alignment - 1);
    }

    public void Clear()
    {
        for (var i = 0; i < _placedResources.Count; i++)
        {
            _pool.Return(_placedResources[i]);
        }
        _placedResources.Clear();
        _logicalToPlaced.Clear();
        _heaps.Clear();
    }

    /// <summary>
    /// Restores aliasing state from cache.
    /// </summary>
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
            placed.heapIndex = data.heapIndex;
            placed.heapOffset = data.heapOffset;
            placed.sizeInBytes = data.sizeInBytes;
            placed.textureDesc = data.textureDesc;
            placed.bufferDesc = data.bufferDesc;
            placed.firstUsePass = data.firstUsePass;
            placed.lastUsePass = data.lastUsePass;

            placed.aliasedLogicalResources.Clear();

            _placedResources.Add(placed);
        }
    }

    /// <summary>
    /// Stores current aliasing state to cache.
    /// </summary>
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
                heapIndex = placed.heapIndex,
                heapOffset = placed.heapOffset,
                sizeInBytes = placed.sizeInBytes,
                textureDesc = placed.textureDesc,
                bufferDesc = placed.bufferDesc,
                firstUsePass = placed.firstUsePass,
                lastUsePass = placed.lastUsePass
            });
        }
    }
}
