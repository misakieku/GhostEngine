using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Runtime.CompilerServices;

namespace Ghost.Graphics.RenderGraphModule;

[Flags]
internal enum BarrierFlags
{
    None = 0,
    FirstUsage = 1 << 0,
    Discard = 1 << 1,
    ExplicitSource = 1 << 2,
    Force = 1 << 3,
    QueueRelease = 1 << 4,
    QueueAcquire = 1 << 5
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
    public bool discard;
    public PassResourceUsageClass usageClass;
    public PassResourceUsagePriority priority;
    public ResourceBarrierData targetState;
}

internal struct CompiledBarrier
{
    public Identifier<RGResource> resource;
    public ResourceBarrierData sourceState;
    public ResourceBarrierData handoffState;
    public ResourceBarrierData targetState;
    public Identifier<RGResource> aliasingPredecessor; // Invalid if not aliasing
    public BarrierFlags flags;
    public RGResourceType resourceType;
    public CommandQueueType sourceQueue;
    public CommandQueueType destinationQueue;

    public override readonly string ToString()
    {
        return aliasingPredecessor.IsValid
            ? $"Aliasing: {aliasingPredecessor.Value}->{resource.Value} -> {targetState.layout}"
            : $"Transition: {resource.Value} -> {targetState.layout}";
    }
}

