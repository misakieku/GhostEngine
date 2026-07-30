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
    private readonly RenderGraphContext _context;

    public RenderGraphExecutor(
        ResourceManager resourceManager,
        IResourceDatabase resourceDatabase,
        RenderGraphResourceRegistry resources,
        RenderGraphContext context)
    {
        _resourceManager = resourceManager;
        _resourceDatabase = resourceDatabase;
        _resources = resources;
        _context = context;
    }

    private void SetViewport(ReadOnlySpan<RenderTargetInfo> color, DepthStencilInfo depthStencil)
    {
        // This should not happened since the compiler should have rejected any render pass with an invalid render target configuration, but just in case, we use Logger.DebugAssert to validate our assumptions.
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

        _context.SetViewport(viewportDesc);
        _context.SetScissorRect(scissorDesc);
    }

    public unsafe Error Execute(
        ICommandBuffer commandBuffer,
        IReadOnlyList<RenderGraphPass> compiledPasses,
        IReadOnlyList<NativeRenderPass> nativePasses,
        IReadOnlyList<CompiledBarrier> compiledBarriers,
        BufferReader reader)
    {
        _context.BeginNewFrame(commandBuffer);

        var pPassRTDescs = stackalloc PassRenderTargetDesc[8];
        var pRtFormats = stackalloc TextureFormat[8];
        var insideNativePass = false;

        try
        {
            while (reader.RemainingBytes > 0)
            {
                var op = reader.Read<RGExecutionOpType>();

                switch (op)
                {
                    case RGExecutionOpType.IssueBarriers:
                    {
                        var barrierStart = reader.Read<int>();
                        var barrierCount = reader.Read<int>();
                        var e = ExecuteBarrierBatch(commandBuffer, barrierStart, barrierCount, compiledBarriers);
                        if (e != Error.None)
                        {
                            return e;
                        }
                        break;
                    }

                    case RGExecutionOpType.BeginNativePass:
                    {
                        var nativePassIdx = reader.Read<int>();
                        var nativePass = nativePasses[nativePassIdx];

                        SetViewport(nativePass.colorAttachments, nativePass.depthAttachment);

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

                        commandBuffer.BeginRenderPass(new Span<PassRenderTargetDesc>(pPassRTDescs, nativePass.colorAttachmentCount), in depthDesc);
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
                        _context.SetRenderTargetFormats(new ReadOnlySpan<TextureFormat>(pRtFormats, nativePass.colorAttachmentCount), depthFormat);
                        break;
                    }

                    case RGExecutionOpType.ExecutePass:
                    {
                        var passIdx = reader.Read<int>();
                        var pass = compiledPasses[passIdx];
                        pass.Execute(_context);
                        break;
                    }

                    case RGExecutionOpType.EndNativePass:
                    {
                        commandBuffer.EndRenderPass();
                        insideNativePass = false;
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
                commandBuffer.EndRenderPass();
            }

            // Insert Full Pipeline Barrier
            var barrier = BarrierDesc.Global(BarrierSync.All, BarrierSync.All, BarrierAccess.Common, BarrierAccess.Common);
            commandBuffer.Barrier(barrier);

            Logger.Error(ex);
            return Error.InternalError;
        }
    }

    private unsafe Error ExecuteBarrierBatch(
        ICommandBuffer cmd,
        int barrierStart,
        int barrierCount,
        IReadOnlyList<CompiledBarrier> compiledBarriers)
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
            var compiledBarrier = compiledBarriers[barrierStart + i];
            var resourceHandle = _resources.GetResource(compiledBarrier.resource).backingResource;
            var target = compiledBarrier.targetState;

            BarrierDesc desc;
            if (compiledBarrier.resourceType == RGResourceType.Texture)
            {
                desc = BarrierDesc.Texture(
                            resourceHandle.AsTexture(),
                            target.sync,
                            target.access,
                            target.layout,
                            discard: compiledBarrier.flags.HasFlag(BarrierFlags.Discard));
            }
            else
            {
                desc = BarrierDesc.Buffer(resourceHandle.AsBuffer(), target.sync, target.access);
            }

            if (compiledBarrier.aliasingPredecessor.IsValid)
            {
                desc.IsAliasing = true;
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
