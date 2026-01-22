using Ghost.Core;
using Ghost.Graphics.RHI;
using System.Runtime.InteropServices;

namespace Ghost.Graphics.RenderGraphModule;

[Flags]
internal enum BarrierFlags
{
    None = 0,
    FirstUsage = 1 << 0,
    Discard = 1 << 1
}

/// <summary>
/// Represents a resource barrier requirement that needs to be resolved at runtime.
/// </summary>
internal struct ResourceBarrier
{
    public int PassIndex;
    public Identifier<RGResource> Resource;
    public ResourceBarrierData TargetState;
    public Identifier<RGResource> AliasingPredecessor; // Invalid if not aliasing
    public BarrierFlags Flags;

    public static ResourceBarrier CreateTransition(int passIndex, Identifier<RGResource> resource, ResourceBarrierData targetState, BarrierFlags flags = BarrierFlags.None)
    {
        return new ResourceBarrier
        {
            PassIndex = passIndex,
            Resource = resource,
            TargetState = targetState,
            AliasingPredecessor = Identifier<RGResource>.Invalid,
            Flags = flags
        };
    }

    public static ResourceBarrier CreateAliasing(int passIndex, Identifier<RGResource> resource, Identifier<RGResource> predecessor, ResourceBarrierData targetState)
    {
        return new ResourceBarrier
        {
            PassIndex = passIndex,
            Resource = resource,
            TargetState = targetState,
            AliasingPredecessor = predecessor,
            Flags = BarrierFlags.FirstUsage | BarrierFlags.Discard // Aliasing implies starting fresh
        };
    }

    public override readonly string ToString()
    {
        return AliasingPredecessor.IsValid 
            ? $"[Pass {PassIndex}] Aliasing Barrier: {AliasingPredecessor.Value}->{Resource.Value} Target: {TargetState.Layout}" 
            : $"[Pass {PassIndex}] Barrier: {Resource.Value} Target: {TargetState.Layout}";
    }
}

/// <summary>
/// Tracks the current state of a resource across passes during compilation.
/// </summary>
internal sealed class ResourceStateTracker
{
    public int resourceIndex;
    public ResourceBarrierData currentState;
    public int lastAccessPass = -1;

    public void Reset()
    {
        resourceIndex = -1;
        currentState = default;
        lastAccessPass = -1;
    }
}
