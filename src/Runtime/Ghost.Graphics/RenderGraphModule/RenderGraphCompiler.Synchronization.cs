using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Ghost.Graphics.RenderGraphModule;

internal unsafe partial class RenderGraphCompiler
{
    private struct CompiledResourceState
    {
        public ResourceBarrierData state;
        public bool isValid;
        public bool writes;

        public CompiledResourceState(ResourceBarrierData state, bool writes)
        {
            this.state = state;
            this.writes = writes;
            isValid = true;
        }
    }

    private struct PassResourceUsageRange
    {
        public int start;
        public int count;
    }

    private struct LastScheduledResourceUse
    {
        public ResourceBarrierData state;
        public Identifier<RGResource> resource;
        public RGResourceType resourceType;
        public CommandQueueType queue;
        public int scheduleIndex;
        public int commandBufferId;
        public bool isValid;
        public bool writes;
    }

    private struct QueueHandoff
    {
        public Identifier<RGResource> sourceResource;
        public Identifier<RGResource> destinationResource;
        public Identifier<RGResource> aliasingPredecessor;
        public ResourceBarrierData sourceState;
        public ResourceBarrierData handoffState;
        public ResourceBarrierData targetState;
        public RGResourceType sourceResourceType;
        public RGResourceType destinationResourceType;
        public CommandQueueType sourceQueue;
        public CommandQueueType destinationQueue;
        public int releaseBoundaryIndex;
        public int acquireScheduleIndex;
    }

    private UnsafeList<ResolvedPassResourceUsage> BuildPassResourceUsagePlan(
        List<RenderGraphPass> passes,
        ReadOnlySpan<int> compiledPasses,
        Span<PassResourceUsageRange> usageRanges,
        AllocationHandle allocationHandle)
    {
        var usageRecords = new UnsafeList<ResolvedPassResourceUsage>(
            Math.Max(1, compiledPasses.Length * 4),
            allocationHandle);
        var resourceCount = _resourceRegistry.ResourceCount;
        if (resourceCount == 0)
        {
            usageRanges.Clear();
            return usageRecords;
        }

        using var scope = AllocationManager.CreateStackScope();
        using var resolvedUsages = new UnsafeArray<ResolvedPassResourceUsage>(
            resourceCount,
            scope.AllocationHandle,
            AllocationOption.Clear);

        for (var scheduleIndex = 0; scheduleIndex < compiledPasses.Length; scheduleIndex++)
        {
            ResolvePassResourceUsages(passes[compiledPasses[scheduleIndex]], resolvedUsages.AsSpan());

            ref var range = ref usageRanges[scheduleIndex];
            range.start = usageRecords.Count;
            for (var resourceIndex = 0; resourceIndex < resourceCount; resourceIndex++)
            {
                ref readonly var usage = ref resolvedUsages[resourceIndex];
                if (usage.usageClass != PassResourceUsageClass.None)
                {
                    usageRecords.Add(usage);
                    range.count++;
                }
            }
        }

        return usageRecords;
    }

