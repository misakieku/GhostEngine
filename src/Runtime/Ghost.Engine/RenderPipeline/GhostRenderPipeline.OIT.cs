using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Engine.ShaderProperties;
using Ghost.Engine.Streaming;
using Ghost.Engine.Utilities;
using Ghost.Engine.Volumes;
using Ghost.Graphics.Core;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Mathematics;
using System.Numerics;

namespace Ghost.Engine.RenderPipeline;

internal partial class GhostRenderPipeline
{
    private const uint OIT_TILE_SCALE = 8u;
    private const uint OIT_OCC_TILE_SIZE = 8u;
    private const uint OIT_ZERO_TRANS_QUAD_SIZE = 16u;
    private const uint OIT_TILE_DWORD_STRIDE = 5u;

    private const uint OIT_INDIRECT_CLEAR_EXTINCTION = 0u;
    private const uint OIT_INDIRECT_INTEGRATE_EXTINCTION = 16u;
    private const uint OIT_INDIRECT_SPAWN_QUADS = 32u;
    private const uint OIT_INDIRECT_EARLY_DEPTH_PRIME = 48u;
    private const uint OIT_INDIRECT_RESOLVE = 64u;
    private const uint OIT_INDIRECT_BUFFER_BYTE_SIZE = 80u;

    private struct OitFrameConfig
    {
        public uint2 renderSize;
        public uint2 oitSize;
        public uint2 occTiles;
        public uint2 quadTiles;
        public uint physicalSliceCount;
        public uint maxVirtualSliceCount;
        public uint scalarExtinctionSlices;
        public uint occupancyWordsPerTile;
        public uint totalOccTiles;
        public uint maxQuadCount;
        public uint maxVisibleMeshlets;
        public float linearizationFactor;
        public float farPlane;
        public float zeroTransThreshold;
        public float sampleBias;
        public uint viewId;
    }

    private struct OitFrameResources
    {
        public Identifier<RGBuffer> scalarOccupancy;
        public Identifier<RGBuffer> rgbOccupancy;
        public Identifier<RGBuffer> tileFlagsBuffer;
        public Identifier<RGBuffer> depthWarpLut;
        public Identifier<RGBuffer> oitIndirectArgs;
        public Identifier<RGBuffer> occupancyDispatchArgs;
        public Identifier<RGBuffer> quadBuffer;
        public Identifier<RGTexture> d32Depth;
        public Identifier<RGTexture> oitDepth;
        public Identifier<RGTexture> scalarExtinction;
        public Identifier<RGTexture> rgbExtinction;
        public Identifier<RGTexture> scalarOverflowMin;
        public Identifier<RGTexture> rgbOverflowMin;
        public Identifier<RGTexture> integralLut;
        public Identifier<RGTexture> zeroTransDepth;
        public Identifier<RGTexture> mrt0;
        public Identifier<RGTexture> mrt1;
        public Identifier<RGTexture> mrt2;
    }

    private struct OitCopyDepthPassData
    {
        public Identifier<RGTexture> sceneDepthTexture;
        public Handle<Shader> shader;
        public int passIndex;
        public uint2 renderSize;
        public uint2 oitSize;
    }

    private struct OitPrepareOccupancyArgsPassData
    {
        public Identifier<RGBuffer> counterBuffer;
        public Identifier<RGBuffer> indirectArgsBuffer;
        public Handle<ComputeShader> shader;
        public uint maxVisibleMeshlets;
    }

    private struct OitOccupancyPrepassPassData
    {
        public Identifier<RGBuffer> visibleMeshlets;
        public Identifier<RGBuffer> counterBuffer;
        public Identifier<RGBuffer> scalarOccupancy;
        public Identifier<RGBuffer> rgbOccupancy;
        public Identifier<RGBuffer> tileFlagsBuffer;
        public Identifier<RGBuffer> indirectArgsBuffer;
        public ICommandSignature commandSignature;
        public Handle<ComputeShader> shader;
        public OitFrameConfig config;
    }

    private struct OitBuildDepthWarpLutPassData
    {
        public Identifier<RGBuffer> scalarOccupancy;
        public Identifier<RGBuffer> rgbOccupancy;
        public Identifier<RGBuffer> depthWarpLut;
        public Identifier<RGBuffer> indirectArgsBuffer;
        public Identifier<RGBuffer> quadBuffer;
        public Handle<ComputeShader> shader;
        public OitFrameConfig config;
    }

    private struct OitClearExtinctionPassData
    {
        public Identifier<RGBuffer> depthWarpLut;
        public Identifier<RGBuffer> tileFlagsBuffer;
        public Identifier<RGBuffer> indirectArgsBuffer;
        public Identifier<RGTexture> scalarExtinction;
        public Identifier<RGTexture> rgbExtinction;
        public Identifier<RGTexture> scalarOverflowMin;
        public Identifier<RGTexture> rgbOverflowMin;
        public ICommandSignature commandSignature;
        public Handle<ComputeShader> shader;
        public OitFrameConfig config;
    }

    private struct OitTransparentPrepassData
    {
        public Identifier<RGBuffer> visibleMeshlets;
        public Identifier<RGBuffer> binOffsetsBuffer;
        public Identifier<RGBuffer> indirectArgsBuffer;
        public Identifier<RGBuffer> depthWarpLut;
        public Identifier<RGTexture> scalarExtinction;
        public Identifier<RGTexture> rgbExtinction;
        public Identifier<RGTexture> scalarOverflowMin;
        public Identifier<RGTexture> rgbOverflowMin;
        public ICommandSignature commandSignature;
        public ShaderVariantRegistry variantRegistry;
        public OitFrameConfig config;
    }

    private struct OitIntegrateExtinctionPassData
    {
        public Identifier<RGBuffer> depthWarpLut;
        public Identifier<RGBuffer> tileFlagsBuffer;
        public Identifier<RGBuffer> indirectArgsBuffer;
        public Identifier<RGTexture> scalarExtinction;
        public Identifier<RGTexture> rgbExtinction;
        public Identifier<RGTexture> scalarOverflowMin;
        public Identifier<RGTexture> rgbOverflowMin;
        public Identifier<RGTexture> integralLut;
        public Identifier<RGTexture> zeroTransDepth;
        public ICommandSignature commandSignature;
        public Handle<ComputeShader> shader;
        public OitFrameConfig config;
    }

    private struct OitSpawnZeroTransQuadsPassData
    {
        public Identifier<RGTexture> zeroTransDepth;
        public Identifier<RGBuffer> quadBuffer;
        public Identifier<RGBuffer> indirectArgsBuffer;
        public ICommandSignature commandSignature;
        public Handle<ComputeShader> shader;
        public OitFrameConfig config;
    }

    private struct OitEarlyDepthPrimePassData
    {
        public Identifier<RGBuffer> quadBuffer;
        public Identifier<RGBuffer> indirectArgsBuffer;
        public ICommandSignature commandSignature;
        public Handle<Shader> shader;
        public OitFrameConfig config;
    }

