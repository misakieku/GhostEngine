using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Ghost.Graphics.RenderGraphModule;

internal sealed class RenderGraphExecutor
{
    private readonly ResourceManager _resourceManager;
    private readonly IResourceDatabase _resourceDatabase;
    private readonly RenderGraphResourceRegistry _resources;

    public RenderGraphExecutor(
        ResourceManager resourceManager,
        IResourceDatabase resourceDatabase,
        RenderGraphResourceRegistry resources)
    {
        _resourceManager = resourceManager;
        _resourceDatabase = resourceDatabase;
        _resources = resources;
    }

    private void SetViewport(RenderGraphContext context, ReadOnlySpan<RenderTargetInfo> color, DepthStencilInfo depthStencil)
    {
        // The compiler should reject invalid render-target configurations.
        // Retain a debug assertion here to protect the executor invariant.
        Logger.DebugAssert(color.Length > 0 || depthStencil.texture.IsValid);

        ViewportDesc viewportDesc = default;
        ScissorRectDesc scissorDesc = default;

        if (depthStencil.texture.IsValid)
        {
            viewportDesc = new ViewportDesc
            {
                X = 0,
                Y = 0,
                Width = _resources.GetResource(depthStencil.texture).resolvedWidth,
                Height = _resources.GetResource(depthStencil.texture).resolvedHeight,
                MinDepth = 0,
                MaxDepth = 1
            };

            scissorDesc = new ScissorRectDesc
            {
                Left = 0,
                Top = 0,
                Right = (uint)viewportDesc.Width,
                Bottom = (uint)viewportDesc.Height
            };
        }
        else if (color[0].texture.IsValid)
        {
            viewportDesc = new ViewportDesc
            {
                X = 0,
                Y = 0,
                Width = _resources.GetResource(color[0].texture).resolvedWidth,
                Height = _resources.GetResource(color[0].texture).resolvedHeight,
                MinDepth = 0,
                MaxDepth = 1
            };

            scissorDesc = new ScissorRectDesc
            {
                Left = 0,
                Top = 0,
                Right = (uint)viewportDesc.Width,
                Bottom = (uint)viewportDesc.Height
            };
        }

        context.SetViewport(viewportDesc);
        context.SetScissorRect(scissorDesc);
    }

