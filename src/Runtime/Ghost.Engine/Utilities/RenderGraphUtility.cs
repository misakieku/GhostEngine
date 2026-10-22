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
    private struct ClearRenderTargetPassData
    {
        public Identifier<RGTexture> target;
        public Color128 clearColor;
    }

    private struct DiscardRenderTargetPassData
    {
        public Identifier<RGTexture> target;
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
        public bool gammaCorrection;
    }

    /// <summary>
    /// Adds an explicit pass to blit a texture to another texture using a specified blit shader.
    /// </summary>
    public static void AddBlitPass(this RenderGraph rg, Identifier<RGTexture> src, Identifier<RGTexture> dst, Handle<Shader> blitShader, bool gammaCorrection = false)
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
            blitShader = blitShader,
            gammaCorrection = gammaCorrection
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
                sampler_mainTex = renderCtx.ResourceManager.StaticSampler.GetSampler(StaticSamplerType.LinearClamp),
                linearToSRGB = passData.gammaCorrection ? 1u : 0u
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
        builder.UseRenderTargetTexture(target, AccessFlags.WriteAll);

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

    public static void AddDiscardRenderTargetPass(this RenderGraph rg, Identifier<RGTexture> target, string passName = "DiscardRenderTarget")
    {
        using var builder = rg.AddUnsafeRenderPass<DiscardRenderTargetPassData>(passName);
        builder.UseRenderTargetTexture(target, AccessFlags.WriteAll);
        builder.SetPassData(new DiscardRenderTargetPassData
        {
            target = target
        });

        builder.SetRenderFunc<DiscardRenderTargetPassData>(static (ref readonly passData, unsafeCtx) =>
        {
            var actualTarget = unsafeCtx.GetActualTexture(passData.target);
            unsafeCtx.GetCommandBufferUnsafe().DiscardResource(actualTarget.AsResource(), null);
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
