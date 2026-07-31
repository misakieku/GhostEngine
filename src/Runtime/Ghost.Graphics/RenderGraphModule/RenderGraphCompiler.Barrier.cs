using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Ghost.Graphics.RenderGraphModule;

[Flags]
internal enum BarrierFlags
{
    None = 0,
    FirstUsage = 1 << 0,
    Discard = 1 << 1
}

internal struct CompiledBarrier
{
    public Identifier<RGResource> resource;
    public ResourceBarrierData targetState;
    public Identifier<RGResource> aliasingPredecessor; // Invalid if not aliasing
    public BarrierFlags flags;
    public RGResourceType resourceType;

    public override readonly string ToString()
    {
        return aliasingPredecessor.IsValid
            ? $"Aliasing: {aliasingPredecessor.Value}->{resource.Value} -> {targetState.layout}"
            : $"Transition: {resource.Value} -> {targetState.layout}";
    }
}

internal partial class RenderGraphCompiler
{
    private int EmitBarriersForPass(
        RenderGraphPass pass,
        int passIdx,
        ref BufferWriter writer,
        AliasingPlan aliasingPlan)
    {
        var startPos = writer.Position;

        // Reserve opcode (1 byte) + barrier count (4 bytes)
        writer.Write(RGExecutionOpType.IssueBarriers);
        writer.Write(0); // Count placeholder

        var count = 0;
        count += EmitAliasingBarriers(pass, passIdx, ref writer, aliasingPlan);
        count += EmitImplicitTransitions(pass, passIdx, ref writer);

        if (count > 0)
        {
            var endPos = writer.Position;
            writer.Position = startPos + 1; // Backpatch count right after opcode
            writer.Write(count);
            writer.Position = endPos; // Restore position at end of stream
        }
        else
        {
            writer.Position = startPos; // Rewind if no barriers were emitted
        }

        return count;
    }

    private int EmitAliasingBarriers(
        RenderGraphPass pass,
        int passIdx,
        ref BufferWriter writer,
        AliasingPlan aliasingPlan)
    {
        var count = 0;

        for (var resType = 0; resType < (int)RGResourceType.Count; resType++)
        {
            var writeList = pass.resourceWrites[resType];
            for (var i = 0; i < writeList.Count; i++)
            {
                var id = writeList[i];
                ref readonly var resource = ref _resources.GetResource(id);

                if (resource.isImported)
                {
                    continue;
                }

                if (resource.firstUsePass == pass.index)
                {
                    var placedIndex = aliasingPlan.GetPlacedResourceIndex(id.Value);
                    if (placedIndex >= 0)
                    {
                        var placed = aliasingPlan.GetPlacedResource(placedIndex);

                        if (placed.IsSuccess && placed.Value.aliasedLogicalResources.Count > 1)
                        {
                            Identifier<RGResource> resourceBefore = default;
                            var mostRecentLastUse = -1;

                            foreach (var otherLogicalIndex in placed.Value.aliasedLogicalResources)
                            {
                                if (otherLogicalIndex != id.Value)
                                {
                                    var otherResource = _resources.GetResourceByIndex(otherLogicalIndex);

                                    if (otherResource.lastUsePass < pass.index &&
                                        otherResource.lastUsePass > mostRecentLastUse)
                                    {
                                        mostRecentLastUse = otherResource.lastUsePass;
                                        resourceBefore = new Identifier<RGResource>(otherLogicalIndex);
                                    }
                                }
                            }

                            if (mostRecentLastUse >= 0)
                            {
                                var targetState = new ResourceBarrierData(BarrierLayout.Undefined, BarrierAccess.NoAccess, BarrierSync.None);
                                var barrier = new CompiledBarrier
                                {
                                    resource = id,
                                    targetState = targetState,
                                    aliasingPredecessor = resourceBefore,
                                    flags = BarrierFlags.FirstUsage | BarrierFlags.Discard,
                                    resourceType = resource.type
                                };
                                writer.Write(barrier);
                                count++;
                            }
                        }
                    }
                }
            }
        }

        return count;
    }