    private struct OitTransparentForwardPassData
    {
        public Identifier<RGBuffer> visibleMeshlets;
        public Identifier<RGBuffer> binOffsetsBuffer;
        public Identifier<RGBuffer> indirectArgsBuffer;
        public Identifier<RGTexture> sceneDepthTexture;
        public Identifier<RGBuffer> clusterLightGrid;
        public Identifier<RGBuffer> clusterLightIndexList;
        public uint clusterTilesX;
        public uint clusterSliceCount;
        public Identifier<RGTexture> integralLut;
        public Identifier<RGBuffer> depthWarpLut;
        public Identifier<RGTexture> shadowAtlas;
        public uint shadowViewsBufferSrv;
        public uint shadowIndicesBufferSrv;
        public Identifier<RGTexture> directionalShadowAtlas;
        public uint directionalShadowViewsBufferSrv;
        public uint cascadeCount;
        public float maxShadowDistance;
        public float shadowFadeRange;
        public float4 cascadeSplits;
        public float4 cascadeBlends;
        public SkyContext skyContext;
        public Identifier<RGBuffer> persistentViewStateBuffer;
        public uint enableContactShadows;
        public float contactShadowDistance;
        public float contactShadowTolerance;
        public ICommandSignature commandSignature;
        public ShaderVariantRegistry variantRegistry;
        public OitFrameConfig config;
    }

    private struct OitResolvePassData
    {
        public Identifier<RGTexture> mrt0;
        public Identifier<RGTexture> mrt1;
        public Identifier<RGTexture> mrt2;
        public Identifier<RGTexture> litColor;
        public Identifier<RGBuffer> tileFlagsBuffer;
        public Identifier<RGBuffer> indirectArgsBuffer;
        public ICommandSignature commandSignature;
        public Handle<ComputeShader> shader;
        public uint2 renderSize;
        public uint2 occTiles;
    }

    private void InitializeOit()
    {
    }

    private void DisposeOit()
    {
    }

    private void AddOitPasses(RenderGraph rg, uint viewId, GhostRenderPayload ghostPayload, in SkyContext skyContext, Identifier<RGBuffer> persistentViewStateBuffer, Identifier<RGTexture> litColor, Identifier<RGTexture> sceneDepthTexture, uint2 renderSize)
    {
        if (!_settings.EnableOit || renderSize.x == 0u || renderSize.y == 0u)
        {
            return;
        }

        ref readonly var transVis = ref rg.Blackboard.TryGet<TransparentVisibilityData>(out var hasTransVis);
        if (!hasTransVis || !transVis.isValid)
        {
            return;
        }

        ref readonly var clusterLightData = ref rg.Blackboard.TryGet<ClusterLightData>(out var hasClusterLight);
        if (!hasClusterLight)
        {
            return;
        }

        var config = BuildOitFrameConfig(viewId, renderSize);
        var resources = CreateOitFrameResources(rg, in config);

        rg.AddClearBufferPass(resources.scalarOccupancy, _meshPipelineResource.clearBufferShader, (ulong)config.totalOccTiles * config.occupancyWordsPerTile * sizeof(uint), passName: "OitClearScalarOccupancy");
        rg.AddClearBufferPass(resources.rgbOccupancy, _meshPipelineResource.clearBufferShader, (ulong)config.totalOccTiles * config.occupancyWordsPerTile * sizeof(uint), passName: "OitClearRgbOccupancy");
        rg.AddClearBufferPass(resources.tileFlagsBuffer, _meshPipelineResource.clearBufferShader, (ulong)config.totalOccTiles * OIT_TILE_DWORD_STRIDE * sizeof(uint), passName: "OitClearTileFlags");

        AddOitPrepareOccupancyArgsPass(rg, in transVis, in resources, in config);
        AddOitOccupancyPrepassPass(rg, in transVis, in resources, in config);
        AddOitCopySceneDepthPass(rg, sceneDepthTexture, resources.d32Depth, 0, in config, "OitCopySceneDepthFullRes");
        AddOitCopySceneDepthPass(rg, sceneDepthTexture, resources.oitDepth, 1, in config, "OitMinDownsampleSceneDepth");
        AddOitBuildDepthWarpLutPass(rg, in resources, in config);
        AddOitClearExtinctionPass(rg, in resources, in config);
        AddOitTransparentPrepass(rg, in transVis, in resources, in config);
        AddOitIntegrateExtinctionPass(rg, in resources, in config);
        AddOitSpawnZeroTransQuadsPass(rg, in resources, in config);
        AddOitEarlyDepthPrimePass(rg, in resources, in config);
        AddOitTransparentForwardPass(rg, ghostPayload, in transVis, in clusterLightData, in skyContext, persistentViewStateBuffer, sceneDepthTexture, in resources, in config);
        AddOitResolvePass(rg, litColor, in resources, in config);
    }

    private OitFrameConfig BuildOitFrameConfig(uint viewId, uint2 renderSize)
    {
        var oitWidth = Math.Max(1u, (renderSize.x + OIT_TILE_SCALE - 1u) / OIT_TILE_SCALE);
        var oitHeight = Math.Max(1u, (renderSize.y + OIT_TILE_SCALE - 1u) / OIT_TILE_SCALE);
        var occTilesX = Math.Max(1u, (oitWidth + OIT_OCC_TILE_SIZE - 1u) / OIT_OCC_TILE_SIZE);
        var occTilesY = Math.Max(1u, (oitHeight + OIT_OCC_TILE_SIZE - 1u) / OIT_OCC_TILE_SIZE);
        var quadTilesX = Math.Max(1u, (renderSize.x + OIT_ZERO_TRANS_QUAD_SIZE - 1u) / OIT_ZERO_TRANS_QUAD_SIZE);
        var quadTilesY = Math.Max(1u, (renderSize.y + OIT_ZERO_TRANS_QUAD_SIZE - 1u) / OIT_ZERO_TRANS_QUAD_SIZE);

        var physSlices = Math.Clamp(_settings.OitPhysicalSliceCount, 4u, 128u);
        physSlices = (physSlices + 3u) & ~3u;
        var maxVirtSlices = Math.Clamp(BitOperations.RoundUpToPowerOf2(_settings.OitMaxVirtualSliceCount), physSlices, 8192u);

        return new OitFrameConfig
        {
            renderSize = renderSize,
            oitSize = new uint2(oitWidth, oitHeight),
            occTiles = new uint2(occTilesX, occTilesY),
            quadTiles = new uint2(quadTilesX, quadTilesY),
            physicalSliceCount = physSlices,
            maxVirtualSliceCount = maxVirtSlices,
            scalarExtinctionSlices = physSlices >> 2,
            occupancyWordsPerTile = (maxVirtSlices + 31u) >> 5,
            totalOccTiles = occTilesX * occTilesY,
            maxQuadCount = quadTilesX * quadTilesY,
            maxVisibleMeshlets = _settings.MaxVisibleMeshletsOnScreen,
            linearizationFactor = _settings.OitLinearizationFactor,
            farPlane = _settings.OitFarPlane,
            zeroTransThreshold = _settings.OitZeroTransmittanceThreshold,
            sampleBias = _settings.OitSampleBias,
            viewId = viewId
        };
    }

