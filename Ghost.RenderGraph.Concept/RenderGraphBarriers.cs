using System.Runtime.InteropServices;

namespace Ghost.RenderGraph.Concept;

/// <summary>
/// GPU resource states for barrier tracking.
/// Based on D3D12 resource states.
/// </summary>
[Flags]
public enum ResourceState
{
    Undefined = 0,
    RenderTarget = 1 << 0,
    DepthWrite = 1 << 1,
    DepthRead = 1 << 2,
    ShaderResource = 1 << 3,
    UnorderedAccess = 1 << 4,
    CopySource = 1 << 5,
    CopyDest = 1 << 6,
    Present = 1 << 7,
}

/// <summary>
/// Types of barriers that can be inserted.
/// </summary>
public enum BarrierType
{
    Transition,     // State transition (e.g., RenderTarget -> ShaderResource)
    Aliasing,       // Aliasing barrier (new resource reusing memory)
    UAV,            // UAV barrier (synchronize UAV access)
}

/// <summary>
/// Represents a resource barrier that needs to be inserted.
/// For D3D12 aliasing barriers: ResourceBefore is the old resource, ResourceAfter is the new resource.
/// </summary>
internal struct ResourceBarrier
{
    [StructLayout(LayoutKind.Explicit)]
    private struct barrier_union
    {
        internal struct barrier_union_transition
        {
            public RenderGraphTextureHandle Resource;
            public ResourceState StateBefore;
            public ResourceState StateAfter;
        }

        internal struct barrier_union_aliasing
        {
            public RenderGraphTextureHandle ResourceBefore;
            public RenderGraphTextureHandle ResourceAfter;
        }

        // TODO: union can not have non-blittable types

        [FieldOffset(0)]
        public barrier_union_transition Transition;
        [FieldOffset(0)]
        public barrier_union_aliasing Aliasing;
    }

    public BarrierType Type;
    
    // For Transition and UAV barriers
    public RenderGraphTextureHandle Resource;
    public ResourceState StateBefore;
    public ResourceState StateAfter;
    
    // For Aliasing barriers (D3D12_RESOURCE_BARRIER::Aliasing)
    public RenderGraphTextureHandle ResourceBefore;  // pResourceBefore
    public RenderGraphTextureHandle ResourceAfter;   // pResourceAfter
    
    public int PassIndex;

    // Constructor for Transition and UAV barriers
    public ResourceBarrier(BarrierType type, RenderGraphTextureHandle resource, 
        ResourceState before, ResourceState after, int passIndex)
    {
        Type = type;
        Resource = resource;
        StateBefore = before;
        StateAfter = after;
        ResourceBefore = default;
        ResourceAfter = default;
        PassIndex = passIndex;
    }

    // Constructor for Aliasing barriers
    public static ResourceBarrier CreateAliasingBarrier(
        RenderGraphTextureHandle resourceBefore, 
        RenderGraphTextureHandle resourceAfter, 
        int passIndex)
    {
        return new ResourceBarrier
        {
            Type = BarrierType.Aliasing,
            ResourceBefore = resourceBefore,
            ResourceAfter = resourceAfter,
            PassIndex = passIndex,
            Resource = default,
            StateBefore = ResourceState.Undefined,
            StateAfter = ResourceState.Undefined
        };
    }

    public static ResourceBarrier CreateTransitionBarrier(
        RenderGraphTextureHandle resource,
        ResourceState before,
        ResourceState after,
        int passIndex)
    {
        return new ResourceBarrier
        {
            Type = BarrierType.Transition,
            Resource = resource,
            StateBefore = before,
            StateAfter = after,
            PassIndex = passIndex,
            ResourceBefore = default,
            ResourceAfter = default
        };
    }

#if DEBUG
    public override readonly string ToString()
    {
        return Type switch
        {
            BarrierType.Transition => $"[Pass {PassIndex}] TRANSITION: {Resource.Name} ({StateBefore} -> {StateAfter})",
            BarrierType.Aliasing => $"[Pass {PassIndex}] ALIASING: {ResourceBefore.Name} -> {ResourceAfter.Name} (reusing physical memory)",
            BarrierType.UAV => $"[Pass {PassIndex}] UAV: {Resource.Name}",
            _ => $"[Pass {PassIndex}] UNKNOWN BARRIER"
        };
    }
#endif
}

/// <summary>
/// Tracks the current state of a resource across passes.
/// </summary>
internal sealed class ResourceStateTracker
{
    public int ResourceIndex;
    public ResourceState CurrentState = ResourceState.Undefined;
    public int LastAccessPass = -1;

    public void Reset()
    {
        ResourceIndex = -1;
        CurrentState = ResourceState.Undefined;
        LastAccessPass = -1;
    }
}
