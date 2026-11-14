using Ghost.Core;
using Ghost.Engine.ShaderProperties;
using Ghost.Engine.Volumes;
using Ghost.Graphics;
using Ghost.Graphics.Core;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Engine.RenderPipeline;

internal partial class GhostRenderPipeline
{
    private const uint LUMINANCE_HISTOGRAM_BINS = 128u;
    private const uint LUMINANCE_HISTOGRAM_SIZE_BYTES = LUMINANCE_HISTOGRAM_BINS * 4u;

    private struct PrepareExposurePassData
    {
        public Handle<ComputeShader> shader;
        public Identifier<RGBuffer> persistentViewStateBuffer;
        public uint viewId;
        public ExposureMode mode;
        public uint historyValid;
        public float fixedEV100;
        public float initialEV100;
    }

    private struct ClearLuminanceHistogramPassData
    {
        public Handle<ComputeShader> shader;
        public Identifier<RGBuffer> histogramBuffer;
    }

    private struct BuildLuminanceHistogramPassData
    {
        public Handle<ComputeShader> shader;
        public Identifier<RGTexture> hdrColor;
        public Identifier<RGBuffer> histogramBuffer;
        public Identifier<RGBuffer> persistentViewStateBuffer;
        public uint viewId;
        public uint2 viewSize;
        public MeteringMode meteringMode;
        public float histogramMinEV;
        public float histogramMaxEV;
    }

    private struct AutoExposureAdaptPassData
    {
        public Handle<ComputeShader> shader;
        public Identifier<RGBuffer> histogramBuffer;
        public Identifier<RGBuffer> persistentViewStateBuffer;
        public uint viewId;
        public uint historyValid;
        public float deltaTime;
        public float minEV100;
        public float maxEV100;
        public float compensation;
        public float speedUp;
        public float speedDown;
        public float lowPercent;
        public float highPercent;
        public float histogramMinEV;
        public float histogramMaxEV;
    }

    private struct UberPostPassData
    {
        public Handle<ComputeShader> shader;
        public uint2 viewSize;

        public Identifier<RGTexture> src;
        public Identifier<RGTexture> dst;
        public Identifier<RGBuffer> persistentViewStateBuffer;
        public uint viewId;

        public TonemapMode tonemapMode;
        public bool agxPunchy;
    }

    private ExposureMode AddPrepareExposurePass(RenderGraph renderGraph, GPUViewContext viewContext, in RenderRequest request, GhostRenderPayload payload, Identifier<RGBuffer> persistentViewStateBuffer)
    {
        ref readonly var exposureSettings = ref payload.GetViewVolumeData<ExposureSettings>(request.viewId);
        var defaults = ExposureSettings.Default;

        var mode = exposureSettings.mode.GetValueOrDefault(defaults.mode!.Value);
        var compensation = exposureSettings.compensation.GetValueOrDefault(defaults.compensation!.Value);
        var minEV100 = exposureSettings.minEV100.GetValueOrDefault(defaults.minEV100!.Value);
        var maxEV100 = math.max(minEV100, exposureSettings.maxEV100.GetValueOrDefault(defaults.maxEV100!.Value));
        var fixedExposure = exposureSettings.fixedExposure.GetValueOrDefault(defaults.fixedExposure!.Value);

        float fixedEV100;
        if (mode == ExposureMode.UsePhysicalCamera)
        {
            var aperture = math.max(request.view.aperture, 0.01f);
            var shutter = math.max(request.view.shutterSpeed, 1e-6f);
            var iso = math.max(request.view.iso, 1.0f);
            var cameraEV100 = math.log2((aperture * aperture) / shutter * (100.0f / iso));
            fixedEV100 = cameraEV100 - compensation;
        }
        else
        {
            fixedEV100 = fixedExposure - compensation;
        }

        var initialEV100 = math.clamp((minEV100 + maxEV100) * 0.5f, minEV100, maxEV100);

        using var builder = renderGraph.AddComputeRenderPass<PrepareExposurePassData>("PrepareExposure");
        builder.AllowPassCulling(false);
        builder.UseBuffer(persistentViewStateBuffer, AccessFlags.ReadWrite);

        builder.SetPassData(new PrepareExposurePassData
        {
            shader = _postProcessPipelineResource.prepareExposureShader,
            persistentViewStateBuffer = persistentViewStateBuffer,
            viewId = request.viewId,
            mode = mode,
            historyValid = viewContext.isExposureInitialized ? 1u : 0u,
            fixedEV100 = fixedEV100,
            initialEV100 = initialEV100,
        });

        builder.SetRenderFunc<PrepareExposurePassData>(static (ref readonly passData, ctx) =>
        {
            if (!passData.shader.IsValid)
            {
                return;
            }

            ctx.SetActiveCompute(passData.shader, 0);
            ctx.SetPassProperties(new PostProcessPrepareExposureShaderProperties
            {
                persistentViewStateUav = ctx.GetActualBindlessIndex(passData.persistentViewStateBuffer, BindlessAccess.UnorderedAccess),
                viewId = passData.viewId,
                mode = (uint)passData.mode,
                historyValid = passData.historyValid,
                fixedEV100 = passData.fixedEV100,
                initialEV100 = passData.initialEV100,
            });

            ctx.DispatchCompute(1, 1, 1);
        });

        return mode;
    }

