using Ghost.Core;
using Ghost.Graphics.RHI;
using System.Runtime.InteropServices;

namespace Ghost.Graphics.RenderGraphModule;

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
}

/// <summary>
/// Tracks the current state of a resource across passes.
/// </summary>
internal sealed class ResourceStateTracker
{
    public int resourceIndex;
    public ResourceState currentState = ResourceState.Common;
    public int lastAccessPass = -1;

    public void Reset()
    {
        resourceIndex = -1;
        currentState = ResourceState.Common;
        lastAccessPass = -1;
    }
}
