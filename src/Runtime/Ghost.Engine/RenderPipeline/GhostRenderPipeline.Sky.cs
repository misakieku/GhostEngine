using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.Engine.ShaderProperties;
using Ghost.Engine.Volumes;
using Ghost.Graphics.Core;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Engine.RenderPipeline;

internal readonly struct SkyContext
{
    public readonly SkyType skyType;
    public readonly Identifier<RGTexture> skyboxTexture;
    public readonly Identifier<RGTexture> iblLutTexture;
    public readonly Identifier<RGBuffer> persistentViewStateBuffer;
    public readonly uint skyboxMipCount;
    public readonly float4 skyTintAndIntensity;
    public readonly float2 skyRotationSinCos;
    public readonly bool hasSky;

    public SkyContext(
        SkyType skyType,
        Identifier<RGTexture> skyboxTexture,
        Identifier<RGTexture> iblLutTexture,
        Identifier<RGBuffer> persistentViewStateBuffer,
        uint skyboxMipCount,
        float4 skyTintAndIntensity,
        float2 skyRotationSinCos,
        bool hasSky)
    {
        this.skyType = skyType;
        this.skyboxTexture = skyboxTexture;
        this.iblLutTexture = iblLutTexture;
        this.persistentViewStateBuffer = persistentViewStateBuffer;
        this.skyboxMipCount = skyboxMipCount;
        this.skyTintAndIntensity = skyTintAndIntensity;
        this.skyRotationSinCos = skyRotationSinCos;
        this.hasSky = hasSky;
    }

    public static SkyContext None => new(
        SkyType.None,
        Identifier<RGTexture>.Invalid,
        Identifier<RGTexture>.Invalid,
        Identifier<RGBuffer>.Invalid,
        0,
        float4.zero,
        new float2(0.0f, 1.0f),
        false);
}

internal unsafe partial class GhostRenderPipeline
{
    private struct ConvolveSkyIrradiancePassData
    {
        public Identifier<RGTexture> skyboxTexture;
        public Identifier<RGBuffer> persistentViewStateBuffer;
        public Handle<ComputeShader> convolveSkyIrradianceShader;
        public uint viewId;
        public uint skyboxMipCount;
        public uint skyboxWidth;
        public float4 skyboxTintAndIntensity;
        public float2 skyboxRotationSinCos;
    }

    private struct HDRISkyPassData
    {
        public Identifier<RGTexture> litColorTarget;
        public Identifier<RGTexture> depthTexture;
        public Identifier<RGTexture> skyboxTexture;
        public Handle<Shader> hdriSkyShader;
        public float4 skyboxTintAndIntensity;
        public float2 skyboxRotationSinCos;
    }

    private SkyContext PrepareSky(RenderGraph renderGraph, GPUViewContext viewContext, in RenderRequest request, GhostRenderPayload ghostPayload, Identifier<RGBuffer> persistentViewStateBuffer)
    {
        ref readonly var environment = ref ghostPayload.GetViewVolumeData<EnvironmentSettings>(request.viewId);
        var skyType = environment.skyType.GetValueOrDefault(SkyType.HDRI);

        return skyType switch
        {
            SkyType.HDRI => PrepareHDRISky(renderGraph, viewContext, in request, ghostPayload, persistentViewStateBuffer),
            SkyType.None => PrepareNoneSky(viewContext),
            _ => SkyContext.None,
        };
    }

    private static SkyContext PrepareNoneSky(GPUViewContext viewContext)
    {
        viewContext.prevSkyHash = 0;
        return SkyContext.None;
    }