    private void AddAutoExposurePasses(RenderGraph renderGraph, GhostRenderPayload payload, Identifier<RGBuffer> persistentViewStateBuffer, Identifier<RGTexture> hdrColor, uint viewId, uint2 viewSize, bool historyValid)
    {
        ref readonly var exposureSettings = ref payload.GetViewVolumeData<ExposureSettings>(viewId);
        var defaults = ExposureSettings.Default;

        var meteringMode = exposureSettings.meteringMode.GetValueOrDefault(defaults.meteringMode!.Value);
        var compensation = exposureSettings.compensation.GetValueOrDefault(defaults.compensation!.Value);
        var minEV100 = exposureSettings.minEV100.GetValueOrDefault(defaults.minEV100!.Value);
        var maxEV100 = math.max(minEV100, exposureSettings.maxEV100.GetValueOrDefault(defaults.maxEV100!.Value));
        var speedUp = exposureSettings.speedUp.GetValueOrDefault(defaults.speedUp!.Value);
        var speedDown = exposureSettings.speedDown.GetValueOrDefault(defaults.speedDown!.Value);
        var lowPercent = exposureSettings.lowPercent.GetValueOrDefault(defaults.lowPercent!.Value);
        var highPercent = exposureSettings.highPercent.GetValueOrDefault(defaults.highPercent!.Value);
        var histogramMinEV = exposureSettings.histogramMinEV.GetValueOrDefault(defaults.histogramMinEV!.Value);
        var histogramMaxEV = math.max(histogramMinEV + 0.01f, exposureSettings.histogramMaxEV.GetValueOrDefault(defaults.histogramMaxEV!.Value));

        Identifier<RGBuffer> histogramBuffer;
        using (var builder = renderGraph.AddComputeRenderPass<ClearLuminanceHistogramPassData>("ClearLuminanceHistogram"))
        {
            var histDesc = new BufferDesc
            {
                Size = LUMINANCE_HISTOGRAM_SIZE_BYTES,
                Stride = 4,
                Usage = BufferUsage.Raw | BufferUsage.ShaderResource | BufferUsage.UnorderedAccess,
                HeapType = HeapType.Default
            };
            histogramBuffer = builder.CreateBuffer(in histDesc, "LuminanceHistogram");
            builder.UseBuffer(histogramBuffer, AccessFlags.Write);

            builder.SetPassData(new ClearLuminanceHistogramPassData
            {
                shader = _postProcessPipelineResource.buildLuminanceHistogramShader,
                histogramBuffer = histogramBuffer,
            });

            builder.SetRenderFunc<ClearLuminanceHistogramPassData>(static (ref readonly passData, ctx) =>
            {
                if (!passData.shader.IsValid)
                {
                    return;
                }

                ctx.SetActiveCompute(passData.shader, 1);
                ctx.SetPassProperties(new PostProcessBuildLuminanceHistogramShaderProperties
                {
                    histogramUav = ctx.GetActualBindlessIndex(passData.histogramBuffer, BindlessAccess.UnorderedAccess),
                });

                ctx.DispatchCompute(1, 1, 1);
            });
        }

        using (var builder = renderGraph.AddComputeRenderPass<BuildLuminanceHistogramPassData>("BuildLuminanceHistogram"))
        {
            builder.UseTexture(hdrColor, AccessFlags.Read);
            builder.UseBuffer(persistentViewStateBuffer, AccessFlags.Read);
            builder.UseBuffer(histogramBuffer, AccessFlags.ReadWrite);

            builder.SetPassData(new BuildLuminanceHistogramPassData
            {
                shader = _postProcessPipelineResource.buildLuminanceHistogramShader,
                hdrColor = hdrColor,
                histogramBuffer = histogramBuffer,
                persistentViewStateBuffer = persistentViewStateBuffer,
                viewId = viewId,
                viewSize = viewSize,
                meteringMode = meteringMode,
                histogramMinEV = histogramMinEV,
                histogramMaxEV = histogramMaxEV,
            });

            builder.SetRenderFunc<BuildLuminanceHistogramPassData>(static (ref readonly passData, ctx) =>
            {
                if (!passData.shader.IsValid)
                {
                    return;
                }

                ctx.SetActiveCompute(passData.shader, 0);
                ctx.SetPassProperties(new PostProcessBuildLuminanceHistogramShaderProperties
                {
                    hdrColorSrv = ctx.GetActualBindlessIndex(passData.hdrColor, BindlessAccess.ShaderResource),
                    histogramUav = ctx.GetActualBindlessIndex(passData.histogramBuffer, BindlessAccess.UnorderedAccess),
                    persistentViewStateBufferSrv = ctx.GetActualBindlessIndex(passData.persistentViewStateBuffer, BindlessAccess.ShaderResource),
                    viewId = passData.viewId,
                    renderWidth = passData.viewSize.x,
                    renderHeight = passData.viewSize.y,
                    meteringMode = (uint)passData.meteringMode,
                    histogramMinEV = passData.histogramMinEV,
                    histogramMaxEV = passData.histogramMaxEV,
                });

                ctx.DispatchCompute((passData.viewSize.x + 15u) / 16u, (passData.viewSize.y + 15u) / 16u, 1);
            });
        }

        using (var builder = renderGraph.AddComputeRenderPass<AutoExposureAdaptPassData>("AutoExposureAdapt"))
        {
            builder.AllowPassCulling(false);
            builder.UseBuffer(histogramBuffer, AccessFlags.Read);
            builder.UseBuffer(persistentViewStateBuffer, AccessFlags.ReadWrite);

            builder.SetPassData(new AutoExposureAdaptPassData
            {
                shader = _postProcessPipelineResource.autoExposureAdaptShader,
                histogramBuffer = histogramBuffer,
                persistentViewStateBuffer = persistentViewStateBuffer,
                viewId = viewId,
                historyValid = historyValid ? 1u : 0u,
                deltaTime = payload.DeltaTime,
                minEV100 = minEV100,
                maxEV100 = maxEV100,
                compensation = compensation,
                speedUp = speedUp,
                speedDown = speedDown,
                lowPercent = lowPercent,
                highPercent = highPercent,
                histogramMinEV = histogramMinEV,
                histogramMaxEV = histogramMaxEV,
            });

            builder.SetRenderFunc<AutoExposureAdaptPassData>(static (ref readonly passData, ctx) =>
            {
                if (!passData.shader.IsValid)
                {
                    return;
                }

                ctx.SetActiveCompute(passData.shader, 0);
                ctx.SetPassProperties(new PostProcessAutoExposureAdaptShaderProperties
                {
                    histogramSrv = ctx.GetActualBindlessIndex(passData.histogramBuffer, BindlessAccess.ShaderResource),
                    persistentViewStateUav = ctx.GetActualBindlessIndex(passData.persistentViewStateBuffer, BindlessAccess.UnorderedAccess),
                    viewId = passData.viewId,
                    historyValid = passData.historyValid,
                    deltaTime = passData.deltaTime,
                    minEV100 = passData.minEV100,
                    maxEV100 = passData.maxEV100,
                    compensation = passData.compensation,
                    speedUp = passData.speedUp,
                    speedDown = passData.speedDown,
                    lowPercent = passData.lowPercent,
                    highPercent = passData.highPercent,
                    histogramMinEV = passData.histogramMinEV,
                    histogramMaxEV = passData.histogramMaxEV,
                });

                ctx.DispatchCompute(1, 1, 1);
            });
        }
    }

