using Ghost.Core;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;

namespace Ghost.Graphics.RenderGraphModule;

/// <summary>
/// Handles execution of compiled render graphs, including barrier execution and native render passes.
/// </summary>
internal sealed class RenderGraphExecutor
{
    private readonly IGraphicsEngine _graphicsEngine;
    private readonly RenderGraphResourceRegistry _resources;
    private readonly RenderGraphContext _context;

    public RenderGraphExecutor(
        IGraphicsEngine graphicsEngine,
        RenderGraphResourceRegistry resources,
        RenderGraphContext context)
    {
        _graphicsEngine = graphicsEngine;
        _resources = resources;
        _context = context;
    }

    /// <summary>
    /// Executes all compiled passes using native render passes where possible.
    /// </summary>
    public unsafe void Execute(
        ICommandBuffer cmd,
        List<RenderGraphPassBase> compiledPasses,
        List<NativeRenderPass> nativePasses,
        List<CompiledBarrier> compiledBarriers)
    {
        var barrierIndex = 0;
        var nativePassIndex = 0;
        var logicalPassIndex = 0;

        _context.SetCommandBuffer(cmd);

        var pPassRTDescs = stackalloc PassRenderTargetDesc[8];
        var pRtFormats = stackalloc TextureFormat[8];

        while (logicalPassIndex < compiledPasses.Count)
        {
            var pass = compiledPasses[logicalPassIndex];

            // Check if this pass is part of a native render pass
            if (pass.type == RenderPassType.Raster && nativePassIndex < nativePasses.Count)
            {
                var nativePass = nativePasses[nativePassIndex];

                // Build barriers for ALL merged passes before beginning the native render pass
                for (var i = 0; i < nativePass.mergedPassIndices.Count; i++)
                {
                    var mergedPassIdx = nativePass.mergedPassIndices[i];
                    ExecuteBarriersForPass(cmd, mergedPassIdx, ref barrierIndex, compiledBarriers);
                }

                // Begin native render pass
                for (var i = 0; i < nativePass.colorAttachmentCount; i++)
                {
                    var attachment = nativePass.colorAttachments[i];
                    pPassRTDescs[i] = new PassRenderTargetDesc
                    {
                        Texture = _resources.GetResource(attachment.texture).backingResource.AsTexture(),
                        ClearColor = attachment.clearColor,
                        LoadOp = attachment.loadOp,
                        StoreOp = attachment.storeOp
                    };
                }

                var depthDesc = new PassDepthStencilDesc
                {
                    Texture = nativePass.hasDepthAttachment
                        ? _resources.GetResource(nativePass.depthAttachment.texture).backingResource.AsTexture()
                        : Handle<Texture>.Invalid,
                    ClearDepth = nativePass.depthAttachment.clearDepth,
                    ClearStencil = nativePass.depthAttachment.clearStencil,
                    DepthLoadOp = nativePass.hasDepthAttachment
                        ? nativePass.depthAttachment.loadOp
                        : AttachmentLoadOp.DontCare,
                    DepthStoreOp = nativePass.hasDepthAttachment
                        ? nativePass.depthAttachment.storeOp
                        : AttachmentStoreOp.DontCare,
                    StencilLoadOp = nativePass.hasDepthAttachment
                        ? nativePass.depthAttachment.loadOp
                        : AttachmentLoadOp.DontCare,
                    StencilStoreOp = nativePass.hasDepthAttachment
                        ? nativePass.depthAttachment.storeOp
                        : AttachmentStoreOp.DontCare
                };

                cmd.BeginRenderPass(new Span<PassRenderTargetDesc>(pPassRTDescs, nativePass.colorAttachmentCount), depthDesc);

                for (var i = 0; i < nativePass.colorAttachmentCount; i++)
                {
                    var attachment = nativePass.colorAttachments[i];
                    var resource = _resources.GetResource(attachment.texture);
                    pRtFormats[i] = resource.rgTextureDesc.format;
                }

                var depthFormat = nativePass.hasDepthAttachment
                    ? _resources.GetResource(nativePass.depthAttachment.texture).rgTextureDesc.format
                    : TextureFormat.Unknown;
                _context.SetRenderTargetFormats(new ReadOnlySpan<TextureFormat>(pRtFormats, nativePass.colorAttachmentCount), depthFormat);

                // Build all merged logical passes within this native render pass
                for (var i = 0; i < nativePass.mergedPassIndices.Count; i++)
                {
                    var mergedPassIdx = nativePass.mergedPassIndices[i];
                    var mergedPass = compiledPasses[mergedPassIdx];
                    mergedPass.Execute(_context);
                    logicalPassIndex++;
                }

                cmd.EndRenderPass();
                nativePassIndex++;
            }
            else
            {
                // Compute pass or standalone raster pass (not merged) or Unsafe pass
                ExecuteBarriersForPass(cmd, logicalPassIndex, ref barrierIndex, compiledBarriers);
                pass.Execute(_context);
                logicalPassIndex++;
            }

        }
    }

