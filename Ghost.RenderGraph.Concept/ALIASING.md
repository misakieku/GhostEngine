# Resource Aliasing in Render Graph

## Overview

Resource aliasing is a memory optimization technique where multiple virtual resources share the same physical memory allocation. This significantly reduces memory usage for transient resources.

## How It Works

### 1. Lifetime Analysis
The render graph analyzes when each transient resource is first used and last used:
```
GBuffer.Albedo:  [0..1]  ━━━━━━━━
SSAO:             [2..4]          ━━━━━━━━━━━
```

### 2. Aliasing Detection
Resources with non-overlapping lifetimes can share memory:
```
Physical_Texture_2:
  [0..1] GBuffer.Albedo  ━━━━━━━━
  [2..4] SSAO                    ━━━━━━━━━━━  ← ALIAS!
```

### 3. Memory Allocation
Instead of creating 2 separate 8MB textures (16MB total), we create 1 physical allocation (8MB) that both virtual resources map to.

## Aliasing Barriers

In D3D12/Vulkan, when you reuse memory for a different resource, you must insert an **aliasing barrier** to inform the GPU that the memory interpretation has changed.

### When Aliasing Barriers Are Needed

An aliasing barrier is required when:
1. Two or more resources share the same physical memory
2. You're switching from one resource to another
3. Both resources are accessed within overlapping command buffer scopes

In this implementation, aliasing barriers are automatically inserted when:
- A pass accesses a resource that shares a physical allocation
- A different resource was previously active on that allocation
- The active resource hasn't been explicitly destroyed

## Example Output

```
[RG] ===== RESOURCE ALIASING ANALYSIS =====
[ALLOC] 'GBuffer.Albedo' gets new allocation 'Physical_Texture_2' (size: 8.29 MB, lifetime: [0..1])
[ALIAS] 'SSAO' aliases with 'Physical_Texture_2' (offset: 0, size: 8.29 MB, lifetime: [2..4])
[ALIAS] 'BloomDownsample' aliases with 'Physical_Texture_1' (offset: 0, size: 8.29 MB, lifetime: [3..5])

[RG] Memory Statistics:
     Total memory without aliasing: 80.64 MB
     Total memory with aliasing: 47.46 MB
     Memory saved: 33.18 MB (41.1%)
     Allocations: 5 physical allocations for 8 resources
```

## Aliasing Algorithm

The allocator uses a **First-Fit** strategy:

```csharp
foreach (var resource in transientResources.OrderBy(FirstUse).ThenBy(Size))
{
    // Try to find existing allocation
    foreach (var slot in allocationSlots)
    {
        if (slot.LargeEnough && 
            slot.SameType && 
            !HasLifetimeOverlap(slot, resource))
        {
            // REUSE!
            slot.AddResource(resource);
            return;
        }
    }
    
    // No compatible slot found, create new allocation
    CreateNewAllocation(resource);
}
```

### Key Constraints

1. **Size**: The physical allocation must be >= required size
2. **Type**: Textures can only alias with textures, buffers with buffers
3. **Lifetime**: Resources must have non-overlapping lifetimes
4. **Alignment**: Resources must satisfy GPU alignment requirements

## Real-World Benefits

### Deferred Rendering Pipeline

| Resource | Size | Lifetime | Physical Alloc |
|----------|------|----------|----------------|
| GBuffer.Albedo | 8MB | [0..1] | Physical_1 |
| GBuffer.Normal | 16MB | [0..2] | Physical_2 |
| GBuffer.Depth | 8MB | [0..2] | Physical_3 |
| Lighting | 16MB | [1..3] | Physical_4 |
| SSAO | 8MB | [2..4] | **Physical_1** ✓ |
| TAA | 16MB | [3..4] | **Physical_2** ✓ |
| Bloom | 8MB | [3..5] | **Physical_3** ✓ |

**Without aliasing**: 80MB  
**With aliasing**: 48MB  
**Savings**: 40% (32MB)

### At 4K Resolution (3840x2160)

| Format | Size (1080p) | Size (4K) | 
|--------|-------------|-----------|
| RGBA8 | 8.3 MB | 33.2 MB |
| RGBA16F | 16.6 MB | 66.4 MB |
| Depth32F | 8.3 MB | 33.2 MB |

**4K Savings**: 128MB → Modern AAA games save **hundreds of megabytes** to **gigabytes** using this technique.

## Advanced Optimizations (Not Implemented)

### 1. Pool-Based Allocation
Instead of individual allocations, use large memory pools and sub-allocate from them.

### 2. Heap-Aware Aliasing
D3D12 has specific heap types (Default, Upload, Readback). Resources can only alias within the same heap type.

### 3. Subresource Aliasing
Alias mip levels or array slices independently for more granular reuse.

### 4. Multi-Queue Aliasing
Resources on different queues (Graphics, Compute, Copy) need special synchronization.

## Comparison with Production Systems

### Unreal Engine 5 RDG
```cpp
// Automatic aliasing based on lifetimes
FRDGTextureRef TextureA = GraphBuilder.CreateTexture(Desc, TEXT("A"));
FRDGTextureRef TextureB = GraphBuilder.CreateTexture(Desc, TEXT("B"));
// RDG automatically aliases if lifetimes don't overlap
```

### Frostbite Frame Graph
- Uses explicit aliasing groups
- Developers can hint which resources should share memory
- More control but less automatic

### Unity HDRP Render Graph
```csharp
// Unity's approach (similar to ours)
var tempRT = renderGraph.CreateTexture(desc);
// Automatic aliasing through lifetime analysis
```

## Performance Impact

**Memory**: 30-50% reduction typical  
**CPU Overhead**: <1ms during compile phase  
**GPU Performance**: Same or better (fewer allocations)  
**Bandwidth**: Reduced due to better cache locality

## Debugging Tips

1. **Print Allocation Map**: See which resources share memory
2. **Visualize Lifetimes**: Graph timeline to spot aliasing opportunities
3. **Track Peak Memory**: Identify frames with poor aliasing
4. **Monitor Aliasing Barriers**: Too many can hurt performance

## Conclusion

Resource aliasing is a critical optimization in modern rendering. This proof of concept demonstrates:
- ✅ Automatic lifetime analysis
- ✅ First-fit allocation strategy
- ✅ Type-safe aliasing (textures vs buffers)
- ✅ Memory savings tracking
- ✅ Aliasing barrier insertion points (simulated)

For production use, integrate with actual D3D12/Vulkan memory heaps and implement proper aliasing barriers as defined by the API specifications.