    private OitFrameResources CreateOitFrameResources(RenderGraph rg, in OitFrameConfig config)
    {
        var occBufferSize = (ulong)config.totalOccTiles * config.occupancyWordsPerTile * sizeof(uint);
        var scalarOccDesc = new BufferDesc
        {
            Size = occBufferSize,
            Stride = sizeof(uint),
            Usage = BufferUsage.Raw | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource
        };
        var tileFlagsDesc = new BufferDesc
        {
            Size = (ulong)config.totalOccTiles * OIT_TILE_DWORD_STRIDE * sizeof(uint),
            Stride = sizeof(uint),
            Usage = BufferUsage.Raw | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource
        };
        var warpLutDesc = new BufferDesc
        {
            Size = (4u + config.maxVirtualSliceCount + config.physicalSliceCount) * sizeof(uint),
            Stride = sizeof(uint),
            Usage = BufferUsage.Raw | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource
        };
        var indirectDesc = new BufferDesc
        {
            Size = OIT_INDIRECT_BUFFER_BYTE_SIZE,
            Stride = sizeof(uint),
            Usage = BufferUsage.IndirectArgument | BufferUsage.Raw | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource
        };
        var occArgsDesc = new BufferDesc
        {
            Size = 16u,
            Stride = sizeof(uint),
            Usage = BufferUsage.IndirectArgument | BufferUsage.Raw | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource
        };
        var quadBufDesc = new BufferDesc
        {
            Size = (4u + (ulong)config.maxQuadCount * 2u) * sizeof(uint),
            Stride = sizeof(uint),
            Usage = BufferUsage.Raw | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource
        };

        var d32DepthDesc = RGTextureDesc.AbsoluteDepth(config.renderSize.x, config.renderSize.y, TextureFormat.D32_Float, clearDepth: 0.0f, clearAtFirstUse: false, discardAtLastUse: true, usage: TextureUsage.DepthStencil);
        var oitDepthDesc = RGTextureDesc.AbsoluteDepth(config.oitSize.x, config.oitSize.y, TextureFormat.D32_Float, clearDepth: 0.0f, clearAtFirstUse: false, discardAtLastUse: true, usage: TextureUsage.DepthStencil);
        var scalarExtDesc = RGTextureDesc.Absolute(config.oitSize.x, config.oitSize.y, TextureFormat.R32_UInt, clearAtFirstUse: false, discardAtLastUse: true, dimension: TextureDimension.Texture3D, mipLevels: 1, slice: (ushort)config.scalarExtinctionSlices, usage: TextureUsage.UnorderedAccess | TextureUsage.ShaderResource);
        var rgbExtDesc = RGTextureDesc.Absolute(config.oitSize.x, config.oitSize.y, TextureFormat.R32_UInt, clearAtFirstUse: false, discardAtLastUse: true, dimension: TextureDimension.Texture3D, mipLevels: 1, slice: (ushort)config.physicalSliceCount, usage: TextureUsage.UnorderedAccess | TextureUsage.ShaderResource);
        var scalarOverflowDesc = RGTextureDesc.Absolute(config.oitSize.x, config.oitSize.y, TextureFormat.R32_UInt, clearAtFirstUse: false, discardAtLastUse: true, usage: TextureUsage.UnorderedAccess | TextureUsage.ShaderResource);
        var rgbOverflowDesc = RGTextureDesc.Absolute(config.oitSize.x, config.oitSize.y, TextureFormat.R32_UInt, clearAtFirstUse: false, discardAtLastUse: true, dimension: TextureDimension.Texture3D, mipLevels: 1, slice: 3, usage: TextureUsage.UnorderedAccess | TextureUsage.ShaderResource);
        var integralLutDesc = RGTextureDesc.Absolute(config.oitSize.x, config.oitSize.y, TextureFormat.R8G8B8A8_UNorm, clearAtFirstUse: false, discardAtLastUse: true, dimension: TextureDimension.Texture3D, mipLevels: 1, slice: (ushort)config.physicalSliceCount, usage: TextureUsage.UnorderedAccess | TextureUsage.ShaderResource);
        var zeroTransDesc = RGTextureDesc.Absolute(config.oitSize.x, config.oitSize.y, TextureFormat.R32_Float, clearAtFirstUse: false, discardAtLastUse: true, usage: TextureUsage.UnorderedAccess | TextureUsage.ShaderResource);
        // Suballocated RT aliasing expects optimized clear alpha = 1.0f; R11G11B10_Float stores only RGB (0, 0, 0).
        var mrtDesc = RGTextureDesc.Absolute(config.renderSize.x, config.renderSize.y, TextureFormat.R11G11B10_Float, clearColor: new Color128(0.0f, 0.0f, 0.0f, 1.0f), clearAtFirstUse: true, discardAtLastUse: true, usage: TextureUsage.RenderTarget | TextureUsage.ShaderResource);

        return new OitFrameResources
        {
            scalarOccupancy = rg.CreateBuffer(in scalarOccDesc, "OitScalarOccupancy"),
            rgbOccupancy = rg.CreateBuffer(in scalarOccDesc, "OitRgbOccupancy"),
            tileFlagsBuffer = rg.CreateBuffer(in tileFlagsDesc, "OitTileFlags"),
            depthWarpLut = rg.CreateBuffer(in warpLutDesc, "OitDepthWarpLut"),
            oitIndirectArgs = rg.CreateBuffer(in indirectDesc, "OitIndirectArgs"),
            occupancyDispatchArgs = rg.CreateBuffer(in occArgsDesc, "OitOccupancyDispatchArgs"),
            quadBuffer = rg.CreateBuffer(in quadBufDesc, "OitZeroTransQuadBuffer"),
            d32Depth = rg.CreateTexture(in d32DepthDesc, "OitD32Depth"),
            oitDepth = rg.CreateTexture(in oitDepthDesc, "OitLowResDepth"),
            scalarExtinction = rg.CreateTexture(in scalarExtDesc, "OitScalarExtinction"),
            rgbExtinction = rg.CreateTexture(in rgbExtDesc, "OitRgbExtinction"),
            scalarOverflowMin = rg.CreateTexture(in scalarOverflowDesc, "OitScalarOverflowMin"),
            rgbOverflowMin = rg.CreateTexture(in rgbOverflowDesc, "OitRgbOverflowMin"),
            integralLut = rg.CreateTexture(in integralLutDesc, "OitIntegralLut"),
            zeroTransDepth = rg.CreateTexture(in zeroTransDesc, "OitZeroTransDepth"),
            mrt0 = rg.CreateTexture(in mrtDesc, "OitAccumColorMRT0"),
            mrt1 = rg.CreateTexture(in mrtDesc, "OitAccumAlphaMRT1"),
            mrt2 = rg.CreateTexture(in mrtDesc, "OitTotalExtinctionMRT2")
        };
    }

    private void AddOitPrepareOccupancyArgsPass(RenderGraph rg, in TransparentVisibilityData transVis, in OitFrameResources resources, in OitFrameConfig config)
    {
        using var builder = rg.AddComputeRenderPass<OitPrepareOccupancyArgsPassData>("OitPrepareOccupancyArgs");
        builder.UseBuffer(transVis.counterBuffer, AccessFlags.Read);
        builder.UseBuffer(resources.occupancyDispatchArgs, AccessFlags.Write);

        builder.SetPassData(new OitPrepareOccupancyArgsPassData
        {
            counterBuffer = transVis.counterBuffer,
            indirectArgsBuffer = resources.occupancyDispatchArgs,
            shader = _oitPipelineResource.prepareOccupancyArgsShader,
            maxVisibleMeshlets = config.maxVisibleMeshlets
        });

        builder.SetRenderFunc<OitPrepareOccupancyArgsPassData>(static (ref readonly passData, computeCtx) =>
        {
            var props = new OitPrepareOccupancyArgsShaderProperties
            {
                counterBufferSrv = computeCtx.GetActualBindlessIndex(passData.counterBuffer, BindlessAccess.ShaderResource),
                indirectArgsBufferUav = computeCtx.GetActualBindlessIndex(passData.indirectArgsBuffer, BindlessAccess.UnorderedAccess),
                maxVisibleMeshlets = passData.maxVisibleMeshlets
            };

            computeCtx.SetActiveCompute(passData.shader, 0);
            computeCtx.SetUserDataWithProperties(in props);
            computeCtx.DispatchCompute(1u, 1u, 1u);
        });
    }

