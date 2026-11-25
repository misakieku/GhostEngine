using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.Mathematics;
using System.Numerics;

namespace Ghost.Graphics.RenderGraphModule;

internal struct CompiledGraph
{
    public required ReadOnlyView<PlacedResourceData> placedResources;
    public required ReadOnlyView<int> aliasedLogicalResources;
    public required ReadOnlyView<int> logicalToPlaced;
    public required ulong totalHeapSize;
    public required float2 scale;
    public required ulong graphHash;
    public required IReadOnlyList<RenderGraphPass> passes;
    public required ReadOnlyView<int> compiledPasses;
    public required ReadOnlyView<NativeRenderPass> nativePasses;
    public required ReadOnlyView<byte> commandStream;
    public required ReadOnlyView<int> resourceFirstUseScheduleIndices;
    public required ReadOnlyView<int> resourceLastUseScheduleIndices;
    public required bool cacheHit;
}

/// <summary>
/// Handles compilation of the render graph including pass culling, heap allocation orchestration,
/// barrier compilation, and cache management.
/// </summary>
internal unsafe partial class RenderGraphCompiler : IDisposable
{
    private struct PassDependencyNode : IDisposable
    {
        public int passIndex;
        public int inDegree; // Number of prerequisite passes that must execute first
        public UnsafeList<int> dependents; // Passes that depend on this pass

        public PassDependencyNode(int index, AllocationHandle allocationHandle)
        {
            passIndex = index;
            inDegree = 0;
            dependents = new UnsafeList<int>(10, allocationHandle);
        }

        public void Dispose()
        {
            dependents.Dispose();
        }
    }

    internal struct SyncBoundary
    {
        public bool isValid;
        public CommandQueueType nextCommandBufferType;
        public int nextCommandBufferId;
        public int producerCommandBufferId;
    }

    private readonly IResourceAllocator _resourceAllocator;
    private readonly RenderGraphResourceRegistry _resourceRegistry;
    private readonly RenderGraphCompilationCache _compilationCache;
#if GHOST_SAFETY_CHECKS
    private ulong _validatedGraphHash;
    private bool _hasValidatedGraphHash;
#endif

    public RenderGraphCompiler(IResourceAllocator resourceAllocator, RenderGraphResourceRegistry resourceRegistry)
    {
        _resourceAllocator = resourceAllocator;
        _resourceRegistry = resourceRegistry;

        _compilationCache = new RenderGraphCompilationCache();
    }

