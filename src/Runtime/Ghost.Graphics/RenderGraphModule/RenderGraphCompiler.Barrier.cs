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

internal enum PassResourceUsagePriority : byte
{
    None,
    GenericRead,
    GenericWrite,
    Explicit
}

internal struct ResolvedPassResourceUsage
{
    public Identifier<RGResource> resource;
    public RGResourceType resourceType;
    public bool reads;
    public bool writes;
    public PassResourceUsageClass usageClass;
    public PassResourceUsagePriority priority;
    public ResourceBarrierData targetState;
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

internal unsafe partial class RenderGraphCompiler
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

        var count = EmitImplicitTransitions(pass, ref writer, aliasingPlan);

        if (count > 0)
        {
            var endPos = writer.Position;
            writer.Position = startPos + sizeof(RGExecutionOpType); // Backpatch count right after opcode
            writer.Write(count);
            writer.Position = endPos; // Restore position at end of stream
        }
        else
        {
            writer.Position = startPos; // Rewind if no barriers were emitted
        }

        return count;
    }

    private bool TryGetAliasingPredecessor(
        RenderGraphPass pass,
        Identifier<RGResource> resourceId,
        AliasingPlan aliasingPlan,
        out Identifier<RGResource> predecessor)
    {
        predecessor = Identifier<RGResource>.Invalid;
        ref readonly var resource = ref _resourceRegistry.GetResource(resourceId);
        if (resource.isImported || resource.firstUsePass != pass.index)
        {
            return false;
        }

        var placedIndex = aliasingPlan.GetPlacedResourceIndex(resourceId.Value);
        if (placedIndex < 0)
        {
            return false;
        }

        var placed = aliasingPlan.GetPlacedResource(placedIndex);
        if (placed.IsFailure || placed.Value.aliasedLogicalResources.Count <= 1)
        {
            return false;
        }

        var mostRecentLastUse = -1;
        foreach (var otherLogicalIndex in placed.Value.aliasedLogicalResources)
        {
            if (otherLogicalIndex == resourceId.Value)
            {
                continue;
            }

            ref readonly var otherResource = ref _resourceRegistry.GetResourceByIndex(otherLogicalIndex);
            if (otherResource.lastUsePass < pass.index && otherResource.lastUsePass > mostRecentLastUse)
            {
                mostRecentLastUse = otherResource.lastUsePass;
                predecessor = new Identifier<RGResource>(otherLogicalIndex);
            }
        }

        return predecessor.IsValid;
    }

    private static void ApplyUsage(
        ref ResolvedPassResourceUsage usage,
        Identifier<RGResource> resource,
        RGResourceType resourceType,
        bool reads,
        bool writes,
        PassResourceUsageClass usageClass,
        PassResourceUsagePriority priority,
        ResourceBarrierData targetState)
    {
        usage.resource = resource;
        usage.resourceType = resourceType;
        usage.reads |= reads;
        usage.writes |= writes;

        if (usageClass == PassResourceUsageClass.None)
        {
            return;
        }

        if (usage.usageClass == usageClass &&
            usage.targetState.layout == targetState.layout &&
            usage.targetState.access == targetState.access)
        {
            usage.targetState.sync |= targetState.sync;
            if (priority > usage.priority)
            {
                usage.priority = priority;
            }
            return;
        }

        if (priority >= usage.priority)
        {
            usage.usageClass = usageClass;
            usage.priority = priority;
            usage.targetState = targetState;
        }
    }

    private int EmitImplicitTransitions(RenderGraphPass pass, ref BufferWriter writer, AliasingPlan aliasingPlan)
    {
        var resourceCount = _resourceRegistry.ResourceCount;
        if (resourceCount == 0)
        {
            return 0;
        }

        using var scope = AllocationManager.CreateStackScope();
        using var usages = new UnsafeArray<ResolvedPassResourceUsage>(resourceCount, scope.AllocationHandle, AllocationOption.Clear);

        for (var resourceType = 0; resourceType < (int)RGResourceType.Count; resourceType++)
        {
            foreach (var resource in pass.resourceReads[resourceType])
            {
                var targetState = GetBufferReadBarrierData(resource, (RGResourceType)resourceType);
                var usageClass = targetState.access == BarrierAccess.IndirectArgument
                    ? PassResourceUsageClass.IndirectArgument
                    : PassResourceUsageClass.ShaderRead;
                ref var usage = ref usages[resource.Value];
                ApplyUsage(
                    ref usage,
                    resource,
                    (RGResourceType)resourceType,
                    reads: true,
                    writes: false,
                    usageClass,
                    PassResourceUsagePriority.GenericRead,
                    targetState);
            }
        }

        for (var resourceType = 0; resourceType < (int)RGResourceType.Count; resourceType++)
        {
            foreach (var resource in pass.resourceWrites[resourceType])
            {
                ref var usage = ref usages[resource.Value];
                switch (pass.type)
                {
                    case RenderPassType.Compute:
                        ApplyUsage(
                            ref usage,
                            resource,
                            (RGResourceType)resourceType,
                            reads: false,
                            writes: true,
                            PassResourceUsageClass.UnorderedAccess,
                            PassResourceUsagePriority.GenericWrite,
                            new ResourceBarrierData(BarrierLayout.UnorderedAccess, BarrierAccess.UnorderedAccess, BarrierSync.ComputeShading));
                        break;

                    default:
                        ApplyUsage(
                            ref usage,
                            resource,
                            (RGResourceType)resourceType,
                            reads: false,
                            writes: true,
                            PassResourceUsageClass.None,
                            PassResourceUsagePriority.None,
                            default);
                        break;
                }
            }
        }

        if (pass.type == RenderPassType.Raster)
        {
            for (var i = 0; i <= pass.maxColorIndex; i++)
            {
                ref readonly var access = ref pass.colorAccess[i];
                if (access.id.IsValid)
                {
                    var resource = access.id.AsResource();
                    ref var usage = ref usages[resource.Value];
                    ApplyUsage(
                        ref usage,
                        resource,
                        RGResourceType.Texture,
                        (access.accessFlags & AccessFlags.Read) != 0,
                        (access.accessFlags & AccessFlags.Write) != 0,
                        PassResourceUsageClass.ColorAttachment,
                        PassResourceUsagePriority.Explicit,
                        access.usage);
                }
            }

            if (pass.depthAccess.id.IsValid)
            {
                var resource = pass.depthAccess.id.AsResource();
                var usageClass = pass.depthAccess.usage.layout == BarrierLayout.DepthStencilWrite
                    ? PassResourceUsageClass.DepthWrite
                    : PassResourceUsageClass.DepthRead;
                ref var usage = ref usages[resource.Value];
                ApplyUsage(
                    ref usage,
                    resource,
                    RGResourceType.Texture,
                    (pass.depthAccess.accessFlags & AccessFlags.Read) != 0,
                    (pass.depthAccess.accessFlags & AccessFlags.Write) != 0,
                    usageClass,
                    PassResourceUsagePriority.Explicit,
                    pass.depthAccess.usage);
            }
        }

        var renderTargetState = new ResourceBarrierData(BarrierLayout.RenderTarget, BarrierAccess.RenderTarget, BarrierSync.RenderTarget);
        foreach (var resource in pass.renderTargetWrites)
        {
            ref var usage = ref usages[resource.Value];
            ApplyUsage(
                ref usage,
                resource,
                RGResourceType.Texture,
                pass.resourceReads[(int)RGResourceType.Texture].Contains(resource),
                pass.resourceWrites[(int)RGResourceType.Texture].Contains(resource),
                PassResourceUsageClass.ColorAttachment,
                PassResourceUsagePriority.Explicit,
                renderTargetState);
        }

        var randomAccessState = new ResourceBarrierData(
            BarrierLayout.UnorderedAccess,
            BarrierAccess.UnorderedAccess,
            pass.type == RenderPassType.Compute ? BarrierSync.ComputeShading : BarrierSync.AllShading);
        foreach (var resource in pass.randomAccess)
        {
            ref readonly var resourceInfo = ref _resourceRegistry.GetResource(resource);
            ref var usage = ref usages[resource.Value];
            ApplyUsage(
                ref usage,
                resource,
                resourceInfo.type,
                reads: true,
                writes: true,
                PassResourceUsageClass.UnorderedAccess,
                PassResourceUsagePriority.Explicit,
                randomAccessState);
        }

        var count = 0;
        for (var resourceIndex = 0; resourceIndex < resourceCount; resourceIndex++)
        {
            ref readonly var usage = ref usages[resourceIndex];
            if (usage.usageClass != PassResourceUsageClass.None)
            {
                var aliasingPredecessor = Identifier<RGResource>.Invalid;
                var flags = BarrierFlags.None;
                if (TryGetAliasingPredecessor(pass, usage.resource, aliasingPlan, out aliasingPredecessor))
                {
                    flags = BarrierFlags.FirstUsage | BarrierFlags.Discard;
                }

                count += AddTransition(usage.resource, usage.targetState, aliasingPredecessor, flags, ref writer);
            }
        }

        return count;
    }

    private int AddTransition(
        Identifier<RGResource> id,
        ResourceBarrierData targetState,
        Identifier<RGResource> aliasingPredecessor,
        BarrierFlags flags,
        ref BufferWriter writer)
    {
        ref readonly var resource = ref _resourceRegistry.GetResource(id);
        var barrier = new CompiledBarrier
        {
            resource = id,
            targetState = targetState,
            aliasingPredecessor = aliasingPredecessor,
            flags = flags,
            resourceType = resource.type
        };
        writer.Write(barrier);
        return 1;
    }

    private ResourceBarrierData GetBufferReadBarrierData(Identifier<RGResource> handle, RGResourceType resourceType)
    {
        if (resourceType == RGResourceType.Texture)
        {
            return new ResourceBarrierData(BarrierLayout.ShaderResource, BarrierAccess.ShaderResource, BarrierSync.PixelShading | BarrierSync.NonPixelShading);
        }

        var sync = BarrierSync.PixelShading | BarrierSync.NonPixelShading;
        var access = BarrierAccess.ShaderResource;

        ref readonly var resource = ref _resourceRegistry.GetResource(handle);
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
        List<RenderGraphPass> passes,
        ReadOnlySpan<int> compiledPasses,
        RenderGraphResourceRegistry resources,
        AliasingPlan aliasingPlan)
    {
        var laterPass = passes[passB];

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
            foreach (var id in writeList)
            {
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