    private SkyContext PrepareHDRISky(RenderGraph renderGraph, GPUViewContext viewContext, in RenderRequest request, GhostRenderPayload ghostPayload, Identifier<RGBuffer> persistentViewStateBuffer)
    {
        ref readonly var hdri = ref ghostPayload.GetViewVolumeData<HDRISettings>(request.viewId);
        var skyTextureHandle = hdri.texture.GetValueOrDefault();
        var skyIntensity = hdri.intensity.GetValueOrDefault(1.0f);
        var skyRotation = hdri.rotation.GetValueOrDefault(0.0f);
        var skyTint = hdri.tint.GetValueOrDefault(float3.one);
        var skyTintAndIntensity = new float4(skyTint, skyIntensity);
        var skyRotationSinCos = new float2(math.sin(skyRotation), math.cos(skyRotation));

        // TODO: Currently render graph will generate BarrierSync.ComputeShading and PixelShading for these two textures.
        // But they already in all shading stages, so the render graph should skip the barrier.
        var iblLutTexture = _lightingPipelineResource.iblLutTexture.IsValid
            ? renderGraph.ImportTexture(_lightingPipelineResource.iblLutTexture, ResourceBarrierData.ShaderResource, ResourceBarrierData.ShaderResource)
            : Identifier<RGTexture>.Invalid;

        var skyboxTexture = skyTextureHandle.IsValid
            ? renderGraph.ImportTexture(skyTextureHandle, ResourceBarrierData.ShaderResource, ResourceBarrierData.ShaderResource)
            : Identifier<RGTexture>.Invalid;

        if (!skyboxTexture.IsValid)
        {
            viewContext.prevSkyHash = 0;
            return new SkyContext(
                SkyType.HDRI,
                Identifier<RGTexture>.Invalid,
                iblLutTexture,
                persistentViewStateBuffer,
                0,
                skyTintAndIntensity,
                skyRotationSinCos,
                false);
        }

        ref readonly var skyDesc = ref renderGraph.GetTextureDesc(skyboxTexture);
        var skyboxMipCount = skyDesc.mipLevels;

        var hTex = (uint)skyTextureHandle.ID | ((ulong)(uint)skyTextureHandle.Generation << 32);
        var hIntensity = (ulong)BitConverter.SingleToUInt32Bits(skyIntensity);
        var hRotation = (ulong)BitConverter.SingleToUInt32Bits(skyRotation);
        var hTint = BitConverter.SingleToUInt32Bits(skyTint.x) ^
                    ((ulong)BitConverter.SingleToUInt32Bits(skyTint.y) << 16) ^
                    ((ulong)BitConverter.SingleToUInt32Bits(skyTint.z) << 32);
        var skyHash = Hash.Combine64(hTex, hIntensity, hRotation, hTint);
        if (skyHash == 0)
        {
            skyHash = 1;
        }

        if (skyHash != viewContext.prevSkyHash)
        {
            AddConvolveSkyIrradiancePass(
                renderGraph,
                skyboxTexture,
                persistentViewStateBuffer,
                request.viewId,
                skyboxMipCount,
                skyDesc.width,
                skyTintAndIntensity,
                skyRotationSinCos);

            viewContext.prevSkyHash = skyHash;
        }

        return new SkyContext(
            SkyType.HDRI,
            skyboxTexture,
            iblLutTexture,
            persistentViewStateBuffer,
            skyboxMipCount,
            skyTintAndIntensity,
            skyRotationSinCos,
            true);
    }

    private void AddSkyPass(RenderGraph renderGraph, in SkyContext skyContext, Identifier<RGTexture> litColorTarget, Identifier<RGTexture> depthTexture)
    {
        switch (skyContext.skyType)
        {
            case SkyType.HDRI when skyContext.hasSky && skyContext.skyboxTexture.IsValid:
                AddHDRISkyPass(renderGraph, litColorTarget, depthTexture, skyContext.skyboxTexture, skyContext.skyTintAndIntensity, skyContext.skyRotationSinCos);
                break;
            case SkyType.None:
            default:
                AddClearSkyPass(renderGraph, litColorTarget, depthTexture);
                break;
        }
    }

    private void AddClearSkyPass(RenderGraph rg, Identifier<RGTexture> litColorTarget, Identifier<RGTexture> depthTexture)
    {
        AddHDRISkyPass(rg, litColorTarget, depthTexture, Identifier<RGTexture>.Invalid, float4.zero, new float2(0.0f, 1.0f));
    }