// TODO: Split barrier.
internal unsafe partial class RenderGraphCompiler
{
    private const BarrierSync ALL_SHADING_STAGES = BarrierSync.AllShading
        | BarrierSync.VertexShading
        | BarrierSync.PixelShading
        | BarrierSync.ComputeShading
        | BarrierSync.Raytracing
        | BarrierSync.NonPixelShading;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsSyncSatisfied(BarrierSync currentSync, BarrierSync requiredSync)
    {
        if (currentSync == requiredSync)
        {
            return true;
        }

        if ((currentSync & BarrierSync.All) != 0)
        {
            return true;
        }

        if ((currentSync & BarrierSync.AllShading) != 0 && (requiredSync & ~ALL_SHADING_STAGES) == 0)
        {
            return true;
        }

        if ((currentSync & BarrierSync.NonPixelShading) != 0 && (requiredSync & ~BarrierSync.VertexShading) == 0)
        {
            return true;
        }

        return (currentSync & requiredSync) == requiredSync;
    }
    private int EmitPassPrologueBarriers(
        ReadOnlySpan<ResolvedPassResourceUsage> usages,
        int scheduleIndex,
        CommandQueueType effectiveQueue,
        ref BufferWriter writer,
        ref AliasingPlan aliasingPlan,
        RenderGraphResourceOrdering resourceOrdering,
        Span<CompiledResourceState> resourceStates,
        ReadOnlySpan<QueueHandoff> handoffs)
    {
        var startPos = writer.Position;

        // Reserve opcode (1 byte) + barrier count (4 bytes)
        writer.Write(RGExecutionOpType.IssueBarriers);
        writer.Write(0); // Count placeholder

        var count = WriteQueueHandoffBarriers(ref writer, handoffs, scheduleIndex, release: false);
        count += EmitImplicitTransitions(
            usages,
            scheduleIndex,
            effectiveQueue,
            ref writer,
            ref aliasingPlan,
            resourceOrdering,
            resourceStates,
            handoffs);

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

    private int EmitPassPrologueBarriersForMergedPasses(
        ReadOnlySpan<ResolvedPassResourceUsage> allUsages,
        ReadOnlySpan<PassResourceUsageRange> usageRanges,
        int startScheduleIndex,
        int passCount,
        ReadOnlySpan<CommandQueueType> effectiveQueues,
        ref BufferWriter writer,
        ref AliasingPlan aliasingPlan,
        RenderGraphResourceOrdering resourceOrdering,
        Span<CompiledResourceState> resourceStates,
        ReadOnlySpan<QueueHandoff> handoffs)
    {
        var startPos = writer.Position;

        writer.Write(RGExecutionOpType.IssueBarriers);
        writer.Write(0);

        var count = 0;
        for (var i = 0; i < passCount; i++)
        {
            var mergedScheduleIndex = startScheduleIndex + i;
            count += WriteQueueHandoffBarriers(ref writer, handoffs, mergedScheduleIndex, release: false);

            ref readonly var usageRange = ref usageRanges[mergedScheduleIndex];
            count += EmitImplicitTransitions(
                allUsages.Slice(usageRange.start, usageRange.count),
                mergedScheduleIndex,
                effectiveQueues[mergedScheduleIndex],
            ref writer,
            ref aliasingPlan,
            resourceOrdering,
            resourceStates,
            handoffs);
        }

        if (count > 0)
        {
            var endPos = writer.Position;
            writer.Position = startPos + sizeof(RGExecutionOpType);
            writer.Write(count);
            writer.Position = endPos;
        }
        else
        {
            writer.Position = startPos;
        }

        return count;
    }

    private bool TryGetAliasingPredecessor(int scheduleIndex, Identifier<RGResource> resourceId, ref AliasingPlan aliasingPlan, RenderGraphResourceOrdering resourceOrdering, out Identifier<RGResource> predecessor)
    {
        predecessor = Identifier<RGResource>.Invalid;
        ref readonly var resource = ref _resourceRegistry.GetResource(resourceId);
        if (resource.isImported || resource.inPlacePredecessor.IsValid || !resourceOrdering.IsFirstUse(resourceId.Value, scheduleIndex))
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
            if (otherLogicalIndex == resourceId.Value
                || !resourceOrdering.AllUsesHappenBefore(otherLogicalIndex, resourceId.Value))
            {
                continue;
            }

            var otherLastUse = resourceOrdering.GetLastUseScheduleIndex(otherLogicalIndex);
            if (otherLastUse > mostRecentLastUse)
            {
                mostRecentLastUse = otherLastUse;
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
        bool discard,
        PassResourceUsageClass usageClass,
        PassResourceUsagePriority priority,
        ResourceBarrierData targetState)
    {
        usage.resource = resource;
        usage.resourceType = resourceType;
        usage.reads |= reads;
        usage.writes |= writes;
        usage.discard |= discard;

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

    private void ResolvePassResourceUsages(RenderGraphPass pass, Span<ResolvedPassResourceUsage> usages)
    {
        usages.Clear();

        for (var resourceType = 0; resourceType < (int)RGResourceType.Count; resourceType++)
        {
            foreach (var resource in pass.resourceReads[resourceType])
            {
                var targetState = GetBufferReadBarrierData(resource, (RGResourceType)resourceType, pass.type);
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
                    discard: pass.resourceDiscards[resourceType].Contains(resource),
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
                var discard = pass.resourceDiscards[resourceType].Contains(resource);

                if ((RGResourceType)resourceType == RGResourceType.Buffer)
                {
                    ref readonly var resourceInfo = ref _resourceRegistry.GetResource(resource);
                    if (resourceInfo.bufferDesc.Usage.HasFlag(BufferUsage.AccelerationStructure))
                    {
                        ApplyUsage(
                            ref usage,
                            resource,
                            RGResourceType.Buffer,
                            reads: false,
                            writes: true,
                            discard: discard,
                            PassResourceUsageClass.UnorderedAccess,
                            PassResourceUsagePriority.Explicit,
                            new ResourceBarrierData(BarrierLayout.Undefined, BarrierAccess.RaytracingAccelerationStructureWrite, BarrierSync.BuildRaytracingAccelerationStructure));
                        continue;
                    }
                }

                switch (pass.type)
                {
                    case RenderPassType.Compute:
                        ApplyUsage(
                            ref usage,
                            resource,
                            (RGResourceType)resourceType,
                            reads: false,
                            writes: true,
                            discard: discard,
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
                            discard: discard,
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
                        (access.accessFlags & AccessFlags.Discard) != 0 || pass.resourceDiscards[(int)RGResourceType.Texture].Contains(resource),
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
                    (pass.depthAccess.accessFlags & AccessFlags.Discard) != 0 || pass.resourceDiscards[(int)RGResourceType.Texture].Contains(resource),
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
                pass.resourceDiscards[(int)RGResourceType.Texture].Contains(resource),
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

            var targetState = randomAccessState;
            if (resourceInfo.type == RGResourceType.Buffer && resourceInfo.bufferDesc.Usage.HasFlag(BufferUsage.AccelerationStructure))
            {
                targetState = new ResourceBarrierData(BarrierLayout.Undefined, BarrierAccess.RaytracingAccelerationStructureWrite, BarrierSync.BuildRaytracingAccelerationStructure);
            }

            ApplyUsage(
                ref usage,
                resource,
                resourceInfo.type,
                reads: true,
                writes: true,
                discard: pass.resourceDiscards[(int)resourceInfo.type].Contains(resource),
                PassResourceUsageClass.UnorderedAccess,
                PassResourceUsagePriority.Explicit,
                targetState);
        }
    }

    private int EmitImplicitTransitions(
        ReadOnlySpan<ResolvedPassResourceUsage> usages,
        int scheduleIndex,
        CommandQueueType effectiveQueue,
        ref BufferWriter writer,
        ref AliasingPlan aliasingPlan,
        RenderGraphResourceOrdering resourceOrdering,
        Span<CompiledResourceState> resourceStates,
        ReadOnlySpan<QueueHandoff> handoffs)
    {
        var count = 0;
        for (var usageIndex = 0; usageIndex < usages.Length; usageIndex++)
        {
            ref readonly var usage = ref usages[usageIndex];
            ref readonly var res = ref _resourceRegistry.GetResource(usage.resource);
            if (res.isImported && !res.isAllocated)
            {
                continue;
            }

            ref var resourceState = ref resourceStates[usage.resource.Value];
            if (HasQueueAcquire(handoffs, scheduleIndex, usage.resource))
            {
                resourceState = new CompiledResourceState(usage.targetState, usage.writes);
                continue;
            }

            if (!resourceState.isValid && res.inPlacePredecessor.IsValid)
            {
                var predState = resourceStates[res.inPlacePredecessor.Value];
                if (predState.isValid)
                {
                    resourceState = predState;
                }
                else
                {
                    ref readonly var predRes = ref _resourceRegistry.GetResource(res.inPlacePredecessor);
                    if (predRes.hasInitialBarrierState)
                    {
                        resourceState = new CompiledResourceState(predRes.initialBarrierState, writes: false);
                    }
                }
            }

            var flags = BarrierFlags.None;
            if (TryGetAliasingPredecessor(scheduleIndex, usage.resource, ref aliasingPlan, resourceOrdering, out var aliasingPredecessor))
            {
                flags = BarrierFlags.FirstUsage | BarrierFlags.Discard;
            }

            if (usage.discard)
            {
                flags |= BarrierFlags.Discard;
            }

            var hasSameState = resourceState.isValid
                && resourceState.state.layout == usage.targetState.layout
                && resourceState.state.access == usage.targetState.access
                && IsSyncSatisfied(resourceState.state.sync, usage.targetState.sync);
            var forceUavOrdering = hasSameState
                && usage.targetState.access == BarrierAccess.UnorderedAccess
                && (resourceState.writes || usage.writes);
            if (hasSameState && !forceUavOrdering && aliasingPredecessor.IsInvalid && !usage.discard)
            {
                resourceState.writes = usage.writes;
                continue;
            }

            var sourceState = resourceState.isValid && !usage.discard
                ? resourceState.state
                : new ResourceBarrierData(BarrierLayout.Undefined, BarrierAccess.NoAccess, BarrierSync.None);

            if (!resourceState.isValid)
            {
                flags |= BarrierFlags.FirstUsage | BarrierFlags.Discard;
            }
            else
            {
                flags |= BarrierFlags.ExplicitSource;
            }

            if (forceUavOrdering)
            {
                flags |= BarrierFlags.Force;
            }

            count += AddTransition(
                usage.resource,
                sourceState,
                usage.targetState,
                aliasingPredecessor,
                flags,
                effectiveQueue,
                ref writer);
            resourceState = new CompiledResourceState(usage.targetState, usage.writes);
        }

        return count;
    }

    private int AddTransition(Identifier<RGResource> id, ResourceBarrierData sourceState, ResourceBarrierData targetState, Identifier<RGResource> aliasingPredecessor, BarrierFlags flags, CommandQueueType queue, ref BufferWriter writer)
    {
        ref readonly var resource = ref _resourceRegistry.GetResource(id);
        var barrier = new CompiledBarrier
        {
            resource = id,
            sourceState = sourceState,
            handoffState = targetState,
            targetState = targetState,
            aliasingPredecessor = aliasingPredecessor,
            flags = flags,
            resourceType = resource.type,
            sourceQueue = queue,
            destinationQueue = queue
        };
        writer.Write(barrier);
        return 1;
    }

    private int EmitClosingBarriers(ref BufferWriter writer, CommandQueueType activeQueue, Span<CompiledResourceState> resourceStates)
    {
        var startPos = writer.Position;
        writer.Write(RGExecutionOpType.IssueBarriers);
        writer.Write(0);

        var count = 0;
        for (var i = 0; i < _resourceRegistry.ResourceCount; i++)
        {
            ref readonly var resource = ref _resourceRegistry.GetResourceByIndex(i);
            if (resource.isImported)
            {
                if (!resource.isAllocated || !resource.hasFinalBarrierState)
                {
                    continue;
                }

                if (resource.inPlaceSuccessor.IsValid)
                {
                    ref readonly var succState = ref resourceStates[resource.inPlaceSuccessor.Value];
                    if (succState.isValid)
                    {
                        resourceStates[i] = succState;
                    }
                }

                var finalState = resource.finalBarrierState;

                ref var currentState = ref resourceStates[i];
                if (!currentState.isValid)
                {
                    if (resource.hasInitialBarrierState
                        && (resource.initialBarrierState.layout != finalState.layout
                            || resource.initialBarrierState.access != finalState.access
                            || !IsSyncSatisfied(resource.initialBarrierState.sync, finalState.sync)))
                    {
                        count += AddTransition(
                            new Identifier<RGResource>(i),
                            resource.initialBarrierState,
                            finalState,
                            Identifier<RGResource>.Invalid,
                            BarrierFlags.None,
                            activeQueue,
                            ref writer);
                        currentState = new CompiledResourceState(finalState, writes: false);
                    }
                    continue;
                }

                if (currentState.state.layout != finalState.layout
                    || currentState.state.access != finalState.access
                    || !IsSyncSatisfied(currentState.state.sync, finalState.sync))
                {
                    count += AddTransition(
                        new Identifier<RGResource>(i),
                        currentState.state,
                        finalState,
                        Identifier<RGResource>.Invalid,
                        BarrierFlags.None,
                        activeQueue,
                        ref writer);
                    currentState = new CompiledResourceState(finalState, writes: false);
                }
            }
            else if (resource.isExtracted && !resource.inPlacePredecessor.IsValid && resource.extractionTarget.IsValid)
            {
                for (var j = 0; j < _resourceRegistry.ResourceCount; j++)
                {
                    ref readonly var targetRes = ref _resourceRegistry.GetResourceByIndex(j);
                    if (targetRes.isImported && targetRes.backingResource == resource.extractionTarget && targetRes.hasFinalBarrierState)
                    {
                        ref var bState = ref resourceStates[i];
                        if (bState.isValid && (bState.state.layout != targetRes.finalBarrierState.layout
                            || bState.state.access != targetRes.finalBarrierState.access
                            || !IsSyncSatisfied(bState.state.sync, targetRes.finalBarrierState.sync)))
                        {
                            count += AddTransition(
                                new Identifier<RGResource>(i),
                                bState.state,
                                targetRes.finalBarrierState,
                                Identifier<RGResource>.Invalid,
                                BarrierFlags.None,
                                activeQueue,
                                ref writer);
                            bState = new CompiledResourceState(targetRes.finalBarrierState, writes: false);
                        }
                        break;
                    }
                }
            }
        }

        if (count > 0)
        {
            var endPos = writer.Position;
            writer.Position = startPos + sizeof(RGExecutionOpType);
            writer.Write(count);
            writer.Position = endPos;
        }
        else
        {
            writer.Position = startPos;
        }

        return count;
    }

    private ResourceBarrierData GetBufferReadBarrierData(Identifier<RGResource> handle, RGResourceType resourceType, RenderPassType passType)
    {
        var sync = passType switch
        {
            RenderPassType.Compute => BarrierSync.ComputeShading,
            RenderPassType.Raster => BarrierSync.PixelShading | BarrierSync.NonPixelShading,
            _ => BarrierSync.All
        };

        if (resourceType == RGResourceType.Texture)
        {
            return new ResourceBarrierData(BarrierLayout.ShaderResource, BarrierAccess.ShaderResource, sync);
        }

        var access = BarrierAccess.ShaderResource;

        ref readonly var resource = ref _resourceRegistry.GetResource(handle);
        if (resource.bufferDesc.Usage.HasFlag(BufferUsage.AccelerationStructure))
        {
            access = BarrierAccess.RaytracingAccelerationStructureRead;
        }
        else if (resource.bufferDesc.Usage.HasFlag(BufferUsage.IndirectArgument))
        {
            sync = BarrierSync.ExecuteIndirect;
            access = BarrierAccess.IndirectArgument;
        }

        return new ResourceBarrierData(BarrierLayout.Undefined, access, sync);
    }

    public static bool RequiresBarrierBetweenPasses(RenderGraphPass laterPass, int scheduleIndex, RenderGraphResourceRegistry resources, ref AliasingPlan aliasingPlan, RenderGraphResourceOrdering resourceOrdering)
    {

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
                if (!resource.isImported && resourceOrdering.IsFirstUse(id.Value, scheduleIndex))
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