    public unsafe Error Execute(
        ICommandBuffer graphicsCommandBuffer,
        RenderGraphContext context,
        scoped in CompiledGraph graph)
    {
        var activeCommandBuffer = graphicsCommandBuffer;
        context.BeginNewFrame(activeCommandBuffer);

        var pPassRTDescs = stackalloc PassRenderTargetDesc[8];
        var pRtFormats = stackalloc TextureFormat[8];
        var insideNativePass = false;
        var reader = new SpanReader(graph.commandStream);

        try
        {
            while (reader.RemainingBytes > 0)
            {
                var op = reader.Read<RGExecutionOpType>();

                switch (op)
                {
                    case RGExecutionOpType.IssueBarriers:
                    {
                        var barrierCount = reader.Read<int>();
                        var e = ExecuteBarrierBatch(activeCommandBuffer, barrierCount, ref reader);
                        if (e != Error.None)
                        {
                            return e;
                        }
                        break;
                    }

                    case RGExecutionOpType.BeginNativePass:
                    {
                        var nativePassIdx = reader.Read<int>();
                        var nativePass = graph.nativePasses[nativePassIdx];

                        SetViewport(context, nativePass.colorAttachments, nativePass.depthAttachment);

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
                                : Handle<GPUTexture>.Invalid,
                            ClearDepth = nativePass.depthAttachment.clearDepth,
                            ClearStencil = nativePass.depthAttachment.clearStencil,
                            DepthLoadOp = nativePass.hasDepthAttachment
                                ? nativePass.depthAttachment.loadOp
                                : AttachmentLoadOp.NoAccess,
                            DepthStoreOp = nativePass.hasDepthAttachment
                                ? nativePass.depthAttachment.storeOp
                                : AttachmentStoreOp.NoAccess,
                            StencilLoadOp = nativePass.hasDepthAttachment
                                ? nativePass.depthAttachment.stencilLoadOp
                                : AttachmentLoadOp.NoAccess,
                            StencilStoreOp = nativePass.hasDepthAttachment
                                ? nativePass.depthAttachment.stencilStoreOp
                                : AttachmentStoreOp.NoAccess,
                        };

                        activeCommandBuffer.BeginRenderPass(new Span<PassRenderTargetDesc>(pPassRTDescs, nativePass.colorAttachmentCount), in depthDesc);
                        insideNativePass = true;

                        for (var i = 0; i < nativePass.colorAttachmentCount; i++)
                        {
                            var attachment = nativePass.colorAttachments[i];
                            var resource = _resources.GetResource(attachment.texture);
                            pRtFormats[i] = resource.rgTextureDesc.format;
                        }

                        var depthFormat = nativePass.hasDepthAttachment
                            ? _resources.GetResource(nativePass.depthAttachment.texture).rgTextureDesc.format
                            : TextureFormat.Unknown;
                        context.SetRenderTargetFormats(new ReadOnlySpan<TextureFormat>(pRtFormats, nativePass.colorAttachmentCount), depthFormat);
                        break;
                    }

                    case RGExecutionOpType.ExecutePass:
                    {
                        var passIdx = reader.Read<int>();
                        var pass = graph.passes[passIdx];
                        pass.Execute(context);
                        break;
                    }

                    case RGExecutionOpType.EndNativePass:
                    {
                        activeCommandBuffer.EndRenderPass();
                        insideNativePass = false;
                        break;
                    }

                    case RGExecutionOpType.CommandBufferSyncPoint:
                    {
                        // Phase 2 serializes the planned command-buffer topology for diagnostics and caching.
                        // Native command-buffer splitting remains disabled, so all commands stay on Graphics.
                        _ = RGCommandStream.ReadSyncMarker(ref reader);
                        break;
                    }

                    default:
                        throw new NotSupportedException($"Unsupported RGExecutionOpType: {op}");
                }
            }

            return Error.None;
        }
        catch (Exception ex)
        {
            if (insideNativePass)
            {
                activeCommandBuffer.EndRenderPass();
            }

            // Insert Full Pipeline Barrier
            var barrier = BarrierDesc.Global(BarrierSync.All, BarrierSync.All, BarrierAccess.Common, BarrierAccess.Common);
            activeCommandBuffer.Barrier(barrier);

            Logger.Error(ex);
            return Error.InternalError;
        }
    }

    private Error ExecuteBarrierBatch(
        ICommandBuffer cmd,
        int barrierCount,
        ref SpanReader reader)
    {
        if (barrierCount <= 0)
        {
            return Error.None;
        }

        const int MaxBatch = 64;
        using var scope = AllocationManager.CreateStackScope();
        using var barriers = new UnsafeList<BarrierDesc>(MaxBatch, scope.AllocationHandle);

        void Flush()
        {
            if (barriers.Count > 0)
            {
                cmd.Barrier(barriers);
                barriers.Clear();
            }
        }

        for (var i = 0; i < barrierCount; i++)
        {
            var compiledBarrier = reader.Read<CompiledBarrier>();
            if (compiledBarrier.flags.HasFlag(BarrierFlags.QueueRelease))
            {
                // Phase 3 keeps execution contained on one Graphics command buffer. The acquire
                // record below lowers the full producer-to-consumer transition for this mode.
                continue;
            }

            var resourceHandle = _resources.GetResource(compiledBarrier.resource).backingResource;
            var target = compiledBarrier.targetState;
            var explicitSource = compiledBarrier.flags.HasFlag(BarrierFlags.ExplicitSource);
            var isAliasing = compiledBarrier.aliasingPredecessor.IsValid;
            var force = compiledBarrier.flags.HasFlag(BarrierFlags.Force);

            BarrierDesc desc;
            if (compiledBarrier.resourceType == RGResourceType.Texture)
            {
                desc = explicitSource
                    ? BarrierDesc.TextureExplicit(
                        resourceHandle.AsTexture(),
                        compiledBarrier.sourceState,
                        target,
                        force: force,
                        discard: compiledBarrier.flags.HasFlag(BarrierFlags.Discard),
                        isAliasing: isAliasing)
                    : BarrierDesc.Texture(
                        resourceHandle.AsTexture(),
                        target.sync,
                        target.access,
                        target.layout,
                        discard: compiledBarrier.flags.HasFlag(BarrierFlags.Discard),
                        isAliasing: isAliasing);
            }
            else
            {
                desc = explicitSource
                    ? BarrierDesc.BufferExplicit(
                        resourceHandle.AsBuffer(),
                        compiledBarrier.sourceState,
                        target,
                        force: force,
                        isAliasing: isAliasing)
                    : BarrierDesc.Buffer(
                        resourceHandle.AsBuffer(),
                        target.sync,
                        target.access,
                        isAliasing: isAliasing);
            }

            if (barriers.Count >= MaxBatch)
            {
                Flush();
            }

            barriers.AddNoResize(desc);
        }

        Flush();

        return Error.None;
    }
}
