using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Core.Utilities;
using Ghost.Engine.ShaderProperties;
using Ghost.Graphics.Core;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RHI;
using System.Runtime.CompilerServices;

namespace Ghost.Engine.Utilities;

/// <summary>
/// Utility extension methods for creating common explicit passes in RenderGraph
/// (e.g. buffer clearing, buffer copying, texture copying, and manual clears).
/// Matches modern rendering architectures such as Unreal Engine's RenderGraphUtils.
/// </summary>
public static class RenderGraphUtility
{
    private struct ClearBufferPassData
    {
        public Identifier<RGBuffer> targetBuffer;
        public Identifier<RGBuffer> uploadBuffer;
        public ulong sizeInBytes;
        public uint clearValue;
    }

    private struct CopyBufferPassData
    {
        public Identifier<RGBuffer> dstBuffer;
        public Identifier<RGBuffer> srcBuffer;
        public ulong dstOffset;
        public ulong srcOffset;
        public ulong numBytes;
    }

    private struct CopyTexturePassData
    {
        public Identifier<RGTexture> dstTexture;
        public Identifier<RGTexture> srcTexture;
    }

    private struct ClearRenderTargetPassData
    {
        public Identifier<RGTexture> target;
        public Color128 clearColor;
    }

    private struct ClearDepthStencilPassData
    {
        public Identifier<RGTexture> target;
        public float clearDepth;
        public byte clearStencil;
        public bool clearDepthFlag;
        public bool clearStencilFlag;
    }

    private struct BlitPassData
    {
        public Identifier<RGTexture> srcBuffer;
        public Handle<Shader> blitShader;
    }

    /// <summary>
    /// Adds an explicit pass to zero-fill or clear a buffer using an upload staging buffer and DMA copy.
    /// If sizeInBytes is 0, the full buffer capacity is cleared.
    /// </summary>
    public static unsafe void AddClearBufferPass(this RenderGraph rg, Identifier<RGBuffer> targetBuffer, ulong sizeInBytes = 0, uint clearValue = 0, string passName = "ClearBuffer")
    {
        if (sizeInBytes == 0)
        {
            sizeInBytes = rg.GetBufferDesc(targetBuffer).Size;
        }

        using var builder = rg.AddUnsafeRenderPass<ClearBufferPassData>(passName);

        var uploadDesc = new BufferDesc
        {
            Size = sizeInBytes,
            Stride = 4,
            Usage = BufferUsage.Raw | BufferUsage.ShaderResource,
            HeapType = HeapType.Upload
        };

        var uploadBuffer = builder.CreateBuffer(in uploadDesc, StringUtility.DebugFormat("{0}_Upload", passName));

        builder.UseBuffer(uploadBuffer, AccessFlags.Read);
        builder.UseRandomAccessBuffer(targetBuffer);

        builder.SetPassData(new ClearBufferPassData
        {
            targetBuffer = targetBuffer,
            uploadBuffer = uploadBuffer,
            sizeInBytes = sizeInBytes,
            clearValue = clearValue
        });

        builder.SetRenderFunc<ClearBufferPassData>(static (ref readonly passData, unsafeCtx) =>
        {
            var pMapped = (uint*)unsafeCtx.MapBuffer(passData.uploadBuffer);
            if (pMapped != null)
            {
                if (passData.clearValue == 0)
                {
                    Unsafe.InitBlockUnaligned(pMapped, 0, (uint)passData.sizeInBytes);
                }
                else
                {
                    var count = (int)(passData.sizeInBytes / 4);
                    for (var i = 0; i < count; i++)
                    {
                        pMapped[i] = passData.clearValue;
                    }
                }
                unsafeCtx.UnmapBuffer(passData.uploadBuffer);
            }

            var actualDst = unsafeCtx.GetActualBuffer(passData.targetBuffer);
            var actualSrc = unsafeCtx.GetActualBuffer(passData.uploadBuffer);
            unsafeCtx.GetCommandBufferUnsafe().CopyBuffer(actualDst, actualSrc, 0, 0, passData.sizeInBytes);
        });
    }

    /// <summary>
    /// Adds an explicit pass to copy data from one buffer to another using DMA copy.
    /// If numBytes is 0, the remaining size of the source or destination buffer is used.
    /// </summary>
    public static void AddCopyBufferPass(this RenderGraph rg, Identifier<RGBuffer> dstBuffer, Identifier<RGBuffer> srcBuffer, ulong numBytes = 0, ulong dstOffset = 0, ulong srcOffset = 0, string passName = "CopyBuffer")
    {
        if (numBytes == 0)
        {
            var dstSize = rg.GetBufferDesc(dstBuffer).Size;
            var srcSize = rg.GetBufferDesc(srcBuffer).Size;
            numBytes = Math.Min(dstSize - dstOffset, srcSize - srcOffset);
        }

        using var builder = rg.AddUnsafeRenderPass<CopyBufferPassData>(passName);
        builder.UseBuffer(srcBuffer, AccessFlags.Read);
        builder.UseBuffer(dstBuffer, AccessFlags.Write);

        builder.SetPassData(new CopyBufferPassData
        {
            dstBuffer = dstBuffer,
            srcBuffer = srcBuffer,
            dstOffset = dstOffset,
            srcOffset = srcOffset,
            numBytes = numBytes
        });

        builder.SetRenderFunc<CopyBufferPassData>(static (ref readonly passData, unsafeCtx) =>
        {
            var actualDst = unsafeCtx.GetActualBuffer(passData.dstBuffer);
            var actualSrc = unsafeCtx.GetActualBuffer(passData.srcBuffer);
            unsafeCtx.GetCommandBufferUnsafe().CopyBuffer(actualDst, actualSrc, passData.dstOffset, passData.srcOffset, passData.numBytes);
        });
    }

