using Ghost.Core;
using Ghost.Engine.ShaderProperties;
using Ghost.Engine.Volumes;
using Ghost.Graphics.Core;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Engine.RenderPipeline;

internal partial class GhostRenderPipeline
{
    private struct UberPostPassData
    {
        public Handle<ComputeShader> shader;
        public uint2 viewSize;

        public Identifier<RGTexture> src;
        public Identifier<RGTexture> dst;

        public TonemapMode tonemapMode;
        public bool agxPunchy;
    }

    private Identifier<RGTexture> AddPostProcessPass(RenderGraph renderGraph, GhostRenderPayload payload, uint2 viewSize, uint viewId, Identifier<RGTexture> color, Identifier<RGTexture> depth)
    {
        ref readonly var tonemapSettings = ref payload.GetViewVolumeData<TonemapSettings>(viewId);

        Identifier<RGTexture> intermediate;
        using (var builder = renderGraph.AddComputeRenderPass<UberPostPassData>("Uber Post"))
        {
            intermediate = builder.CreateTexture(RGTextureDesc.Relative(1.0f, TextureFormat.R11G11B10_Float, usage: TextureUsage.ShaderResource | TextureUsage.UnorderedAccess));

            builder.UseTexture(color, AccessFlags.Read);
            builder.UseTexture(intermediate, AccessFlags.WriteAll);

            builder.SetPassData(new UberPostPassData
            {
                shader = _postProcessPipelineResource.uberPostShader,
                viewSize = viewSize,

                src = color,
                dst = intermediate,
                tonemapMode = tonemapSettings.mode.GetValueOrDefault(),
                agxPunchy = tonemapSettings.punchy.GetValueOrDefault(),
            });

            builder.SetRenderFunc<UberPostPassData>(static (ref readonly passData, ctx) =>
            {
                ctx.SetActiveCompute(passData.shader, 0);
                ctx.SetPassProperties(new PostProcessUberPostShaderProperties
                {
                    src = ctx.GetActualBindlessIndex(passData.src, BindlessAccess.ShaderResource),
                    dst = ctx.GetActualBindlessIndex(passData.dst, BindlessAccess.UnorderedAccess),
                    tonemapMode = (uint)passData.tonemapMode,
                    agxPunchy = passData.agxPunchy ? 1u : 0u,
                });

                ctx.DispatchCompute((passData.viewSize.x + 7) / 8, (passData.viewSize.y + 7) / 8, 1);
            });
        }

        return intermediate;
    }
}