    private void AddOitOccupancyPrepassPass(RenderGraph rg, in TransparentVisibilityData transVis, in OitFrameResources resources, in OitFrameConfig config)
    {
        using var builder = rg.AddComputeRenderPass<OitOccupancyPrepassPassData>("OitOccupancyPrepass");
        builder.UseBuffer(transVis.visibleMeshlets, AccessFlags.Read);
        builder.UseBuffer(transVis.counterBuffer, AccessFlags.Read);
        builder.UseBuffer(resources.occupancyDispatchArgs, AccessFlags.Read);
        builder.UseBuffer(resources.scalarOccupancy, AccessFlags.ReadWrite);
        builder.UseBuffer(resources.rgbOccupancy, AccessFlags.ReadWrite);
        builder.UseBuffer(resources.tileFlagsBuffer, AccessFlags.ReadWrite);

        builder.SetPassData(new OitOccupancyPrepassPassData
        {
            visibleMeshlets = transVis.visibleMeshlets,
            counterBuffer = transVis.counterBuffer,
            scalarOccupancy = resources.scalarOccupancy,
            rgbOccupancy = resources.rgbOccupancy,
            tileFlagsBuffer = resources.tileFlagsBuffer,
            indirectArgsBuffer = resources.occupancyDispatchArgs,
            commandSignature = _dispatchCommandSignature,
            shader = _oitPipelineResource.occupancyPrepassShader,
            config = config
        });

        builder.SetRenderFunc<OitOccupancyPrepassPassData>(static (ref readonly passData, computeCtx) =>
        {
            var props = new OitOccupancyPrepassShaderProperties
            {
                visibleMeshletsBufferSrv = computeCtx.GetActualBindlessIndex(passData.visibleMeshlets, BindlessAccess.ShaderResource),
                counterBufferSrv = computeCtx.GetActualBindlessIndex(passData.counterBuffer, BindlessAccess.ShaderResource),
                scalarOccupancyUav = computeCtx.GetActualBindlessIndex(passData.scalarOccupancy, BindlessAccess.UnorderedAccess),
                rgbOccupancyUav = computeCtx.GetActualBindlessIndex(passData.rgbOccupancy, BindlessAccess.UnorderedAccess),
                tileFlagsUav = computeCtx.GetActualBindlessIndex(passData.tileFlagsBuffer, BindlessAccess.UnorderedAccess),
                oitWidth = passData.config.oitSize.x,
                oitHeight = passData.config.oitSize.y,
                occTilesX = passData.config.occTiles.x,
                occTilesY = passData.config.occTiles.y,
                virtualSliceCount = passData.config.maxVirtualSliceCount,
                maxVisibleMeshlets = passData.config.maxVisibleMeshlets,
                linearizationFactor = passData.config.linearizationFactor,
                farPlane = passData.config.farPlane
            };

            computeCtx.SetActiveCompute(passData.shader, 0);
            computeCtx.SetUserDataWithProperties(in props);

            var actualIndirectBuf = computeCtx.GetActualBuffer(passData.indirectArgsBuffer);
            computeCtx.ExecuteIndirect(passData.commandSignature, 1, actualIndirectBuf, 0);
        });
    }

    private void AddOitCopySceneDepthPass(RenderGraph rg, Identifier<RGTexture> sceneDepthTexture, Identifier<RGTexture> targetDepth, int passIndex, in OitFrameConfig config, string passName)
    {
        using var builder = rg.AddRasterRenderPass<OitCopyDepthPassData>(passName);
        builder.SetDepthAttachment(targetDepth, AccessFlags.WriteAll);
        builder.UseTexture(sceneDepthTexture, AccessFlags.Read);

        builder.SetPassData(new OitCopyDepthPassData
        {
            sceneDepthTexture = sceneDepthTexture,
            shader = _oitPipelineResource.copySceneDepthShader,
            passIndex = passIndex,
            renderSize = config.renderSize,
            oitSize = config.oitSize
        });

        builder.SetRenderFunc<OitCopyDepthPassData>(static (ref readonly passData, rasterCtx) =>
        {
            if (!passData.shader.IsValid || !rasterCtx.TrySetActiveShaderPass(passData.shader, passData.passIndex))
            {
                return;
            }

            var props = new OitCopySceneDepthShaderProperties
            {
                sceneDepthSrv = rasterCtx.GetActualBindlessIndex(passData.sceneDepthTexture, BindlessAccess.ShaderResource),
                fullWidth = passData.renderSize.x,
                fullHeight = passData.renderSize.y,
                oitWidth = passData.oitSize.x,
                oitHeight = passData.oitSize.y
            };

            rasterCtx.SetUserDataWithProperties(in props, target: DataTarget.Graphics);
            rasterCtx.DispatchMesh(1u, 1u, 1u);
        });
    }

    private void AddOitBuildDepthWarpLutPass(RenderGraph rg, in OitFrameResources resources, in OitFrameConfig config)
    {
        using var builder = rg.AddComputeRenderPass<OitBuildDepthWarpLutPassData>("OitBuildDepthWarpLut");
        builder.UseBuffer(resources.scalarOccupancy, AccessFlags.Read);
        builder.UseBuffer(resources.rgbOccupancy, AccessFlags.Read);
        builder.UseBuffer(resources.depthWarpLut, AccessFlags.Write);
        builder.UseBuffer(resources.oitIndirectArgs, AccessFlags.Write);
        builder.UseBuffer(resources.quadBuffer, AccessFlags.Write);

        builder.SetPassData(new OitBuildDepthWarpLutPassData
        {
            scalarOccupancy = resources.scalarOccupancy,
            rgbOccupancy = resources.rgbOccupancy,
            depthWarpLut = resources.depthWarpLut,
            indirectArgsBuffer = resources.oitIndirectArgs,
            quadBuffer = resources.quadBuffer,
            shader = _oitPipelineResource.buildDepthWarpLutShader,
            config = config
        });

        builder.SetRenderFunc<OitBuildDepthWarpLutPassData>(static (ref readonly passData, computeCtx) =>
        {
            var props = new OitBuildDepthWarpLutShaderProperties
            {
                scalarOccupancySrv = computeCtx.GetActualBindlessIndex(passData.scalarOccupancy, BindlessAccess.ShaderResource),
                rgbOccupancySrv = computeCtx.GetActualBindlessIndex(passData.rgbOccupancy, BindlessAccess.ShaderResource),
                depthWarpLutUav = computeCtx.GetActualBindlessIndex(passData.depthWarpLut, BindlessAccess.UnorderedAccess),
                indirectArgsBufferUav = computeCtx.GetActualBindlessIndex(passData.indirectArgsBuffer, BindlessAccess.UnorderedAccess),
                quadBufferUav = computeCtx.GetActualBindlessIndex(passData.quadBuffer, BindlessAccess.UnorderedAccess),
                occTilesX = passData.config.occTiles.x,
                occTilesY = passData.config.occTiles.y,
                maxVirtualSliceCount = passData.config.maxVirtualSliceCount,
                targetPhysicalSliceCount = passData.config.physicalSliceCount,
                linearizationFactor = passData.config.linearizationFactor,
                farPlane = passData.config.farPlane,
                renderWidth = passData.config.renderSize.x,
                renderHeight = passData.config.renderSize.y
            };

            computeCtx.SetActiveCompute(passData.shader, 0);
            computeCtx.SetUserDataWithProperties(in props);
            computeCtx.DispatchCompute(1u, 1u, 1u);
        });
    }