    /// <summary>
    /// Adds an explicit pass to copy the full region of one texture to another.
    /// </summary>
    public static void AddCopyTexturePass(this RenderGraph rg, Identifier<RGTexture> dstTexture, Identifier<RGTexture> srcTexture, string passName = "CopyTexture")
    {
        using var builder = rg.AddUnsafeRenderPass<CopyTexturePassData>(passName);
        builder.UseTexture(srcTexture, AccessFlags.Read);
        builder.UseTexture(dstTexture, AccessFlags.Write);

        builder.SetPassData(new CopyTexturePassData
        {
            dstTexture = dstTexture,
            srcTexture = srcTexture
        });

        builder.SetRenderFunc<CopyTexturePassData>(static (ref readonly passData, unsafeCtx) =>
        {
            var actualDst = unsafeCtx.GetActualTexture(passData.dstTexture);
            var actualSrc = unsafeCtx.GetActualTexture(passData.srcTexture);
            unsafeCtx.GetCommandBufferUnsafe().CopyTexture(actualDst, null, actualSrc, null);
        });
    }

    /// <summary>
    /// Adds an explicit pass to blit a texture to another texture using a specified blit shader.
    /// </summary>
    public static void AddBlitPass(this RenderGraph rg, Identifier<RGTexture> src, Identifier<RGTexture> dst, Handle<Shader> blitShader)
    {
        if (blitShader.IsInvalid)
        {
            return;
        }

        using var builder = rg.AddRasterRenderPass<BlitPassData>("BlitPass");
        builder.SetColorAttachment(dst, 0, AccessFlags.WriteAll);
        builder.UseTexture(src, AccessFlags.Read);

        builder.SetPassData(new BlitPassData
        {
            srcBuffer = src,
            blitShader = blitShader
        });

        builder.SetRenderFunc<BlitPassData>(static (ref readonly passData, renderCtx) =>
        {
            if (!renderCtx.TrySetActiveShaderPass(passData.blitShader, PassSemantic.Forward))
            {
                return;
            }

            var property = new HiddenBlitShaderProperties
            {
                mainTex = renderCtx.GetActualBindlessIndex(passData.srcBuffer),
                sampler_mainTex = (uint)renderCtx.ResourceManager.StaticSampler.LinearClamp.Value,
            };

            renderCtx.SetUserDataWithProperties(property, target: DataTarget.Graphics);
            renderCtx.DispatchMesh(1, 1, 1);
        });
    }

    /// <summary>
    /// Adds an explicit pass to clear a render target view with a specified color.
    /// </summary>
    public static void AddClearRenderTargetPass(this RenderGraph rg, Identifier<RGTexture> target, Color128 clearColor, string passName = "ClearRenderTarget")
    {
        using var builder = rg.AddUnsafeRenderPass<ClearRenderTargetPassData>(passName);
        builder.UseTexture(target, AccessFlags.WriteAll);

        builder.SetPassData(new ClearRenderTargetPassData
        {
            target = target,
            clearColor = clearColor
        });

        builder.SetRenderFunc<ClearRenderTargetPassData>(static (ref readonly passData, unsafeCtx) =>
        {
            var actualTarget = unsafeCtx.GetActualTexture(passData.target);
            unsafeCtx.GetCommandBufferUnsafe().ClearRenderTargetView(actualTarget, passData.clearColor);
        });
    }

    /// <summary>
    /// Adds an explicit pass to clear a depth-stencil view with specified depth and stencil values.
    /// </summary>
    public static void AddClearDepthStencilPass(this RenderGraph rg, Identifier<RGTexture> target, float clearDepth = 0.0f, byte clearStencil = 0, bool clearDepthFlag = true, bool clearStencilFlag = false, string passName = "ClearDepthStencil")
    {
        using var builder = rg.AddUnsafeRenderPass<ClearDepthStencilPassData>(passName);
        builder.UseTexture(target, AccessFlags.WriteAll);

        builder.SetPassData(new ClearDepthStencilPassData
        {
            target = target,
            clearDepth = clearDepth,
            clearStencil = clearStencil,
            clearDepthFlag = clearDepthFlag,
            clearStencilFlag = clearStencilFlag
        });

        builder.SetRenderFunc<ClearDepthStencilPassData>(static (ref readonly passData, unsafeCtx) =>
        {
            var actualTarget = unsafeCtx.GetActualTexture(passData.target);
            unsafeCtx.GetCommandBufferUnsafe().ClearDepthStencilView(
                actualTarget,
                passData.clearDepthFlag,
                passData.clearStencilFlag,
                passData.clearDepth,
                passData.clearStencil);
        });
    }
}
