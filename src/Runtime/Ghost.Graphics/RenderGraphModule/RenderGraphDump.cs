using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.Graphics.RHI;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ghost.Graphics.RenderGraphModule;

/// <summary>
/// Diagnostic description of one structural command-buffer boundary adjacent to a pass.
/// </summary>
public readonly struct PassSyncBoundaryDumpInfo
{
    /// <summary>Type of the command buffer that ends at the boundary.</summary>
    public CommandQueueType SourceType
    {
        get; init;
    }

    /// <summary>Type of the command buffer that begins at the boundary.</summary>
    public CommandQueueType DestinationType
    {
        get; init;
    }

    /// <summary>Relative producer command-buffer IDs required by the destination.</summary>
    public int[] ProducerCommandBufferIds
    {
        get; init;
    }

    /// <summary>Command-buffer types corresponding to <see cref="ProducerCommandBufferIds"/>.</summary>
    public CommandQueueType[] ProducerTypes
    {
        get; init;
    }
}

public readonly struct HeapBlockDumpInfo
{
    public ulong Offset
    {
        get; init;
    }

    public ulong Size
    {
        get; init;
    }

    public bool IsFree
    {
        get; init;
    }

    // Resources occupying this memory block
    public List<int> AliasedLogicalResources
    {
        get; init;
    }
}

public readonly struct PassDumpInfo
{
    public int Index
    {
        get; init;
    }

    public string Name
    {
        get; init;
    }

    public RenderPassType Type
    {
        get; init;
    }

    public bool IsCulled
    {
        get; init;
    }

    public bool AsyncCompute
    {
        get; init;
    }

    public bool AsyncRequested
    {
        get; init;
    }

    public bool AllowAsyncComputeOverlap
    {
        get; init;
    }

    public CommandQueueType? EffectiveQueue
    {
        get; init;
    }

    /// <summary>Explains the effective command-buffer assignment.</summary>
    public RGQueueDecision QueueDecision
    {
        get; init;
    }

    /// <summary>Structural command-buffer boundary immediately before this pass, when present.</summary>
    public PassSyncBoundaryDumpInfo? SyncBoundaryBefore
    {
        get; init;
    }

    /// <summary>Structural command-buffer boundary immediately after this pass, when present.</summary>
    public PassSyncBoundaryDumpInfo? SyncBoundaryAfter
    {
        get; init;
    }

    public int NativePassIndex
    {
        get; init;
    }

    public List<int> ResourceReads
    {
        get; init;
    }

    public List<int> ResourceWrites
    {
        get; init;
    }

    public List<int> ResourceCreates
    {
        get; init;
    }
}

public readonly struct ResourceDumpInfo
{
    public int LogicalResourceId
    {
        get; init;
    }

    public Handle<GPUResource> BackingResource
    {
        get; init;
    }

    public string Name
    {
        get; init;
    }

    public RGResourceType Type
    {
        get; init;
    }

    public ulong SizeInBytes
    {
        get; init;
    }

    public bool IsImported
    {
        get; init;
    }

    public bool IsExtracted
    {
        get; init;
    }

    public ulong HeapOffset
    {
        get; init;
    }

    public int FirstUsePass
    {
        get; init;
    }

    public int LastUsePass
    {
        get; init;
    }

    public int ScheduledFirstUseIndex
    {
        get; init;
    }

    public int ScheduledLastUseIndex
    {
        get; init;
    }

    public int[] ProducerPass
    {
        get; init;
    }

    public int[] ConsumerPasses
    {
        get; init;
    }

    // Other resources sharing the same heap offset range
    public List<int> AliasedWithResources
    {
        get; init;
    }
}

public sealed class RenderGraphDump
{
    public ulong GraphHash
    {
        get; init;
    }

    public bool IsCacheHit
    {
        get; init;
    }

    public ViewportState Viewport
    {
        get; init;
    }

    // Memory Heap Aliasing Map
    public ulong TotalHeapSize
    {
        get; init;
    }

    public ulong SizeWithoutAliasing
    {
        get; init;
    }

    public List<HeapBlockDumpInfo> MemoryBlocks
    {
        get; init;
    } = new();

    // Complete Pass List (with culling & merge info)
    public List<PassDumpInfo> Passes
    {
        get; init;
    } = new();

    // Disassembled Binary Command Stream
    public List<string> CommandStream
    {
        get; init;
    } = new();

    // Complete Resource List (with offsets & lifetimes)
    public List<ResourceDumpInfo> Resources
    {
        get; init;
    } = new();

    private static List<string> DisassembleCommandStream(RenderGraphResourceRegistry resourceRegistry, IReadOnlyList<RenderGraphPass> passes, ref SpanReader reader, ReadOnlySpan<NativeRenderPass> nativePasses, Dictionary<int, CommandQueueType> effectiveQueues, Dictionary<int, PassSyncBoundaryDumpInfo> syncBoundariesBefore, Dictionary<int, PassSyncBoundaryDumpInfo> syncBoundariesAfter)
    {
        var lines = new List<string>();
        var commandBufferTypes = new List<CommandQueueType> { CommandQueueType.Graphics };
        var instIndex = 0;
        var lastPassIndex = -1;
        PassSyncBoundaryDumpInfo? pendingBoundary = null;
        var effectiveQueue = CommandQueueType.Graphics;

        while (reader.RemainingBytes > 0)
        {
            var op = reader.Read<RGExecutionOpType>();

            switch (op)
            {
                case RGExecutionOpType.IssueBarriers:
                {
                    var count = reader.Read<int>();
                    lines.Add($"[{instIndex++:D4}] IssueBarriers ({count} barriers)");
                    for (var i = 0; i < count; i++)
                    {
                        var barrier = reader.Read<CompiledBarrier>();
                        var resourceId = barrier.resource.Value;
                        var resName = resourceRegistry.GetResourceName(resourceId);
                        var resourceLabel = $"{resName} [{barrier.resourceType} #{resourceId}]";
                        var sourceStateLabel = $"Layout: {barrier.sourceState.layout}, Access: {barrier.sourceState.access}, Sync: {barrier.sourceState.sync}";
                        var handoffStateLabel = $"Layout: {barrier.handoffState.layout}, Access: {barrier.handoffState.access}, Sync: {barrier.handoffState.sync}";
                        var targetStateLabel = $"Layout: {barrier.targetState.layout}, Access: {barrier.targetState.access}, Sync: {barrier.targetState.sync}";
                        if (barrier.flags.HasFlag(BarrierFlags.QueueRelease))
                        {
                            lines.Add(
                                $"       ├─ QueueRelease: {resourceLabel}, {barrier.sourceQueue} -> {barrier.destinationQueue}, " +
                                $"Source: [{sourceStateLabel}], Handoff: [{handoffStateLabel}], Flags: {barrier.flags}");
                        }
                        else if (barrier.flags.HasFlag(BarrierFlags.QueueAcquire))
                        {
                            lines.Add(
                                $"       ├─ QueueAcquire: {resourceLabel}, {barrier.sourceQueue} -> {barrier.destinationQueue}, " +
                                $"Handoff: [{handoffStateLabel}], Target: [{targetStateLabel}], Flags: {barrier.flags}");
                        }
                        else if (barrier.aliasingPredecessor.IsValid)
                        {
                            var predecessorId = barrier.aliasingPredecessor.Value;
                            ref readonly var predecessor = ref resourceRegistry.GetResource(barrier.aliasingPredecessor);
                            var predName = resourceRegistry.GetResourceName(predecessorId);
                            var predecessorLabel = $"{predName} [{predecessor.type} #{predecessorId}]";
                            lines.Add($"       ├─ Aliasing: {predecessorLabel} -> {resourceLabel} -> {targetStateLabel}, Flags: {barrier.flags}");
                        }
                        else
                        {
                            lines.Add(
                                $"       ├─ Transition: {resourceLabel}, Source: [{sourceStateLabel}], " +
                                $"Target: [{targetStateLabel}], Flags: {barrier.flags}");
                        }
                    }
                    break;
                }

                case RGExecutionOpType.BeginNativePass:
                {
                    var nativePassIdx = reader.Read<int>();
                    var np = (nativePassIdx >= 0 && nativePassIdx < nativePasses.Length) ? nativePasses[nativePassIdx] : default;
                    lines.Add($"[{instIndex++:D4}] BeginNativePass #{nativePassIdx} (ColorCount: {np.colorAttachmentCount}, HasDepth: {np.hasDepthAttachment})");
                    break;
                }

                case RGExecutionOpType.ExecutePass:
                {
                    var passIdx = reader.Read<int>();
                    var isKnownPass = passIdx >= 0 && passIdx < passes.Count;
                    var passName = isKnownPass ? passes[passIdx].name : $"Pass#{passIdx}";
                    var passType = isKnownPass ? passes[passIdx].type.ToString() : "Unknown";
                    var asyncRequested = isKnownPass && passes[passIdx].asyncCompute;
                    if (isKnownPass)
                    {
                        effectiveQueues[passIdx] = effectiveQueue;
                        if (pendingBoundary.HasValue)
                        {
                            syncBoundariesBefore[passIdx] = pendingBoundary.Value;
                            pendingBoundary = null;
                        }
                    }
                    lastPassIndex = passIdx;
                    var queueDecision = isKnownPass
                        ? GetQueueDecision(passes[passIdx], effectiveQueue)
                        : RGQueueDecision.IneligiblePassType;
                    lines.Add(
                        $"[{instIndex++:D4}] ExecutePass #{passIdx} '{passName}' [{passType}] -> " +
                        $"AsyncRequested: {asyncRequested}, EffectiveQueue: {effectiveQueue}, QueueDecision: {queueDecision}");
                    break;
                }

                case RGExecutionOpType.EndNativePass:
                {
                    lines.Add($"[{instIndex++:D4}] EndNativePass");
                    break;
                }

                case RGExecutionOpType.CommandBufferSyncPoint:
                {
                    var marker = RGCommandStream.ReadSyncMarker(ref reader);
                    var producerIds = marker.producerCommandBufferIds.ToArray();
                    var producerTypes = new CommandQueueType[producerIds.Length];
                    for (var i = 0; i < producerIds.Length; i++)
                    {
                        producerTypes[i] = commandBufferTypes[producerIds[i]];
                    }

                    var boundary = new PassSyncBoundaryDumpInfo
                    {
                        SourceType = effectiveQueue,
                        DestinationType = marker.nextCommandBufferType,
                        ProducerCommandBufferIds = producerIds,
                        ProducerTypes = producerTypes
                    };
                    if (lastPassIndex >= 0)
                    {
                        syncBoundariesAfter[lastPassIndex] = boundary;
                    }
                    pendingBoundary = boundary;

                    var deps = producerIds.Length > 0 ? string.Join(", ", producerIds) : "none";
                    var dependencyTypes = producerTypes.Length > 0 ? string.Join(", ", producerTypes) : "none";
                    lines.Add(
                        $"[{instIndex++:D4}] CommandBufferSyncPoint -> SourceType: {effectiveQueue}, NextType: {marker.nextCommandBufferType}, " +
                        $"DependsOn: [{deps}], DependencyTypes: [{dependencyTypes}]");
                    effectiveQueue = marker.nextCommandBufferType;
                    commandBufferTypes.Add(effectiveQueue);
                    break;
                }
            }
        }

        return lines;
    }

