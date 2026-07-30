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

internal sealed class ResourceStateTracker
{
    public int resourceIndex;
    public int lastAccessPass = -1;
    public ResourceBarrierData currentState;

    public void Reset()
    {
        resourceIndex = -1;
        lastAccessPass = -1;
        currentState = default;
    }
}

internal struct CompiledBarrier
{
    public int passIndex;
    public Identifier<RGResource> resource;
    public ResourceBarrierData targetState;
    public Identifier<RGResource> aliasingPredecessor; // Invalid if not aliasing
    public BarrierFlags flags;
    public RGResourceType resourceType;

    public override readonly string ToString()
    {
        return aliasingPredecessor.IsValid
            ? $"[Pass {passIndex}] Aliasing: {aliasingPredecessor.Value}->{resource.Value} -> {targetState.layout}"
            : $"[Pass {passIndex}] Transition: {resource.Value} -> {targetState.layout}";
    }
}

internal static class RenderGraphBarriers
{
    public static void CompileBarriers(
        List<RenderGraphPass> compiledPasses,
        List<CompiledBarrier> compiledBarriers,
        RenderGraphResourceRegistry resources,
        AliasingPlan aliasingPlan)
    {
        compiledBarriers.Clear();

        // Process each compiled pass in order
        for (var passIdx = 0; passIdx < compiledPasses.Count; passIdx++)
        {
            var pass = compiledPasses[passIdx];

            // 1. Insert aliasing barriers for resources that reuse physical memory
            InsertAliasingBarriers(pass, passIdx, compiledBarriers, resources, aliasingPlan);

            // 2. Compile implicit transitions for all resources accessed by this pass
            CompileImplicitTransitions(pass, passIdx, compiledBarriers, resources);
        }
    }

    private static void InsertAliasingBarriers(
        RenderGraphPass pass,
        int passIdx,
        List<CompiledBarrier> compiledBarriers,
        RenderGraphResourceRegistry resources,
        AliasingPlan aliasingPlan)
    {
        // Check all resources written by this pass (both textures and buffers)
        for (var resType = 0; resType < (int)RGResourceType.Count; resType++)
        {
            var writeList = pass.resourceWrites[resType];
            for (var i = 0; i < writeList.Count; i++)
            {
                var id = writeList[i];
                ref readonly var resource = ref resources.GetResource(id);

                // Skip imported resources
                if (resource.isImported)
                {
                    continue;
                }

                // Check if this is the first use of this logical heap
                if (resource.firstUsePass == pass.index)
                {
                    // Get the placed heap
                    var placedIndex = aliasingPlan.GetPlacedResourceIndex(id.Value);
                    if (placedIndex >= 0)
                    {
                        var placed = aliasingPlan.GetPlacedResource(placedIndex);

                        // If this placed heap has multiple aliased resources,
                        // we need an aliasing barrier when switching between them
                        if (placed.IsSuccess && placed.Value.aliasedLogicalResources.Count > 1)
                        {
                            // Find the heap that used this placed memory most recently before this pass
                            Identifier<RGResource> resourceBefore = default;
                            var mostRecentLastUse = -1;

                            foreach (var otherLogicalIndex in placed.Value.aliasedLogicalResources)
                            {
                                if (otherLogicalIndex != id.Value)
                                {
                                    // Get heap by global index
                                    var otherResource = resources.GetResourceByIndex(otherLogicalIndex);

                                    // Check if this heap finished before our heap starts
                                    if (otherResource.lastUsePass < pass.index &&
                                        otherResource.lastUsePass > mostRecentLastUse)
                                    {
                                        mostRecentLastUse = otherResource.lastUsePass;
                                        resourceBefore = new Identifier<RGResource>(otherLogicalIndex);
                                    }
                                }
                            }

                            // If we found a previous heap, insert aliasing barrier
                            if (mostRecentLastUse >= 0)
                            {
                                // Aliasing Requirement: Transition to Undefined, Sync with Predecessor
                                var targetState = new ResourceBarrierData(BarrierLayout.Undefined, BarrierAccess.NoAccess, BarrierSync.None);
                                var barrier = new CompiledBarrier
                                {
                                    passIndex = passIdx,
                                    resource = id,
                                    targetState = targetState,
                                    aliasingPredecessor = resourceBefore,
                                    flags = BarrierFlags.FirstUsage | BarrierFlags.Discard,
                                    resourceType = resource.type
                                };
                                compiledBarriers.Add(barrier);
                            }
                        }
                    }
                }
            }
        }
    }

    private static void CompileImplicitTransitions(
        RenderGraphPass pass,
        int passIdx,
        List<CompiledBarrier> compiledBarriers,
        RenderGraphResourceRegistry resources)
    {
        // Helper to add a compiled barrier for a heap transition
        void AddTransition(Identifier<RGResource> id, ResourceBarrierData targetState)
        {
            ref readonly var resource = ref resources.GetResource(id);
            var barrier = new CompiledBarrier
            {
                passIndex = passIdx,
                resource = id,
                targetState = targetState,
                aliasingPredecessor = Identifier<RGResource>.Invalid,
                flags = BarrierFlags.None,
                resourceType = resource.type
            };
            compiledBarriers.Add(barrier);
        }

        // Compile transitions for read resources
        for (var i = 0; i < (int)RGResourceType.Count; i++)
        {
            var readList = pass.resourceReads[i];
            for (var j = 0; j < readList.Count; j++)
            {
                var handle = readList[j];

                var isExplicitlyHandled = false;
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

                var targetState = GetBufferReadBarrierData(handle, pass, (RGResourceType)i, resources);
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
                for (var i = 0; i < (int)RGResourceType.Count; i++)
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
                for (var i = 0; i < (int)RGResourceType.Count; i++)
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
        RenderGraphPass pass,
        RGResourceType resourceType,
        RenderGraphResourceRegistry resources)
    {
        if (resourceType == RGResourceType.Texture)
        {
            return new ResourceBarrierData(BarrierLayout.ShaderResource, BarrierAccess.ShaderResource, BarrierSync.PixelShading | BarrierSync.NonPixelShading);
        }

        var sync = BarrierSync.PixelShading | BarrierSync.NonPixelShading;
        var access = BarrierAccess.ShaderResource;

        ref readonly var resource = ref resources.GetResource(handle);
        if (resource.bufferDesc.Usage.HasFlag(BufferUsage.IndirectArgument))
        {
            sync = BarrierSync.ExecuteIndirect;
            access = BarrierAccess.IndirectArgument;
        }

        return new ResourceBarrierData(BarrierLayout.Undefined, access, sync);
    }
}