    private void AddOitClearExtinctionPass(RenderGraph rg, in OitFrameResources resources, in OitFrameConfig config)
    {
        using var builder = rg.AddComputeRenderPass<OitClearExtinctionPassData>("OitClearExtinction");
        builder.UseBuffer(resources.depthWarpLut, AccessFlags.Read);
        builder.UseBuffer(resources.tileFlagsBuffer, AccessFlags.Read);
        builder.UseBuffer(resources.oitIndirectArgs, AccessFlags.Read);
        builder.UseTexture(resources.scalarExtinction, AccessFlags.Write);
        builder.UseTexture(resources.rgbExtinction, AccessFlags.Write);
        builder.UseTexture(resources.scalarOverflowMin, AccessFlags.Write);
        builder.UseTexture(resources.rgbOverflowMin, AccessFlags.Write);

        builder.SetPassData(new OitClearExtinctionPassData
        {
            depthWarpLut = resources.depthWarpLut,
            tileFlagsBuffer = resources.tileFlagsBuffer,
            indirectArgsBuffer = resources.oitIndirectArgs,
            scalarExtinction = resources.scalarExtinction,
            rgbExtinction = resources.rgbExtinction,
            scalarOverflowMin = resources.scalarOverflowMin,
            rgbOverflowMin = resources.rgbOverflowMin,
            commandSignature = _dispatchCommandSignature,
            shader = _oitPipelineResource.clearExtinctionShader,
            config = config
        });

        builder.SetRenderFunc<OitClearExtinctionPassData>(static (ref readonly passData, computeCtx) =>
        {
            var props = new OitClearExtinctionShaderProperties
            {
                depthWarpLutSrv = computeCtx.GetActualBindlessIndex(passData.depthWarpLut, BindlessAccess.ShaderResource),
                tileFlagsSrv = computeCtx.GetActualBindlessIndex(passData.tileFlagsBuffer, BindlessAccess.ShaderResource),
                scalarExtinctionUav = computeCtx.GetActualBindlessIndex(passData.scalarExtinction, BindlessAccess.UnorderedAccess),
                rgbExtinctionUav = computeCtx.GetActualBindlessIndex(passData.rgbExtinction, BindlessAccess.UnorderedAccess),
                scalarOverflowMinUav = computeCtx.GetActualBindlessIndex(passData.scalarOverflowMin, BindlessAccess.UnorderedAccess),
                rgbOverflowMinUav = computeCtx.GetActualBindlessIndex(passData.rgbOverflowMin, BindlessAccess.UnorderedAccess),
                oitWidth = passData.config.oitSize.x,
                oitHeight = passData.config.oitSize.y,
                occTilesX = passData.config.occTiles.x
            };

            computeCtx.SetActiveCompute(passData.shader, 0);
            computeCtx.SetUserDataWithProperties(in props);

            var actualIndirectBuf = computeCtx.GetActualBuffer(passData.indirectArgsBuffer);
            computeCtx.ExecuteIndirect(passData.commandSignature, 1, actualIndirectBuf, OIT_INDIRECT_CLEAR_EXTINCTION);
        });
    }

    private void AddOitTransparentPrepass(RenderGraph rg, in TransparentVisibilityData transVis, in OitFrameResources resources, in OitFrameConfig config)
    {
        using var builder = rg.AddRasterRenderPass<OitTransparentPrepassData>("OitTransparentPrepass");
        builder.SetDepthAttachment(resources.oitDepth, AccessFlags.Read);
        builder.UseBuffer(transVis.visibleMeshlets, AccessFlags.Read);
        builder.UseBuffer(transVis.binOffsetsBuffer, AccessFlags.Read);
        builder.UseBuffer(transVis.indirectArgsBuffer, AccessFlags.Read);
        builder.UseBuffer(resources.depthWarpLut, AccessFlags.Read);
        builder.UseRandomAccessTexture(resources.scalarExtinction);
        builder.UseRandomAccessTexture(resources.rgbExtinction);
        builder.UseRandomAccessTexture(resources.scalarOverflowMin);
        builder.UseRandomAccessTexture(resources.rgbOverflowMin);

        builder.SetPassData(new OitTransparentPrepassData
        {
            visibleMeshlets = transVis.visibleMeshlets,
            binOffsetsBuffer = transVis.binOffsetsBuffer,
            indirectArgsBuffer = transVis.indirectArgsBuffer,
            depthWarpLut = resources.depthWarpLut,
            scalarExtinction = resources.scalarExtinction,
            rgbExtinction = resources.rgbExtinction,
            scalarOverflowMin = resources.scalarOverflowMin,
            rgbOverflowMin = resources.rgbOverflowMin,
            commandSignature = _dispatchMeshCommandSignature,
            variantRegistry = _assetManager.ShaderVariants,
            config = config
        });

        builder.SetRenderFunc<OitTransparentPrepassData>(static (ref readonly passData, rasterCtx) =>
        {
            var actualIndirectBuf = rasterCtx.GetActualBuffer(passData.indirectArgsBuffer);
            var visibleSrv = rasterCtx.GetActualBindlessIndex(passData.visibleMeshlets, BindlessAccess.ShaderResource);
            var binOffsetsSrv = rasterCtx.GetActualBindlessIndex(passData.binOffsetsBuffer, BindlessAccess.ShaderResource);
            var depthWarpSrv = rasterCtx.GetActualBindlessIndex(passData.depthWarpLut, BindlessAccess.ShaderResource);
            var scalarExtUav = rasterCtx.GetActualBindlessIndex(passData.scalarExtinction, BindlessAccess.UnorderedAccess);
            var rgbExtUav = rasterCtx.GetActualBindlessIndex(passData.rgbExtinction, BindlessAccess.UnorderedAccess);
            var scalarOverflowUav = rasterCtx.GetActualBindlessIndex(passData.scalarOverflowMin, BindlessAccess.UnorderedAccess);
            var rgbOverflowUav = rasterCtx.GetActualBindlessIndex(passData.rgbOverflowMin, BindlessAccess.UnorderedAccess);

            var variants = passData.variantRegistry.GetVariants(PassSemantic.TransparentPrepass);
            for (var i = 0; i < variants.Length; i++)
            {
                var variantIndex = variants[i];
                ref readonly var variant = ref passData.variantRegistry.GetVariant(variantIndex);
                if (!passData.variantRegistry.IsBytecodeReady(variant.Index) ||
                    !rasterCtx.TrySetActiveShaderPass(variant.Shader, PassSemantic.TransparentPrepass))
                {
                    continue;
                }

                var bin = (uint)variantIndex.Value;
                var props = new TransparentPrepassShaderProperties
                {
                    visibleMeshletsBufferIndex = visibleSrv,
                    binOffsetsBufferIndex = binOffsetsSrv,
                    binIndex = bin,
                    scalarExtinctionUav = scalarExtUav,
                    rgbExtinctionUav = rgbExtUav,
                    scalarOverflowMinUav = scalarOverflowUav,
                    rgbOverflowMinUav = rgbOverflowUav,
                    depthWarpLutSrv = depthWarpSrv,
                    oitWidth = passData.config.oitSize.x,
                    oitHeight = passData.config.oitSize.y,
                    oitPhysicalSliceCount = passData.config.physicalSliceCount,
                    oitVirtualSliceCount = passData.config.maxVirtualSliceCount,
                    oitLinearizationFactor = passData.config.linearizationFactor,
                    oitFarPlane = passData.config.farPlane
                };

                rasterCtx.SetUserDataWithProperties(in props, target: DataTarget.Graphics);
                rasterCtx.ExecuteIndirect(passData.commandSignature, 1, actualIndirectBuf, (ulong)bin * INDIRECT_ARGS_STRIDE);
            }
        });
    }