    private static RGQueueDecision GetQueueDecision(RenderGraphPass pass, CommandQueueType? effectiveQueue)
    {
        if (pass.culled || !effectiveQueue.HasValue)
        {
            return RGQueueDecision.Culled;
        }

        if (pass.type != RenderPassType.Compute)
        {
            return RGQueueDecision.IneligiblePassType;
        }

        if (!pass.asyncCompute)
        {
            return RGQueueDecision.AsyncNotRequested;
        }

        return effectiveQueue == CommandQueueType.Compute
            ? RGQueueDecision.AsyncComputeSelected
            : RGQueueDecision.NoLegalOverlapWindow;
    }

    internal static RenderGraphDump Create(RenderGraphResourceRegistry resourceRegistry, IReadOnlyList<RenderGraphPass> passes, scoped in CompiledGraph graph, ViewportState viewport)
    {
        var sizeWithoutAliasing = 0UL;
        for (var i = 0; i < graph.plan.placedResources.Count; i++)
        {
            sizeWithoutAliasing += graph.plan.placedResources[i].sizeInBytes;
        }

        var dump = new RenderGraphDump
        {
            GraphHash = graph.graphHash,
            TotalHeapSize = graph.plan.totalHeapSize,
            SizeWithoutAliasing = sizeWithoutAliasing,
            IsCacheHit = graph.cacheHit,
            Viewport = viewport
        };

        var effectiveQueues = new Dictionary<int, CommandQueueType>();
        var syncBoundariesBefore = new Dictionary<int, PassSyncBoundaryDumpInfo>();
        var syncBoundariesAfter = new Dictionary<int, PassSyncBoundaryDumpInfo>();
        var reader = new SpanReader(graph.commandStream.AsSpan());
        dump.CommandStream.AddRange(DisassembleCommandStream(resourceRegistry, passes, ref reader, graph.nativePasses, effectiveQueues, syncBoundariesBefore, syncBoundariesAfter));

        // Collect Memory Placement Blocks (Heap Blocks)
        var uniqueBlocks = graph.plan.placedResources.AsSpan().ToArray()
            .GroupBy(p => p.heapOffset)
            .OrderBy(g => g.Key);

        var currentOffset = 0UL;
        foreach (var group in uniqueBlocks)
        {
            if (group.Key > currentOffset)
            {
                dump.MemoryBlocks.Add(new HeapBlockDumpInfo
                {
                    Offset = currentOffset,
                    Size = group.Key - currentOffset,
                    IsFree = true,
                    AliasedLogicalResources = new List<int>()
                });
            }

            var maxBlockSize = 0UL;
            var allLogicalIds = new List<int>();

            foreach (var placed in group)
            {
                maxBlockSize = Math.Max(maxBlockSize, placed.sizeInBytes);
                foreach (var id in placed.aliasedLogicalResources)
                {
                    if (!allLogicalIds.Contains(id))
                    {
                        allLogicalIds.Add(id);
                    }
                }
            }

            dump.MemoryBlocks.Add(new HeapBlockDumpInfo
            {
                Offset = group.Key,
                Size = maxBlockSize,
                IsFree = false,
                AliasedLogicalResources = allLogicalIds
            });

            currentOffset = Math.Max(currentOffset, group.Key + maxBlockSize);
        }

        if (currentOffset < dump.TotalHeapSize)
        {
            dump.MemoryBlocks.Add(new HeapBlockDumpInfo
            {
                Offset = currentOffset,
                Size = dump.TotalHeapSize - currentOffset,
                IsFree = true,
                AliasedLogicalResources = new List<int>()
            });
        }

        // Collect Passes Info
        for (var i = 0; i < passes.Count; i++)
        {
            var pass = passes[i];
            CommandQueueType? effectiveQueue = effectiveQueues.TryGetValue(pass.index, out var queue)
                ? queue
                : null;

            var nativePassIndex = NativeRenderPass.Invalid.index;
            foreach (var np in graph.nativePasses)
            {
                if (np.mergedPassIndices.Contains(pass.index))
                {
                    nativePassIndex = np.index;
                    break;
                }
            }

            var passInfo = new PassDumpInfo
            {
                Index = pass.index,
                NativePassIndex = nativePassIndex,
                Name = pass.name,
                Type = pass.type,
                IsCulled = pass.culled,
                AsyncCompute = pass.asyncCompute,
                AsyncRequested = pass.asyncCompute,
                AllowAsyncComputeOverlap = pass.allowAsyncComputeOverlap,
                EffectiveQueue = effectiveQueue,
                QueueDecision = GetQueueDecision(pass, effectiveQueue),
                SyncBoundaryBefore = syncBoundariesBefore.TryGetValue(pass.index, out var boundaryBefore)
                    ? boundaryBefore
                    : null,
                SyncBoundaryAfter = syncBoundariesAfter.TryGetValue(pass.index, out var boundaryAfter)
                    ? boundaryAfter
                    : null,
                ResourceReads = pass.resourceReads[(int)RGResourceType.Texture]
                    .Concat(pass.resourceReads[(int)RGResourceType.Buffer])
                    .Select(r => r.Value).ToList(),
                ResourceWrites = pass.resourceWrites[(int)RGResourceType.Texture]
                    .Concat(pass.resourceWrites[(int)RGResourceType.Buffer])
                    .Select(r => r.Value).ToList(),
                ResourceCreates = pass.resourceCreates[(int)RGResourceType.Texture]
                    .Concat(pass.resourceCreates[(int)RGResourceType.Buffer])
                    .Select(r => r.Value).ToList(),
            };

            dump.Passes.Add(passInfo);
        }

        // Collect Resource & Aliasing Info
        var plan = graph.plan;
        for (var i = 0; i < resourceRegistry.ResourceCount; i++)
        {
            ref readonly var res = ref resourceRegistry.GetResourceByIndex(i);
            var placedIndex = plan.GetPlacedResourceIndex(i);
            var placedResult = plan.GetPlacedResource(placedIndex);
            var resInfo = new ResourceDumpInfo
            {
                LogicalResourceId = res.index,
                BackingResource = res.backingResource,
                Name = resourceRegistry.GetResourceName(i),
                Type = res.type,
                IsImported = res.isImported,
                IsExtracted = res.isExtracted,
                HeapOffset = placedResult.IsSuccess ? placedResult.Value.heapOffset : 0,
                SizeInBytes = placedResult.IsSuccess ? placedResult.Value.sizeInBytes : 0,
                FirstUsePass = res.firstUsePass,
                LastUsePass = res.lastUsePass,
                ScheduledFirstUseIndex = graph.resourceFirstUseScheduleIndices[i],
                ScheduledLastUseIndex = graph.resourceLastUseScheduleIndices[i],
                ProducerPass = [.. res.producerPasses],
                ConsumerPasses = [.. res.consumerPasses],
                AliasedWithResources = placedResult.IsSuccess
                    ? placedResult.Value.aliasedLogicalResources.ToList()
                    : new List<int>()
            };

            dump.Resources.Add(resInfo);
        }

        return dump;
    }