    /// <summary>
    /// Executes all barriers for a specific pass.
    /// Uses pre-compiled barriers and queries before state from ResourceDatabase.
    /// </summary>
    private unsafe void ExecuteBarriersForPass(
        ICommandBuffer cmd,
        int passIndex,
        ref int barrierIndex,
        List<CompiledBarrier> compiledBarriers)
    {
        const int MaxBatch = 64;
        var barriers = stackalloc BarrierDesc[MaxBatch];
        var barrierCount = 0;

        void Flush()
        {
            if (barrierCount > 0)
            {
                cmd.ResourceBarrier(new ReadOnlySpan<BarrierDesc>(barriers, barrierCount));
                barrierCount = 0;
            }
        }

        // Process all pre-compiled barriers for this pass
        while (barrierIndex < compiledBarriers.Count && compiledBarriers[barrierIndex].PassIndex == passIndex)
        {
            var compiledBarrier = compiledBarriers[barrierIndex++];
            var resource = _resources.GetResource(compiledBarrier.Resource);
            var resourceHandle = resource.backingResource;

            // Always query the before state from ResourceDatabase (single source of truth)
            var currentState = _graphicsEngine.ResourceDatabase.GetResourceBarrierData(resourceHandle).GetValueOrThrow();

            BarrierLayout layoutBefore;
            BarrierAccess accessBefore;
            BarrierSync syncBefore;

            // Handle aliasing barriers specially
            if (compiledBarrier.AliasingPredecessor.IsValid)
            {
                var predHandle = _resources.GetResource(compiledBarrier.AliasingPredecessor).backingResource;
                var predState = _graphicsEngine.ResourceDatabase.GetResourceBarrierData(predHandle).GetValueOrThrow();

                layoutBefore = BarrierLayout.Undefined;
                accessBefore = BarrierAccess.NoAccess;
                syncBefore = predState.sync;
            }
            else
            {
                layoutBefore = currentState.layout;
                accessBefore = currentState.access;
                syncBefore = currentState.sync;
            }

            var target = compiledBarrier.TargetState;

            // Skip if already in target state (optimization)
            if (!compiledBarrier.AliasingPredecessor.IsValid && 
                layoutBefore == target.layout && 
                accessBefore == target.access && 
                syncBefore == target.sync)
            {
                continue;
            }

            // Create barrier descriptor
            BarrierDesc desc;
            if (compiledBarrier.ResourceType == RenderGraphResourceType.Texture)
            {
                desc = BarrierDesc.Texture(resourceHandle,
                    syncBefore, target.sync,
                    accessBefore, target.access,
                    layoutBefore, target.layout,
                    discard: compiledBarrier.Flags.HasFlag(BarrierFlags.Discard));
            }
            else
            {
                desc = BarrierDesc.Buffer(resourceHandle,
                    syncBefore, target.sync,
                    accessBefore, target.access);
            }

            if (barrierCount >= MaxBatch)
            {
                Flush();
            }

            barriers[barrierCount++] = desc;
        }

        Flush();
    }
}
