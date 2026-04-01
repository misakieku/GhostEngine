using Ghost.Core;
using Ghost.Graphics.RHI;

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
            ? $"[Pass {PassIndex}] Aliasing Barrier: {AliasingPredecessor.Value}->{Resource.Value} Target: {TargetState.layout}"
            : $"[Pass {PassIndex}] Barrier: {Resource.Value} Target: {TargetState.layout}";
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

/// <summary>
/// Represents a compiled barrier with only the target state.
/// The before state is always queried from ResourceManager at execution time.
/// </summary>
internal struct CompiledBarrier
{
    public int PassIndex;
    public Identifier<RGResource> Resource;
    public ResourceBarrierData TargetState;
    public Identifier<RGResource> AliasingPredecessor; // Invalid if not aliasing
    public BarrierFlags Flags;
    public RenderGraphResourceType ResourceType;

    public override readonly string ToString()
    {
        return AliasingPredecessor.IsValid
            ? $"[Pass {PassIndex}] Aliasing: {AliasingPredecessor.Value}->{Resource.Value} -> {TargetState.layout}"
            : $"[Pass {PassIndex}] Transition: {Resource.Value} -> {TargetState.layout}";
    }
}

/// <summary>
/// Static class containing barrier compilation logic.
/// Compiles barriers at graph compilation time, storing only target states.
/// </summary>
internal static class RenderGraphBarriers
{
    /// <summary>
    /// Compiles all barriers needed for execution, storing only target states.
    /// Barriers include aliasing barriers and implicit state transitions.
    /// </summary>
    public static void CompileBarriers(
        List<RenderGraphPassBase> compiledPasses,
        List<CompiledBarrier> compiledBarriers,
        RenderGraphResourceRegistry resources,
        ResourceAliasingManager aliasingManager)
    {
        compiledBarriers.Clear();

        // Process each compiled pass in order
        for (var passIdx = 0; passIdx < compiledPasses.Count; passIdx++)
        {
            var pass = compiledPasses[passIdx];

            // 1. Insert aliasing barriers for resources that reuse physical memory
            InsertAliasingBarriers(pass, passIdx, compiledBarriers, resources, aliasingManager);

            // 2. Compile implicit transitions for all resources accessed by this pass
            CompileImplicitTransitions(pass, passIdx, compiledBarriers, resources);
        }
    }

