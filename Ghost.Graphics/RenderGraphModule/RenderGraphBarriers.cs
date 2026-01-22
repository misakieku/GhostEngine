using Ghost.Core;
using Ghost.Graphics.RHI;
using System.Runtime.InteropServices;

namespace Ghost.Graphics.RenderGraphModule;

/// <summary>
/// Represents a resource barrier that needs to be inserted.
/// </summary>
internal struct ResourceBarrier
{
    public int PassIndex;
    public BarrierDesc Desc;
    public Identifier<RGResource> LogicalResource;

    public readonly Identifier<RGResource> Resource => LogicalResource;

    public static ResourceBarrier Create(int passIndex, BarrierDesc desc, Identifier<RGResource> logicalResource)
    {
        return new ResourceBarrier
        {
            PassIndex = passIndex,
            Desc = desc,
            LogicalResource = logicalResource
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
