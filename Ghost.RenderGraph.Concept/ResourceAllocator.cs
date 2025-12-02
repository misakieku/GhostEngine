namespace Ghost.RenderGraph.Concept;

/// <summary>
/// Represents a physical memory allocation that can be shared by multiple transient resources
/// </summary>
internal class PhysicalResourceAllocation
{
    public int AllocationId { get; }
    public ulong SizeInBytes { get; }
    public ulong OffsetInBytes { get; }
    public string DebugName { get; }
    public List<RenderGraphResourceHandle> AliasedResources { get; } = new();

    public PhysicalResourceAllocation(int allocationId, ulong sizeInBytes, ulong offsetInBytes, string debugName)
    {
        AllocationId = allocationId;
        SizeInBytes = sizeInBytes;
        OffsetInBytes = offsetInBytes;
        DebugName = debugName;
    }
}

/// <summary>
/// Manages memory allocation and aliasing for transient resources
/// </summary>
internal class ResourceAllocator
{
    private readonly List<PhysicalResourceAllocation> _allocations = new();
    private int _allocationIdCounter = 0;

    public IReadOnlyList<PhysicalResourceAllocation> Allocations => _allocations;

    /// <summary>
    /// Allocate physical memory for resources, enabling aliasing where possible
    /// </summary>
    public void AllocateResources(
        IReadOnlyList<ResourceLifetime> resourceLifetimes,
        List<RenderGraphPass> passes)
    {
        Console.WriteLine("\n[RG] ===== RESOURCE ALIASING ANALYSIS =====");

        // Separate imported and transient resources
        // Sort by SIZE FIRST (descending), then by FIRST USE (ascending)
        // This allows smaller resources (A, B) to alias into larger resources' (C) space
        // Example: C=10MB[1..2], A=4MB[0..1], B=6MB[0..1] → Allocate C first, then A and B alias into C's space
        var transientResources = resourceLifetimes
            .Where(lt => !lt.Handle.IsImported && lt.FirstUse != int.MaxValue)
            .OrderByDescending(lt => GetResourceSize(lt.Handle))
            .ThenBy(lt => lt.FirstUse)
            .ToList();

        if (!transientResources.Any())
        {
            Console.WriteLine("No transient resources to allocate.");
            return;
        }

        // Track which allocation slots are occupied at each pass
        var allocationSlots = new List<AllocationSlot>();

        foreach (var resource in transientResources)
        {
            var size = GetResourceSize(resource.Handle);
            var alignment = GetResourceAlignment(resource.Handle);

            // Find an existing allocation slot that:
            // 1. Is large enough
            // 2. Has no lifetime overlap
            // 3. Matches resource type (texture/buffer)
            AllocationSlot? reuseSlot = null;
            foreach (var slot in allocationSlots)
            {
                if (CanAlias(slot, resource, size, alignment))
                {
                    reuseSlot = slot;
                    break;
                }
            }

            if (reuseSlot != null)
            {
                // Reuse existing allocation - find offset within the allocation
                ulong offsetInAllocation = reuseSlot.FindFreeOffset(size, alignment, resource);
                reuseSlot.AddResource(resource, offsetInAllocation, size);
                
                Console.WriteLine($"[ALIAS] '{resource.Handle.Name}' aliases with '{reuseSlot.Allocation.DebugName}' " +
                                $"(heap offset: {reuseSlot.Allocation.OffsetInBytes}, resource offset: {offsetInAllocation}, size: {size} bytes, " +
                                $"lifetime: [{resource.FirstUse}..{resource.LastUse}])");
            }
            else
            {
                // Create new allocation
                // Calculate heap offset (simulated - in real D3D12MA this would be the actual heap offset)
                ulong heapOffset = allocationSlots.Count > 0 
                    ? allocationSlots.Max(s => s.Allocation.OffsetInBytes + s.Allocation.SizeInBytes)
                    : 0;
                
                var allocation = new PhysicalResourceAllocation(
                    _allocationIdCounter++,
                    size,
                    offsetInBytes: heapOffset,
                    $"Physical_{resource.Handle.Type}_{_allocationIdCounter}");

                var newSlot = new AllocationSlot(allocation, resource.Handle.Type);
                newSlot.AddResource(resource, 0, size); // Offset 0 within this new allocation
                allocationSlots.Add(newSlot);

                Console.WriteLine($"[ALLOC] '{resource.Handle.Name}' gets new allocation '{allocation.DebugName}' " +
                                $"(heap offset: {heapOffset}, size: {size} bytes, lifetime: [{resource.FirstUse}..{resource.LastUse}])");
            }
        }

        _allocations.AddRange(allocationSlots.Select(s => s.Allocation));

        // Print summary
        Console.WriteLine($"\n[RG] Memory Statistics:");
        var totalWithoutAliasing = transientResources.Sum(r => (long)GetResourceSize(r.Handle));
        var totalWithAliasing = _allocations.Sum(a => (long)a.SizeInBytes);
        var savedMemory = totalWithoutAliasing - totalWithAliasing;
        var savingPercentage = totalWithoutAliasing > 0 ? (savedMemory * 100.0 / totalWithoutAliasing) : 0;

        Console.WriteLine($"     Total memory without aliasing: {FormatBytes(totalWithoutAliasing)}");
        Console.WriteLine($"     Total memory with aliasing: {FormatBytes(totalWithAliasing)}");
        Console.WriteLine($"     Memory saved: {FormatBytes(savedMemory)} ({savingPercentage:F1}%)");
        Console.WriteLine($"     Allocations: {_allocations.Count} physical allocations for {transientResources.Count} resources");
    }

