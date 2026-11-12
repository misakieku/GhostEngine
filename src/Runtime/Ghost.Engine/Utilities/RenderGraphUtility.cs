using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Engine.ShaderProperties;
using Ghost.Graphics.Core;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RHI;

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
        public bool includeDepth;
        public bool includeStencil;
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
    /// <param name="rg">The render graph to add the pass to.</param>
    /// <param name="src">The source texture.</param>
    /// <param name="dst">The destination texture.</param>
    /// <param name="blitShader">The blit shader to use.</param>
    /// <param name="gammaCorrection">Whether to apply gamma correction.</param>
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
    /// <param name="rg">The render graph to add the pass to.</param>
    /// <param name="target">The render target view to clear.</param>
    /// <param name="clearColor">The color to clear the render target view with.</param>
    /// <param name="passName">The name of the pass.</param>
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

    /// <summary>
    /// Adds an explicit pass to discard a render target view, indicating that its contents are no longer needed.
    /// </summary>
    /// <param name="rg">The render graph to add the pass to.</param>
    /// <param name="target">The render target view to discard.</param>
    /// <param name="passName">The name of the pass.</param>
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
    /// <param name="rg">The render graph to add the pass to.</param>
    /// <param name="target">The depth-stencil view to clear.</param>
    /// <param name="clearDepth">The depth value to clear with.</param>
    /// <param name="clearStencil">The stencil value to clear with.</param>
    /// <param name="includeDepth">Whether to clear the depth component.</param>
    /// <param name="includeStencil">Whether to clear the stencil component.</param>
    /// <param name="passName">The name of the pass.</param>
    public static void AddClearDepthStencilPass(this RenderGraph rg, Identifier<RGTexture> target, float clearDepth = 0.0f, byte clearStencil = 0, bool includeDepth = true, bool includeStencil = false, string passName = "ClearDepthStencil")
    {
        using var builder = rg.AddUnsafeRenderPass<ClearDepthStencilPassData>(passName);
        builder.UseDepthTargetTexture(target, AccessFlags.WriteAll);

        builder.SetPassData(new ClearDepthStencilPassData
        {
            target = target,
            clearDepth = clearDepth,
            clearStencil = clearStencil,
            includeDepth = includeDepth,
            includeStencil = includeStencil
        });

        builder.SetRenderFunc<ClearDepthStencilPassData>(static (ref readonly passData, unsafeCtx) =>
        {
            var actualTarget = unsafeCtx.GetActualTexture(passData.target);
            unsafeCtx.GetCommandBufferUnsafe().ClearDepthStencilView(
                actualTarget,
                passData.includeDepth,
                passData.includeStencil,
                passData.clearDepth,
                passData.clearStencil);
        });
    }
}
