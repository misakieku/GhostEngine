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
        var transientResources = resourceLifetimes
            .Where(lt => !lt.Handle.IsImported && lt.FirstUse != int.MaxValue)
            .OrderBy(lt => lt.FirstUse)
            .ThenByDescending(lt => GetResourceSize(lt.Handle))
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
                // Reuse existing allocation
                reuseSlot.AddResource(resource);
                Console.WriteLine($"[ALIAS] '{resource.Handle.Name}' aliases with '{reuseSlot.Allocation.DebugName}' " +
                                $"(offset: {reuseSlot.Allocation.OffsetInBytes}, size: {size} bytes, " +
                                $"lifetime: [{resource.FirstUse}..{resource.LastUse}])");
            }
            else
            {
                // Create new allocation
                var allocation = new PhysicalResourceAllocation(
                    _allocationIdCounter++,
                    size,
                    offsetInBytes: 0, // In a real implementation, this would be a heap offset
                    $"Physical_{resource.Handle.Type}_{_allocationIdCounter}");

                var newSlot = new AllocationSlot(allocation, resource.Handle.Type);
                newSlot.AddResource(resource);
                allocationSlots.Add(newSlot);

                Console.WriteLine($"[ALLOC] '{resource.Handle.Name}' gets new allocation '{allocation.DebugName}' " +
                                $"(size: {size} bytes, lifetime: [{resource.FirstUse}..{resource.LastUse}])");
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

        public AllocationSlot(PhysicalResourceAllocation allocation, ResourceType resourceType)
        {
            Allocation = allocation;
            ResourceType = resourceType;
        }

        public void AddResource(ResourceLifetime resource)
        {
            Resources.Add(resource);
            Allocation.AliasedResources.Add(resource.Handle);
        }
    }
}
