using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.Mathematics;
using Misaki.HighPerformance.Utilities;

namespace Ghost.Graphics.RenderGraphModule;

internal struct CompiledGraph : IDisposable
{
    public required AliasingPlan plan;
    public required float2 scale;
    public required ulong graphHash;
    public required IReadOnlyList<RenderGraphPass> compiledPasses;
    public required IReadOnlyList<NativeRenderPass> nativePasses;
    public required BufferReader commandReader;
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
internal partial class RenderGraphCompiler
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

    private readonly IResourceAllocator _resourceAllocator;
    private readonly RenderGraphResourceRegistry _resources;
    private readonly RenderGraphNativePassBuilder _nativePassBuilder;
    private readonly RenderGraphCompilationCache _compilationCache;

    // TODO: We should store compiledPasses as indices, nativePasses as unmanaged struct, compiledBarriers as command ops.
    private readonly List<RenderGraphPass> _compiledPasses;
    private readonly List<NativeRenderPass> _nativePasses;

    public RenderGraphCompiler(
        IResourceAllocator resourceAllocator,
        RenderGraphResourceRegistry resources,
        RenderGraphNativePassBuilder nativePassBuilder,
        RenderGraphCompilationCache compilationCache)
    {
        _resourceAllocator = resourceAllocator;
        _resources = resources;
        _nativePassBuilder = nativePassBuilder;
        _compilationCache = compilationCache;

        _compiledPasses = new List<RenderGraphPass>(64);
        _nativePasses = new List<NativeRenderPass>(32);
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
                _resources.ResolveTextureSizes(in viewState);

                aliasingPlan = RenderGraphAliasingBuilder.Build(_resources, _resourceAllocator, allocationHandle);
                error = _resources.AllocateBackingResources(aliasingPlan, _compilationCache);
                if (error != Error.None)
                {
                    return error;
                }

                cached.viewState = viewState;
                _nativePassBuilder.BuildNativeRenderPasses(_compiledPasses, _nativePasses, _resources, aliasingPlan);

                return new CompiledGraph
                {
                    scale = float2.one,
                    plan = aliasingPlan,
                    graphHash = graphHash,
                    compiledPasses = _compiledPasses,
                    nativePasses = _nativePasses,
                    commandReader = cached.commandBytes.AsReader(),
                    cacheHit = true
                };
            }
            else
            {
                // Perfect cache hit - restore everything
                aliasingPlan = RestoreFromCache(cached, passes, allocationHandle);
                _resources.RestoreBackingResources(cached.backingResources);
                return new CompiledGraph
                {
                    scale = scale,
                    plan = aliasingPlan,
                    graphHash = graphHash,
                    compiledPasses = _compiledPasses,
                    nativePasses = _nativePasses,
                    commandReader = cached.commandBytes.AsReader(),
                    cacheHit = true
                };
            }
        }

        // Fresh compilation needed
        _compiledPasses.Clear();

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
                _compiledPasses.Add(pass);
            }
        }

        // Reorder passes to ensure best performance
        ReorderPasses(_compiledPasses);

        aliasingPlan = RenderGraphAliasingBuilder.Build(_resources, _resourceAllocator, allocationHandle);
        error = _resources.AllocateBackingResources(aliasingPlan, _compilationCache);
        if (error != Error.None)
        {
            return error;
        }

        _nativePassBuilder.BuildNativeRenderPasses(_compiledPasses, _nativePasses, _resources, aliasingPlan);

        ref var cacheData = ref _compilationCache.PrepareForStore(graphHash, in viewState);
        PopulateCacheData(ref cacheData, passes, aliasingPlan);
        BuildExecutionCommands(ref cacheData.commandBytes, aliasingPlan);

        return new CompiledGraph
        {
            scale = float2.one,
            plan = aliasingPlan,
            graphHash = graphHash,
            compiledPasses = _compiledPasses,
            nativePasses = _nativePasses,
            commandReader = cacheData.commandBytes.AsReader(),
            cacheHit = false
        };
    }

    private void MarkPassesWithSideEffects(List<RenderGraphPass> passes)
    {
        for (var i = 0; i < passes.Count; i++)
        {
            var pass = passes[i];

            for (var j = 0; j < (int)RGResourceType.Count; j++)
            {
                var writeList = pass.resourceWrites[j];
                for (var k = 0; k < writeList.Count; k++)
                {
                    var writeHandle = writeList[k];
                    ref readonly var resource = ref _resources.GetResource(writeHandle);
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
            for (var j = 0; j < readList.Count; j++)
            {
                UncullProducer(readList[j], passes);
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

        for (var i = 0; i < pass.randomAccess.Count; i++)
        {
            UncullProducer(pass.randomAccess[i], passes);
        }
    }

    private void UncullProducer(Identifier<RGResource> resource, List<RenderGraphPass> passes)
    {
        ref readonly var res = ref _resources.GetResource(resource);
        if (res.producerPass >= 0)
        {
            var producer = passes[res.producerPass];
            if (producer.culled)
            {
                producer.culled = false;
                UncullDependencies(producer, passes);
            }
        }
    }

    private static void BuildDAG(
        List<RenderGraphPass> compiledPasses,
        RenderGraphResourceRegistry resources,
        Span<PassDependencyNode> nodes,
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

        var passCount = compiledPasses.Count;
        var resourceCount = resources.ResourceCount;

        using var scope = AllocationManager.CreateStackScope();

        // Track last access per resource
        using var lastWriter = new UnsafeArray<int>(resourceCount, scope.AllocationHandle);
        lastWriter.AsSpan().Fill(-1);

        // Track readers since last write per resource
        using var lastReaders = new UnsafeList<UnsafeList<int>>(resourceCount, scope.AllocationHandle);
        for (var i = 0; i < resourceCount; i++)
        {
            lastReaders.Add(new UnsafeList<int>(4, scope.AllocationHandle));
        }

        // Initialize nodes
        for (var i = 0; i < passCount; i++)
        {
            nodes[i] = new PassDependencyNode(i, allocationHandle);
        }

        // Iterate over non-culled passes
        for (var i = 0; i < passCount; i++)
        {
            var pass = compiledPasses[i];

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

            // Collect Reads & Writes from Pass
            // Inputs (SRVs)
            for (var t = 0; t < (int)RGResourceType.Count; t++)
            {
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
        }

        for (var i = 0; i < lastReaders.Count; i++)
        {
            lastReaders[i].Dispose();
        }
    }
    private static int SelectBestCandidatePass(
        ReadOnlySpan<int> readyPassIndices,
        RenderGraphPass? lastScheduledPass,
        List<RenderGraphPass> compiledPasses)
    {
        var bestIndex = 0;
        var maxScore = float.MinValue;
        for (var i = 0; i < readyPassIndices.Length; i++)
        {
            var candidate = compiledPasses[readyPassIndices[i]];
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
            foreach (var rA in passA.resourceWrites[t])
            {
                if (passB.resourceReads[t].AsSpan().Contains(rA))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void ReorderPasses(List<RenderGraphPass> compiledPasses)
    {
        var passCount = compiledPasses.Count;
        if (passCount <= 1)
        {
            return;
        }

        // Build DAG Node array
        using var scope = AllocationManager.CreateStackScope();
        var nodes = new PassDependencyNode[passCount];

        BuildDAG(compiledPasses, _resources, nodes, scope.AllocationHandle);

        // Initialize Ready List (Passes with 0 incoming dependencies)
        using var readyList = new UnsafeList<int>(passCount, scope.AllocationHandle);
        for (var i = 0; i < passCount; i++)
        {
            if (nodes[i].inDegree == 0)
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
            var bestReadyIdx = SelectBestCandidatePass(readyList.AsSpan(), lastScheduledPass, compiledPasses);
            var chosenPassIndex = readyList[bestReadyIdx];
            readyList.RemoveAtSwapBack(bestReadyIdx);
            reorderedIndices.Add(chosenPassIndex);
            var chosenPass = compiledPasses[chosenPassIndex];
            lastScheduledPass = chosenPass;

            // Decrement in-degree for all downstream dependent passes
            ref var chosenNode = ref nodes[chosenPassIndex];
            for (var i = 0; i < chosenNode.dependents.Count; i++)
            {
                var dstIdx = chosenNode.dependents[i];
                ref var dstNode = ref nodes[dstIdx];

                dstNode.inDegree--;
                if (dstNode.inDegree == 0)
                {
                    readyList.Add(dstIdx);
                }
            }
        }

        // Update _compiledPasses with the reordered sequence
        // (If for any reason a cycle occurred, reorderedIndices count matches passCount)
        if (reorderedIndices.Count == passCount)
        {
            var tempPasses = new List<RenderGraphPass>(passCount);
            for (var i = 0; i < passCount; i++)
            {
                tempPasses.Add(compiledPasses[reorderedIndices[i]]);
            }

            compiledPasses.Clear();
            for (var i = 0; i < passCount; i++)
            {
                compiledPasses.Add(tempPasses[i]);
            }
        }

        for (var i = 0; i < passCount; i++)
        {
            nodes[i].dependents.Dispose();
        }
    }

    private AliasingPlan RestoreFromCache(
        CachedCompilation cached,
        List<RenderGraphPass> passes,
        AllocationHandle allocationHandle)
    {
        _compiledPasses.Clear();
        for (var i = 0; i < cached.compiledPassIndices.Count; i++)
        {
            var passIndex = cached.compiledPassIndices[i];
            _compiledPasses.Add(passes[passIndex]);
        }

        for (var i = 0; i < passes.Count && i < cached.passCulledFlags.Count; i++)
        {
            passes[i].culled = cached.passCulledFlags[i];
        }

        var plan = RenderGraphAliasingBuilder.RestoreFromCache(ref cached.logicalToPhysical, ref cached.placedResources, allocationHandle);

        // TODO: We should store native passes in cache as well, but for now we can just rebuild them since they are cheap to construct.
        _nativePassBuilder.BuildNativeRenderPasses(_compiledPasses, _nativePasses, _resources, plan);

        return plan;
    }

    private void BuildExecutionCommands(ref BufferWriter writer, AliasingPlan aliasingPlan)
    {
        var nativePassIndex = 0;
        var logicalPassIndex = 0;
        var currentQueue = CommandQueueType.Graphics;
        var nextFenceValue = 1UL;
        var fenceId = 1;

        while (logicalPassIndex < _compiledPasses.Count)
        {
            var pass = _compiledPasses[logicalPassIndex];
            var passQueue = (pass.asyncCompute && pass.type == RenderPassType.Compute)
                ? CommandQueueType.Compute
                : CommandQueueType.Graphics;

            if (passQueue != currentQueue)
            {
                // Emit cross-queue fence signal & submit on current queue
                writer.Write(RGExecutionOpType.SignalFence);
                writer.Write((byte)currentQueue);
                writer.Write(fenceId);
                writer.Write(nextFenceValue);

                writer.Write(RGExecutionOpType.SubmitQueue);
                writer.Write((byte)currentQueue);

                // Emit GPU wait on destination queue
                writer.Write(RGExecutionOpType.GPUWait);
                writer.Write((byte)passQueue);
                writer.Write(fenceId);
                writer.Write(nextFenceValue);

                currentQueue = passQueue;
                nextFenceValue++;
            }

            if (pass.type == RenderPassType.Raster && nativePassIndex < _nativePasses.Count)
            {
                var nativePass = _nativePasses[nativePassIndex];

                // 1. Issue barriers for all merged passes before beginning native pass
                for (var i = 0; i < nativePass.mergedPassIndices.Count; i++)
                {
                    var mergedPassIdx = nativePass.mergedPassIndices[i];
                    var mergedPass = _compiledPasses[mergedPassIdx];
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
                    logicalPassIndex++;
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
                logicalPassIndex++;
            }
        }

        if (currentQueue != CommandQueueType.Graphics)
        {
            writer.Write(RGExecutionOpType.SignalFence);
            writer.Write((byte)currentQueue);
            writer.Write(fenceId);
            writer.Write(nextFenceValue);

            writer.Write(RGExecutionOpType.SubmitQueue);
            writer.Write((byte)currentQueue);

            writer.Write(RGExecutionOpType.GPUWait);
            writer.Write((byte)CommandQueueType.Graphics);
            writer.Write(fenceId);
            writer.Write(nextFenceValue);
        }
    }

    private void PopulateCacheData(
        ref CachedCompilation cacheData,
        List<RenderGraphPass> passes,
        AliasingPlan aliasingPlan)
    {
        for (var i = 0; i < _compiledPasses.Count; i++)
        {
            cacheData.compiledPassIndices.Add(_compiledPasses[i].index);
        }

        for (var i = 0; i < passes.Count; i++)
        {
            cacheData.passCulledFlags.Add(passes[i].culled);
        }

        aliasingPlan.StoreToCache(ref cacheData.logicalToPhysical, ref cacheData.placedResources);

        for (var i = 0; i < _resources.ResourceCount; i++)
        {
            var res = _resources.Resources[i];
            cacheData.backingResources.Add(res.backingResource);
        }
    }
}
