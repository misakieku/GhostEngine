using Ghost.Core.Utilities;

namespace Ghost.RenderGraph.Concept;

/// <summary>
/// Represents a physical GPU resource that can be aliased by multiple logical resources.
/// </summary>
internal sealed class PhysicalResource
{
    public int Index;
    public int Width;
    public int Height;
    public TextureFormat Format;
    public int SizeInBytes;
    
    // Lifetime tracking
    public int FirstUsePass = int.MaxValue;
    public int LastUsePass = -1;
    
    // Aliasing tracking
    public readonly List<int> AliasedLogicalResources = new(4);

    public void Reset()
    {
        Index = -1;
        Width = 0;
        Height = 0;
        Format = TextureFormat.RGBA8;
        SizeInBytes = 0;
        FirstUsePass = int.MaxValue;
        LastUsePass = -1;
        AliasedLogicalResources.Clear();
    }

    public bool CanAlias(TextureDescriptor descriptor)
    {
        // For aliasing, resources must be identical in size and format
        // In a real implementation, you could be more flexible (e.g., same size but different format)
        return Width == descriptor.Width &&
               Height == descriptor.Height &&
               Format == descriptor.Format;
    }

    public void UpdateLifetime(int passIndex)
    {
        FirstUsePass = Math.Min(FirstUsePass, passIndex);
        LastUsePass = Math.Max(LastUsePass, passIndex);
    }

    public bool IsAliveAt(int passIndex)
    {
        return passIndex >= FirstUsePass && passIndex <= LastUsePass;
    }

    public int CalculateSize()
    {
        int bytesPerPixel = Format switch
        {
            TextureFormat.RGBA8 => 4,
            TextureFormat.RGBA16F => 8,
            TextureFormat.RGBA32F => 16,
            TextureFormat.Depth32F => 4,
            TextureFormat.Depth24Stencil8 => 4,
            _ => 4
        };
        return Width * Height * bytesPerPixel;
    }
}

/// <summary>
/// Manages physical resource allocation and aliasing.
/// Uses interval scheduling algorithm to minimize memory usage.
/// </summary>
internal sealed class ResourceAliasingManager
{
    private readonly List<PhysicalResource> _physicalResources = new(32);
    private readonly RenderGraphObjectPool _pool = new();
    private int _physicalResourceCount;
    
    // Mapping from logical resource index to physical resource index
    private readonly Dictionary<int, int> _logicalToPhysical = new(64);

    public void BeginFrame()
    {
        _physicalResourceCount = 0;
        _logicalToPhysical.Clear();
        
        // Reset physical resources but keep them in the pool
        for (int i = 0; i < _physicalResources.Count; i++)
        {
            _physicalResources[i].Reset();
        }
    }

    /// <summary>
    /// Assigns physical resources to logical resources using greedy interval scheduling.
    /// This minimizes total GPU memory usage.
    /// </summary>
    public void AssignPhysicalResources(RenderGraphResourceRegistry registry, int passCount)
    {
#if DEBUG
        Console.WriteLine("\n=== Resource Aliasing Analysis ===");
        int totalLogicalSize = 0;
#endif

        // Build list of all logical resources with their lifetimes
        var logicalResources = ListPool<(int index, RenderGraphResource resource)>.Rent();
        
        for (int i = 0; i < registry.TextureResourceCount; i++)
        {
            var resource = registry.GetTextureResourceByIndex(i);
            if (!resource.IsImported) // Don't alias imported resources
            {
                logicalResources.Add((i, resource));
#if DEBUG
                int size = CalculateSize(resource.Descriptor);
                totalLogicalSize += size;
                Console.WriteLine($"Logical Resource {i}: {resource.Descriptor.Name}");
                Console.WriteLine($"  Lifetime: Pass {resource.FirstUsePass} -> {resource.LastUsePass}");
                Console.WriteLine($"  Size: {size / 1024.0:F2} KB");
#endif
            }
        }

        // Sort by first use pass (earlier resources first)
        logicalResources.Sort((a, b) => a.resource.FirstUsePass.CompareTo(b.resource.FirstUsePass));

        // Greedy interval scheduling: assign each logical resource to a physical resource
        foreach (var (logicalIndex, logicalResource) in logicalResources)
        {
            PhysicalResource? assignedPhysical = null;

            // Try to find an existing physical resource that:
            // 1. Has compatible format/size
            // 2. Is not alive during this logical resource's lifetime
            for (int i = 0; i < _physicalResourceCount; i++)
            {
                var physical = _physicalResources[i];
                
                if (physical.CanAlias(logicalResource.Descriptor) &&
                    !HasLifetimeOverlap(physical, logicalResource))
                {
                    assignedPhysical = physical;
                    break;
                }
            }

            // No compatible physical resource found, allocate a new one
            if (assignedPhysical == null)
            {
                assignedPhysical = GetOrCreatePhysicalResource();
                assignedPhysical.Index = _physicalResourceCount - 1;
                assignedPhysical.Width = logicalResource.Descriptor.Width;
                assignedPhysical.Height = logicalResource.Descriptor.Height;
                assignedPhysical.Format = logicalResource.Descriptor.Format;
                assignedPhysical.SizeInBytes = assignedPhysical.CalculateSize();
                
#if DEBUG
                Console.WriteLine($"\nAllocated NEW Physical Resource {assignedPhysical.Index}:");
                Console.WriteLine($"  Size: {assignedPhysical.Width}x{assignedPhysical.Height}");
                Console.WriteLine($"  Format: {assignedPhysical.Format}");
                Console.WriteLine($"  Memory: {assignedPhysical.SizeInBytes / 1024.0:F2} KB");
#endif
            }
#if DEBUG
            else
            {
                Console.WriteLine($"\nALIASING: {logicalResource.Descriptor.Name} -> Physical Resource {assignedPhysical.Index}");
            }
#endif

            // Update physical resource lifetime
            assignedPhysical.UpdateLifetime(logicalResource.FirstUsePass);
            assignedPhysical.UpdateLifetime(logicalResource.LastUsePass);
            assignedPhysical.AliasedLogicalResources.Add(logicalIndex);

            // Record the mapping
            _logicalToPhysical[logicalIndex] = assignedPhysical.Index;
        }

#if DEBUG
        int totalPhysicalSize = 0;
        for (int i = 0; i < _physicalResourceCount; i++)
        {
            totalPhysicalSize += _physicalResources[i].SizeInBytes;
        }
        
        Console.WriteLine($"\n=== Aliasing Summary ===");
        Console.WriteLine($"Logical Resources: {logicalResources.Count}");
        Console.WriteLine($"Physical Resources: {_physicalResourceCount}");
        Console.WriteLine($"Total Logical Memory: {totalLogicalSize / 1024.0:F2} KB");
        Console.WriteLine($"Total Physical Memory: {totalPhysicalSize / 1024.0:F2} KB");
        Console.WriteLine($"Memory Saved: {(totalLogicalSize - totalPhysicalSize) / 1024.0:F2} KB ({(1.0 - (double)totalPhysicalSize / totalLogicalSize) * 100.0:F1}%)");
        Console.WriteLine("================================\n");
#endif

        ListPool<(int index, RenderGraphResource resource)>.Return(logicalResources);
    }