    private Identifier<RGTexture> AddPostProcessPass(RenderGraph renderGraph, GhostRenderPayload payload, uint2 viewSize, uint viewId, Identifier<RGTexture> color, Identifier<RGTexture> depth, Identifier<RGBuffer> persistentViewStateBuffer)
    {
        ref readonly var tonemapSettings = ref payload.GetViewVolumeData<TonemapSettings>(viewId);

        Identifier<RGTexture> intermediate;
        using (var builder = renderGraph.AddComputeRenderPass<UberPostPassData>("Uber Post"))
        {
            intermediate = builder.CreateTexture(RGTextureDesc.Relative(1.0f, TextureFormat.R11G11B10_Float, usage: TextureUsage.ShaderResource | TextureUsage.UnorderedAccess));

            builder.UseTexture(color, AccessFlags.Read);
            builder.UseBuffer(persistentViewStateBuffer, AccessFlags.Read);
            builder.UseTexture(intermediate, AccessFlags.WriteAll);

            builder.SetPassData(new UberPostPassData
            {
                shader = _postProcessPipelineResource.uberPostShader,
                viewSize = viewSize,

                src = color,
                dst = intermediate,
                persistentViewStateBuffer = persistentViewStateBuffer,
                viewId = viewId,
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
                    persistentViewStateBufferSrv = ctx.GetActualBindlessIndex(passData.persistentViewStateBuffer, BindlessAccess.ShaderResource),
                    viewId = passData.viewId,
                    tonemapMode = (uint)passData.tonemapMode,
                    agxPunchy = passData.agxPunchy ? 1u : 0u,
                });

                ctx.DispatchCompute((passData.viewSize.x + 7) / 8, (passData.viewSize.y + 7) / 8, 1);
            });
        }

        return intermediate;
    }
}