    /// <summary>
    /// Compiles the render graph by culling passes, allocating resources, and preparing barriers.
    /// </summary>
    public Result<CompiledGraph, Error> Compile(in ViewportState viewport, ulong graphHash, List<RenderGraphPass> passes, RGFlags flags, AllocationHandle allocationHandle)
    {
#if GHOST_SAFETY_CHECKS
        if (!_hasValidatedGraphHash || _validatedGraphHash != graphHash)
        {
            var validationError = RenderGraphValidator.ValidateGraph(passes, _resourceRegistry);
            if (validationError is not null)
            {
                throw new InvalidOperationException(validationError);
            }

            _validatedGraphHash = graphHash;
            _hasValidatedGraphHash = true;
        }
#endif

        Error error;

        // Try to restore from cache
        if (_compilationCache.TryGetCached(graphHash, flags, out var cached))
        {
            // Check if view state changed
            var scale = viewport.CalculateScale(cached.viewport);
            if (math.any(scale > float2.one))
            {
                // View state changed - re-resolve sizes and recreate GPU resources
                _resourceRegistry.ResolveTextureSizes(in viewport);

                var resizedPlan = RenderGraphAliasingBuilder.ResizeCachedSlots(
                    _resourceRegistry,
                    _resourceAllocator,
                    cached.logicalToPlaced,
                    cached.placedResources,
                    cached.aliasedLogicalResources,
                    allocationHandle);
                try
                {
                    error = _resourceRegistry.AllocateBackingResources(ref resizedPlan, _compilationCache);
                    if (error != Error.None)
                    {
                        return error;
                    }

                    ref readonly var updated = ref _compilationCache.UpdateResizedAliasing(ref resizedPlan, viewport, allocationHandle);

                    return new CompiledGraph
                    {
                        scale = float2.one,
                        placedResources = updated.placedResources,
                        aliasedLogicalResources = updated.aliasedLogicalResources,
                        logicalToPlaced = updated.logicalToPlaced,
                        totalHeapSize = updated.totalHeapSize,
                        graphHash = graphHash,
                        passes = passes,
                        compiledPasses = updated.compiledPassIndices,
                        nativePasses = updated.nativePasses,
                        commandStream = updated.commandBytes,
                        resourceFirstUseScheduleIndices = updated.resourceFirstUseScheduleIndices,
                        resourceLastUseScheduleIndices = updated.resourceLastUseScheduleIndices,
                        cacheHit = true
                    };
                }
                finally
                {
                    resizedPlan.Dispose();
                }
            }
            else
            {
                // Perfect cache hit - restore everything without allocating an AliasingPlan
                RestorePassCulledFlags(cached, passes);
                _resourceRegistry.RestoreBackingResources(cached.backingResources);
                return new CompiledGraph
                {
                    scale = scale,
                    placedResources = cached.placedResources,
                    aliasedLogicalResources = cached.aliasedLogicalResources,
                    logicalToPlaced = cached.logicalToPlaced,
                    totalHeapSize = cached.totalHeapSize,
                    graphHash = graphHash,
                    passes = passes,
                    compiledPasses = cached.compiledPassIndices,
                    nativePasses = cached.nativePasses,
                    commandStream = cached.commandBytes,
                    resourceFirstUseScheduleIndices = cached.resourceFirstUseScheduleIndices,
                    resourceLastUseScheduleIndices = cached.resourceLastUseScheduleIndices,
                    cacheHit = true
                };
            }
        }

        // Fresh compilation needed
        using var compiledPasses = new UnsafeList<int>(passes.Count, allocationHandle);

        // Mark passes with side effects (writes to imported resources)
        MarkPassesWithSideEffects(passes);

        // Cull passes based on dependency analysis
        CullPasses(passes);

        // Build final pass list (only non-culled passes)
        for (var i = 0; i < passes.Count; i++)
        {
            var pass = passes[i];
            if (!pass.culled)
            {
                compiledPasses.Add(i);
            }
        }

        using var schedulingScope = AllocationManager.CreateStackScope();

        var nodes = BuildDAG(passes, compiledPasses, _resourceRegistry, schedulingScope.AllocationHandle);

        try
        {
            // Reorder passes to ensure best performance
            if (!flags.HasFlag(RGFlags.DisablePassReordering))
            {
                ReorderPasses(passes, nodes.AsSpan(), &compiledPasses);
            }

            var compiledPassCount = compiledPasses.Count;
            using var effectiveQueues = new UnsafeArray<CommandQueueType>(compiledPassCount, schedulingScope.AllocationHandle);
            using var syncBoundaries = new UnsafeArray<SyncBoundary>(compiledPassCount, schedulingScope.AllocationHandle);
            using var scheduleIndexByPassIndex = new UnsafeArray<int>(passes.Count, schedulingScope.AllocationHandle);
            using var commandBufferIds = new UnsafeArray<int>(compiledPassCount, schedulingScope.AllocationHandle);
            var reachability = new RenderGraphBitMatrix(compiledPassCount, compiledPassCount, schedulingScope.AllocationHandle);

            BuildPassReachability(compiledPasses, nodes, scheduleIndexByPassIndex, ref reachability);
            BuildDependencyWindowSchedule(passes, compiledPasses, effectiveQueues, syncBoundaries, in reachability);
            FinalizeScheduleReachability(effectiveQueues, syncBoundaries, commandBufferIds, ref reachability);

            using var resourceOrdering = RenderGraphResourceOrdering.Build(_resourceRegistry, scheduleIndexByPassIndex, in reachability, compiledPassCount, schedulingScope.AllocationHandle);

            var aliasingPlan = RenderGraphAliasingBuilder.Build(_resourceRegistry, _resourceAllocator, resourceOrdering, flags.HasFlag(RGFlags.DisableAliasing), allocationHandle, effectiveQueues);
            try
            {
                error = _resourceRegistry.AllocateBackingResources(ref aliasingPlan, _compilationCache, in resourceOrdering);

                if (error != Error.None)
                {
                    return error;
                }

                var nativePasses = RenderGraphNativePassBuilder.BuildNativeRenderPasses(
                    _resourceRegistry,
                    passes,
                    compiledPasses,
                    syncBoundaries,
                    scheduleIndexByPassIndex,
                    ref aliasingPlan,
                    resourceOrdering,
                    allocationHandle);
                var commandWriter = new BufferWriter(Math.Max(4096, compiledPassCount * 256), allocationHandle);

                try
                {
                    BuildExecutionCommands(
                        ref commandWriter,
                        passes,
                        compiledPasses,
                        nativePasses,
                        ref aliasingPlan,
                        resourceOrdering,
                        effectiveQueues,
                        syncBoundaries,
                        commandBufferIds,
                        in reachability,
                        flags);

                    ref readonly var cacheData = ref _compilationCache.SetCached(
                        _resourceRegistry,
                        graphHash,
                        flags,
                        viewport,
                        passes,
                        compiledPasses,
                        nativePasses,
                        commandWriter.AsSpan(),
                        ref aliasingPlan,
                        resourceOrdering,
                        allocationHandle);

                    return new CompiledGraph
                    {
                        scale = float2.one,
                        placedResources = cacheData.placedResources,
                        aliasedLogicalResources = cacheData.aliasedLogicalResources,
                        logicalToPlaced = cacheData.logicalToPlaced,
                        totalHeapSize = cacheData.totalHeapSize,
                        graphHash = graphHash,
                        passes = passes,
                        compiledPasses = cacheData.compiledPassIndices,
                        nativePasses = cacheData.nativePasses,
                        commandStream = cacheData.commandBytes,
                        resourceFirstUseScheduleIndices = cacheData.resourceFirstUseScheduleIndices,
                        resourceLastUseScheduleIndices = cacheData.resourceLastUseScheduleIndices,
                        cacheHit = false
                    };
                }
                finally
                {
                    for (var i = 0; i < nativePasses.Count; i++)
                    {
                        nativePasses[i].Dispose();
                    }

                    nativePasses.Dispose();
                    commandWriter.Dispose();
                }
            }
            finally
            {
                aliasingPlan.Dispose();
                reachability.Dispose();
            }
        }
        finally
        {
            for (var i = 0; i < nodes.Count; i++)
            {
                nodes[i].Dispose();
            }

            nodes.Dispose();
        }
    }

