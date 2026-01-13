using Ghost.Core;
using System.Runtime.InteropServices;

namespace Ghost.RenderGraph.Concept;

/// <summary>
/// GPU resource states for barrier tracking.
/// Based on D3D12 resource states.
/// </summary>
[Flags]
public enum ResourceState
{
    Common = 0,
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
            public Identifier<RGResource> resource;
            public ResourceState stateBefore;
            public ResourceState stateAfter;
        }

        internal struct barrier_union_aliasing
        {
            public Identifier<RGResource> resourceBefore;
            public Identifier<RGResource> resourceAfter;
        }

        // TODO: union can not have non-blittable types

        [FieldOffset(0)]
        public barrier_union_transition transition;
        [FieldOffset(0)]
        public barrier_union_aliasing aliasing;
    }

    private barrier_union _union;

    public BarrierType Type
    {
        get; init;
    }

    public int PassIndex
    {
        get; init;
    }

    // For Transition and UAV barriers
    public readonly Identifier<RGResource> Resource => _union.transition.resource;
    public readonly ResourceState StateBefore => _union.transition.stateBefore;
    public readonly ResourceState StateAfter => _union.transition.stateAfter;

    // For Aliasing barriers (D3D12_RESOURCE_BARRIER::Aliasing)
    public readonly Identifier<RGResource> ResourceBefore => _union.aliasing.resourceBefore;
    public readonly Identifier<RGResource> ResourceAfter => _union.aliasing.resourceAfter;

    // Constructor for Aliasing barriers
    public static ResourceBarrier CreateAliasingBarrier(
        Identifier<RGResource> resourceBefore,
        Identifier<RGResource> resourceAfter,
        int passIndex)
    {
        return new ResourceBarrier
        {
            Type = BarrierType.Aliasing,
            PassIndex = passIndex,
            _union = new barrier_union
            {
                aliasing = new barrier_union.barrier_union_aliasing
                {
                    resourceBefore = resourceBefore,
                    resourceAfter = resourceAfter
                }
            }
        };
    }

    public static ResourceBarrier CreateTransitionBarrier(
        Identifier<RGResource> resource,
        ResourceState before,
        ResourceState after,
        int passIndex)
    {
        return new ResourceBarrier
        {
            Type = BarrierType.Transition,
            PassIndex = passIndex,
            _union = new barrier_union
            {
                transition = new barrier_union.barrier_union_transition
                {
                    resource = resource,
                    stateBefore = before,
                    stateAfter = after
                }
            }
        };
    }

    public override readonly string ToString()
    {
        return Type switch
        {
            BarrierType.Transition => $"[Pass {PassIndex}] Transition Barrier: Resource {Resource.Value} from {StateBefore} to {StateAfter}",
            BarrierType.Aliasing => $"[Pass {PassIndex}] Aliasing Barrier: ResourceBefore {ResourceBefore.Value} to ResourceAfter {ResourceAfter.Value}",
            _ => "Unknown Barrier Type"
        };
    }
}

/// <summary>
/// Tracks the current state of a resource across passes.
/// </summary>
internal sealed class ResourceStateTracker
{
    public int ResourceIndex;
    public ResourceState CurrentState = ResourceState.Common;
    public int LastAccessPass = -1;

    public void Reset()
    {
        ResourceIndex = -1;
        CurrentState = ResourceState.Common;
        LastAccessPass = -1;
    }
}