    private bool CanAlias(AllocationSlot slot, ResourceLifetime resource, ulong requiredSize, ulong requiredAlignment)
    {
        // Must be same resource type
        if (slot.ResourceType != resource.Handle.Type)
            return false;

        // Must be large enough
        if (slot.Allocation.SizeInBytes < requiredSize)
            return false;

        // Check for lifetime overlap with any resource in this slot
        foreach (var existingResource in slot.Resources)
        {
            if (LifetimesOverlap(existingResource, resource))
                return false;
        }

        return true;
    }

    private bool LifetimesOverlap(ResourceLifetime a, ResourceLifetime b)
    {
        // Two resources overlap if their lifetimes intersect
        return !(a.LastUse < b.FirstUse || b.LastUse < a.FirstUse);
    }

    private ulong GetResourceSize(RenderGraphResourceHandle handle)
    {
        return handle switch
        {
            RenderGraphTextureHandle texture => CalculateTextureSize(texture.Descriptor),
            RenderGraphBufferHandle buffer => (ulong)buffer.Descriptor.SizeInBytes,
            _ => 0
        };
    }

    private ulong GetResourceAlignment(RenderGraphResourceHandle handle)
    {
        // In a real implementation, this would query D3D12_RESOURCE_ALLOCATION_INFO
        return handle switch
        {
            RenderGraphTextureHandle => 65536, // 64KB texture alignment (typical)
            RenderGraphBufferHandle => 256,    // 256 byte buffer alignment
            _ => 256
        };
    }

    private ulong CalculateTextureSize(TextureDescriptor desc)
    {
        // Simplified size calculation
        var bytesPerPixel = desc.Format switch
        {
            TextureFormat.RGBA8 => 4,
            TextureFormat.RGBA16F => 8,
            TextureFormat.RGBA32F => 16,
            TextureFormat.Depth32F => 4,
            TextureFormat.R32Uint => 4,
            _ => 4
        };

        return (ulong)(desc.Width * desc.Height * bytesPerPixel);
    }

    private string FormatBytes(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";
        if (bytes < 1024 * 1024)
            return $"{bytes / 1024.0:F2} KB";
        return $"{bytes / (1024.0 * 1024.0):F2} MB";
    }

    public PhysicalResourceAllocation? GetAllocation(RenderGraphResourceHandle handle)
    {
        return _allocations.FirstOrDefault(a => a.AliasedResources.Any(r => r.Id == handle.Id));
    }