    private void AddOitIntegrateExtinctionPass(RenderGraph rg, in OitFrameResources resources, in OitFrameConfig config)
    {
        using var builder = rg.AddComputeRenderPass<OitIntegrateExtinctionPassData>("OitIntegrateExtinction");
        builder.UseBuffer(resources.depthWarpLut, AccessFlags.Read);
        builder.UseBuffer(resources.tileFlagsBuffer, AccessFlags.Read);
        builder.UseBuffer(resources.oitIndirectArgs, AccessFlags.Read);
        builder.UseTexture(resources.scalarExtinction, AccessFlags.Read);
        builder.UseTexture(resources.rgbExtinction, AccessFlags.Read);
        builder.UseTexture(resources.scalarOverflowMin, AccessFlags.Read);
        builder.UseTexture(resources.rgbOverflowMin, AccessFlags.Read);
        builder.UseTexture(resources.integralLut, AccessFlags.Write);
        builder.UseTexture(resources.zeroTransDepth, AccessFlags.Write);

        builder.SetPassData(new OitIntegrateExtinctionPassData
        {
            depthWarpLut = resources.depthWarpLut,
            tileFlagsBuffer = resources.tileFlagsBuffer,
            indirectArgsBuffer = resources.oitIndirectArgs,
            scalarExtinction = resources.scalarExtinction,
            rgbExtinction = resources.rgbExtinction,
            scalarOverflowMin = resources.scalarOverflowMin,
            rgbOverflowMin = resources.rgbOverflowMin,
            integralLut = resources.integralLut,
            zeroTransDepth = resources.zeroTransDepth,
            commandSignature = _dispatchCommandSignature,
            shader = _oitPipelineResource.integrateExtinctionShader,
            config = config
        });

        builder.SetRenderFunc<OitIntegrateExtinctionPassData>(static (ref readonly passData, computeCtx) =>
        {
            var props = new OitIntegrateExtinctionShaderProperties
            {
                depthWarpLutSrv = computeCtx.GetActualBindlessIndex(passData.depthWarpLut, BindlessAccess.ShaderResource),
                tileFlagsSrv = computeCtx.GetActualBindlessIndex(passData.tileFlagsBuffer, BindlessAccess.ShaderResource),
                scalarExtinctionSrv = computeCtx.GetActualBindlessIndex(passData.scalarExtinction, BindlessAccess.ShaderResource),
                rgbExtinctionSrv = computeCtx.GetActualBindlessIndex(passData.rgbExtinction, BindlessAccess.ShaderResource),
                scalarOverflowMinSrv = computeCtx.GetActualBindlessIndex(passData.scalarOverflowMin, BindlessAccess.ShaderResource),
                rgbOverflowMinSrv = computeCtx.GetActualBindlessIndex(passData.rgbOverflowMin, BindlessAccess.ShaderResource),
                integralLutUav = computeCtx.GetActualBindlessIndex(passData.integralLut, BindlessAccess.UnorderedAccess),
                zeroTransDepthUav = computeCtx.GetActualBindlessIndex(passData.zeroTransDepth, BindlessAccess.UnorderedAccess),
                oitWidth = passData.config.oitSize.x,
                oitHeight = passData.config.oitSize.y,
                occTilesX = passData.config.occTiles.x,
                maxVirtualSliceCount = passData.config.maxVirtualSliceCount,
                zeroTransThreshold = passData.config.zeroTransThreshold,
                sampleBias = passData.config.sampleBias
            };

            computeCtx.SetActiveCompute(passData.shader, 0);
            computeCtx.SetUserDataWithProperties(in props);

            var actualIndirectBuf = computeCtx.GetActualBuffer(passData.indirectArgsBuffer);
            computeCtx.ExecuteIndirect(passData.commandSignature, 1, actualIndirectBuf, OIT_INDIRECT_INTEGRATE_EXTINCTION);
        });
    }

    private void AddOitSpawnZeroTransQuadsPass(RenderGraph rg, in OitFrameResources resources, in OitFrameConfig config)
    {
        using var builder = rg.AddComputeRenderPass<OitSpawnZeroTransQuadsPassData>("OitSpawnZeroTransQuads");
        builder.UseTexture(resources.zeroTransDepth, AccessFlags.Read);
        builder.UseBuffer(resources.quadBuffer, AccessFlags.ReadWrite);
        builder.UseBuffer(resources.oitIndirectArgs, AccessFlags.ReadWrite);

        builder.SetPassData(new OitSpawnZeroTransQuadsPassData
        {
            zeroTransDepth = resources.zeroTransDepth,
            quadBuffer = resources.quadBuffer,
            indirectArgsBuffer = resources.oitIndirectArgs,
            commandSignature = _dispatchCommandSignature,
            shader = _oitPipelineResource.spawnZeroTransQuadsShader,
            config = config
        });

        builder.SetRenderFunc<OitSpawnZeroTransQuadsPassData>(static (ref readonly passData, computeCtx) =>
        {
            var props = new OitSpawnZeroTransQuadsShaderProperties
            {
                zeroTransDepthSrv = computeCtx.GetActualBindlessIndex(passData.zeroTransDepth, BindlessAccess.ShaderResource),
                quadBufferUav = computeCtx.GetActualBindlessIndex(passData.quadBuffer, BindlessAccess.UnorderedAccess),
                indirectArgsBufferUav = computeCtx.GetActualBindlessIndex(passData.indirectArgsBuffer, BindlessAccess.UnorderedAccess),
                renderWidth = passData.config.renderSize.x,
                renderHeight = passData.config.renderSize.y,
                oitWidth = passData.config.oitSize.x,
                oitHeight = passData.config.oitSize.y,
                tilesX = passData.config.quadTiles.x,
                tilesY = passData.config.quadTiles.y,
                maxQuadCount = passData.config.maxQuadCount
            };

            computeCtx.SetActiveCompute(passData.shader, 0);
            computeCtx.SetUserDataWithProperties(in props);

            var actualIndirectBuf = computeCtx.GetActualBuffer(passData.indirectArgsBuffer);
            computeCtx.ExecuteIndirect(passData.commandSignature, 1, actualIndirectBuf, OIT_INDIRECT_SPAWN_QUADS);
        });
    }