    private UnsafeList<QueueHandoff> BuildQueueHandoffs(
        ReadOnlySpan<ResolvedPassResourceUsage> usageRecords,
        ReadOnlySpan<PassResourceUsageRange> usageRanges,
        ReadOnlySpan<CommandQueueType> effectiveQueues,
        ReadOnlySpan<int> commandBufferIds,
        ReadOnlySpan<byte> reachability,
        AliasingPlan aliasingPlan,
        RenderGraphResourceOrdering resourceOrdering,
        AllocationHandle allocationHandle)
    {
        var handoffs = new UnsafeList<QueueHandoff>(
            Math.Max(1, _resourceRegistry.ResourceCount),
            allocationHandle);
        var resourceCount = _resourceRegistry.ResourceCount;
        if (resourceCount == 0)
        {
            return handoffs;
        }

        const int trackedQueueCount = 2;
        using var lastUses = new UnsafeArray<LastScheduledResourceUse>(
            resourceCount * trackedQueueCount,
            allocationHandle,
            AllocationOption.Clear);
        var passCount = usageRanges.Length;

        for (var scheduleIndex = 0; scheduleIndex < passCount; scheduleIndex++)
        {
            ref readonly var range = ref usageRanges[scheduleIndex];
            var destinationQueue = effectiveQueues[scheduleIndex];
            var destinationQueueIndex = (int)destinationQueue;
            Logger.DebugAssert(destinationQueueIndex >= 0 && destinationQueueIndex < trackedQueueCount);

            for (var recordOffset = 0; recordOffset < range.count; recordOffset++)
            {
                ref readonly var usage = ref usageRecords[range.start + recordOffset];
                var resourceUseOffset = usage.resource.Value * trackedQueueCount;
                for (var sourceQueueIndex = 0; sourceQueueIndex < trackedQueueCount; sourceQueueIndex++)
                {
                    if (sourceQueueIndex == destinationQueueIndex)
                    {
                        continue;
                    }

                    ref var sourceUse = ref lastUses[resourceUseOffset + sourceQueueIndex];
                    if (!sourceUse.isValid
                        || sourceUse.commandBufferId == commandBufferIds[scheduleIndex]
                        || (!sourceUse.writes && !usage.writes)
                        || reachability[(sourceUse.scheduleIndex * passCount) + scheduleIndex] == 0)
                    {
                        continue;
                    }

                    AddQueueHandoff(
                        in sourceUse,
                        usage.resource,
                        usage.resourceType,
                        usage.targetState,
                        destinationQueue,
                        scheduleIndex,
                        commandBufferIds,
                        ref handoffs);
                    sourceUse.isValid = false;
                }

                lastUses[resourceUseOffset + destinationQueueIndex] = new LastScheduledResourceUse
                {
                    state = usage.targetState,
                    resource = usage.resource,
                    resourceType = usage.resourceType,
                    queue = destinationQueue,
                    scheduleIndex = scheduleIndex,
                    commandBufferId = commandBufferIds[scheduleIndex],
                    isValid = true,
                    writes = usage.writes
                };
            }
        }

        // Alias successors use a different logical resource ID, so add their physical-memory
        // handoff after all logical-resource last uses are known.
        for (var scheduleIndex = 0; scheduleIndex < passCount; scheduleIndex++)
        {
            ref readonly var range = ref usageRanges[scheduleIndex];
            for (var recordOffset = 0; recordOffset < range.count; recordOffset++)
            {
                ref readonly var usage = ref usageRecords[range.start + recordOffset];
                if (!TryGetAliasingPredecessor(
                    scheduleIndex,
                    usage.resource,
                    aliasingPlan,
                    resourceOrdering,
                    out var predecessor))
                {
                    continue;
                }

                var destinationQueueIndex = (int)effectiveQueues[scheduleIndex];
                var predecessorUseOffset = predecessor.Value * trackedQueueCount;
                for (var sourceQueueIndex = 0; sourceQueueIndex < trackedQueueCount; sourceQueueIndex++)
                {
                    if (sourceQueueIndex == destinationQueueIndex)
                    {
                        continue;
                    }

                    ref var predecessorUse = ref lastUses[predecessorUseOffset + sourceQueueIndex];
                    if (!predecessorUse.isValid
                        || predecessorUse.commandBufferId == commandBufferIds[scheduleIndex])
                    {
                        continue;
                    }

                    AddQueueHandoff(
                        in predecessorUse,
                        usage.resource,
                        usage.resourceType,
                        usage.targetState,
                        effectiveQueues[scheduleIndex],
                        scheduleIndex,
                        commandBufferIds,
                        ref handoffs,
                        predecessor);
                    predecessorUse.isValid = false;
                }
            }
        }

        return handoffs;
    }