    public void InvalidateCache()
    {
        _compilationCache.Invalidate();
    }

    private void MarkPassesWithSideEffects(List<RenderGraphPass> passes)
    {
        for (var i = 0; i < passes.Count; i++)
        {
            var pass = passes[i];

            for (var j = 0; j < (int)RGResourceType.Count; j++)
            {
                var writeList = pass.resourceWrites[j];
                foreach (var writeHandle in writeList)
                {
                    ref readonly var resource = ref _resourceRegistry.GetResource(writeHandle);
                    if (resource.isImported || resource.isExtracted)
                    {
                        pass.hasSideEffects = true;
                        break;
                    }
                }
            }
        }
    }

    private void CullPasses(List<RenderGraphPass> passes)
    {
        for (var i = 0; i < passes.Count; i++)
        {
            passes[i].culled = passes[i].allowCulling && !passes[i].hasSideEffects;
        }

        for (var i = passes.Count - 1; i >= 0; i--)
        {
            var pass = passes[i];
            if (!pass.culled)
            {
                UncullDependencies(pass, passes);
            }
        }
    }

    private void UncullDependencies(RenderGraphPass pass, List<RenderGraphPass> passes)
    {
        for (var i = 0; i < (int)RGResourceType.Count; i++)
        {
            var readList = pass.resourceReads[i];
            foreach (var res in readList)
            {
                UncullProducer(res, passes);
            }
        }

        for (var i = 0; i <= pass.maxColorIndex; i++)
        {
            if (pass.colorAccess[i].id.IsValid)
            {
                UncullProducer(pass.colorAccess[i].id.AsResource(), passes);
            }
        }

        if (pass.depthAccess.id.IsValid)
        {
            UncullProducer(pass.depthAccess.id.AsResource(), passes);
        }

        foreach (var res in pass.randomAccess)
        {
            UncullProducer(res, passes);
        }
    }

    private void UncullProducer(Identifier<RGResource> resource, List<RenderGraphPass> passes)
    {
        foreach (var producerIdx in _resourceRegistry.GetProducerPasses(resource.Value))
        {
            var producer = passes[producerIdx];
            if (producer.culled)
            {
                producer.culled = false;
                UncullDependencies(producer, passes);
            }
        }
    }