    private void AddOitEarlyDepthPrimePass(RenderGraph rg, in OitFrameResources resources, in OitFrameConfig config)
    {
        using var builder = rg.AddRasterRenderPass<OitEarlyDepthPrimePassData>("OitEarlyDepthPrime");
        builder.SetDepthAttachment(resources.d32Depth, AccessFlags.ReadWrite);
        builder.UseBuffer(resources.quadBuffer, AccessFlags.Read);
        builder.UseBuffer(resources.oitIndirectArgs, AccessFlags.Read);

        builder.SetPassData(new OitEarlyDepthPrimePassData
        {
            quadBuffer = resources.quadBuffer,
            indirectArgsBuffer = resources.oitIndirectArgs,
            commandSignature = _dispatchMeshCommandSignature,
            shader = _oitPipelineResource.earlyDepthPrimeShader,
            config = config
        });

        builder.SetRenderFunc<OitEarlyDepthPrimePassData>(static (ref readonly passData, rasterCtx) =>
        {
            if (!passData.shader.IsValid || !rasterCtx.TrySetActiveShaderPass(passData.shader, 0))
            {
                return;
            }

            var props = new OitEarlyDepthPrimeShaderProperties
            {
                quadBufferSrv = rasterCtx.GetActualBindlessIndex(passData.quadBuffer, BindlessAccess.ShaderResource),
                renderWidth = passData.config.renderSize.x,
                renderHeight = passData.config.renderSize.y,
                maxQuadCount = passData.config.maxQuadCount
            };

            rasterCtx.SetUserDataWithProperties(in props, target: DataTarget.Graphics);

            var actualIndirectBuf = rasterCtx.GetActualBuffer(passData.indirectArgsBuffer);
            rasterCtx.ExecuteIndirect(passData.commandSignature, 1, actualIndirectBuf, OIT_INDIRECT_EARLY_DEPTH_PRIME);
        });
    }

    private void AddOitTransparentForwardPass(RenderGraph rg, GhostRenderPayload ghostPayload, in TransparentVisibilityData transVis, in ClusterLightData clusterLightData, in SkyContext skyContext, Identifier<RGBuffer> persistentViewStateBuffer, Identifier<RGTexture> sceneDepthTexture, in OitFrameResources resources, in OitFrameConfig config)
    {
        ref readonly var shadowMap = ref rg.Blackboard.TryGet<ShadowMapPassData>(out var hasShadowMap);
        ref readonly var shadowSettings = ref ghostPayload.GetViewVolumeData<ShadowSettings>(config.viewId);
        ref readonly var contactShadowSettings = ref ghostPayload.GetViewVolumeData<ContactShadowSettings>(config.viewId);

        using var builder = rg.AddRasterRenderPass<OitTransparentForwardPassData>("OitTransparentForward");
        builder.SetColorAttachment(resources.mrt0, 0, AccessFlags.Write);
        builder.SetColorAttachment(resources.mrt1, 1, AccessFlags.Write);
        builder.SetColorAttachment(resources.mrt2, 2, AccessFlags.Write);
        builder.SetDepthAttachment(resources.d32Depth, AccessFlags.Read);

        builder.UseBuffer(transVis.visibleMeshlets, AccessFlags.Read);
        builder.UseBuffer(transVis.binOffsetsBuffer, AccessFlags.Read);
        builder.UseBuffer(transVis.indirectArgsBuffer, AccessFlags.Read);
        builder.UseBuffer(clusterLightData.clusterLightGrid, AccessFlags.Read);
        builder.UseBuffer(clusterLightData.clusterLightIndexList, AccessFlags.Read);
        builder.UseBuffer(resources.depthWarpLut, AccessFlags.Read);
        builder.UseTexture(resources.integralLut, AccessFlags.Read);
        builder.UseTexture(sceneDepthTexture, AccessFlags.Read);

        if (persistentViewStateBuffer.IsValid)
        {
            builder.UseBuffer(persistentViewStateBuffer, AccessFlags.Read);
        }

        if (skyContext.hasSky && skyContext.skyboxTexture.IsValid)
        {
            builder.UseTexture(skyContext.skyboxTexture, AccessFlags.Read);
        }

        if (skyContext.hasSky && skyContext.iblLutTexture.IsValid)
        {
            builder.UseTexture(skyContext.iblLutTexture, AccessFlags.Read);
        }

        var shadowAtlas = hasShadowMap ? shadowMap.shadowAtlas : Identifier<RGTexture>.Invalid;
        var directionalShadowAtlas = hasShadowMap ? shadowMap.directionalShadowAtlas : Identifier<RGTexture>.Invalid;

        if (shadowAtlas.IsValid)
        {
            builder.UseTexture(shadowAtlas, AccessFlags.Read);
        }

        if (directionalShadowAtlas.IsValid)
        {
            builder.UseTexture(directionalShadowAtlas, AccessFlags.Read);
        }

        builder.SetPassData(new OitTransparentForwardPassData
        {
            visibleMeshlets = transVis.visibleMeshlets,
            binOffsetsBuffer = transVis.binOffsetsBuffer,
            indirectArgsBuffer = transVis.indirectArgsBuffer,
            sceneDepthTexture = sceneDepthTexture,
            clusterLightGrid = clusterLightData.clusterLightGrid,
            clusterLightIndexList = clusterLightData.clusterLightIndexList,
            clusterTilesX = clusterLightData.tilesX,
            clusterSliceCount = clusterLightData.sliceCount,
            integralLut = resources.integralLut,
            depthWarpLut = resources.depthWarpLut,
            shadowAtlas = shadowAtlas,
            shadowViewsBufferSrv = hasShadowMap ? shadowMap.shadowViewsBufferSrv : RHIUtility.INVALID_BINDLESS_INDEX,
            shadowIndicesBufferSrv = hasShadowMap ? shadowMap.shadowIndicesBufferSrv : RHIUtility.INVALID_BINDLESS_INDEX,
            directionalShadowAtlas = directionalShadowAtlas,
            directionalShadowViewsBufferSrv = hasShadowMap ? shadowMap.directionalShadowViewsBufferSrv : RHIUtility.INVALID_BINDLESS_INDEX,
            cascadeCount = hasShadowMap ? shadowMap.cascadeCount : (uint)shadowSettings.cascadeCount.GetValueOrDefault(),
            maxShadowDistance = hasShadowMap ? shadowMap.maxShadowDistance : shadowSettings.maxDistance.GetValueOrDefault(),
            shadowFadeRange = hasShadowMap ? shadowMap.shadowFadeRange : 0.0f,
            cascadeSplits = hasShadowMap ? shadowMap.cascadeSplits : default,
            cascadeBlends = hasShadowMap ? shadowMap.cascadeBlends : default,
            skyContext = skyContext,
            persistentViewStateBuffer = persistentViewStateBuffer,
            enableContactShadows = contactShadowSettings.enabled.GetValueOrDefault() ? 1u : 0u,
            contactShadowDistance = contactShadowSettings.maxDistance.GetValueOrDefault(),
            contactShadowTolerance = contactShadowSettings.tolerance.GetValueOrDefault(),
            commandSignature = _dispatchMeshCommandSignature,
            variantRegistry = _assetManager.ShaderVariants,
            config = config
        });

        builder.SetRenderFunc<OitTransparentForwardPassData>(static (ref readonly passData, rasterCtx) =>
        {
            var actualIndirectBuf = rasterCtx.GetActualBuffer(passData.indirectArgsBuffer);
            var visibleSrv = rasterCtx.GetActualBindlessIndex(passData.visibleMeshlets, BindlessAccess.ShaderResource);
            var binOffsetsSrv = rasterCtx.GetActualBindlessIndex(passData.binOffsetsBuffer, BindlessAccess.ShaderResource);
            var depthSrv = rasterCtx.GetActualBindlessIndex(passData.sceneDepthTexture, BindlessAccess.ShaderResource);
            var lightGridSrv = rasterCtx.GetActualBindlessIndex(passData.clusterLightGrid, BindlessAccess.ShaderResource);
            var lightIndicesSrv = rasterCtx.GetActualBindlessIndex(passData.clusterLightIndexList, BindlessAccess.ShaderResource);
            var integralLutSrv = rasterCtx.GetActualBindlessIndex(passData.integralLut, BindlessAccess.ShaderResource);
            var depthWarpLutSrv = rasterCtx.GetActualBindlessIndex(passData.depthWarpLut, BindlessAccess.ShaderResource);

            var shadowAtlasSrv = passData.shadowAtlas.IsValid
                ? rasterCtx.GetActualBindlessIndex(passData.shadowAtlas, BindlessAccess.ShaderResource)
                : RHIUtility.INVALID_BINDLESS_INDEX;
            var dirShadowAtlasSrv = passData.directionalShadowAtlas.IsValid
                ? rasterCtx.GetActualBindlessIndex(passData.directionalShadowAtlas, BindlessAccess.ShaderResource)
                : RHIUtility.INVALID_BINDLESS_INDEX;
            var skyboxSrv = passData.skyContext.hasSky && passData.skyContext.skyboxTexture.IsValid
                ? rasterCtx.GetActualBindlessIndex(passData.skyContext.skyboxTexture, BindlessAccess.ShaderResource)
                : RHIUtility.INVALID_BINDLESS_INDEX;
            var iblLutSrv = passData.skyContext.hasSky && passData.skyContext.iblLutTexture.IsValid
                ? rasterCtx.GetActualBindlessIndex(passData.skyContext.iblLutTexture, BindlessAccess.ShaderResource)
                : RHIUtility.INVALID_BINDLESS_INDEX;
            var viewStateSrv = passData.persistentViewStateBuffer.IsValid
                ? rasterCtx.GetActualBindlessIndex(passData.persistentViewStateBuffer, BindlessAccess.ShaderResource)
                : RHIUtility.INVALID_BINDLESS_INDEX;

            var variants = passData.variantRegistry.GetVariants(PassSemantic.TransparentForward);
            for (var i = 0; i < variants.Length; i++)
            {
                var variantIndex = variants[i];
                ref readonly var variant = ref passData.variantRegistry.GetVariant(variantIndex);
                if (!passData.variantRegistry.IsBytecodeReady(variant.Index) ||
                    !rasterCtx.TrySetActiveShaderPass(variant.Shader, PassSemantic.TransparentForward))
                {
                    continue;
                }

                var bin = (uint)variantIndex.Value;
                var props = new TransparentForwardShaderProperties
                {
                    visibleMeshletsBufferIndex = visibleSrv,
                    binOffsetsBufferIndex = binOffsetsSrv,
                    binIndex = bin,
                    depthTextureIndex = depthSrv,
                    clusterLightGridBufferIndex = lightGridSrv,
                    clusterLightIndexBufferIndex = lightIndicesSrv,
                    clusterTilesX = passData.clusterTilesX,
                    clusterSliceCount = passData.clusterSliceCount,
                    oitIntegralLutSrv = integralLutSrv,
                    oitDepthWarpLutSrv = depthWarpLutSrv,
                    oitWidth = passData.config.oitSize.x,
                    oitHeight = passData.config.oitSize.y,
                    oitPhysicalSliceCount = passData.config.physicalSliceCount,
                    oitVirtualSliceCount = passData.config.maxVirtualSliceCount,
                    oitLinearizationFactor = passData.config.linearizationFactor,
                    oitFarPlane = passData.config.farPlane,
                    oitSampleBias = passData.config.sampleBias,
                    shadowAtlasSrv = shadowAtlasSrv,
                    shadowViewsBufferSrv = passData.shadowViewsBufferSrv,
                    shadowIndicesBufferSrv = passData.shadowIndicesBufferSrv,
                    skyboxTextureSrv = skyboxSrv,
                    iblLutTextureSrv = iblLutSrv,
                    persistentViewStateBufferSrv = viewStateSrv,
                    viewId = passData.config.viewId,
                    skyboxMipCount = passData.skyContext.skyboxMipCount,
                    skyboxTintAndIntensity = passData.skyContext.skyTintAndIntensity,
                    skyboxRotationSinCos = passData.skyContext.skyRotationSinCos,
                    directionalShadowAtlasSrv = dirShadowAtlasSrv,
                    directionalShadowViewsBufferSrv = passData.directionalShadowViewsBufferSrv,
                    cascadeCount = passData.cascadeCount,
                    maxShadowDistance = passData.maxShadowDistance,
                    shadowFadeRange = passData.shadowFadeRange,
                    cascadeSplits = passData.cascadeSplits,
                    cascadeBlends = passData.cascadeBlends,
                    enableContactShadows = passData.enableContactShadows,
                    contactShadowDistance = passData.contactShadowDistance,
                    contactShadowTolerance = passData.contactShadowTolerance
                };

                rasterCtx.SetUserDataWithProperties(in props, target: DataTarget.Graphics);
                rasterCtx.ExecuteIndirect(passData.commandSignature, 1, actualIndirectBuf, (ulong)bin * INDIRECT_ARGS_STRIDE);
            }
        });
    }

