using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Graphics.RenderGraphModule;

internal struct CompiledGraph : IDisposable
{
    public required AliasingPlan plan;
    public required float2 scale;
    public required ulong graphHash;
    public required IReadOnlyList<RenderGraphPass> passes;
    public required ReadOnlyView<int> compiledPasses;
    public required ReadOnlyView<NativeRenderPass> nativePasses;
    public required ReadOnlyView<byte> commandStream;
    public required bool cacheHit;

    public void Dispose()
    {
        plan.Dispose();
    }
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

    private struct SyncBoundary
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
    public Result<CompiledGraph, Error> Compile(
        in ViewState viewState,
        ulong graphHash,
        List<RenderGraphPass> passes,
        AllocationHandle allocationHandle)
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
        AliasingPlan aliasingPlan;

        // Try to restore from cache
        if (_compilationCache.TryGetCached(graphHash, out var cached))
        {
            // Check if view state changed
            var scale = viewState.CalculateScale(cached.viewState);
            if (math.any(scale > float2.one))
            {
                // View state changed - re-resolve sizes and recreate GPU resources
                _resourceRegistry.ResolveTextureSizes(in viewState);

                aliasingPlan = RenderGraphAliasingBuilder.Build(_resourceRegistry, _resourceAllocator, allocationHandle);
                error = _resourceRegistry.AllocateBackingResources(aliasingPlan, _compilationCache);
                if (error != Error.None)
                {
                    return error;
                }

                cached.viewState = viewState;

                return new CompiledGraph
                {
                    scale = float2.one,
                    plan = aliasingPlan,
                    graphHash = graphHash,
                    passes = passes,
                    compiledPasses = cached.compiledPassIndices,
                    nativePasses = cached.nativePasses,
                    commandStream = cached.commandBytes,
                    cacheHit = true
                };
            }
            else
            {
                // Perfect cache hit - restore everything
                aliasingPlan = RestoreFromCache(cached, passes, allocationHandle);
                _resourceRegistry.RestoreBackingResources(cached.backingResources);
                return new CompiledGraph
                {
                    scale = scale,
                    plan = aliasingPlan,
                    graphHash = graphHash,
                    passes = passes,
                    compiledPasses = cached.compiledPassIndices,
                    nativePasses = cached.nativePasses,
                    commandStream = cached.commandBytes,
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
            ReorderPasses(passes, nodes.AsSpan(), &compiledPasses);

            using var effectiveQueues = new UnsafeArray<CommandQueueType>(compiledPasses.Count, schedulingScope.AllocationHandle);
            using var syncBoundaries = new UnsafeArray<SyncBoundary>(compiledPasses.Count, schedulingScope.AllocationHandle);
            BuildDependencyWindowSchedule(
                passes,
                compiledPasses.AsSpan(),
                effectiveQueues.AsSpan(),
                syncBoundaries.AsSpan(),
                nodes.AsSpan(),
                schedulingScope.AllocationHandle);

            aliasingPlan = RenderGraphAliasingBuilder.Build(_resourceRegistry, _resourceAllocator, allocationHandle);
            error = _resourceRegistry.AllocateBackingResources(aliasingPlan, _compilationCache);
            if (error != Error.None)
            {
                return error;
            }

            var nativePasses = RenderGraphNativePassBuilder.BuildNativeRenderPasses(_resourceRegistry, passes, compiledPasses, aliasingPlan, allocationHandle);
            var commandWriter = new BufferWriter(1024 * 1024, allocationHandle);

            try
            {
                BuildExecutionCommands(
                    ref commandWriter,
                    passes,
                    compiledPasses,
                    nativePasses,
                    aliasingPlan,
                    effectiveQueues.AsSpan(),
                    syncBoundaries.AsSpan());

                ref readonly var cacheData = ref _compilationCache.SetCached(
                    _resourceRegistry,
                    graphHash,
                    viewState,
                    passes,
                    compiledPasses,
                    nativePasses,
                    commandWriter.AsSpan(),
                    aliasingPlan,
                    allocationHandle);

                return new CompiledGraph
                {
                    scale = float2.one,
                    plan = aliasingPlan,
                    graphHash = graphHash,
                    passes = passes,
                    compiledPasses = cacheData.compiledPassIndices,
                    nativePasses = cacheData.nativePasses,
                    commandStream = cacheData.commandBytes,
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
        ref readonly var res = ref _resourceRegistry.GetResource(resource);
        foreach (var producerIdx in res.producerPasses)
        {
            var producer = passes[producerIdx];
            if (producer.culled)
            {
                producer.culled = false;
                UncullDependencies(producer, passes);
            }
        }
    }

    private static UnsafeArray<PassDependencyNode> BuildDAG(
        List<RenderGraphPass> passes,
        ReadOnlySpan<int> compiledPasses,
        RenderGraphResourceRegistry resources,
        AllocationHandle allocationHandle)
    {
        void AddEdge(int passA, int passB, Span<PassDependencyNode> nodes)
        {
            if (passA == passB)
            {
                return;
            }

            ref var nodeA = ref nodes[passA];
            if (!nodeA.dependents.Contains(passB))
            {
                nodeA.dependents.Add(passB);
                nodes[passB].inDegree++;
            }
        }

        var passCount = compiledPasses.Length;
        var resourceCount = resources.ResourceCount;

        // Track last access per resource. These use the caller-owned scheduling scope so
        // returning from BuildDAG cannot rewind and invalidate the graph allocations.
        using var lastWriter = new UnsafeArray<int>(resourceCount, allocationHandle);
        lastWriter.AsSpan().Fill(-1);

        // Track readers since last write per resource
        using var lastReaders = new UnsafeList<UnsafeList<int>>(resourceCount, allocationHandle);
        for (var i = 0; i < resourceCount; i++)
        {
            lastReaders.Add(new UnsafeList<int>(4, allocationHandle));
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
            void ProcessRead(Identifier<RGResource> resId, Span<PassDependencyNode> nodes, Span<int> lastWriter)
            {
                if (resId.IsInvalid)
                {
                    return;
                }

                var resIdx = resId.Value;
                // RAW: Depend on previous writer
                var writer = lastWriter[resIdx];
                if (writer >= 0)
                {
                    AddEdge(writer, i, nodes);
                }

                // Track this pass as a reader
                if (!lastReaders[resIdx].AsSpan().Contains(i))
                {
                    lastReaders[resIdx].Add(i);
                }
            }

            // Process WRITES (WAR & WAW Dependencies)
            void ProcessWrite(Identifier<RGResource> resId, Span<PassDependencyNode> nodes, Span<int> lastWriter)
            {
                if (resId.IsInvalid)
                {
                    return;
                }

                var resIdx = resId.Value;

                // WAW: Depend on previous writer
                var writer = lastWriter[resIdx];
                if (writer >= 0)
                {
                    AddEdge(writer, i, nodes);
                }

                // WAR: Depend on all readers since last write
                ref var readers = ref lastReaders[resIdx];
                for (var r = 0; r < readers.Count; r++)
                {
                    AddEdge(readers[r], i, nodes);
                }

                // Reset readers and update last writer
                readers.Clear();
                lastWriter[resIdx] = i;
            }

            // Collect creation, read, and write hazards from the canonical declarations.
            for (var t = 0; t < (int)RGResourceType.Count; t++)
            {
                foreach (var res in pass.resourceCreates[t])
                {
                    ProcessWrite(res, nodes, lastWriter);
                }

                foreach (var res in pass.resourceReads[t])
                {
                    ProcessRead(res, nodes, lastWriter);
                }

                foreach (var res in pass.resourceWrites[t])
                {
                    ProcessWrite(res, nodes, lastWriter);
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
                        ProcessRead(res, nodes, lastWriter);
                    }

                    if (access.accessFlags.HasFlag(AccessFlags.Write))
                    {
                        ProcessWrite(res, nodes, lastWriter);
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
                    ProcessRead(res, nodes, lastWriter);
                }

                if (pass.depthAccess.accessFlags.HasFlag(AccessFlags.Write))
                {
                    ProcessWrite(res, nodes, lastWriter);
                }
#pragma warning restore MHP002 // Defensive copy detected
            }

            // UAVs (Random Access is both READ and WRITE)
            foreach (var uav in pass.randomAccess)
            {
                ProcessRead(uav, nodes, lastWriter);
                ProcessWrite(uav, nodes, lastWriter);
            }

            if (pass.hasSideEffects)
            {
                if (lastSideEffect >= 0)
                {
                    AddEdge(lastSideEffect, i, nodes);
                }

                lastSideEffect = i;
            }
        }

        for (var i = 0; i < lastReaders.Count; i++)
        {
            lastReaders[i].Dispose();
        }

        return nodes;
    }
    private static int SelectBestCandidatePass(
        ReadOnlySpan<int> readyPassIndices,
        RenderGraphPass? lastScheduledPass,
        List<RenderGraphPass> passes,
        ReadOnlySpan<int> compiledPasses)
    {
        var bestIndex = 0;
        var maxScore = float.MinValue;
        for (var i = 0; i < readyPassIndices.Length; i++)
        {
            var candidate = passes[compiledPasses[readyPassIndices[i]]];
            var score = 0.0f;
            if (lastScheduledPass != null)
            {
                // Grouping Rule 1: Maximize Native Pass Merging
                // If previous pass was Raster, strongly prefer a ready Raster pass with matching attachments
                if (lastScheduledPass.type == RenderPassType.Raster && candidate.type == RenderPassType.Raster)
                {
                    if (AttachmentsMatch(lastScheduledPass, candidate))
                    {
                        score += 1000.0f; // Top priority: merge into native render pass!
                    }
                }
                // Grouping Rule 2: Memory Locality (Shorten Lifetimes)
                // Prefer passes that consume or produce resources touched by lastScheduledPass
                if (SharesResources(lastScheduledPass, candidate))
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
            score -= readyPassIndices[i] * 0.01f;
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

    private static bool SharesResources(RenderGraphPass passA, RenderGraphPass passB)
    {
        for (var t = 0; t < (int)RGResourceType.Count; t++)
        {
            var readsB = passB.resourceReads[t];
            foreach (var readA in passA.resourceReads[t])
            {
                if (readsB.Contains(readA)) return true;
            }
        }

        return false;
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

        // Kahn's Topological Sort with Grouping Heuristics
        while (readyList.Count > 0)
        {
            // Select best candidate pass from readyList based on grouping rules
            var bestReadyIdx = SelectBestCandidatePass(readyList.AsSpan(), lastScheduledPass, passes, pCompiledPasses->AsSpan());
            var chosenPassIndex = readyList[bestReadyIdx];
            readyList.RemoveAtSwapBack(bestReadyIdx);

            var chosenPass = (*pCompiledPasses)[chosenPassIndex];
            reorderedIndices.Add(chosenPass);
            lastScheduledPass = passes[chosenPass];

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

        pCompiledPasses->Clear();
        pCompiledPasses->AddRange(reorderedIndices);
    }

    private void BuildDependencyWindowSchedule(
        List<RenderGraphPass> passes,
        ReadOnlySpan<int> compiledPasses,
        Span<CommandQueueType> effectiveQueues,
        Span<SyncBoundary> syncBoundaries,
        ReadOnlySpan<PassDependencyNode> dependencyNodes,
        AllocationHandle allocationHandle)
    {
        effectiveQueues.Fill(CommandQueueType.Graphics);
        syncBoundaries.Clear();

        var passCount = compiledPasses.Length;
        if (passCount < 4)
        {
            return;
        }

        using var dagIndexByPassIndex = new UnsafeArray<int>(passes.Count, allocationHandle);
        using var dagIndexByScheduleIndex = new UnsafeArray<int>(passCount, allocationHandle);
        using var scheduleIndexByDagIndex = new UnsafeArray<int>(passCount, allocationHandle);
        using var reachability = new UnsafeArray<byte>(passCount * passCount, allocationHandle);
        dagIndexByPassIndex.AsSpan().Fill(-1);
        reachability.AsSpan().Clear();

        for (var dagIndex = 0; dagIndex < passCount; dagIndex++)
        {
            dagIndexByPassIndex[dependencyNodes[dagIndex].passIndex] = dagIndex;
        }

        for (var scheduleIndex = 0; scheduleIndex < passCount; scheduleIndex++)
        {
            var dagIndex = dagIndexByPassIndex[compiledPasses[scheduleIndex]];
            Logger.DebugAssert(dagIndex >= 0);
            dagIndexByScheduleIndex[scheduleIndex] = dagIndex;
            scheduleIndexByDagIndex[dagIndex] = scheduleIndex;
        }

        for (var source = passCount - 1; source >= 0; source--)
        {
            ref readonly var sourceNode = ref dependencyNodes[dagIndexByScheduleIndex[source]];
            for (var edgeIndex = 0; edgeIndex < sourceNode.dependents.Count; edgeIndex++)
            {
                var dependent = scheduleIndexByDagIndex[sourceNode.dependents[edgeIndex]];
                Logger.DebugAssert(dependent > source);
                reachability[(source * passCount) + dependent] = 1;

                var dependentRow = dependent * passCount;
                var sourceRow = source * passCount;
                for (var destination = dependent + 1; destination < passCount; destination++)
                {
                    if (reachability[dependentRow + destination] != 0)
                    {
                        reachability[sourceRow + destination] = 1;
                    }
                }
            }
        }

        for (var candidateIndex = 1; candidateIndex < passCount; candidateIndex++)
        {
            var candidate = passes[compiledPasses[candidateIndex]];
                if (!IsAsyncComputeCandidate(candidate))
                {
                    continue;
                }

                var joinIndex = FindFirstDependent(candidateIndex, passCount, reachability.AsSpan());
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
                        || FindFirstDependent(nextIndex, passCount, reachability.AsSpan()) != joinIndex)
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
                        legalWindow = false;
                        break;
                    }

                    if (overlapPass.type == RenderPassType.Raster)
                    {
                        hasIndependentGraphicsWork = true;
                    }
                }

                if (!legalWindow || !hasIndependentGraphicsWork || WouldSplitNativePass(passes, compiledPasses, joinIndex))
                {
                    continue;
                }

                var hasGraphicsProducer = false;
                for (var producerIndex = 0; producerIndex < candidateIndex && !hasGraphicsProducer; producerIndex++)
                {
                    for (var computeIndex = candidateIndex; computeIndex <= groupEndIndex; computeIndex++)
                    {
                        if (reachability[(producerIndex * passCount) + computeIndex] != 0)
                        {
                            hasGraphicsProducer = true;
                            break;
                        }
                    }
                }

                for (var computeIndex = candidateIndex; computeIndex <= groupEndIndex; computeIndex++)
                {
                    effectiveQueues[computeIndex] = CommandQueueType.Compute;
                }

                syncBoundaries[candidateIndex] = new SyncBoundary
                {
                    isValid = true,
                    nextCommandBufferType = CommandQueueType.Compute,
                    nextCommandBufferId = 1,
                    producerCommandBufferId = hasGraphicsProducer ? 0 : -1
                };
                syncBoundaries[groupEndIndex + 1] = new SyncBoundary
                {
                    isValid = true,
                    nextCommandBufferType = CommandQueueType.Graphics,
                    nextCommandBufferId = 2,
                    producerCommandBufferId = -1
                };
                syncBoundaries[joinIndex] = new SyncBoundary
                {
                    isValid = true,
                    nextCommandBufferType = CommandQueueType.Graphics,
                    nextCommandBufferId = 3,
                    producerCommandBufferId = 1
                };

            // The initial planner materializes one active Compute region. Later candidates are
            // deterministically demoted by original pass order unless they joined this group.
            break;
        }
    }

    private static bool IsAsyncComputeCandidate(RenderGraphPass pass)
    {
        return pass.type == RenderPassType.Compute
            && pass.asyncCompute
            && !pass.hasSideEffects
            && pass.maxColorIndex < 0
            && pass.depthAccess.id.IsInvalid
            && pass.renderTargetWrites.Count == 0;
    }

    private static int FindFirstDependent(int sourceIndex, int passCount, ReadOnlySpan<byte> reachability)
    {
        var sourceRow = sourceIndex * passCount;
        for (var destinationIndex = sourceIndex + 1; destinationIndex < passCount; destinationIndex++)
        {
            if (reachability[sourceRow + destinationIndex] != 0)
            {
                return destinationIndex;
            }
        }

        return -1;
    }

    private static bool WouldSplitNativePass(List<RenderGraphPass> passes, ReadOnlySpan<int> compiledPasses, int joinIndex)
    {
        var beforeJoin = passes[compiledPasses[joinIndex - 1]];
        var joinPass = passes[compiledPasses[joinIndex]];
        return beforeJoin.type == RenderPassType.Raster
            && joinPass.type == RenderPassType.Raster
            && beforeJoin.randomAccess.Count == 0
            && joinPass.randomAccess.Count == 0
            && AttachmentsMatch(beforeJoin, joinPass);
    }

    private static AliasingPlan RestoreFromCache(
        CachedCompilation cached,
        List<RenderGraphPass> passes,
        AllocationHandle allocationHandle)
    {
        for (var i = 0; i < passes.Count && i < cached.passCulledFlags.Count; i++)
        {
            passes[i].culled = cached.passCulledFlags[i];
        }

        return RenderGraphAliasingBuilder.RestoreFromCache(
            cached.logicalToPhysical,
            cached.placedResources,
            cached.aliasedLogicalResources,
            cached.totalHeapSize,
            allocationHandle);
    }

    private void BuildExecutionCommands(
        ref BufferWriter writer,
        List<RenderGraphPass> passes,
        ReadOnlySpan<int> compiledPasses,
        ReadOnlySpan<NativeRenderPass> nativePasses,
        AliasingPlan aliasingPlan,
        ReadOnlySpan<CommandQueueType> effectiveQueues,
        ReadOnlySpan<SyncBoundary> syncBoundaries)
    {
        var nativePassIndex = 0;
        var commandBufferId = 0;
        var activeQueue = CommandQueueType.Graphics;
        var idx = 0;

        while (idx < compiledPasses.Length)
        {
            ref readonly var boundary = ref syncBoundaries[idx];
            if (boundary.isValid)
            {
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

                // 1. Issue barriers for all merged passes before beginning native pass
                for (var i = 0; i < nativePass.mergedPassIndices.Count; i++)
                {
                    var mergedPassIdx = nativePass.mergedPassIndices[i];
                    var mergedPass = passes[mergedPassIdx];
                    EmitBarriersForPass(mergedPass, mergedPassIdx, ref writer, aliasingPlan);
                }

                // 2. Begin Native Render Pass
                writer.Write(RGExecutionOpType.BeginNativePass);
                writer.Write(nativePassIndex);

                // 3. Execute merged passes
                for (var i = 0; i < nativePass.mergedPassIndices.Count; i++)
                {
                    var mergedPassIdx = nativePass.mergedPassIndices[i];
                    writer.Write(RGExecutionOpType.ExecutePass);
                    writer.Write(mergedPassIdx);
                    idx++;
                }

                // 4. End Native Render Pass
                writer.Write(RGExecutionOpType.EndNativePass);
                nativePassIndex++;
            }
            else
            {
                // Non-raster pass (Compute or Unsafe)
                EmitBarriersForPass(pass, logicalPassIndex, ref writer, aliasingPlan);

                writer.Write(RGExecutionOpType.ExecutePass);
                writer.Write(logicalPassIndex);
                idx++;
            }
        }
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