    private static UnsafeArray<PassDependencyNode> BuildDAG(List<RenderGraphPass> passes, ReadOnlySpan<int> compiledPasses, RenderGraphResourceRegistry resources, AllocationHandle allocationHandle)
    {
        var passCount = compiledPasses.Length;
        var resourceCount = resources.ResourceCount;

        using var lastWriter = new UnsafeArray<int>(resourceCount, allocationHandle);
        lastWriter.AsSpan().Fill(-1);

        var lastReaders = new RenderGraphBitMatrix(resourceCount, passCount, allocationHandle);
        var edgeMatrix = new RenderGraphBitMatrix(passCount, passCount, allocationHandle);

        static void AddEdge(int passA, int passB, Span<PassDependencyNode> nodes, ref RenderGraphBitMatrix edgeMatrix)
        {
            if (passA == passB || edgeMatrix.Get(passA, passB))
            {
                return;
            }

            edgeMatrix.Set(passA, passB);
            nodes[passA].dependents.Add(passB);
            nodes[passB].inDegree++;
        }

        // Initialize nodes in the original compiled-pass index space.
        var nodes = new UnsafeArray<PassDependencyNode>(passCount, allocationHandle);
        for (var i = 0; i < passCount; i++)
        {
            nodes[i] = new PassDependencyNode(compiledPasses[i], allocationHandle);
        }

        var lastSideEffect = -1;

        // Iterate over non-culled passes
        for (var i = 0; i < passCount; i++)
        {
            var pass = passes[compiledPasses[i]];

            // Process READS (RAW Dependencies)
            void ProcessRead(Identifier<RGResource> resId, Span<PassDependencyNode> nodes, Span<int> lastWriter, ref RenderGraphBitMatrix lastReaders, ref RenderGraphBitMatrix edgeMatrix)
            {
                if (resId.IsInvalid)
                {
                    return;
                }

                var resIdx = resId.Value;
                var writer = lastWriter[resIdx];
                if (writer >= 0)
                {
                    AddEdge(writer, i, nodes, ref edgeMatrix);
                }

                lastReaders.Set(resIdx, i);
            }

            // Process WRITES (WAR & WAW Dependencies)
            void ProcessWrite(Identifier<RGResource> resId, Span<PassDependencyNode> nodes, Span<int> lastWriter, ref RenderGraphBitMatrix lastReaders, ref RenderGraphBitMatrix edgeMatrix)
            {
                if (resId.IsInvalid)
                {
                    return;
                }

                var resIdx = resId.Value;
                var writer = lastWriter[resIdx];
                if (writer >= 0)
                {
                    AddEdge(writer, i, nodes, ref edgeMatrix);
                }

                var readerRow = lastReaders.GetReadOnlyRow(resIdx);
                for (var w = 0; w < readerRow.Length; w++)
                {
                    var word = readerRow[w];
                    while (word != 0)
                    {
                        var bit = BitOperations.TrailingZeroCount(word);
                        word &= word - 1;
                        AddEdge((w << 6) + bit, i, nodes, ref edgeMatrix);
                    }
                }

                lastReaders.ClearRow(resIdx);
                lastWriter[resIdx] = i;
            }

            // Collect creation, read, and write hazards from the canonical declarations.
            for (var t = 0; t < (int)RGResourceType.Count; t++)
            {
                foreach (var res in pass.resourceCreates[t])
                {
                    ProcessWrite(res, nodes, lastWriter, ref lastReaders, ref edgeMatrix);
                }

                foreach (var res in pass.resourceReads[t])
                {
                    ProcessRead(res, nodes, lastWriter, ref lastReaders, ref edgeMatrix);
                }

                foreach (var res in pass.resourceWrites[t])
                {
                    ProcessWrite(res, nodes, lastWriter, ref lastReaders, ref edgeMatrix);
                }
            }

            // Color Attachments
            for (var c = 0; c <= pass.maxColorIndex; c++)
            {
                var access = pass.colorAccess[c];
                if (access.id.IsValid)
                {
                    var res = access.id.AsResource();
#pragma warning disable MHP002 // Defensive copy detected
                    if (access.accessFlags.HasFlag(AccessFlags.Read))
                    {
                        ProcessRead(res, nodes, lastWriter, ref lastReaders, ref edgeMatrix);
                    }

                    if (access.accessFlags.HasFlag(AccessFlags.Write))
                    {
                        ProcessWrite(res, nodes, lastWriter, ref lastReaders, ref edgeMatrix);
                    }
#pragma warning restore MHP002 // Defensive copy detected
                }
            }

            // Depth Attachment
            if (pass.depthAccess.id.IsValid)
            {
                var res = pass.depthAccess.id.AsResource();
#pragma warning disable MHP002 // Defensive copy detected
                if (pass.depthAccess.accessFlags.HasFlag(AccessFlags.Read))
                {
                    ProcessRead(res, nodes, lastWriter, ref lastReaders, ref edgeMatrix);
                }

                if (pass.depthAccess.accessFlags.HasFlag(AccessFlags.Write))
                {
                    ProcessWrite(res, nodes, lastWriter, ref lastReaders, ref edgeMatrix);
                }
#pragma warning restore MHP002 // Defensive copy detected
            }

            // UAVs (Random Access is both READ and WRITE)
            foreach (var uav in pass.randomAccess)
            {
                ProcessRead(uav, nodes, lastWriter, ref lastReaders, ref edgeMatrix);
                ProcessWrite(uav, nodes, lastWriter, ref lastReaders, ref edgeMatrix);
            }

            if (pass.hasSideEffects)
            {
                if (lastSideEffect >= 0)
                {
                    AddEdge(lastSideEffect, i, nodes, ref edgeMatrix);
                }

                lastSideEffect = i;
            }
        }

        edgeMatrix.Dispose();
        lastReaders.Dispose();
        return nodes;
    }