    private void AddOitResolvePass(RenderGraph rg, Identifier<RGTexture> litColor, in OitFrameResources resources, in OitFrameConfig config)
    {
        using var builder = rg.AddComputeRenderPass<OitResolvePassData>("OitResolve");
        builder.UseTexture(resources.mrt0, AccessFlags.Read);
        builder.UseTexture(resources.mrt1, AccessFlags.Read);
        builder.UseTexture(resources.mrt2, AccessFlags.Read);
        builder.UseBuffer(resources.tileFlagsBuffer, AccessFlags.Read);
        builder.UseBuffer(resources.oitIndirectArgs, AccessFlags.Read);
        builder.UseTexture(litColor, AccessFlags.ReadWrite);

        builder.SetPassData(new OitResolvePassData
        {
            mrt0 = resources.mrt0,
            mrt1 = resources.mrt1,
            mrt2 = resources.mrt2,
            litColor = litColor,
            tileFlagsBuffer = resources.tileFlagsBuffer,
            indirectArgsBuffer = resources.oitIndirectArgs,
            commandSignature = _dispatchCommandSignature,
            shader = _oitPipelineResource.resolveShader,
            renderSize = config.renderSize,
            occTiles = config.occTiles
        });

        builder.SetRenderFunc<OitResolvePassData>(static (ref readonly passData, computeCtx) =>
        {
            var props = new OitResolveShaderProperties
            {
                mrt0Srv = computeCtx.GetActualBindlessIndex(passData.mrt0, BindlessAccess.ShaderResource),
                mrt1Srv = computeCtx.GetActualBindlessIndex(passData.mrt1, BindlessAccess.ShaderResource),
                mrt2Srv = computeCtx.GetActualBindlessIndex(passData.mrt2, BindlessAccess.ShaderResource),
                litColorUav = computeCtx.GetActualBindlessIndex(passData.litColor, BindlessAccess.UnorderedAccess),
                tileFlagsSrv = computeCtx.GetActualBindlessIndex(passData.tileFlagsBuffer, BindlessAccess.ShaderResource),
                renderWidth = passData.renderSize.x,
                renderHeight = passData.renderSize.y,
                occTilesX = passData.occTiles.x,
                occTilesY = passData.occTiles.y
            };

            computeCtx.SetActiveCompute(passData.shader, 0);
            computeCtx.SetUserDataWithProperties(in props);

            var actualIndirectBuf = computeCtx.GetActualBuffer(passData.indirectArgsBuffer);
            computeCtx.ExecuteIndirect(passData.commandSignature, 1, actualIndirectBuf, OIT_INDIRECT_RESOLVE);
        });
    }
}