    /// <summary>
    /// Inserts aliasing barriers when a placed resource is reused.
    /// </summary>
    private static void InsertAliasingBarriers(
        RenderGraphPassBase pass,
        int passIdx,
        List<CompiledBarrier> compiledBarriers,
        RenderGraphResourceRegistry resources,
        ResourceAliasingManager aliasingManager)
    {
        // Check all resources written by this pass (both textures and buffers)
        for (var resType = 0; resType < (int)RenderGraphResourceType.Count; resType++)
        {
            var writeList = pass.resourceWrites[resType];
            for (var i = 0; i < writeList.Count; i++)
            {
                var id = writeList[i];
                var resource = resources.GetResource(id);

                // Skip imported resources
                if (resource.isImported)
                {
                    continue;
                }

                // Check if this is the first use of this logical resource
                if (resource.firstUsePass == pass.index)
                {
                    // Get the placed resource
                    var placedIndex = aliasingManager.GetPlacedResourceIndex(id.Value);
                    if (placedIndex >= 0)
                    {
                        var placed = aliasingManager.GetPlacedResource(placedIndex);

                        // If this placed resource has multiple aliased resources,
                        // we need an aliasing barrier when switching between them
                        if (placed != null && placed.aliasedLogicalResources.Count > 1)
                        {
                            // Find the resource that used this placed memory most recently before this pass
                            Identifier<RGResource> resourceBefore = default;
                            var mostRecentLastUse = -1;

                            foreach (var otherLogicalIndex in placed.aliasedLogicalResources)
                            {
                                if (otherLogicalIndex != id.Value)
                                {
                                    // Get resource by global index
                                    var otherResource = resources.GetResourceByIndex(otherLogicalIndex);

                                    // Check if this resource finished before our resource starts
                                    if (otherResource.lastUsePass < pass.index &&
                                        otherResource.lastUsePass > mostRecentLastUse)
                                    {
                                        mostRecentLastUse = otherResource.lastUsePass;
                                        resourceBefore = new Identifier<RGResource>(otherLogicalIndex);
                                    }
                                }
                            }

                            // If we found a previous resource, insert aliasing barrier
                            if (mostRecentLastUse >= 0)
                            {
                                // Aliasing Requirement: Transition to Undefined, Sync with Predecessor
                                var targetState = new ResourceBarrierData(BarrierLayout.Undefined, BarrierAccess.NoAccess, BarrierSync.None);
                                var barrier = new CompiledBarrier
                                {
                                    PassIndex = passIdx,
                                    Resource = id,
                                    TargetState = targetState,
                                    AliasingPredecessor = resourceBefore,
                                    Flags = BarrierFlags.FirstUsage | BarrierFlags.Discard,
                                    ResourceType = resource.type
                                };
                                compiledBarriers.Add(barrier);
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Compiles implicit state transitions for all resources accessed by a pass.
    /// Stores only the target state - the before state will be queried from ResourceManager at execution time.
    /// </summary>
    private static void CompileImplicitTransitions(
        RenderGraphPassBase pass,
        int passIdx,
        List<CompiledBarrier> compiledBarriers,
        RenderGraphResourceRegistry resources)
    {
        // Helper to add a compiled barrier for a resource transition
        void AddTransition(Identifier<RGResource> id, ResourceBarrierData targetState)
        {
            var resource = resources.GetResource(id);
            var barrier = new CompiledBarrier
            {
                PassIndex = passIdx,
                Resource = id,
                TargetState = targetState,
                AliasingPredecessor = Identifier<RGResource>.Invalid,
                Flags = BarrierFlags.None,
                ResourceType = resource.type
            };
            compiledBarriers.Add(barrier);
        }

        // Compile transitions for read resources
        for (var i = 0; i < (int)RenderGraphResourceType.Count; i++)
        {
            var readList = pass.resourceReads[i];
            for (var j = 0; j < readList.Count; j++)
            {
                var handle = readList[j];

                bool isExplicitlyHandled = false;
                if (pass.type == RenderPassType.Raster)
                {
                    for (var c = 0; c <= pass.maxColorIndex; c++)
                    {
                        if (pass.colorAccess[c].id.IsValid && pass.colorAccess[c].id.Value == handle.Value)
                        {
                            isExplicitlyHandled = true;
                        }
                    }

                    if (!isExplicitlyHandled && pass.depthAccess.id.IsValid && pass.depthAccess.id.Value == handle.Value)
                    {
                        isExplicitlyHandled = true;
                    }
                }

                if (!isExplicitlyHandled)
                {
                    for (var u = 0; u < pass.randomAccess.Count; u++)
                    {
                        if (pass.randomAccess[u].Value == handle.Value)
                        {
                            isExplicitlyHandled = true;
                        }
                    }
                }

                // Skip generic SRV barrier if handled specifically
                if (isExplicitlyHandled)
                {
                    continue;
                }

                var targetState = GetBufferReadBarrierData(handle, pass, (RenderGraphResourceType)i, resources);
                AddTransition(handle, targetState);
            }
        }

        // Compile transitions based on pass type
        switch (pass.type)
        {
            case RenderPassType.Raster:
                // Color attachments
                for (var i = 0; i <= pass.maxColorIndex; i++)
                {
                    if (pass.colorAccess[i].id.IsValid)
                    {
                        var usage = pass.colorAccess[i].usage;
                        var targetState = new ResourceBarrierData(usage.layout, usage.access, usage.sync);
                        AddTransition(pass.colorAccess[i].id.AsResource(), targetState);
                    }
                }

                // Depth attachment
                if (pass.depthAccess.id.IsValid)
                {
                    var usage = pass.depthAccess.usage;
                    var targetState = new ResourceBarrierData(usage.layout, usage.access, usage.sync);
                    AddTransition(pass.depthAccess.id.AsResource(), targetState);
                }

                // UAV resources
                var uavState = new ResourceBarrierData(BarrierLayout.UnorderedAccess, BarrierAccess.UnorderedAccess, BarrierSync.AllShading);
                for (var i = 0; i < pass.randomAccess.Count; i++)
                {
                    AddTransition(pass.randomAccess[i], uavState);
                }
                break;

            case RenderPassType.Compute:
                var computeUavState = new ResourceBarrierData(BarrierLayout.UnorderedAccess, BarrierAccess.UnorderedAccess, BarrierSync.ComputeShading);
                for (var i = 0; i < (int)RenderGraphResourceType.Count; i++)
                {
                    var writeList = pass.resourceWrites[i];
                    for (var j = 0; j < writeList.Count; j++)
                    {
                        AddTransition(writeList[j], computeUavState);
                    }
                }
                break;

            case RenderPassType.Unsafe:
                var rtState = new ResourceBarrierData(BarrierLayout.RenderTarget, BarrierAccess.RenderTarget, BarrierSync.RenderTarget);
                for (var i = 0; i < (int)RenderGraphResourceType.Count; i++)
                {
                    var writeList = pass.resourceWrites[i];
                    for (var j = 0; j < writeList.Count; j++)
                    {
                        AddTransition(writeList[j], rtState);
                    }
                }

                var unsafeUavState = new ResourceBarrierData(BarrierLayout.UnorderedAccess, BarrierAccess.UnorderedAccess, BarrierSync.AllShading);
                for (var i = 0; i < pass.randomAccess.Count; i++)
                {
                    AddTransition(pass.randomAccess[i], unsafeUavState);
                }
                break;
        }
    }

    private static ResourceBarrierData GetBufferReadBarrierData(
        Identifier<RGResource> handle,
        RenderGraphPassBase pass,
        RenderGraphResourceType resourceType,
        RenderGraphResourceRegistry resources)
    {
        if (resourceType == RenderGraphResourceType.Texture)
        {
            return new ResourceBarrierData(BarrierLayout.ShaderResource, BarrierAccess.ShaderResource, BarrierSync.PixelShading | BarrierSync.NonPixelShading);
        }

        var sync = BarrierSync.PixelShading | BarrierSync.NonPixelShading;
        var access = BarrierAccess.ShaderResource;

        var resource = resources.GetResource(handle);
        if (resource.bufferDesc.Usage.HasFlag(BufferUsage.IndirectArgument))
        {
            sync = BarrierSync.ExecuteIndirect;
            access = BarrierAccess.IndirectArgument;
        }

        return new ResourceBarrierData(BarrierLayout.Undefined, access, sync);
    }
}