    public string ExportHtml()
    {
        var passesJson = JsonSerializer.Serialize(Passes, RGDumpJsonContext.Default.ListPassDumpInfo);
        var resourcesJson = JsonSerializer.Serialize(Resources, RGDumpJsonContext.Default.ListResourceDumpInfo);
        var memoryJson = JsonSerializer.Serialize(MemoryBlocks, RGDumpJsonContext.Default.ListHeapBlockDumpInfo);

        var html = $$"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
            <meta charset="utf-8">
            <title>GhostEngine - Render Graph Inspector</title>
            <style>
                :root {
                    --bg-dark: #121418;
                    --bg-panel: #181b20;
                    --bg-card: #20242c;
                    --border-color: #2e3440;
                    --border-light: #3d4454;
                    --text-primary: #f0f6fc;
                    --text-secondary: #8b949e;
                    --text-muted: #656d76;

                    --color-read: #7ee787;
                    --color-write: #f85149;
                    --color-retained: #383e4a;
                    --color-empty: transparent;

                    --pass-raster: #388bfd;
                    --pass-compute: #a371f7;
                    --pass-async: #e3b341;
                    --pass-unsafe: #8b949e;
                    --pass-culled: #21262d;

                    --badge-tex: #388bfd;
                    --badge-buf: #f0883e;

                    --col-w: 36px;
                    --res-w: 380px;
                    --header-h: 175px;
                    --row-h: 24px;
                }

                * { box-sizing: border-box; }
                body {
                    margin: 0;
                    padding: 12px 16px;
                    background-color: var(--bg-dark);
                    color: var(--text-primary);
                    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, monospace;
                    font-size: 13px;
                    overflow: hidden;
                    height: 100vh;
                    display: flex;
                    flex-direction: column;
                }

                /* Top Header & Stats */
                header {
                    display: flex;
                    flex-wrap: wrap;
                    align-items: center;
                    justify-content: space-between;
                    gap: 16px;
                    background: var(--bg-panel);
                    border: 1px solid var(--border-color);
                    border-radius: 6px;
                    padding: 10px 16px;
                    margin-bottom: 8px;
                    flex-shrink: 0;
                }
                .title-group {
                    display: flex;
                    align-items: center;
                    gap: 12px;
                }
                .title-group h1 {
                    margin: 0;
                    font-size: 17px;
                    font-weight: 700;
                    letter-spacing: 0.5px;
                    color: #58a6ff;
                }
                .stats-group {
                    display: flex;
                    gap: 20px;
                    align-items: center;
                }
                .stat-pill {
                    display: flex;
                    flex-direction: column;
                }
                .stat-pill .label {
                    font-size: 10px;
                    color: var(--text-secondary);
                    font-weight: 600;
                    text-transform: uppercase;
                }
                .stat-pill .val {
                    font-size: 15px;
                    font-weight: 700;
                    color: var(--text-primary);
                }
                .val.saved { color: #3fb950; }

                /* View Switcher Tabs */
                .view-switch-tabs {
                    display: inline-flex;
                    align-items: center;
                    background: #14171d;
                    border: 1px solid var(--border-color);
                    border-radius: 6px;
                    padding: 2px;
                    gap: 2px;
                    margin-left: 12px;
                }
                .view-tab-btn {
                    display: inline-flex;
                    align-items: center;
                    gap: 6px;
                    background: transparent;
                    border: 1px solid transparent;
                    color: var(--text-secondary);
                    font-size: 11.5px;
                    font-weight: 600;
                    padding: 4px 10px;
                    border-radius: 4px;
                    cursor: pointer;
                    transition: all 0.15s ease;
                }
                .view-tab-btn:hover {
                    color: var(--text-primary);
                    background: rgba(255, 255, 255, 0.05);
                }
                .view-tab-btn.active {
                    color: #fff;
                    background: #282e3a;
                    border-color: #3d4454;
                    box-shadow: 0 1px 3px rgba(0, 0, 0, 0.3);
                }
                .tab-badge {
                    font-size: 10px;
                    font-weight: 700;
                    padding: 1px 5px;
                    border-radius: 10px;
                    background: rgba(88, 166, 255, 0.2);
                    color: #58a6ff;
                }

                /* Legend Bar */
                .legend-bar {
                    display: flex;
                    flex-wrap: wrap;
                    gap: 18px;
                    align-items: center;
                    background: var(--bg-card);
                    border: 1px solid var(--border-color);
                    border-radius: 6px;
                    padding: 6px 14px;
                    margin-bottom: 8px;
                    font-size: 11.5px;
                    flex-shrink: 0;
                }
                .legend-section {
                    display: flex;
                    align-items: center;
                    gap: 10px;
                }
                .legend-title {
                    font-weight: bold;
                    color: var(--text-secondary);
                    margin-right: 4px;
                }
                .legend-item {
                    display: flex;
                    align-items: center;
                    gap: 5px;
                }
                .legend-box {
                    width: 13px;
                    height: 13px;
                    border-radius: 2px;
                    border: 1px solid rgba(255,255,255,0.1);
                }
                .box-write { background: var(--color-write); }
                .box-read { background: var(--color-read); }
                .box-readwrite { background: linear-gradient(135deg, var(--color-write) 50%, var(--color-read) 50%); }
                .box-retained { background: var(--color-retained); border-color: #555; }

                .legend-dot {
                    width: 9px;
                    height: 9px;
                    border-radius: 50%;
                }
                .dot-raster { background: var(--pass-raster); }
                .dot-compute { background: var(--pass-compute); }
                .dot-async { background: var(--pass-async); box-shadow: 0 0 6px rgba(227, 179, 65, 0.6); }
                .dot-unsafe { background: var(--pass-unsafe); }
                .dot-culled { background: var(--pass-culled); border: 1px solid #555; }

                /* Controls Toolbar */
                .controls-bar {
                    display: flex;
                    align-items: center;
                    gap: 16px;
                    margin-bottom: 8px;
                    font-size: 12px;
                    flex-shrink: 0;
                }
                .search-box {
                    background: var(--bg-panel);
                    border: 1px solid var(--border-color);
                    border-radius: 4px;
                    padding: 4px 10px;
                    color: var(--text-primary);
                    font-size: 12px;
                    width: 240px;
                }
                .search-box:focus {
                    outline: none;
                    border-color: #58a6ff;
                }
                .control-item {
                    display: flex;
                    align-items: center;
                    gap: 6px;
                    cursor: pointer;
                    user-select: none;
                }
                .btn-zoom {
                    background: var(--bg-card);
                    border: 1px solid var(--border-color);
                    color: var(--text-primary);
                    border-radius: 4px;
                    padding: 3px 8px;
                    cursor: pointer;
                    font-size: 12px;
                    font-weight: bold;
                }
                .btn-zoom:hover {
                    background: #2b313d;
                    border-color: #58a6ff;
                }

                /* Layout Container */
                .inspector-container {
                    flex: 1;
                    display: flex;
                    flex-direction: column;
                    background: var(--bg-panel);
                    border: 1px solid var(--border-color);
                    border-radius: 6px;
                    overflow: hidden;
                    position: relative;
                }

                /* Fixed Header Pane (No Vertical Scroll) */
                .header-pane {
                    display: flex;
                    height: var(--header-h);
                    background: var(--bg-panel);
                    border-bottom: 2px solid var(--border-color);
                    flex-shrink: 0;
                    z-index: 10;
                }
                .corner-header {
                    width: var(--res-w);
                    min-width: var(--res-w);
                    max-width: var(--res-w);
                    height: var(--header-h);
                    background: var(--bg-panel);
                    border-right: 2px solid var(--border-color);
                    padding: 12px 16px;
                    display: flex;
                    flex-direction: column;
                    justify-content: flex-end;
                    box-shadow: 2px 0 6px rgba(0,0,0,0.25);
                    z-index: 15;
                    flex-shrink: 0;
                }
                .corner-title {
                    font-size: 13px;
                    font-weight: 700;
                    color: var(--text-primary);
                    margin-bottom: 3px;
                }
                .corner-sub {
                    font-size: 11px;
                    color: var(--text-secondary);
                }

                .pass-headers-viewport {
                    flex: 1;
                    overflow-x: hidden;
                    overflow-y: hidden;
                    position: relative;
                }
                .pass-headers-track {
                    display: flex;
                    height: var(--header-h);
                    width: max-content;
                }

                .pass-header-col {
                    width: var(--col-w);
                    min-width: var(--col-w);
                    max-width: var(--col-w);
                    height: var(--header-h);
                    position: relative;
                    flex-shrink: 0;
                    border-right: 1px solid #232730;
                }
                .pass-header-col:hover {
                    background: rgba(255,255,255,0.03);
                }
                .pass-name-anchor {
                    position: absolute;
                    bottom: 26px;
                    left: calc(var(--col-w) / 2);
                    width: 0;
                    height: 0;
                }
                .pass-name {
                    position: absolute;
                    bottom: 0;
                    left: 0;
                    transform: rotate(-45deg);
                    transform-origin: bottom left;
                    white-space: nowrap;
                    font-size: 11px;
                    font-weight: 600;
                    color: #e6edf3;
                    letter-spacing: 0.3px;
                    padding-left: 6px;
                    transition: color 0.15s ease;
                    pointer-events: auto;
                    cursor: default;
                }
                .pass-header-col:hover .pass-name {
                    color: #58a6ff;
                    text-shadow: 0 0 8px rgba(88, 166, 255, 0.6);
                }
                .pass-name.is-culled {
                    color: var(--text-muted);
                    text-decoration: line-through;
                }

                .pass-badge {
                    position: absolute;
                    bottom: 0;
                    left: 0;
                    right: 0;
                    height: 24px;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    font-size: 9.5px;
                    font-weight: 700;
                    border-top: 1px solid #2a2e38;
                }
                .pass-badge.badge-raster { background: rgba(56, 139, 253, 0.2); color: #58a6ff; border-bottom: 2px solid var(--pass-raster); }
                .pass-badge.badge-compute { background: rgba(163, 113, 247, 0.2); color: #bc8cff; border-bottom: 2px solid var(--pass-compute); }
                .pass-badge.badge-async { background: rgba(227, 179, 65, 0.25); color: #f1e05a; border-bottom: 2px solid var(--pass-async); }
                .pass-badge.badge-unsafe { background: rgba(139, 148, 158, 0.15); color: #c9d1d9; border-bottom: 2px solid var(--pass-unsafe); }
                .pass-badge.badge-culled { background: #1b1e24; color: var(--text-muted); border-bottom: 2px solid #333; }

                /* Body Pane (Scrollable Vertically & Horizontally) */
                .body-viewport {
                    flex: 1;
                    overflow: auto;
                    position: relative;
                }
                table.body-table {
                    border-collapse: separate;
                    border-spacing: 0;
                    table-layout: fixed;
                    width: max-content;
                }

                /* Resource Rows */
                tbody tr:hover { background: #222733; }
                tbody tr:hover td.res-cell { background: #222733; }
                td.res-cell {
                    position: sticky;
                    left: 0;
                    z-index: 5;
                    width: var(--res-w);
                    min-width: var(--res-w);
                    max-width: var(--res-w);
                    background: var(--bg-panel);
                    border-bottom: 1px solid #232730;
                    border-right: 2px solid var(--border-color);
                    padding: 3px 10px;
                    height: var(--row-h);
                    display: flex;
                    align-items: center;
                    gap: 6px;
                    white-space: nowrap;
                    overflow: hidden;
                    box-shadow: 2px 0 6px rgba(0,0,0,0.25);
                }
                .type-tag {
                    font-size: 9px;
                    font-weight: 800;
                    padding: 1px 4px;
                    border-radius: 3px;
                    letter-spacing: 0.5px;
                }
                .tag-tex { background: #1f6feb; color: #fff; }
                .tag-buf { background: #d97706; color: #fff; }
                .tag-imp { background: #238636; color: #fff; font-size: 8px; padding: 1px 3px; }
                .tag-ext { background: #8957e5; color: #fff; font-size: 8px; padding: 1px 3px; }
                .tag-alias { background: #9e6a03; color: #fff; font-size: 8px; padding: 1px 3px; }

                .res-name {
                    font-weight: 500;
                    color: var(--text-primary);
                    overflow: hidden;
                    text-overflow: ellipsis;
                    flex: 1;
                    font-size: 12px;
                }
                .res-size {
                    font-size: 10.5px;
                    color: var(--text-secondary);
                    font-family: 'Consolas', monospace;
                    margin-left: auto;
                }

                /* Grid Cells */
                td.grid-cell {
                    width: var(--col-w);
                    min-width: var(--col-w);
                    max-width: var(--col-w);
                    height: var(--row-h);
                    padding: 0;
                    border-bottom: 1px solid #232730;
                    border-right: 1px solid #232730;
                    text-align: center;
                    vertical-align: middle;
                }
                .cell-block {
                    width: 100%;
                    height: 18px;
                    transition: transform 0.1s ease;
                }
                .cell-block:hover {
                    transform: scale(1.18);
                    z-index: 2;
                    position: relative;
                    box-shadow: 0 0 6px rgba(255,255,255,0.5);
                }

                .cell-empty { background: transparent; }
                .cell-retained { background: var(--color-retained); opacity: 0.85; }
                .cell-write { background: var(--color-write); border-radius: 2px; }
                .cell-read { background: var(--color-read); border-radius: 2px; }
                .cell-readwrite {
                    background: linear-gradient(135deg, var(--color-write) 50%, var(--color-read) 50%);
                    border-radius: 2px;
                }

                /* Heap Container */
                .heap-container {
                    flex: 1;
                    display: none;
                    flex-direction: column;
                    gap: 12px;
                    background: var(--bg-panel);
                    border: 1px solid var(--border-color);
                    border-radius: 6px;
                    padding: 14px 16px;
                    overflow-y: auto;
                    position: relative;
                }

                /* Heap Metrics Strip */
                .heap-metrics-grid {
                    display: grid;
                    grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
                    gap: 10px;
                    flex-shrink: 0;
                }
                .heap-metric-card {
                    background: var(--bg-card);
                    border: 1px solid var(--border-color);
                    border-radius: 6px;
                    padding: 8px 12px;
                    display: flex;
                    flex-direction: column;
                    gap: 3px;
                }
                .heap-metric-card .hm-label {
                    font-size: 10px;
                    font-weight: 700;
                    color: var(--text-secondary);
                    text-transform: uppercase;
                    letter-spacing: 0.5px;
                }
                .heap-metric-card .hm-val {
                    font-size: 16px;
                    font-weight: 700;
                    color: var(--text-primary);
                }
                .heap-metric-card .hm-sub {
                    font-size: 10.5px;
                    color: var(--text-muted);
                }

                /* Section Cards */
                .heap-section {
                    background: var(--bg-card);
                    border: 1px solid var(--border-color);
                    border-radius: 6px;
                    padding: 12px 14px;
                    display: flex;
                    flex-direction: column;
                    gap: 8px;
                    flex-shrink: 0;
                }
                .heap-section-header {
                    display: flex;
                    justify-content: space-between;
                    align-items: center;
                    flex-wrap: wrap;
                    gap: 8px;
                }
                .heap-section-title {
                    font-size: 12.5px;
                    font-weight: 700;
                    color: var(--text-primary);
                    display: flex;
                    align-items: center;
                    gap: 8px;
                }

                /* 1D Address Bar */
                .heap-bar-ruler {
                    display: flex;
                    justify-content: space-between;
                    font-size: 9.5px;
                    color: var(--text-muted);
                    font-family: 'Consolas', monospace;
                    padding: 0 2px;
                }
                .heap-bar-track {
                    display: flex;
                    height: 38px;
                    width: 100%;
                    background: #14171d;
                    border: 1px solid #2e3440;
                    border-radius: 4px;
                    overflow: hidden;
                    position: relative;
                    user-select: none;
                }
                .heap-bar-segment {
                    height: 100%;
                    position: relative;
                    cursor: pointer;
                    transition: filter 0.12s ease, transform 0.12s ease;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    overflow: hidden;
                    border-right: 1px solid rgba(0, 0, 0, 0.4);
                    min-width: 8px;
                }
                .heap-bar-segment:hover {
                    filter: brightness(1.25);
                    z-index: 5;
                    outline: 2px solid #58a6ff;
                }
                .heap-bar-segment.is-selected {
                    outline: 2px solid #58a6ff;
                    box-shadow: 0 0 10px rgba(88, 166, 255, 0.7);
                    z-index: 6;
                }
                .heap-bar-segment.is-free {
                    background: repeating-linear-gradient(45deg, #181b22, #181b22 6px, #1e222b 6px, #1e222b 12px);
                    cursor: default;
                }
                .heap-bar-segment.is-dedicated {
                    background: linear-gradient(180deg, #1f6feb 0%, #1158c7 100%);
                    color: #fff;
                }
                .heap-bar-segment.is-aliased {
                    background: linear-gradient(180deg, #d29922 0%, #bb8009 100%);
                    color: #fff;
                }
                .heap-bar-segment.is-buf {
                    background: linear-gradient(180deg, #db6d28 0%, #bd4c14 100%);
                    color: #fff;
                }
                .heap-bar-label {
                    font-size: 10px;
                    font-weight: 700;
                    white-space: nowrap;
                    text-overflow: ellipsis;
                    overflow: hidden;
                    padding: 0 4px;
                    pointer-events: none;
                    text-shadow: 0 1px 2px rgba(0, 0, 0, 0.8);
                }

                /* 2D Memory Aliasing Chart */
                .heap-2d-viewport {
                    width: 100%;
                    max-height: 480px;
                    overflow: auto;
                    background: #14171d;
                    border: 1px solid var(--border-color);
                    border-radius: 4px;
                    position: relative;
                }
                .heap-2d-canvas-wrap {
                    display: flex;
                    min-width: max-content;
                    position: relative;
                }
                .heap-2d-yaxis {
                    width: 72px;
                    min-width: 72px;
                    background: var(--bg-panel);
                    border-right: 1px solid var(--border-color);
                    position: sticky;
                    left: 0;
                    z-index: 10;
                    display: flex;
                    flex-direction: column;
                    justify-content: space-between;
                    padding: 24px 6px 6px 6px;
                    font-size: 9.5px;
                    color: var(--text-muted);
                    font-family: 'Consolas', monospace;
                    user-select: none;
                }
                .heap-2d-main {
                    flex: 1;
                    display: flex;
                    flex-direction: column;
                    position: relative;
                }
                .heap-2d-pass-headers {
                    display: flex;
                    height: 24px;
                    background: var(--bg-panel);
                    border-bottom: 1px solid var(--border-color);
                    position: sticky;
                    top: 0;
                    z-index: 8;
                }
                .heap-2d-pass-col-header {
                    width: var(--col-w);
                    min-width: var(--col-w);
                    font-size: 9.5px;
                    font-weight: 700;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    border-right: 1px solid #232730;
                    color: var(--text-secondary);
                }
                .heap-2d-chart-body {
                    position: relative;
                    height: 400px;
                    background-image: 
                        linear-gradient(to right, #232730 1px, transparent 1px),
                        linear-gradient(to bottom, #232730 1px, transparent 1px);
                    background-size: var(--col-w) 40px;
                }
                .heap-2d-resource-block {
                    position: absolute;
                    border-radius: 3px;
                    padding: 2px 4px;
                    font-size: 10px;
                    font-weight: 600;
                    overflow: hidden;
                    cursor: pointer;
                    transition: transform 0.1s ease, box-shadow 0.1s ease, filter 0.1s ease;
                    border: 1px solid rgba(255, 255, 255, 0.2);
                    box-shadow: 0 1px 4px rgba(0, 0, 0, 0.4);
                    display: flex;
                    flex-direction: column;
                    justify-content: center;
                }
                .heap-2d-resource-block:hover {
                    transform: scale(1.02);
                    filter: brightness(1.2);
                    z-index: 15 !important;
                    box-shadow: 0 0 10px rgba(88, 166, 255, 0.7);
                }
                .heap-2d-resource-block.is-aliased {
                    background: linear-gradient(135deg, rgba(210, 153, 34, 0.88) 0%, rgba(187, 128, 9, 0.88) 100%);
                    border-color: #f0883e;
                }
                .heap-2d-resource-block.is-dedicated {
                    background: linear-gradient(135deg, rgba(31, 111, 235, 0.88) 0%, rgba(17, 88, 199, 0.88) 100%);
                    border-color: #58a6ff;
                }
                .heap-2d-resource-block.is-buf {
                    background: linear-gradient(135deg, rgba(219, 109, 40, 0.88) 0%, rgba(189, 76, 20, 0.88) 100%);
                    border-color: #f0883e;
                }
                .heap-2d-resource-block.dimmed {
                    opacity: 0.25;
                    filter: grayscale(0.8);
                }
                .heap-2d-resource-block.highlighted {
                    outline: 2px solid #fff;
                    box-shadow: 0 0 12px rgba(255, 255, 255, 0.85);
                    z-index: 12 !important;
                }

                /* Heap Breakdown Table */
                .heap-table {
                    width: 100%;
                    border-collapse: collapse;
                    font-size: 11.5px;
                }
                .heap-table th {
                    background: #1a1e26;
                    border-bottom: 1px solid var(--border-color);
                    padding: 6px 10px;
                    text-align: left;
                    font-size: 10.5px;
                    color: var(--text-secondary);
                    font-weight: 700;
                }
                .heap-table td {
                    border-bottom: 1px solid #232730;
                    padding: 6px 10px;
                    vertical-align: middle;
                }
                .heap-table tr:hover {
                    background: #1f242e;
                }
                .heap-table tr.is-selected {
                    background: rgba(56, 139, 253, 0.15);
                    outline: 1px solid #58a6ff;
                }
                .occupants-flow {
                    display: flex;
                    flex-wrap: wrap;
                    align-items: center;
                    gap: 6px;
                }
                .occupant-pill {
                    background: #252b36;
                    border: 1px solid var(--border-color);
                    border-radius: 4px;
                    padding: 2px 6px;
                    font-size: 11px;
                    display: inline-flex;
                    align-items: center;
                    gap: 5px;
                    cursor: pointer;
                }
                .occupant-pill:hover {
                    background: #2e3543;
                    border-color: #58a6ff;
                }
                .arrow-separator {
                    color: var(--text-muted);
                    font-size: 10px;
                }
                .btn-jump {
                    background: #21262d;
                    border: 1px solid var(--border-color);
                    color: var(--text-secondary);
                    border-radius: 3px;
                    padding: 1px 5px;
                    font-size: 10px;
                    cursor: pointer;
                }
                .btn-jump:hover {
                    background: #30363d;
                    color: #58a6ff;
                    border-color: #58a6ff;
                }

                /* Custom Tooltip */
                #tooltip {
                    position: fixed;
                    display: none;
                    background: #1c2128;
                    border: 1px solid #444c56;
                    border-radius: 5px;
                    padding: 8px 12px;
                    color: var(--text-primary);
                    font-size: 11.5px;
                    line-height: 1.4;
                    box-shadow: 0 6px 16px rgba(0,0,0,0.6);
                    pointer-events: none;
                    z-index: 1000;
                }
                #tooltip .tt-title { font-weight: bold; color: #58a6ff; margin-bottom: 4px; font-size: 12px; }
                #tooltip .tt-row { display: flex; justify-content: space-between; gap: 14px; color: #adbac7; }
                #tooltip .tt-val { font-weight: 600; color: #cdd9e5; }
            </style>
        </head>
        <body>
            <header>
                <div class="title-group">
                    <h1>GhostEngine Render Graph Inspector</h1>
                    <span style="font-size:11.5px; color:var(--text-secondary);">Hash: <code>{{GraphHash:X16}}</code></span>
                    <div class="view-switch-tabs">
                        <button class="view-tab-btn active" id="viewTabTimeline" onclick="switchView('timeline')">
                            <span>Pass Timeline</span>
                        </button>
                        <button class="view-tab-btn" id="viewTabHeap" onclick="switchView('heap')">
                            <span>Heap Memory Map</span>
                            <span class="tab-badge" id="heapTabBadge">{{TotalHeapSize / (1024.0 * 1024.0):F1}} MB</span>
                        </button>
                    </div>
                </div>
                <div class="stats-group">
                    <div class="stat-pill">
                        <span class="label">Total Heap Committed</span>
                        <span class="val">{{TotalHeapSize / (1024.0 * 1024.0):F2}} MB</span>
                    </div>
                    <div class="stat-pill">
                        <span class="label">Without Aliasing</span>
                        <span class="val">{{SizeWithoutAliasing / (1024.0 * 1024.0):F2}} MB</span>
                    </div>
                    <div class="stat-pill">
                        <span class="label">VRAM Saved</span>
                        <span class="val saved">{{(1.0 - (double)TotalHeapSize / (SizeWithoutAliasing > 0 ? SizeWithoutAliasing : 1)) * 100:F1}}%</span>
                    </div>
                    <div class="stat-pill">
                        <span class="label">Passes (Total / Culled)</span>
                        <span class="val">{{Passes.Count}} / {{Passes.Count(p => p.IsCulled)}}</span>
                    </div>
                    <div class="stat-pill">
                        <span class="label">Cache Status</span>
                        <span class="val" style="color:{{(IsCacheHit ? "#3fb950" : "#d29922")}};">{{(IsCacheHit ? "CACHE HIT" : "COMPILED")}}</span>
                    </div>
                </div>
            </header>

            <div class="legend-bar">
                <div class="legend-section" id="timelineAccessLegend">
                    <span class="legend-title">Access:</span>
                    <div class="legend-item"><div class="legend-box box-write"></div><span>Write</span></div>
                    <div class="legend-item"><div class="legend-box box-read"></div><span>Read</span></div>
                    <div class="legend-item"><div class="legend-box box-readwrite"></div><span>Read & Write</span></div>
                    <div class="legend-item"><div class="legend-box box-retained"></div><span>Retained in Heap</span></div>
                </div>
                <div class="legend-section" id="heapPlacementLegend" style="display:none;">
                    <span class="legend-title">Heap Placement:</span>
                    <div class="legend-item"><div class="legend-box" style="background:#388bfd;"></div><span>Dedicated</span></div>
                    <div class="legend-item"><div class="legend-box" style="background:#d29922;"></div><span>Aliased (Shared)</span></div>
                    <div class="legend-item"><div class="legend-box" style="background:#f0883e;"></div><span>Buffer</span></div>
                    <div class="legend-item"><div class="legend-box" style="background:repeating-linear-gradient(45deg,#181b22,#181b22 3px,#282e3a 3px,#282e3a 6px); border:1px dashed #555;"></div><span>Free Gap</span></div>
                </div>
                <div class="legend-section">
                    <span class="legend-title">Pass Types:</span>
                    <div class="legend-item"><div class="legend-dot dot-raster"></div><span>Raster / Native</span></div>
                    <div class="legend-item"><div class="legend-dot dot-compute"></div><span>Direct Compute</span></div>
                    <div class="legend-item"><div class="legend-dot dot-async"></div><span>Async Compute</span></div>
                    <div class="legend-item"><div class="legend-dot dot-unsafe"></div><span>Unsafe</span></div>
                    <div class="legend-item"><div class="legend-dot dot-culled"></div><span>Culled</span></div>
                </div>
                <div class="legend-section">
                    <span class="legend-title">Resources:</span>
                    <span class="type-tag tag-tex">TEX</span><span>Texture</span>
                    <span class="type-tag tag-buf">BUF</span><span>Buffer</span>
                    <span class="type-tag tag-imp">IMP</span><span>Imported</span>
                    <span class="type-tag tag-ext">EXT</span><span>Extracted</span>
                    <span class="type-tag tag-alias">ALIAS</span><span>Aliased</span>
                </div>
            </div>

            <div class="controls-bar">
                <input type="text" id="searchInput" class="search-box" placeholder="Filter resources by name...">
                <label class="control-item">
                    <input type="checkbox" id="hideCulledCheck" checked>
                    <span>Hide Culled Passes</span>
                </label>
                <label class="control-item">
                    <input type="checkbox" id="onlyAliasedCheck">
                    <span>Only Aliased Resources</span>
                </label>
                <label class="control-item">
                    <span>Filter:</span>
                    <select id="typeFilter" class="search-box" style="width:110px;">
                        <option value="all">All Types</option>
                        <option value="tex">Textures Only</option>
                        <option value="buf">Buffers Only</option>
                    </select>
                </label>
                <label class="control-item">
                    <span>Pass Order:</span>
                    <select id="orderSelect" class="search-box" style="width:130px;">
                        <option value="declaration">Declaration Order</option>
                        <option value="schedule">Schedule Order</option>
                    </select>
                </label>
                <div class="control-item" style="margin-left: auto;">
                    <span>Zoom:</span>
                    <button id="zoomOutBtn" class="btn-zoom">−</button>
                    <span id="zoomLevel" style="min-width:40px; text-align:center; font-weight:bold;">100%</span>
                    <button id="zoomInBtn" class="btn-zoom">＋</button>
                    <button id="zoomResetBtn" class="btn-zoom" style="font-size:11px;">Reset</button>
                </div>
            </div>

            <div class="inspector-container" id="inspectorContainer">
                <!-- Top Header Pane: Fixed, No Vertical Scroll -->
                <div class="header-pane">
                    <div class="corner-header">
                        <div class="corner-title" id="cornerTitle">Resources</div>
                        <div class="corner-sub" id="cornerSub">Loading...</div>
                    </div>
                    <div class="pass-headers-viewport" id="headerViewport">
                        <div class="pass-headers-track" id="headerTrack">
                            <!-- Populated with .pass-header-col via JS -->
                        </div>
                    </div>
                </div>

                <!-- Main Body Pane: Vertical & Horizontal Scroll -->
                <div class="body-viewport" id="bodyViewport">
                    <table class="body-table" id="bodyTable">
                        <!-- Populated with resource rows via JS -->
                    </table>
                </div>
            </div>

            <!-- Heap Debug View Container -->
            <div class="heap-container" id="heapContainer">
                <!-- Metrics Grid -->
                <div class="heap-metrics-grid" id="heapMetricsGrid">
                    <!-- Populated via JS -->
                </div>

                <!-- 1D Linear Address Space Bar -->
                <div class="heap-section">
                    <div class="heap-section-header">
                        <div class="heap-section-title">
                            <span>Linear Address Space (0x00000000 – 0x{{TotalHeapSize:X8}})</span>
                        </div>
                        <div class="heap-legend-mini">
                            <span><span class="heap-legend-dot" style="background:#388bfd; width:8px; height:8px; border-radius:2px; display:inline-block; margin-right:4px;"></span>Dedicated</span>
                            <span><span class="heap-legend-dot" style="background:#d29922; width:8px; height:8px; border-radius:2px; display:inline-block; margin-right:4px;"></span>Aliased</span>
                            <span><span class="heap-legend-dot" style="background:#f0883e; width:8px; height:8px; border-radius:2px; display:inline-block; margin-right:4px;"></span>Buffer</span>
                            <span><span class="heap-legend-dot" style="background:rgba(255,255,255,0.15); width:8px; height:8px; border-radius:2px; display:inline-block; margin-right:4px;"></span>Free Gap</span>
                        </div>
                    </div>
                    <div class="heap-bar-ruler" id="heapRuler">
                        <span>0 MB (0x0)</span>
                        <span>{{(TotalHeapSize * 0.25) / (1024.0 * 1024.0):F1}} MB</span>
                        <span>{{(TotalHeapSize * 0.50) / (1024.0 * 1024.0):F1}} MB</span>
                        <span>{{(TotalHeapSize * 0.75) / (1024.0 * 1024.0):F1}} MB</span>
                        <span>{{TotalHeapSize / (1024.0 * 1024.0):F1}} MB</span>
                    </div>
                    <div class="heap-bar-track" id="heapBarTrack">
                        <!-- Populated via JS -->
                    </div>
                </div>

                <!-- 2D Memory Aliasing Chart -->
                <div class="heap-section" style="flex: 1; min-height: 400px;">
                    <div class="heap-section-header">
                        <div class="heap-section-title">
                            <span>2D Memory Aliasing Map (Offset vs Pass Lifetime)</span>
                        </div>
                        <span style="font-size:11px; color:var(--text-secondary);">Vertical: Heap Offset (MB) · Horizontal: Pass Execution Timeline</span>
                    </div>
                    <div class="heap-2d-viewport" id="heap2dViewport">
                        <div class="heap-2d-canvas-wrap" id="heap2dCanvasWrap">
                            <div class="heap-2d-yaxis" id="heap2dYAxis">
                                <!-- Populated via JS -->
                            </div>
                            <div class="heap-2d-main">
                                <div class="heap-2d-pass-headers" id="heap2dPassHeaders">
                                    <!-- Populated via JS -->
                                </div>
                                <div class="heap-2d-chart-body" id="heap2dChartBody">
                                    <!-- Populated via JS -->
                                </div>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Heap Blocks Breakdown Table -->
                <div class="heap-section">
                    <div class="heap-section-header">
                        <div class="heap-section-title">
                            <span>Memory Blocks & Resource Placements</span>
                        </div>
                        <span id="heapBlockCountLabel" style="font-size:11.5px; color:var(--text-secondary);"></span>
                    </div>
                    <table class="heap-table" id="heapBlocksTable">
                        <thead>
                            <tr>
                                <th style="width:70px;">Block</th>
                                <th style="width:170px;">Offset Range</th>
                                <th style="width:90px;">Size</th>
                                <th style="width:130px;">Status</th>
                                <th>Occupants & Sequential Lifetime</th>
                                <th style="width:100px; text-align:right;">Actions</th>
                            </tr>
                        </thead>
                        <tbody id="heapBlocksTableBody">
                            <!-- Populated via JS -->
                        </tbody>
                    </table>
                </div>
            </div>

            <div id="tooltip"></div>

            <script>
                const rawPasses = {{passesJson}};
                const rawResources = {{resourcesJson}};
                const rawMemory = {{memoryJson}};
                const totalHeapSize = {{TotalHeapSize}};
                const sizeWithoutAliasing = {{SizeWithoutAliasing}};
                let currentView = 'timeline';
                let selectedBlockOffset = null;
                let selectedResourceId = null;

                function switchView(viewName) {
                    currentView = viewName;
                    const tabTimeline = document.getElementById('viewTabTimeline');
                    const tabHeap = document.getElementById('viewTabHeap');
                    const inspectorContainer = document.getElementById('inspectorContainer');
                    const heapContainer = document.getElementById('heapContainer');
                    const timelineLegend = document.getElementById('timelineAccessLegend');
                    const heapLegend = document.getElementById('heapPlacementLegend');

                    if (viewName === 'heap') {
                        tabTimeline.classList.remove('active');
                        tabHeap.classList.add('active');
                        inspectorContainer.style.display = 'none';
                        heapContainer.style.display = 'flex';
                        if (timelineLegend) timelineLegend.style.display = 'none';
                        if (heapLegend) heapLegend.style.display = 'flex';
                        renderHeapView();
                    } else {
                        tabTimeline.classList.add('active');
                        tabHeap.classList.remove('active');
                        inspectorContainer.style.display = 'flex';
                        heapContainer.style.display = 'none';
                        if (timelineLegend) timelineLegend.style.display = 'flex';
                        if (heapLegend) heapLegend.style.display = 'none';
                        renderTable();
                    }
                }

                // Horizontal scroll synchronization: body drives header
                const bodyViewport = document.getElementById('bodyViewport');
                const headerViewport = document.getElementById('headerViewport');
                bodyViewport.addEventListener('scroll', () => {
                    headerViewport.scrollLeft = bodyViewport.scrollLeft;
                }, { passive: true });

                let currentZoom = 1.0;
                function setZoom(val) {
                    currentZoom = Math.max(0.6, Math.min(2.0, Math.round(val * 10) / 10));
                    document.getElementById('inspectorContainer').style.zoom = currentZoom;
                    document.getElementById('heapContainer').style.zoom = currentZoom;
                    document.getElementById('zoomLevel').innerText = Math.round(currentZoom * 100) + '%';
                }
                document.getElementById('zoomInBtn').onclick = () => setZoom(currentZoom + 0.1);
                document.getElementById('zoomOutBtn').onclick = () => setZoom(currentZoom - 0.1);
                document.getElementById('zoomResetBtn').onclick = () => setZoom(1.0);

                function formatBytes(bytes) {
                    if (!bytes || bytes === 0) return '0 B';
                    if (bytes >= 1024 * 1024 * 1024) return (bytes / (1024 * 1024 * 1024)).toFixed(2) + ' GB';
                    if (bytes >= 1024 * 1024) return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
                    if (bytes >= 1024) return (bytes / 1024).toFixed(0) + ' KB';
                    return bytes + ' B';
                }

                function getPassBadgeInfo(pass) {
                    if (pass.IsCulled) return { label: 'CUL', css: 'badge-culled', desc: 'Culled' };
                    if (pass.AsyncCompute) return { label: 'ASYNC', css: 'badge-async', desc: 'Async Compute' };
                    if (pass.Type === 1) return { label: 'COMP', css: 'badge-compute', desc: 'Direct Compute' };
                    if (pass.Type === 0) {
                        const isNative = pass.NativePassIndex !== undefined && pass.NativePassIndex >= 0;
                        return { label: isNative ? ('N' + pass.NativePassIndex) : 'RAST', css: 'badge-raster', desc: isNative ? ('Native RenderPass #' + pass.NativePassIndex) : 'Raster Pass' };
                    }
                    return { label: 'UNSAFE', css: 'badge-unsafe', desc: 'Unsafe Pass' };
                }

                function renderTable() {
                    const hideCulled = document.getElementById('hideCulledCheck').checked;
                    const onlyAliased = document.getElementById('onlyAliasedCheck').checked;
                    const typeFilter = document.getElementById('typeFilter').value;
                    const passOrder = document.getElementById('orderSelect').value;
                    const searchText = document.getElementById('searchInput').value.trim().toLowerCase();

                    // Filter and sort passes
                    let displayPasses = [...rawPasses];
                    if (hideCulled) {
                        displayPasses = displayPasses.filter(p => !p.IsCulled);
                    }
                    if (passOrder === 'schedule') {
                        displayPasses.sort((a, b) => {
                            if (a.IsCulled !== b.IsCulled) return a.IsCulled ? 1 : -1;
                            return a.Index - b.Index;
                        });
                    } else {
                        displayPasses.sort((a, b) => a.Index - b.Index);
                    }

                    // Filter resources
                    let displayResources = rawResources.filter(r => {
                        if (onlyAliased && (!r.AliasedWithResources || r.AliasedWithResources.length === 0)) return false;
                        if (typeFilter === 'tex' && r.Type !== 0) return false;
                        if (typeFilter === 'buf' && r.Type !== 1) return false;
                        if (searchText && !r.Name.toLowerCase().includes(searchText)) return false;
                        return true;
                    });

                    // Update Corner Info
                    document.getElementById('cornerTitle').innerText = `Resources (${displayResources.length})`;
                    document.getElementById('cornerSub').innerText = `Showing ${displayPasses.length} passes`;

                    // Render Pass Headers
                    let headerHtml = '';
                    displayPasses.forEach((pass, pIdx) => {
                        const badge = getPassBadgeInfo(pass);
                        const culledClass = pass.IsCulled ? 'is-culled' : '';
                        headerHtml += `
                        <div class="pass-header-col" data-pass-idx="${pass.Index}">
                            <div class="pass-name-anchor">
                                <span class="pass-name ${culledClass}" title="P${pass.Index}: ${pass.Name} (${badge.desc})">P${pass.Index} ${pass.Name}</span>
                            </div>
                            <div class="pass-badge ${badge.css}" title="${badge.desc}">${badge.label}</div>
                        </div>`;
                    });
                    document.getElementById('headerTrack').innerHTML = headerHtml;

                    // Render Body Rows
                    let bodyHtml = '<tbody>';
                    displayResources.forEach(res => {
                        const isTex = res.Type === 0;
                        const isAliased = res.AliasedWithResources && res.AliasedWithResources.length > 0;

                        bodyHtml += '<tr>';
                        bodyHtml += `<td class="res-cell" id="res-row-${res.LogicalResourceId}" title="${res.Name}&#10;Logical ID: ${res.LogicalResourceId}&#10;Offset: 0x${(res.HeapOffset || 0).toString(16)}&#10;Size: ${formatBytes(res.SizeInBytes)}">
                            <span class="type-tag ${isTex ? 'tag-tex' : 'tag-buf'}">${isTex ? 'TEX' : 'BUF'}</span>
                            ${res.IsImported ? '<span class="type-tag tag-imp">IMP</span>' : ''}
                            ${res.IsExtracted ? '<span class="type-tag tag-ext">EXT</span>' : ''}
                            ${isAliased ? '<span class="type-tag tag-alias">ALIAS</span>' : ''}
                            <span class="res-name">${res.Name}</span>
                            <span class="res-size">${formatBytes(res.SizeInBytes)}</span>
                            ${!res.IsImported && res.SizeInBytes > 0 ? `<button class="btn-jump" onclick="jumpToHeap(${res.LogicalResourceId}); event.stopPropagation();" title="Inspect memory placement in Heap Memory Map">Heap ↗</button>` : ''}
                        </td>`;

                        displayPasses.forEach(pass => {
                            const isWrite = (pass.ResourceWrites && pass.ResourceWrites.includes(res.LogicalResourceId))
                                         || (pass.ResourceCreates && pass.ResourceCreates.includes(res.LogicalResourceId));
                            const isRead = pass.ResourceReads && pass.ResourceReads.includes(res.LogicalResourceId);

                            // Lifetime calculation (declaration order or schedule order)
                            let isAlive = false;
                            if (passOrder === 'schedule' && res.ScheduledFirstUseIndex >= 0) {
                                isAlive = pass.Index >= res.ScheduledFirstUseIndex && pass.Index <= res.ScheduledLastUseIndex;
                            } else if (res.FirstUsePass >= 0) {
                                isAlive = pass.Index >= res.FirstUsePass && pass.Index <= res.LastUsePass;
                            }

                            let blockClass = 'cell-empty';
                            let actionText = 'No Access';

                            if (isWrite && isRead) {
                                blockClass = 'cell-readwrite';
                                actionText = 'Read & Write';
                            } else if (isWrite) {
                                blockClass = 'cell-write';
                                actionText = 'Write';
                            } else if (isRead) {
                                blockClass = 'cell-read';
                                actionText = 'Read';
                            } else if (isAlive && !pass.IsCulled) {
                                blockClass = 'cell-retained';
                                actionText = 'Retained in Memory (Alive)';
                            }

                            bodyHtml += `<td class="grid-cell" 
                                data-pass-name="P${pass.Index}: ${pass.Name}" 
                                data-res-name="${res.Name}" 
                                data-res-type="${isTex ? 'Texture' : 'Buffer'}"
                                data-res-size="${formatBytes(res.SizeInBytes)}"
                                data-action="${actionText}">
                                <div class="cell-block ${blockClass}"></div>
                            </td>`;
                        });

                        bodyHtml += '</tr>';
                    });

                    bodyHtml += '</tbody>';
                    document.getElementById('bodyTable').innerHTML = bodyHtml;
                    attachTooltips();
                }

                function selectHeapBlock(offset) {
                    selectedBlockOffset = (selectedBlockOffset === offset) ? null : offset;
                    selectedResourceId = null;
                    renderHeapView();
                    if (selectedBlockOffset !== null) {
                        const row = document.getElementById(`heap-block-row-${offset}`);
                        if (row) row.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
                    }
                }

                function selectHeapResource(resId, offset) {
                    selectedResourceId = (selectedResourceId === resId) ? null : resId;
                    selectedBlockOffset = offset;
                    renderHeapView();
                }

                function jumpToHeap(resId) {
                    const res = rawResources.find(r => r.LogicalResourceId === resId);
                    if (!res) return;
                    switchView('heap');
                    selectedResourceId = resId;
                    selectedBlockOffset = res.HeapOffset;
                    renderHeapView();
                    setTimeout(() => {
                        const row = document.getElementById(`heap-block-row-${res.HeapOffset}`);
                        if (row) row.scrollIntoView({ behavior: 'smooth', block: 'center' });
                    }, 60);
                }

                function jumpToTimeline(resId) {
                    switchView('timeline');
                    setTimeout(() => {
                        const row = document.getElementById(`res-row-${resId}`);
                        if (row) row.scrollIntoView({ behavior: 'smooth', block: 'center' });
                    }, 60);
                }

                function attachHeapTooltips() {
                    const tooltip = document.getElementById('tooltip');

                    document.querySelectorAll('.heap-bar-segment').forEach(seg => {
                        seg.addEventListener('mouseenter', e => {
                            const offset = seg.getAttribute('data-offset');
                            const size = seg.getAttribute('data-size');
                            const status = seg.getAttribute('data-status');
                            const resources = seg.getAttribute('data-resources');

                            tooltip.innerHTML = `
                                <div class="tt-title">Heap Memory Block</div>
                                <div class="tt-row"><span>Offset:</span><span class="tt-val">${offset}</span></div>
                                <div class="tt-row"><span>Size:</span><span class="tt-val">${size}</span></div>
                                <div class="tt-row"><span>Status:</span><span class="tt-val" style="color:#d29922;">${status}</span></div>
                                ${resources ? `<div class="tt-row"><span>Occupants:</span><span class="tt-val">${resources}</span></div>` : ''}
                            `;
                            tooltip.style.display = 'block';
                            tooltip.style.left = (e.clientX + 14) + 'px';
                            tooltip.style.top = (e.clientY + 14) + 'px';
                        });

                        seg.addEventListener('mousemove', e => {
                            tooltip.style.left = (e.clientX + 14) + 'px';
                            tooltip.style.top = (e.clientY + 14) + 'px';
                        });

                        seg.addEventListener('mouseleave', () => {
                            tooltip.style.display = 'none';
                        });
                    });

                    document.querySelectorAll('.heap-2d-resource-block').forEach(blk => {
                        blk.addEventListener('mouseenter', e => {
                            const name = blk.getAttribute('data-res-name');
                            const type = blk.getAttribute('data-res-type');
                            const size = blk.getAttribute('data-res-size');
                            const offset = blk.getAttribute('data-offset');
                            const lifetime = blk.getAttribute('data-lifetime');
                            const aliased = blk.getAttribute('data-aliased-names');

                            tooltip.innerHTML = `
                                <div class="tt-title">${name} (${type})</div>
                                <div class="tt-row"><span>Offset:</span><span class="tt-val">${offset}</span></div>
                                <div class="tt-row"><span>Size:</span><span class="tt-val">${size}</span></div>
                                <div class="tt-row"><span>Lifetime:</span><span class="tt-val" style="color:#58a6ff;">${lifetime}</span></div>
                                ${aliased ? `<div class="tt-row"><span>Aliased With:</span><span class="tt-val" style="color:#d29922;">${aliased}</span></div>` : ''}
                            `;
                            tooltip.style.display = 'block';
                            tooltip.style.left = (e.clientX + 14) + 'px';
                            tooltip.style.top = (e.clientY + 14) + 'px';
                        });

                        blk.addEventListener('mousemove', e => {
                            tooltip.style.left = (e.clientX + 14) + 'px';
                            tooltip.style.top = (e.clientY + 14) + 'px';
                        });

                        blk.addEventListener('mouseleave', () => {
                            tooltip.style.display = 'none';
                        });
                    });
                }

                function renderHeapView() {
                    const hideCulled = document.getElementById('hideCulledCheck').checked;
                    const onlyAliased = document.getElementById('onlyAliasedCheck').checked;
                    const typeFilter = document.getElementById('typeFilter').value;
                    const passOrder = document.getElementById('orderSelect').value;
                    const searchText = document.getElementById('searchInput').value.trim().toLowerCase();

                    // Passes
                    let displayPasses = [...rawPasses];
                    if (hideCulled) {
                        displayPasses = displayPasses.filter(p => !p.IsCulled);
                    }
                    if (passOrder === 'schedule') {
                        displayPasses.sort((a, b) => {
                            if (a.IsCulled !== b.IsCulled) return a.IsCulled ? 1 : -1;
                            return a.Index - b.Index;
                        });
                    } else {
                        displayPasses.sort((a, b) => a.Index - b.Index);
                    }

                    // Resources Map
                    const resMap = new Map();
                    rawResources.forEach(r => resMap.set(r.LogicalResourceId, r));

                    // Placed resources
                    const placedResources = rawResources.filter(r => !r.IsImported && r.SizeInBytes > 0);

                    // Peak Concurrent VRAM Usage
                    let peakUsage = 0;
                    displayPasses.forEach(pass => {
                        let passUsage = 0;
                        placedResources.forEach(res => {
                            let isAlive = false;
                            if (passOrder === 'schedule' && res.ScheduledFirstUseIndex >= 0) {
                                isAlive = pass.Index >= res.ScheduledFirstUseIndex && pass.Index <= res.ScheduledLastUseIndex;
                            } else if (res.FirstUsePass >= 0) {
                                isAlive = pass.Index >= res.FirstUsePass && pass.Index <= res.LastUsePass;
                            }
                            if (isAlive) passUsage += res.SizeInBytes;
                        });
                        peakUsage = Math.max(peakUsage, passUsage);
                    });

                    // Stats
                    const nonFreeBlocks = rawMemory.filter(b => !b.IsFree);
                    const aliasedBlocks = nonFreeBlocks.filter(b => b.AliasedLogicalResources && b.AliasedLogicalResources.length > 1);

                    const metricsHtml = `
                        <div class="heap-metric-card">
                            <span class="hm-label">Total Committed Heap</span>
                            <span class="hm-val">${formatBytes(totalHeapSize)}</span>
                            <span class="hm-sub">Physical Address Space</span>
                        </div>
                        <div class="heap-metric-card">
                            <span class="hm-label">Peak Concurrent Usage</span>
                            <span class="hm-val" style="color:#58a6ff;">${formatBytes(peakUsage)}</span>
                            <span class="hm-sub">${totalHeapSize > 0 ? ((peakUsage / totalHeapSize) * 100).toFixed(1) : 0}% of committed heap</span>
                        </div>
                        <div class="heap-metric-card">
                            <span class="hm-label">Unaliased Memory Size</span>
                            <span class="hm-val">${formatBytes(sizeWithoutAliasing)}</span>
                            <span class="hm-sub">Sum of all transients</span>
                        </div>
                        <div class="heap-metric-card">
                            <span class="hm-label">Aliasing Savings</span>
                            <span class="hm-val" style="color:#3fb950;">${sizeWithoutAliasing > 0 ? ((1.0 - totalHeapSize / sizeWithoutAliasing) * 100).toFixed(1) : 0}%</span>
                            <span class="hm-sub">${formatBytes(Math.max(0, sizeWithoutAliasing - totalHeapSize))} saved</span>
                        </div>
                        <div class="heap-metric-card">
                            <span class="hm-label">Memory Placement Slots</span>
                            <span class="hm-val">${nonFreeBlocks.length} Blocks</span>
                            <span class="hm-sub">${aliasedBlocks.length} aliased · ${nonFreeBlocks.length - aliasedBlocks.length} dedicated</span>
                        </div>
                    `;
                    document.getElementById('heapMetricsGrid').innerHTML = metricsHtml;

                    // 1D Linear Address Space Bar
                    let barHtml = '';
                    rawMemory.forEach((block, idx) => {
                        if (totalHeapSize === 0) return;
                        const widthPercent = (block.Size / totalHeapSize) * 100;
                        let isAliased = !block.IsFree && block.AliasedLogicalResources && block.AliasedLogicalResources.length > 1;
                        let isBuf = false;
                        let firstRes = null;
                        if (!block.IsFree && block.AliasedLogicalResources && block.AliasedLogicalResources.length > 0) {
                            firstRes = resMap.get(block.AliasedLogicalResources[0]);
                            if (firstRes && firstRes.Type === 1) isBuf = true;
                        }

                        let cssClass = 'is-free';
                        if (!block.IsFree) {
                            cssClass = isAliased ? 'is-aliased' : (isBuf ? 'is-buf' : 'is-dedicated');
                        }

                        const isSelected = selectedBlockOffset === block.Offset;
                        if (isSelected) cssClass += ' is-selected';

                        let labelText = '';
                        if (block.IsFree) {
                            labelText = `Free (${formatBytes(block.Size)})`;
                        } else if (firstRes) {
                            labelText = isAliased ? `[${block.AliasedLogicalResources.length} Aliased] ${firstRes.Name}` : firstRes.Name;
                        }

                        barHtml += `<div class="heap-bar-segment ${cssClass}" 
                            style="width: ${widthPercent}%;" 
                            data-block-idx="${idx}"
                            data-offset="0x${block.Offset.toString(16).toUpperCase()}"
                            data-offset-num="${block.Offset}"
                            data-size="${formatBytes(block.Size)}"
                            data-status="${block.IsFree ? 'Free Gap' : (isAliased ? 'Aliased (' + block.AliasedLogicalResources.length + ')' : 'Dedicated')}"
                            data-resources="${block.AliasedLogicalResources ? block.AliasedLogicalResources.map(id => resMap.get(id)?.Name || '#' + id).join(', ') : ''}"
                            onclick="selectHeapBlock(${block.Offset})">
                            <span class="heap-bar-label">${labelText}</span>
                        </div>`;
                    });
                    document.getElementById('heapBarTrack').innerHTML = barHtml || '<div style="padding:10px; color:var(--text-muted);">No heap allocations recorded</div>';

                    // 2D Memory Aliasing Chart
                    let passHeadersHtml = '';
                    displayPasses.forEach(pass => {
                        const badge = getPassBadgeInfo(pass);
                        passHeadersHtml += `<div class="heap-2d-pass-col-header" title="P${pass.Index}: ${pass.Name} (${badge.desc})">P${pass.Index}</div>`;
                    });
                    document.getElementById('heap2dPassHeaders').innerHTML = passHeadersHtml;

                    let yAxisHtml = '';
                    const yTickCount = 5;
                    for (let i = 0; i <= yTickCount; i++) {
                        const offsetVal = (totalHeapSize * i) / yTickCount;
                        yAxisHtml += `<div>${formatBytes(offsetVal)}</div>`;
                    }
                    document.getElementById('heap2dYAxis').innerHTML = yAxisHtml;

                    const colWidth = 36;
                    const chartHeight = 400;
                    let chartBlocksHtml = '';

                    placedResources.forEach(res => {
                        if (onlyAliased && (!res.AliasedWithResources || res.AliasedWithResources.length === 0)) return;
                        if (typeFilter === 'tex' && res.Type !== 0) return;
                        if (typeFilter === 'buf' && res.Type !== 1) return;

                        const isMatchSearch = !searchText || res.Name.toLowerCase().includes(searchText);

                        let startPassIndex = -1;
                        let endPassIndex = -1;

                        displayPasses.forEach((p, idx) => {
                            const passNum = p.Index;
                            let isAlive = false;
                            if (passOrder === 'schedule' && res.ScheduledFirstUseIndex >= 0) {
                                isAlive = passNum >= res.ScheduledFirstUseIndex && passNum <= res.ScheduledLastUseIndex;
                            } else if (res.FirstUsePass >= 0) {
                                isAlive = passNum >= res.FirstUsePass && passNum <= res.LastUsePass;
                            }
                            if (isAlive) {
                                if (startPassIndex === -1) startPassIndex = idx;
                                endPassIndex = idx;
                            }
                        });

                        if (startPassIndex === -1) return;

                        const left = startPassIndex * colWidth;
                        const width = Math.max(colWidth - 2, (endPassIndex - startPassIndex + 1) * colWidth - 2);

                        const topRatio = totalHeapSize > 0 ? (res.HeapOffset / totalHeapSize) : 0;
                        const heightRatio = totalHeapSize > 0 ? (res.SizeInBytes / totalHeapSize) : 0;
                        const top = Math.round(topRatio * chartHeight);
                        const height = Math.max(18, Math.round(heightRatio * chartHeight));

                        const isAliased = res.AliasedWithResources && res.AliasedWithResources.length > 0;
                        const isBuf = res.Type === 1;
                        let cssClass = isAliased ? 'is-aliased' : (isBuf ? 'is-buf' : 'is-dedicated');

                        if (!isMatchSearch) cssClass += ' dimmed';
                        if (selectedResourceId === res.LogicalResourceId || selectedBlockOffset === res.HeapOffset) {
                            cssClass += ' highlighted';
                        }

                        const aliasedNames = (res.AliasedWithResources || []).map(id => resMap.get(id)?.Name || '#' + id).join(', ');

                        chartBlocksHtml += `
                            <div class="heap-2d-resource-block ${cssClass}"
                                 style="left: ${left}px; top: ${top}px; width: ${width}px; height: ${height}px;"
                                 data-res-id="${res.LogicalResourceId}"
                                 data-res-name="${res.Name}"
                                 data-res-type="${isBuf ? 'Buffer' : 'Texture'}"
                                 data-res-size="${formatBytes(res.SizeInBytes)}"
                                 data-offset="0x${res.HeapOffset.toString(16).toUpperCase()}"
                                 data-offset-num="${res.HeapOffset}"
                                 data-aliased-names="${aliasedNames}"
                                 data-lifetime="P${res.FirstUsePass}..P${res.LastUsePass}"
                                 onclick="selectHeapResource(${res.LogicalResourceId}, ${res.HeapOffset})">
                                <span style="font-size:9.5px; font-weight:700; white-space:nowrap; overflow:hidden; text-overflow:ellipsis;">
                                    ${isBuf ? 'BUF' : 'TEX'} ${res.Name}
                                </span>
                                <span style="font-size:8.5px; opacity:0.85; white-space:nowrap; overflow:hidden; text-overflow:ellipsis;">
                                    ${formatBytes(res.SizeInBytes)} · 0x${res.HeapOffset.toString(16).toUpperCase()}
                                </span>
                            </div>
                        `;
                    });

                    document.getElementById('heap2dChartBody').style.width = (displayPasses.length * colWidth) + 'px';
                    document.getElementById('heap2dPassHeaders').style.width = (displayPasses.length * colWidth) + 'px';
                    document.getElementById('heap2dChartBody').innerHTML = chartBlocksHtml || '<div style="padding:20px; color:var(--text-muted);">No placed resources match current filter</div>';

                    // 5. Render Heap Blocks Table
                    let tableHtml = '';
                    let visibleBlockCount = 0;

                    rawMemory.forEach((block, idx) => {
                        if (block.IsFree) {
                            if (onlyAliased) return;
                            visibleBlockCount++;
                            tableHtml += `
                                <tr style="opacity:0.6;">
                                    <td><span style="font-family:'Consolas',monospace; color:var(--text-muted);">#${idx}</span></td>
                                    <td style="font-family:'Consolas',monospace;">0x${block.Offset.toString(16).toUpperCase()}</td>
                                    <td>${formatBytes(block.Size)}</td>
                                    <td><span class="type-tag" style="background:#333; color:#aaa;">FREE GAP</span></td>
                                    <td style="color:var(--text-muted);">Unallocated alignment or padding space</td>
                                    <td style="text-align:right;">-</td>
                                </tr>
                            `;
                            return;
                        }

                        const occupants = (block.AliasedLogicalResources || [])
                            .map(id => resMap.get(id))
                            .filter(r => r !== undefined);

                        const hasAliasing = occupants.length > 1;
                        if (onlyAliased && !hasAliasing) return;

                        if (searchText && !occupants.some(o => o.Name.toLowerCase().includes(searchText))) {
                            return;
                        }

                        visibleBlockCount++;
                        const isSelected = selectedBlockOffset === block.Offset;
                        const trClass = isSelected ? 'is-selected' : '';

                        let occupantsHtml = '<div class="occupants-flow">';
                        occupants.forEach((occ, oIdx) => {
                            const isTex = occ.Type === 0;
                            occupantsHtml += `
                                <span class="occupant-pill" onclick="jumpToTimeline(${occ.LogicalResourceId}); event.stopPropagation();" title="Logical ID #${occ.LogicalResourceId}&#10;Click to view in Pass Timeline">
                                    <span class="type-tag ${isTex ? 'tag-tex' : 'tag-buf'}">${isTex ? 'TEX' : 'BUF'}</span>
                                    <strong>${occ.Name}</strong>
                                    <span style="color:var(--text-muted); font-size:10px;">(${formatBytes(occ.SizeInBytes)}, P${occ.FirstUsePass}..P${occ.LastUsePass})</span>
                                </span>
                            `;
                            if (oIdx < occupants.length - 1) {
                                occupantsHtml += '<span class="arrow-separator">➔</span>';
                            }
                        });
                        occupantsHtml += '</div>';

                        tableHtml += `
                            <tr class="${trClass}" id="heap-block-row-${block.Offset}" onclick="selectHeapBlock(${block.Offset})">
                                <td><span style="font-family:'Consolas',monospace; font-weight:700;">#${idx}</span></td>
                                <td style="font-family:'Consolas',monospace; font-size:11px;">
                                    0x${block.Offset.toString(16).toUpperCase()}
                                    <span style="color:var(--text-muted);">(${formatBytes(block.Offset)})</span>
                                </td>
                                <td style="font-weight:600;">${formatBytes(block.Size)}</td>
                                <td>
                                    ${hasAliasing 
                                        ? `<span class="type-tag tag-alias">ALIASED (${occupants.length})</span>` 
                                        : '<span class="type-tag tag-tex" style="background:#1f6feb;">DEDICATED</span>'}
                                </td>
                                <td>${occupantsHtml}</td>
                                <td style="text-align:right;">
                                    <button class="btn-jump" onclick="selectHeapBlock(${block.Offset}); event.stopPropagation();">Inspect</button>
                                </td>
                            </tr>
                        `;
                    });

                    document.getElementById('heapBlockCountLabel').innerText = `Showing ${visibleBlockCount} memory blocks`;
                    document.getElementById('heapBlocksTableBody').innerHTML = tableHtml || '<tr><td colspan="6" style="padding:16px; text-align:center; color:var(--text-muted);">No memory blocks match filter</td></tr>';

                    attachHeapTooltips();
                }

                function onFilterChange() {
                    if (currentView === 'heap') {
                        renderHeapView();
                    } else {
                        renderTable();
                    }
                }

                document.getElementById('searchInput').addEventListener('input', onFilterChange);
                document.getElementById('hideCulledCheck').addEventListener('change', onFilterChange);
                document.getElementById('onlyAliasedCheck').addEventListener('change', onFilterChange);
                document.getElementById('typeFilter').addEventListener('change', onFilterChange);
                document.getElementById('orderSelect').addEventListener('change', onFilterChange);

                renderTable();
            </script>
        </body>
        </html>
        """;

        return html;
    }
}

[JsonSerializable(typeof(List<PassDumpInfo>))]
[JsonSerializable(typeof(List<ResourceDumpInfo>))]
[JsonSerializable(typeof(List<HeapBlockDumpInfo>))]
[JsonSourceGenerationOptions(WriteIndented = true)]
public partial class RGDumpJsonContext : JsonSerializerContext
{
}