    private class AllocationSlot
    {
        public PhysicalResourceAllocation Allocation { get; }
        public ResourceType ResourceType { get; }
        public List<ResourceLifetime> Resources { get; } = new();
        
        // Track occupied regions within this allocation: (offset, size, lifetime)
        private readonly List<(ulong Offset, ulong Size, ResourceLifetime Resource)> _occupiedRegions = new();

        public AllocationSlot(PhysicalResourceAllocation allocation, ResourceType resourceType)
        {
            Allocation = allocation;
            ResourceType = resourceType;
        }

        /// <summary>
        /// Find a free offset within this allocation that can fit the required size
        /// and doesn't conflict with active resources
        /// </summary>
        public ulong FindFreeOffset(ulong requiredSize, ulong alignment, ResourceLifetime newResource)
        {
            // If no resources yet, return 0
            if (_occupiedRegions.Count == 0)
            {
                return 0;
            }
            
            // Sort regions by offset
            var sortedRegions = _occupiedRegions.OrderBy(r => r.Offset).ToList();
            
            // Try to fit at the beginning (offset 0)
            ulong candidateOffset = 0;
            bool fitsAtStart = true;
            
            foreach (var region in sortedRegions)
            {
                // Check if this region overlaps with our candidate position
                if (region.Offset < requiredSize)
                {
                    // Check lifetime - if no overlap, we can still use this space
                    if (LifetimesOverlap(region.Resource, newResource))
                    {
                        fitsAtStart = false;
                        break;
                    }
                }
            }
            
            if (fitsAtStart)
            {
                return AlignUp(0, alignment);
            }
            
            // Try gaps between regions
            for (int i = 0; i < sortedRegions.Count; i++)
            {
                var current = sortedRegions[i];
                
                // Skip if current region's lifetime overlaps with new resource
                if (LifetimesOverlap(current.Resource, newResource))
                {
                    continue;
                }
                
                // Try placing after this region
                candidateOffset = AlignUp(current.Offset + current.Size, alignment);
                
                // Check if it fits before the next region (or end of allocation)
                ulong nextRegionStart = (i + 1 < sortedRegions.Count) 
                    ? sortedRegions[i + 1].Offset 
                    : Allocation.SizeInBytes;
                
                if (candidateOffset + requiredSize <= nextRegionStart)
                {
                    // Check no lifetime conflicts with any regions in this range
                    bool hasConflict = false;
                    for (int j = i + 1; j < sortedRegions.Count; j++)
                    {
                        var other = sortedRegions[j];
                        if (other.Offset < candidateOffset + requiredSize)
                        {
                            if (LifetimesOverlap(other.Resource, newResource))
                            {
                                hasConflict = true;
                                break;
                            }
                        }
                    }
                    
                    if (!hasConflict)
                    {
                        return candidateOffset;
                    }
                }
            }
            
            // Try placing at the end
            if (sortedRegions.Count > 0)
            {
                var last = sortedRegions[^1];
                if (!LifetimesOverlap(last.Resource, newResource))
                {
                    candidateOffset = AlignUp(last.Offset + last.Size, alignment);
                    if (candidateOffset + requiredSize <= Allocation.SizeInBytes)
                    {
                        return candidateOffset;
                    }
                }
            }
            
            // No space found - caller should create new allocation
            return 0;
        }
        
        private bool LifetimesOverlap(ResourceLifetime a, ResourceLifetime b)
        {
            return !(a.LastUse < b.FirstUse || b.LastUse < a.FirstUse);
        }
        
        private ulong AlignUp(ulong value, ulong alignment)
        {
            return (value + alignment - 1) / alignment * alignment;
        }

        public void AddResource(ResourceLifetime resource, ulong offsetInAllocation, ulong size)
        {
            Resources.Add(resource);
            _occupiedRegions.Add((offsetInAllocation, size, resource));
            Allocation.AliasedResources.Add(resource.Handle);
        }
    }
}
