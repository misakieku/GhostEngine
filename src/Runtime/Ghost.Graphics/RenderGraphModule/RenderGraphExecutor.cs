using Ghost.Core;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using System.Diagnostics;

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
        // This should not happened since the compiler should have rejected any render pass with an invalid render target configuration, but just in case, we use Debug.Assert to validate our assumptions.
        Debug.Assert(color.Length > 0 || depthStencil.texture.IsValid);

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
        List<RenderGraphPassBase> compiledPasses,
        List<NativeRenderPass> nativePasses,
        List<CompiledBarrier> compiledBarriers)
    {
        var barrierIndex = 0;
        var nativePassIndex = 0;
        var logicalPassIndex = 0;

        _context.BeginNewFrame(commandBuffer);

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
                    var e = ExecuteBarriersForPass(commandBuffer, mergedPassIdx, ref barrierIndex, compiledBarriers);
                    if (e != Error.None)
                    {
                        return e;
                    }
                }

                // Begin native render pass

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

                commandBuffer.EndRenderPass();
                nativePassIndex++;
            }
            else
            {
                // All the reaster pass should be merged into native render pass, so if we encounter a raster pass here, it means something went wrong during compilation.
                Debug.Assert(pass.type != RenderPassType.Raster);

                // Compute pass or Unsafe pass
                var e = ExecuteBarriersForPass(commandBuffer, logicalPassIndex, ref barrierIndex, compiledBarriers);
                if (e != Error.None)
                {
                    return e;
                }

                pass.Execute(_context);
                logicalPassIndex++;
            }
        }

        return Error.None;
    }

    private unsafe Error ExecuteBarriersForPass(
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
                cmd.Barrier(new ReadOnlySpan<BarrierDesc>(barriers, barrierCount));
                barrierCount = 0;
            }
        }

        // Process all pre-compiled barriers for this pass
        // TODO: We can insert BarrierAccess.NoAccess to the resource that aliased with others after their last usage to reduce cache burden.
        while (barrierIndex < compiledBarriers.Count && compiledBarriers[barrierIndex].passIndex == passIndex)
        {
            var compiledBarrier = compiledBarriers[barrierIndex++];
            var resource = _resources.GetResource(compiledBarrier.resource);
            var resourceHandle = resource.backingResource;

            // Always query the before state from ResourceManager (single source of truth)
            var currentStateResult = _resourceDatabase.GetResourceBarrierData(resourceHandle);
            if (currentStateResult.IsFailure)
            {
                return currentStateResult.Error;
            }

            var currentState = currentStateResult.Value;
            var target = compiledBarrier.targetState;

            // Create barrier descriptor
            BarrierDesc desc;
            if (compiledBarrier.resourceType == RenderGraphResourceType.Texture)
            {
                desc = BarrierDesc.Texture(resourceHandle, target.sync, target.access, target.layout,
                    discard: compiledBarrier.flags.HasFlag(BarrierFlags.Discard));
            }
            else
            {
                desc = BarrierDesc.Buffer(resourceHandle, target.sync, target.access);
            }

            if (compiledBarrier.aliasingPredecessor.IsValid)
            {
                desc.IsAliasing = true;
            }

            if (barrierCount >= MaxBatch)
            {
                Flush();
            }

            barriers[barrierCount++] = desc;
        }

        Flush();

        return Error.None;
    }
}