    private void AddConvolveSkyIrradiancePass(RenderGraph rg, Identifier<RGTexture> skyboxTexture, Identifier<RGBuffer> persistentViewStateBuffer, uint viewId, uint skyboxMipCount, uint skyboxWidth, float4 skyboxTintAndIntensity, float2 skyRotationSinCos)
    {
        using var builder = rg.AddComputeRenderPass<ConvolveSkyIrradiancePassData>("ConvolveSkyIrradianceSH");

        builder.UseTexture(skyboxTexture, AccessFlags.Read);
        builder.UseBuffer(persistentViewStateBuffer, AccessFlags.Write);

        builder.SetPassData(new ConvolveSkyIrradiancePassData
        {
            skyboxTexture = skyboxTexture,
            persistentViewStateBuffer = persistentViewStateBuffer,
            convolveSkyIrradianceShader = _lightingPipelineResource.convolveSkyIrradianceShader,
            viewId = viewId,
            skyboxMipCount = skyboxMipCount,
            skyboxWidth = skyboxWidth,
            skyboxTintAndIntensity = skyboxTintAndIntensity,
            skyboxRotationSinCos = skyRotationSinCos
        });

        builder.SetRenderFunc<ConvolveSkyIrradiancePassData>(static (ref readonly passData, computeCtx) =>
        {
            if (!passData.convolveSkyIrradianceShader.IsValid)
            {
                return;
            }

            var props = new InternalConvolveSkyIrradianceSHShaderProperties
            {
                skyboxTexture = computeCtx.GetActualBindlessIndex(passData.skyboxTexture, BindlessAccess.ShaderResource),
                skyboxSampler = computeCtx.ResourceManager.StaticSampler.GetSampler(StaticSamplerType.TrilinearClamp),
                persistentViewStateUav = computeCtx.GetActualBindlessIndex(passData.persistentViewStateBuffer, BindlessAccess.UnorderedAccess),
                viewId = passData.viewId,
                skyboxMipCount = passData.skyboxMipCount,
                skyboxWidth = passData.skyboxWidth,
                skyboxTintAndIntensity = passData.skyboxTintAndIntensity,
                skyboxRotationSinCos = passData.skyboxRotationSinCos
            };

            computeCtx.SetActiveCompute(passData.convolveSkyIrradianceShader, 0);
            computeCtx.SetUserDataWithProperties(in props);
            computeCtx.DispatchCompute(1, 1, 1);
        });
    }

    private void AddHDRISkyPass(RenderGraph rg, Identifier<RGTexture> litColorTarget, Identifier<RGTexture> depthTexture, Identifier<RGTexture> skyboxTexture, float4 skyboxTintAndIntensity, float2 skyRotationSinCos)
    {
        using var builder = rg.AddRasterRenderPass<HDRISkyPassData>(skyboxTexture.IsValid ? "HDRISky" : "ClearSky");

        builder.SetColorAttachment(litColorTarget, 0, AccessFlags.Write);
        builder.UseTexture(depthTexture, AccessFlags.Read);
        if (skyboxTexture.IsValid)
        {
            builder.UseTexture(skyboxTexture, AccessFlags.Read);
        }

        builder.SetPassData(new HDRISkyPassData
        {
            litColorTarget = litColorTarget,
            depthTexture = depthTexture,
            skyboxTexture = skyboxTexture,
            hdriSkyShader = _lightingPipelineResource.hdriSkyShader,
            skyboxTintAndIntensity = skyboxTintAndIntensity,
            skyboxRotationSinCos = skyRotationSinCos
        });

        builder.SetRenderFunc<HDRISkyPassData>(static (ref readonly passData, rasterCtx) =>
        {
            if (!passData.hdriSkyShader.IsValid || !rasterCtx.TrySetActiveShaderPass(passData.hdriSkyShader, 1))
            {
                return;
            }

            var props = new HiddenHDRISkyShaderProperties
            {
                skyboxTexture = passData.skyboxTexture.IsValid
                    ? rasterCtx.GetActualBindlessIndex(passData.skyboxTexture, BindlessAccess.ShaderResource)
                    : 0u,
                skyboxSampler = rasterCtx.ResourceManager.StaticSampler.GetSampler(StaticSamplerType.TrilinearClamp),
                depthTexture = rasterCtx.GetActualBindlessIndex(passData.depthTexture, BindlessAccess.ShaderResource),
                depthSampler = rasterCtx.ResourceManager.StaticSampler.GetSampler(StaticSamplerType.PointClamp),
                skyboxTintAndIntensity = passData.skyboxTintAndIntensity,
                skyboxRotationSinCos = passData.skyboxRotationSinCos
            };

            rasterCtx.SetUserDataWithProperties(in props, target: DataTarget.Graphics);
            rasterCtx.DispatchMesh(1, 1, 1);
        });
    }
}