    private int EmitImplicitTransitions(
        RenderGraphPass pass,
        int passIdx,
        ref BufferWriter writer)
    {
        var count = 0;

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

                if (isExplicitlyHandled)
                {
                    continue;
                }

                var targetState = GetBufferReadBarrierData(handle, pass, (RGResourceType)i);
                count += AddTransition(handle, targetState, ref writer);
            }
        }

        // Compile transitions based on pass type
        switch (pass.type)
        {
            case RenderPassType.Raster:
                for (var i = 0; i <= pass.maxColorIndex; i++)
                {
                    if (pass.colorAccess[i].id.IsValid)
                    {
                        var usage = pass.colorAccess[i].usage;
                        var targetState = new ResourceBarrierData(usage.layout, usage.access, usage.sync);
                        count += AddTransition(pass.colorAccess[i].id.AsResource(), targetState, ref writer);
                    }
                }

                if (pass.depthAccess.id.IsValid)
                {
                    var usage = pass.depthAccess.usage;
                    var targetState = new ResourceBarrierData(usage.layout, usage.access, usage.sync);
                    count += AddTransition(pass.depthAccess.id.AsResource(), targetState, ref writer);
                }

                var uavState = new ResourceBarrierData(BarrierLayout.UnorderedAccess, BarrierAccess.UnorderedAccess, BarrierSync.AllShading);
                for (var i = 0; i < pass.randomAccess.Count; i++)
                {
                    count += AddTransition(pass.randomAccess[i], uavState, ref writer);
                }
                break;

            case RenderPassType.Compute:
                var computeUavState = new ResourceBarrierData(BarrierLayout.UnorderedAccess, BarrierAccess.UnorderedAccess, BarrierSync.ComputeShading);
                for (var i = 0; i < (int)RGResourceType.Count; i++)
                {
                    var writeList = pass.resourceWrites[i];
                    for (var j = 0; j < writeList.Count; j++)
                    {
                        count += AddTransition(writeList[j], computeUavState, ref writer);
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
                        count += AddTransition(writeList[j], rtState, ref writer);
                    }
                }

                var unsafeUavState = new ResourceBarrierData(BarrierLayout.UnorderedAccess, BarrierAccess.UnorderedAccess, BarrierSync.AllShading);
                for (var i = 0; i < pass.randomAccess.Count; i++)
                {
                    count += AddTransition(pass.randomAccess[i], unsafeUavState, ref writer);
                }
                break;
        }

        return count;
    }

    private int AddTransition(Identifier<RGResource> id, ResourceBarrierData targetState, ref BufferWriter writer)
    {
        ref readonly var resource = ref _resources.GetResource(id);
        var barrier = new CompiledBarrier
        {
            resource = id,
            targetState = targetState,
            aliasingPredecessor = Identifier<RGResource>.Invalid,
            flags = BarrierFlags.None,
            resourceType = resource.type
        };
        writer.Write(barrier);
        return 1;
    }

    private ResourceBarrierData GetBufferReadBarrierData(
        Identifier<RGResource> handle,
        RenderGraphPass pass,
        RGResourceType resourceType)
    {
        if (resourceType == RGResourceType.Texture)
        {
            return new ResourceBarrierData(BarrierLayout.ShaderResource, BarrierAccess.ShaderResource, BarrierSync.PixelShading | BarrierSync.NonPixelShading);
        }

        var sync = BarrierSync.PixelShading | BarrierSync.NonPixelShading;
        var access = BarrierAccess.ShaderResource;

        ref readonly var resource = ref _resources.GetResource(handle);
        if (resource.bufferDesc.Usage.HasFlag(BufferUsage.IndirectArgument))
        {
            sync = BarrierSync.ExecuteIndirect;
            access = BarrierAccess.IndirectArgument;
        }

        return new ResourceBarrierData(BarrierLayout.Undefined, access, sync);
    }

    public static bool RequiresBarrierBetweenPasses(
        int passA,
        int passB,
        List<RenderGraphPass> compiledPasses,
        RenderGraphResourceRegistry resources,
        AliasingPlan aliasingPlan)
    {
        var laterPass = compiledPasses[passB];

        using var scope = AllocationManager.CreateStackScope();
        using var renderTargets = new UnsafeHashSet<Identifier<RGResource>>(laterPass.maxColorIndex + 1, scope.AllocationHandle);

        for (var i = 0; i <= laterPass.maxColorIndex; i++)
        {
            if (!laterPass.colorAccess[i].id.IsInvalid)
            {
                renderTargets.Add(laterPass.colorAccess[i].id.AsResource());
            }
        }

        if (!laterPass.depthAccess.id.IsInvalid)
        {
            renderTargets.Add(laterPass.depthAccess.id.AsResource());
        }

        for (var resType = 0; resType < (int)RGResourceType.Count; resType++)
        {
            var writeList = laterPass.resourceWrites[resType];
            for (var i = 0; i < writeList.Count; i++)
            {
                var id = writeList[i];
                ref readonly var resource = ref resources.GetResource(id);
                if (!resource.isImported && resource.firstUsePass == laterPass.index)
                {
                    var placedIndex = aliasingPlan.GetPlacedResourceIndex(id.Value);
                    if (placedIndex >= 0)
                    {
                        var placed = aliasingPlan.GetPlacedResource(placedIndex);
                        if (placed.IsSuccess && placed.Value.aliasedLogicalResources.Count > 1)
                        {
                            if (renderTargets.Contains(id))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
        }

        return false;
    }
}