    private static void AddQueueHandoff(
        scoped in LastScheduledResourceUse sourceUse,
        Identifier<RGResource> destinationResource,
        RGResourceType destinationResourceType,
        ResourceBarrierData targetState,
        CommandQueueType destinationQueue,
        int acquireScheduleIndex,
        ReadOnlySpan<int> commandBufferIds,
        ref UnsafeList<QueueHandoff> handoffs,
        Identifier<RGResource> aliasingPredecessor = default)
    {
        var releaseBoundaryIndex = FindCommandBufferEndBoundary(
            sourceUse.scheduleIndex,
            sourceUse.commandBufferId,
            commandBufferIds);
        Logger.DebugAssert(releaseBoundaryIndex >= 0);

        for (var handoffIndex = 0; handoffIndex < handoffs.Count; handoffIndex++)
        {
            ref var existing = ref handoffs[handoffIndex];
            if (existing.sourceResource == sourceUse.resource
                && existing.destinationResource == destinationResource
                && existing.releaseBoundaryIndex == releaseBoundaryIndex
                && existing.acquireScheduleIndex == acquireScheduleIndex)
            {
                Logger.DebugAssert(existing.targetState.layout == targetState.layout);
                existing.targetState.access |= targetState.access;
                existing.targetState.sync |= targetState.sync;
                return;
            }
        }

        handoffs.Add(new QueueHandoff
        {
            sourceResource = sourceUse.resource,
            destinationResource = destinationResource,
            aliasingPredecessor = aliasingPredecessor,
            sourceState = sourceUse.state,
            handoffState = new ResourceBarrierData(
                sourceUse.resourceType == RGResourceType.Texture ? BarrierLayout.Common : BarrierLayout.Undefined,
                BarrierAccess.NoAccess,
                BarrierSync.None),
            targetState = targetState,
            sourceResourceType = sourceUse.resourceType,
            destinationResourceType = destinationResourceType,
            sourceQueue = sourceUse.queue,
            destinationQueue = destinationQueue,
            releaseBoundaryIndex = releaseBoundaryIndex,
            acquireScheduleIndex = acquireScheduleIndex
        });
    }

    private static int FindCommandBufferEndBoundary(
        int sourceScheduleIndex,
        int sourceCommandBufferId,
        ReadOnlySpan<int> commandBufferIds)
    {
        for (var scheduleIndex = sourceScheduleIndex + 1; scheduleIndex < commandBufferIds.Length; scheduleIndex++)
        {
            if (commandBufferIds[scheduleIndex] != sourceCommandBufferId)
            {
                return scheduleIndex;
            }
        }

        return -1;
    }

    private static bool HasQueueAcquire(
        ReadOnlySpan<QueueHandoff> handoffs,
        int scheduleIndex,
        Identifier<RGResource> resource)
    {
        for (var handoffIndex = 0; handoffIndex < handoffs.Length; handoffIndex++)
        {
            ref readonly var handoff = ref handoffs[handoffIndex];
            if (handoff.acquireScheduleIndex == scheduleIndex && handoff.destinationResource == resource)
            {
                return true;
            }
        }

        return false;
    }

    private static int WriteQueueHandoffBarriers(
        ref BufferWriter writer,
        ReadOnlySpan<QueueHandoff> handoffs,
        int scheduleIndex,
        bool release)
    {
        var count = 0;
        for (var handoffIndex = 0; handoffIndex < handoffs.Length; handoffIndex++)
        {
            ref readonly var handoff = ref handoffs[handoffIndex];
            if (release
                    ? handoff.releaseBoundaryIndex != scheduleIndex
                    : handoff.acquireScheduleIndex != scheduleIndex)
            {
                continue;
            }

            var flags = BarrierFlags.ExplicitSource
                | (release ? BarrierFlags.QueueRelease : BarrierFlags.QueueAcquire);
            if (!release && handoff.aliasingPredecessor.IsValid)
            {
                flags |= BarrierFlags.FirstUsage | BarrierFlags.Discard;
            }

            writer.Write(new CompiledBarrier
            {
                resource = release ? handoff.sourceResource : handoff.destinationResource,
                sourceState = handoff.sourceState,
                handoffState = handoff.handoffState,
                targetState = handoff.targetState,
                aliasingPredecessor = release
                    ? Identifier<RGResource>.Invalid
                    : handoff.aliasingPredecessor,
                flags = flags,
                resourceType = release ? handoff.sourceResourceType : handoff.destinationResourceType,
                sourceQueue = handoff.sourceQueue,
                destinationQueue = handoff.destinationQueue
            });
            count++;
        }

        return count;
    }

    private static int EmitQueueReleaseBarriers(
        ref BufferWriter writer,
        ReadOnlySpan<QueueHandoff> handoffs,
        int scheduleIndex)
    {
        var startPosition = writer.Position;
        writer.Write(RGExecutionOpType.IssueBarriers);
        writer.Write(0);

        var count = WriteQueueHandoffBarriers(ref writer, handoffs, scheduleIndex, release: true);

        if (count == 0)
        {
            writer.Position = startPosition;
            return 0;
        }

        var endPosition = writer.Position;
        writer.Position = startPosition + sizeof(RGExecutionOpType);
        writer.Write(count);
        writer.Position = endPosition;
        return count;
    }
}