    private static int SelectBestCandidatePass(ReadOnlySpan<int> readyPassIndices, RenderGraphPass? lastScheduledPass, int lastScheduledDagIndex, List<RenderGraphPass> passes, ReadOnlySpan<int> compiledPasses, in RenderGraphBitMatrix passResourceMasks)
    {
        var bestIndex = 0;
        var maxScore = float.MinValue;
        for (var i = 0; i < readyPassIndices.Length; i++)
        {
            var candidateDagIndex = readyPassIndices[i];
            var candidate = passes[compiledPasses[candidateDagIndex]];
            var score = 0.0f;
            if (lastScheduledPass != null)
            {
                // Grouping Rule 1: Maximize Native Pass Merging
                if (lastScheduledPass.type == RenderPassType.Raster && candidate.type == RenderPassType.Raster)
                {
                    if (AttachmentsMatch(lastScheduledPass, candidate))
                    {
                        score += 1000.0f;
                    }
                }

                // Grouping Rule 2: Memory Locality (Shorten Lifetimes)
                if (passResourceMasks.IntersectsRow(lastScheduledDagIndex, candidateDagIndex))
                {
                    score += 100.0f;
                }
            }

            // Grouping Rule 3: Async Compute Scheduling
            if (candidate.asyncCompute)
            {
                score += 50.0f;
            }

            // Grouping Rule 4: Tie-breaker - preserve original submission order
            score -= candidateDagIndex * 0.01f;
            if (score > maxScore)
            {
                maxScore = score;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private static bool AttachmentsMatch(RenderGraphPass passA, RenderGraphPass passB)
    {
        if (passA.maxColorIndex != passB.maxColorIndex)
        {
            return false;
        }

        for (var i = 0; i <= passA.maxColorIndex; i++)
        {
            if (passA.colorAccess[i].id != passB.colorAccess[i].id)
            {
                return false;
            }
        }

        return passA.depthAccess.id == passB.depthAccess.id;
    }

    private static void BuildPassResourceMasks(List<RenderGraphPass> passes, ReadOnlySpan<int> compiledPasses, int resourceCount, ref RenderGraphBitMatrix passResourceMasks)
    {
        if (resourceCount <= 0)
        {
            return;
        }

        for (var dagIndex = 0; dagIndex < compiledPasses.Length; dagIndex++)
        {
            var pass = passes[compiledPasses[dagIndex]];
            for (var t = 0; t < (int)RGResourceType.Count; t++)
            {
                foreach (var res in pass.resourceReads[t])
                {
                    if (res.IsValid) passResourceMasks.Set(dagIndex, res.Value);
                }
            }
        }
    }

    private void ReorderPasses(List<RenderGraphPass> passes, ReadOnlySpan<PassDependencyNode> dependencyNodes, UnsafeList<int>* pCompiledPasses)
    {
        var passCount = pCompiledPasses->Count;
        if (passCount <= 1)
        {
            return;
        }

        using var scope = AllocationManager.CreateStackScope();
        using var remainingInDegrees = new UnsafeArray<int>(passCount, scope.AllocationHandle);
        var passResourceMasks = new RenderGraphBitMatrix(passCount, _resourceRegistry.ResourceCount, scope.AllocationHandle);
        BuildPassResourceMasks(passes, pCompiledPasses->AsSpan(), _resourceRegistry.ResourceCount, ref passResourceMasks);

        // Initialize Ready List (Passes with 0 incoming dependencies)
        using var readyList = new UnsafeList<int>(passCount, scope.AllocationHandle);
        for (var i = 0; i < passCount; i++)
        {
            remainingInDegrees[i] = dependencyNodes[i].inDegree;
            if (remainingInDegrees[i] == 0)
            {
                readyList.Add(i);
            }
        }

        using var reorderedIndices = new UnsafeList<int>(passCount, scope.AllocationHandle);
        RenderGraphPass? lastScheduledPass = null;
        var lastScheduledDagIndex = -1;

        // Kahn's Topological Sort with Grouping Heuristics
        while (readyList.Count > 0)
        {
            var bestReadyIdx = SelectBestCandidatePass(readyList.AsSpan(), lastScheduledPass, lastScheduledDagIndex, passes, pCompiledPasses->AsSpan(), in passResourceMasks);
            var chosenPassIndex = readyList[bestReadyIdx];
            readyList.RemoveAtSwapBack(bestReadyIdx);

            var chosenPass = (*pCompiledPasses)[chosenPassIndex];
            reorderedIndices.Add(chosenPass);
            lastScheduledPass = passes[chosenPass];
            lastScheduledDagIndex = chosenPassIndex;

            // Decrement the scratch in-degree for all downstream dependent passes.
            ref readonly var chosenNode = ref dependencyNodes[chosenPassIndex];
            for (var i = 0; i < chosenNode.dependents.Count; i++)
            {
                var dstIdx = chosenNode.dependents[i];
                remainingInDegrees[dstIdx]--;
                if (remainingInDegrees[dstIdx] == 0)
                {
                    readyList.Add(dstIdx);
                }
            }
        }

        Logger.DebugAssert(reorderedIndices.Count == pCompiledPasses->Count);

        passResourceMasks.Dispose();
        pCompiledPasses->Clear();
        pCompiledPasses->AddRange(reorderedIndices);
    }

    private static void BuildPassReachability(ReadOnlySpan<int> compiledPasses, ReadOnlySpan<PassDependencyNode> dependencyNodes, Span<int> scheduleIndexByPassIndex, ref RenderGraphBitMatrix reachability)
    {
        scheduleIndexByPassIndex.Fill(-1);
        reachability.Clear();

        var passCount = compiledPasses.Length;
        if (passCount == 0)
        {
            return;
        }

        using var scope = AllocationManager.CreateStackScope();
        using var dagIndexByPassIndex = new UnsafeArray<int>(scheduleIndexByPassIndex.Length, scope.AllocationHandle);
        dagIndexByPassIndex.AsSpan().Fill(-1);

        for (var dagIndex = 0; dagIndex < passCount; dagIndex++)
        {
            dagIndexByPassIndex[dependencyNodes[dagIndex].passIndex] = dagIndex;
        }

        for (var scheduleIndex = 0; scheduleIndex < passCount; scheduleIndex++)
        {
            scheduleIndexByPassIndex[compiledPasses[scheduleIndex]] = scheduleIndex;
        }

        for (var source = passCount - 1; source >= 0; source--)
        {
            var sourceDagIndex = dagIndexByPassIndex[compiledPasses[source]];
            Logger.DebugAssert(sourceDagIndex >= 0);
            ref readonly var sourceNode = ref dependencyNodes[sourceDagIndex];
            for (var edgeIndex = 0; edgeIndex < sourceNode.dependents.Count; edgeIndex++)
            {
                var dependentPassIndex = dependencyNodes[sourceNode.dependents[edgeIndex]].passIndex;
                var dependent = scheduleIndexByPassIndex[dependentPassIndex];
                Logger.DebugAssert(dependent > source);
                reachability.Set(source, dependent);
                reachability.OrRow(source, dependent, dependent >> 6);
            }
        }
    }

    private static void BuildDependencyWindowSchedule(List<RenderGraphPass> passes, ReadOnlySpan<int> compiledPasses, Span<CommandQueueType> effectiveQueues, Span<SyncBoundary> syncBoundaries, in RenderGraphBitMatrix reachability)
    {
        effectiveQueues.Clear();
        syncBoundaries.Clear();

        var passCount = compiledPasses.Length;
        if (passCount < 3)
        {
            return;
        }

        var currentGfxCbId = 0;
        var nextCbId = 1;

        for (var candidateIndex = 0; candidateIndex < passCount; candidateIndex++)
        {
            if (syncBoundaries[candidateIndex].isValid)
            {
                continue;
            }

            var candidate = passes[compiledPasses[candidateIndex]];
            if (!IsAsyncComputeCandidate(candidate))
            {
                continue;
            }

            var joinIndex = FindFirstDependent(candidateIndex, in reachability);
            if (joinIndex < 0)
            {
                continue;
            }

            var groupEndIndex = candidateIndex;
            while (groupEndIndex + 1 < joinIndex)
            {
                var nextIndex = groupEndIndex + 1;
                var nextCandidate = passes[compiledPasses[nextIndex]];
                if (!IsAsyncComputeCandidate(nextCandidate)
                    || FindFirstDependent(nextIndex, in reachability) != joinIndex)
                {
                    break;
                }

                groupEndIndex = nextIndex;
            }

            var hasIndependentGraphicsWork = false;
            var legalWindow = true;
            for (var overlapIndex = groupEndIndex + 1; overlapIndex < joinIndex; overlapIndex++)
            {
                var overlapPass = passes[compiledPasses[overlapIndex]];
                if (overlapPass.type == RenderPassType.Unsafe)
                {
                    if (!overlapPass.allowAsyncComputeOverlap)
                    {
                        legalWindow = false;
                        break;
                    }

                    hasIndependentGraphicsWork = true;
                }

                if (overlapPass.type == RenderPassType.Raster)
                {
                    hasIndependentGraphicsWork = true;
                }
            }

            if (!legalWindow || !hasIndependentGraphicsWork)
            {
                continue;
            }

            var hasGraphicsProducer = false;
            for (var producerIndex = 0; producerIndex < candidateIndex; producerIndex++)
            {
                if (effectiveQueues[producerIndex] == CommandQueueType.Compute)
                {
                    continue;
                }

                if (reachability.IntersectsRange(producerIndex, candidateIndex, groupEndIndex))
                {
                    hasGraphicsProducer = true;
                    break;
                }
            }

            for (var computeIndex = candidateIndex; computeIndex <= groupEndIndex; computeIndex++)
            {
                effectiveQueues[computeIndex] = CommandQueueType.Compute;
            }

            var computeCbId = nextCbId++;
            var overlapGfxCbId = nextCbId++;
            var joinGfxCbId = nextCbId++;

            syncBoundaries[candidateIndex] = new SyncBoundary
            {
                isValid = true,
                nextCommandBufferType = CommandQueueType.Compute,
                nextCommandBufferId = computeCbId,
                producerCommandBufferId = hasGraphicsProducer ? currentGfxCbId : -1
            };
            syncBoundaries[groupEndIndex + 1] = new SyncBoundary
            {
                isValid = true,
                nextCommandBufferType = CommandQueueType.Graphics,
                nextCommandBufferId = overlapGfxCbId,
                producerCommandBufferId = -1
            };
            syncBoundaries[joinIndex] = new SyncBoundary
            {
                isValid = true,
                nextCommandBufferType = CommandQueueType.Graphics,
                nextCommandBufferId = joinGfxCbId,
                producerCommandBufferId = computeCbId
            };

            currentGfxCbId = joinGfxCbId;
            candidateIndex = joinIndex;
        }
    }

    private static void FinalizeScheduleReachability(ReadOnlySpan<CommandQueueType> effectiveQueues, ReadOnlySpan<SyncBoundary> syncBoundaries, Span<int> commandBufferIds, ref RenderGraphBitMatrix reachability)
    {
        var passCount = effectiveQueues.Length;
        if (passCount == 0)
        {
            return;
        }

        using var scope = AllocationManager.CreateStackScope();
        using var queueMasks = new RenderGraphBitMatrix(2, passCount, scope.AllocationHandle);

        var currentCommandBufferId = 0;
        for (var passIndex = 0; passIndex < passCount; passIndex++)
        {
            ref readonly var boundary = ref syncBoundaries[passIndex];
            if (boundary.isValid)
            {
                currentCommandBufferId = boundary.nextCommandBufferId;
            }

            commandBufferIds[passIndex] = currentCommandBufferId;
            queueMasks.Set((int)effectiveQueues[passIndex], passIndex);
        }

        var wordCount = reachability.WordCount;
        for (var source = 0; source < passCount - 1; source++)
        {
            var queueRow = queueMasks.GetReadOnlyRow((int)effectiveQueues[source]);
            var sourceRow = reachability.GetRow(source);
            var startCol = source + 1;
            var startWord = startCol >> 6;
            var firstMask = ~0UL << (startCol & 63);
            sourceRow[startWord] |= queueRow[startWord] & firstMask;
            for (var w = startWord + 1; w < wordCount; w++)
            {
                sourceRow[w] |= queueRow[w];
            }
        }

        for (var destination = 0; destination < passCount; destination++)
        {
            ref readonly var boundary = ref syncBoundaries[destination];
            if (!boundary.isValid || boundary.producerCommandBufferId < 0)
            {
                continue;
            }

            for (var source = 0; source < destination; source++)
            {
                if (commandBufferIds[source] == boundary.producerCommandBufferId)
                {
                    reachability.Set(source, destination);
                }
            }
        }

        // All relations point forward in recording order. Reverse propagation closes the
        // command-buffer ordering without introducing a second graph representation.
        for (var source = passCount - 1; source >= 0; source--)
        {
            var startCol = source + 1;
            if (startCol >= passCount)
            {
                continue;
            }

            var minWord = startCol >> 6;
            var firstMask = ~0UL << (startCol & 63);
            for (var w = minWord; w < wordCount; w++)
            {
                var bits = reachability.GetReadOnlyRow(source)[w];
                if (w == minWord)
                {
                    bits &= firstMask;
                }

                while (bits != 0)
                {
                    var bit = BitOperations.TrailingZeroCount(bits);
                    bits &= bits - 1;
                    var intermediate = (w << 6) + bit;
                    reachability.OrRow(source, intermediate, w);
                }
            }
        }
    }

    private static bool IsAsyncComputeCandidate(RenderGraphPass pass)
    {
        return pass.type == RenderPassType.Compute
            && pass.asyncCompute
            && !pass.hasSideEffects
            && pass.maxColorIndex < 0
            && pass.depthAccess.id.IsInvalid
            && pass.renderTargetWrites.Count == 0
            && pass.depthTargetWrites.Count == 0
            && pass.depthTargetReads.Count == 0;
    }

    private static int FindFirstDependent(int sourceIndex, in RenderGraphBitMatrix reachability)
    {
        return reachability.FindFirstSetBitAfter(sourceIndex, sourceIndex);
    }

    private static void RestorePassCulledFlags(CachedCompilation cached, List<RenderGraphPass> passes)
    {
        for (var i = 0; i < passes.Count && i < cached.passCulledFlags.Count; i++)
        {
            passes[i].culled = cached.passCulledFlags[i];
        }
    }

    private void BuildExecutionCommands(
        ref BufferWriter writer,
        List<RenderGraphPass> passes,
        ReadOnlySpan<int> compiledPasses,
        ReadOnlySpan<NativeRenderPass> nativePasses,
        ref AliasingPlan aliasingPlan,
        RenderGraphResourceOrdering resourceOrdering,
        ReadOnlySpan<CommandQueueType> effectiveQueues,
        ReadOnlySpan<SyncBoundary> syncBoundaries,
        ReadOnlySpan<int> commandBufferIds,
        in RenderGraphBitMatrix reachability,
        RGFlags flags)
    {
        using var scope = AllocationManager.CreateStackScope();
        using var usageRanges = new UnsafeArray<PassResourceUsageRange>(compiledPasses.Length, scope.AllocationHandle, AllocationOption.Clear);
        using var resourceStates = new UnsafeArray<CompiledResourceState>(
            _resourceRegistry.ResourceCount,
            scope.AllocationHandle,
            AllocationOption.Clear);
        for (var i = 0; i < _resourceRegistry.ResourceCount; i++)
        {
            ref readonly var res = ref _resourceRegistry.GetResourceByIndex(i);
            if (res.isImported && res.hasInitialBarrierState)
            {
                resourceStates[i] = new CompiledResourceState(res.initialBarrierState, writes: false);
            }
            else
            {
                resourceStates[i] = CompiledResourceState.Undefined;
            }
        }
        using var usageRecords = BuildPassResourceUsagePlan(
            passes,
            compiledPasses,
            usageRanges.AsSpan(),
            scope.AllocationHandle);
        using var handoffReleaseHeads = new UnsafeArray<int>(compiledPasses.Length, scope.AllocationHandle);
        using var handoffAcquireHeads = new UnsafeArray<int>(compiledPasses.Length, scope.AllocationHandle);
        using var handoffs = BuildQueueHandoffs(
            usageRecords.AsSpan(),
            usageRanges.AsSpan(),
            effectiveQueues,
            commandBufferIds,
            in reachability,
            ref aliasingPlan,
            resourceOrdering,
            handoffReleaseHeads.AsSpan(),
            handoffAcquireHeads.AsSpan(),
            scope.AllocationHandle);

        using var splitPlans = new UnsafeList<SplitBarrierPlan>(_resourceRegistry.ResourceCount, scope.AllocationHandle);
        using var splitBeginHeads = new UnsafeArray<int>(compiledPasses.Length, scope.AllocationHandle);
        splitBeginHeads.AsSpan().Fill(-1);
        using var activeSplitPlans = new UnsafeArray<ActiveSplitPlan>(_resourceRegistry.ResourceCount, scope.AllocationHandle, AllocationOption.Clear);

        if (!flags.HasFlag(RGFlags.DisableSplitBarriers))
        {
            BuildSplitBarrierPlan(
                passes,
                compiledPasses,
                usageRanges.AsSpan(),
                usageRecords.AsSpan(),
                nativePasses,
                effectiveQueues,
                commandBufferIds,
                &splitPlans,
                splitBeginHeads.AsSpan());
        }

        var nativePassIndex = 0;
        var commandBufferId = 0;
        var activeQueue = CommandQueueType.Graphics;
        var idx = 0;

        while (idx < compiledPasses.Length)
        {
            ref readonly var boundary = ref syncBoundaries[idx];
            if (boundary.isValid)
            {
                EmitQueueReleaseBarriers(ref writer, handoffs.AsSpan(), handoffReleaseHeads.AsSpan(), idx);
                if (boundary.nextCommandBufferId != commandBufferId + 1)
                {
                    throw new InvalidOperationException("Render-graph sync boundaries must assign contiguous relative command-buffer IDs.");
                }

                WriteSyncBoundary(ref writer, in boundary);
                activeQueue = boundary.nextCommandBufferType;
                commandBufferId = boundary.nextCommandBufferId;
            }

            if (effectiveQueues[idx] != activeQueue)
            {
                throw new InvalidOperationException("Render-graph pass queue assignment does not match its structural sync boundaries.");
            }

            var logicalPassIndex = compiledPasses[idx];
            var pass = passes[logicalPassIndex];

            if (pass.type == RenderPassType.Raster && nativePassIndex < nativePasses.Length)
            {
                var nativePass = nativePasses[nativePassIndex];
                for (var mergedOffset = 1; mergedOffset < nativePass.mergedPassIndices.Count; mergedOffset++)
                {
                    if (syncBoundaries[idx + mergedOffset].isValid)
                    {
                        throw new InvalidOperationException("A native render pass cannot contain a command-buffer sync boundary.");
                    }
                }

                // Issue all prologue barriers (handoff acquire + transitions) for all merged passes before beginning native pass
                EmitPassPrologueBarriersForMergedPasses(
                    usageRecords.AsSpan(),
                    usageRanges.AsSpan(),
                    idx,
                    nativePass.mergedPassIndices.Count,
                    effectiveQueues,
                    commandBufferId,
                    ref writer,
                    ref aliasingPlan,
                    resourceOrdering,
                    resourceStates.AsSpan(),
                    handoffs.AsSpan(),
                    handoffAcquireHeads.AsSpan(),
                    splitBeginHeads.AsSpan(),
                    splitPlans.AsSpan(),
                    activeSplitPlans.AsSpan());

                // Begin Native Render Pass
                writer.Write(RGExecutionOpType.BeginNativePass);
                writer.Write(nativePassIndex);

                // Execute merged passes
                for (var i = 0; i < nativePass.mergedPassIndices.Count; i++)
                {
                    var mergedPassIdx = nativePass.mergedPassIndices[i];
                    writer.Write(RGExecutionOpType.ExecutePass);
                    writer.Write(mergedPassIdx);
                    idx++;
                }

                // End Native Render Pass
                writer.Write(RGExecutionOpType.EndNativePass);
                nativePassIndex++;
            }
            else
            {
                // Non-raster pass (Compute or Unsafe)
                ref readonly var usageRange = ref usageRanges[idx];
                EmitPassPrologueBarriers(
                    usageRecords.AsSpan().Slice(usageRange.start, usageRange.count),
                    idx,
                    effectiveQueues[idx],
                    commandBufferId,
                    ref writer,
                    ref aliasingPlan,
                    resourceOrdering,
                    resourceStates.AsSpan(),
                    handoffs.AsSpan(),
                    handoffAcquireHeads.AsSpan(),
                    splitBeginHeads.AsSpan(),
                    splitPlans.AsSpan(),
                    activeSplitPlans.AsSpan());

                writer.Write(RGExecutionOpType.ExecutePass);
                writer.Write(logicalPassIndex);
                idx++;
            }
        }

        EmitClosingBarriers(
            ref writer,
            activeQueue,
            resourceStates.AsSpan());
    }

    private static void WriteSyncBoundary(ref BufferWriter writer, scoped in SyncBoundary boundary)
    {
        if (boundary.producerCommandBufferId >= 0)
        {
            Span<int> producerIds = stackalloc int[1];
            producerIds[0] = boundary.producerCommandBufferId;
            RGCommandStream.WriteSyncMarker(
                ref writer,
                boundary.nextCommandBufferType,
                producerIds,
                boundary.nextCommandBufferId);
            return;
        }

        RGCommandStream.WriteSyncMarker(
            ref writer,
            boundary.nextCommandBufferType,
            ReadOnlySpan<int>.Empty,
            boundary.nextCommandBufferId);
    }

    public void Dispose()
    {
        _compilationCache.Dispose();
    }
}