    public int GetPhysicalResourceIndex(int logicalIndex)
    {
        return _logicalToPhysical.TryGetValue(logicalIndex, out var physicalIndex) ? physicalIndex : -1;
    }

    public PhysicalResource? GetPhysicalResource(int physicalIndex)
    {
        return physicalIndex >= 0 && physicalIndex < _physicalResourceCount 
            ? _physicalResources[physicalIndex] 
            : null;
    }

    private bool HasLifetimeOverlap(PhysicalResource physical, RenderGraphResource logical)
    {
        // Check if the lifetimes overlap
        // No overlap if: logical.First > physical.Last OR logical.Last < physical.First
        return !(logical.FirstUsePass > physical.LastUsePass || 
                 logical.LastUsePass < physical.FirstUsePass);
    }

    private PhysicalResource GetOrCreatePhysicalResource()
    {
        PhysicalResource resource;
        if (_physicalResourceCount < _physicalResources.Count)
        {
            resource = _physicalResources[_physicalResourceCount];
            resource.Reset();
        }
        else
        {
            resource = _pool.Rent<PhysicalResource>();
            resource.Reset();
            _physicalResources.Add(resource);
        }

        _physicalResourceCount++;
        return resource;
    }

    private static int CalculateSize(TextureDescriptor descriptor)
    {
        int bytesPerPixel = descriptor.Format switch
        {
            TextureFormat.RGBA8 => 4,
            TextureFormat.RGBA16F => 8,
            TextureFormat.RGBA32F => 16,
            TextureFormat.Depth32F => 4,
            TextureFormat.Depth24Stencil8 => 4,
            _ => 4
        };
        return descriptor.Width * descriptor.Height * bytesPerPixel;
    }

    public void Clear()
    {
        for (int i = 0; i < _physicalResources.Count; i++)
        {
            _pool.Return(_physicalResources[i]);
        }
        _physicalResources.Clear();
        _physicalResourceCount = 0;
        _logicalToPhysical.Clear();
    }

    /// <summary>
    /// Restores aliasing state from cache.
    /// </summary>
    public void RestoreFromCache(Dictionary<int, int> logicalToPhysical, List<PhysicalResourceData> physicalData)
    {
        _logicalToPhysical.Clear();
        foreach (var kvp in logicalToPhysical)
        {
            _logicalToPhysical[kvp.Key] = kvp.Value;
        }

        // Restore physical resources
        _physicalResourceCount = physicalData.Count;
        for (int i = 0; i < physicalData.Count; i++)
        {
            PhysicalResource physical;
            if (i < _physicalResources.Count)
            {
                physical = _physicalResources[i];
                physical.Reset();
            }
            else
            {
                physical = _pool.Rent<PhysicalResource>();
                physical.Reset();
                _physicalResources.Add(physical);
            }

            var data = physicalData[i];
            physical.Index = data.Index;
            physical.Width = data.Width;
            physical.Height = data.Height;
            physical.Format = data.Format;
            physical.FirstUsePass = data.FirstUsePass;
            physical.LastUsePass = data.LastUsePass;
            physical.SizeInBytes = physical.CalculateSize();
        }
    }

    /// <summary>
    /// Stores current aliasing state to cache.
    /// </summary>
    public void StoreToCache(Dictionary<int, int> outLogicalToPhysical, List<PhysicalResourceData> outPhysicalData)
    {
        outLogicalToPhysical.Clear();
        foreach (var kvp in _logicalToPhysical)
        {
            outLogicalToPhysical[kvp.Key] = kvp.Value;
        }

        outPhysicalData.Clear();
        for (int i = 0; i < _physicalResourceCount; i++)
        {
            var physical = _physicalResources[i];
            outPhysicalData.Add(new PhysicalResourceData
            {
                Index = physical.Index,
                Width = physical.Width,
                Height = physical.Height,
                Format = physical.Format,
                FirstUsePass = physical.FirstUsePass,
                LastUsePass = physical.LastUsePass
            });
        }
    }
}
