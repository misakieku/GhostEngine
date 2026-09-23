using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Engine.ShaderProperties;
using Ghost.Engine.Streaming;
using Ghost.Graphics.Core;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Engine.RenderPipeline;

internal partial class GhostRenderPipeline
{
    private const uint CLASSIFICATION_TILE_SIZE = 16u;
    private const uint MAX_CLASSIFICATION_VARIANTS = 64u;
    private const uint VARIANT_COUNTER_STRIDE = 8u;
    private const uint INDIRECT_ARGS_STRIDE = 16u;

    private struct ClearClassificationCountersPassData
    {
        public Identifier<RGBuffer> countersBuffer;
        public Handle<ComputeShader> shader;
        public uint maxVariants;
    }

    public struct TileMaterialClassificationPassData
    {
        public Identifier<RGBuffer> visBuffer;
        public Identifier<RGBuffer> visibleMeshletsPass1;
        public Identifier<RGBuffer> visibleMeshletsPass2;
        public Identifier<RGBuffer> variantTileCounters;
        public Identifier<RGBuffer> variantTileList;
        public Handle<ComputeShader> shader;
        public uint2 renderSize;
        public uint tilesPerRow;
        public uint maxTilesPerVariant;
        public uint2 deferredVariantMask;
        public uint2 dispatchGroups;
    }

    public struct PrepareDeferredTexturingIndirectArgsPassData
    {
        public Identifier<RGBuffer> variantTileCounters;
        public Identifier<RGBuffer> indirectArgsBuffer;
        public Handle<ComputeShader> shader;
        public uint maxVariants;
    }

    private struct DebugClassificationPassData
    {
        public Identifier<RGBuffer> variantTileList;
        public Identifier<RGBuffer> indirectArgsBuffer;
        public Identifier<RGTexture> targetTexture;
        public Handle<Shader> shader;
        public ICommandSignature commandSignature;
        public ShaderVariantRegistry variantRegistry;
        public uint tilesPerRow;
        public uint maxTilesPerVariant;
        public uint2 renderSize;
    }

    private Identifier<RGBuffer> AddClearClassificationCountersPass(RenderGraph rg, uint maxVariants)
    {
        using var builder = rg.AddComputeRenderPass<ClearClassificationCountersPassData>("ClearClassificationCounters");

        var countersDesc = new BufferDesc
        {
            Size = maxVariants * VARIANT_COUNTER_STRIDE,
            Stride = 4,
            Usage = BufferUsage.Raw | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource
        };
        var countersBuffer = builder.CreateBuffer(in countersDesc, "VariantTileCounters");
        builder.UseBuffer(countersBuffer, AccessFlags.Write);

        builder.SetPassData(new ClearClassificationCountersPassData
        {
            countersBuffer = countersBuffer,
            shader = _materialPipelineResource.clearClassificationCountersShader,
            maxVariants = maxVariants
        });

        builder.SetRenderFunc<ClearClassificationCountersPassData>(static (ref readonly passData, computeCtx) =>
        {
            var countersUav = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.countersBuffer).AsResource(), BindlessAccess.UnorderedAccess);

            var props = new InternalClearClassificationCountersShaderProperties
            {
                countersBufferUav = countersUav,
                maxVariants = passData.maxVariants
            };

            computeCtx.SetActiveCompute(passData.shader, 0);
            computeCtx.SetUserDataWithProperties(in props);
            computeCtx.DispatchCompute(1, 1, 1);
        });

        return countersBuffer;
    }

    private Identifier<RGBuffer> AddTileMaterialClassificationPass(RenderGraph rg, Identifier<RGBuffer> visBuffer, Identifier<RGBuffer> visibleMeshlets0, Identifier<RGBuffer> visibleMeshlets1, Identifier<RGBuffer> countersBuffer,
        uint2 screenSize, uint maxTilesPerVariant, uint2 deferredVariantMask)
    {
        var tilesX = (screenSize.x + CLASSIFICATION_TILE_SIZE - 1u) / CLASSIFICATION_TILE_SIZE;
        var tilesY = (screenSize.y + CLASSIFICATION_TILE_SIZE - 1u) / CLASSIFICATION_TILE_SIZE;

        using var builder = rg.AddComputeRenderPass<TileMaterialClassificationPassData>("TileMaterialClassification");

        var tileListDesc = new BufferDesc
        {
            Size = MAX_CLASSIFICATION_VARIANTS * maxTilesPerVariant * 4u,
            Stride = 4,
            Usage = BufferUsage.Raw | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource
        };
        var tileListBuffer = builder.CreateBuffer(in tileListDesc, "VariantTileList");

        builder.UseBuffer(visBuffer, AccessFlags.Read);
        builder.UseBuffer(visibleMeshlets0, AccessFlags.Read);
        builder.UseBuffer(visibleMeshlets1, AccessFlags.Read);
        builder.UseBuffer(countersBuffer, AccessFlags.ReadWrite);
        builder.UseBuffer(tileListBuffer, AccessFlags.Write);

        builder.SetPassData(new TileMaterialClassificationPassData
        {
            visBuffer = visBuffer,
            visibleMeshletsPass1 = visibleMeshlets0,
            visibleMeshletsPass2 = visibleMeshlets1,
            variantTileCounters = countersBuffer,
            variantTileList = tileListBuffer,
            shader = _materialPipelineResource.tileMaterialClassificationShader,
            renderSize = screenSize,
            tilesPerRow = tilesX,
            maxTilesPerVariant = maxTilesPerVariant,
            deferredVariantMask = deferredVariantMask,
            dispatchGroups = new uint2(tilesX, tilesY)
        });

        builder.SetRenderFunc<TileMaterialClassificationPassData>(static (ref readonly passData, computeCtx) =>
        {
            var visSrv = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.visBuffer).AsResource(), BindlessAccess.ShaderResource);
            var visibleMeshletsPass1Srv = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.visibleMeshletsPass1).AsResource(), BindlessAccess.ShaderResource);
            var visibleMeshletsPass2Srv = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.visibleMeshletsPass2).AsResource(), BindlessAccess.ShaderResource);
            var countersUav = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.variantTileCounters).AsResource(), BindlessAccess.UnorderedAccess);
            var tileListUav = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.variantTileList).AsResource(), BindlessAccess.UnorderedAccess);

            var props = new InternalTileMaterialClassificationShaderProperties
            {
                visBufferIndex = visSrv,
                visibleMeshletsPass1 = visibleMeshletsPass1Srv,
                visibleMeshletsPass2 = visibleMeshletsPass2Srv,
                variantTileCountersUav = countersUav,
                variantTileListUav = tileListUav,
                renderWidth = passData.renderSize.x,
                renderHeight = passData.renderSize.y,
                tilesPerRow = passData.tilesPerRow,
                maxTilesPerVariant = passData.maxTilesPerVariant,
                deferredVariantMask = passData.deferredVariantMask
            };

            computeCtx.SetActiveCompute(passData.shader, 0);
            computeCtx.SetUserDataWithProperties(in props);
            computeCtx.DispatchCompute(passData.dispatchGroups.x, passData.dispatchGroups.y, 1);
        });

        return tileListBuffer;
    }

    private Identifier<RGBuffer> AddPrepareDeferredTexturingIndirectArgsPass(RenderGraph rg, Identifier<RGBuffer> countersBuffer, uint maxVariants)
    {
        using var builder = rg.AddComputeRenderPass<PrepareDeferredTexturingIndirectArgsPassData>("PrepareDeferredTexturingIndirectArgs");

        var indirectArgsDesc = new BufferDesc
        {
            Size = maxVariants * INDIRECT_ARGS_STRIDE,
            Stride = 4,
            Usage = BufferUsage.IndirectArgument | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource
        };
        var indirectArgsBuffer = builder.CreateBuffer(in indirectArgsDesc, "DeferredTexturingIndirectArgs");

        builder.UseBuffer(countersBuffer, AccessFlags.Read);
        builder.UseBuffer(indirectArgsBuffer, AccessFlags.Write);

        builder.SetPassData(new PrepareDeferredTexturingIndirectArgsPassData
        {
            variantTileCounters = countersBuffer,
            indirectArgsBuffer = indirectArgsBuffer,
            shader = _materialPipelineResource.prepareDeferredTexturingIndirectArgsShader,
            maxVariants = maxVariants
        });

        builder.SetRenderFunc<PrepareDeferredTexturingIndirectArgsPassData>(static (ref readonly passData, computeCtx) =>
        {
            var countersSrv = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.variantTileCounters).AsResource(), BindlessAccess.ShaderResource);
            var indirectUav = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.indirectArgsBuffer).AsResource(), BindlessAccess.UnorderedAccess);

            var props = new InternalPrepareDeferredTexturingIndirectArgsShaderProperties
            {
                countersBufferIndex = countersSrv,
                indirectArgsBufferUav = indirectUav,
                maxVariants = passData.maxVariants
            };

            computeCtx.SetActiveCompute(passData.shader, 0);
            computeCtx.SetUserDataWithProperties(in props);
            computeCtx.DispatchCompute(1, 1, 1);
        });

        return indirectArgsBuffer;
    }

    private void AddDebugClassificationPass(RenderGraph rg, Identifier<RGTexture> targetTexture, Identifier<RGBuffer> tileListBuffer, Identifier<RGBuffer> indirectArgsBuffer, uint2 screenSize)
    {
        var tilesX = (screenSize.x + CLASSIFICATION_TILE_SIZE - 1u) / CLASSIFICATION_TILE_SIZE;
        var tilesY = (screenSize.y + CLASSIFICATION_TILE_SIZE - 1u) / CLASSIFICATION_TILE_SIZE;
        var totalTiles = Math.Max(1u, tilesX * tilesY);

        using var builder = rg.AddRasterRenderPass<DebugClassificationPassData>("DebugClassification");
        builder.SetColorAttachment(targetTexture, 0, AccessFlags.Write);
        builder.UseBuffer(tileListBuffer, AccessFlags.Read);
        builder.UseBuffer(indirectArgsBuffer, AccessFlags.Read);

        builder.SetPassData(new DebugClassificationPassData
        {
            variantTileList = tileListBuffer,
            indirectArgsBuffer = indirectArgsBuffer,
            targetTexture = targetTexture,
            shader = _materialPipelineResource.debugClassificationShader,
            commandSignature = _dispatchMeshCommandSignature,
            variantRegistry = _assetManager.ShaderVariants,
            tilesPerRow = tilesX,
            maxTilesPerVariant = totalTiles,
            renderSize = screenSize
        });

        builder.SetRenderFunc<DebugClassificationPassData>(static (ref readonly passData, renderCtx) =>
        {
            if (!renderCtx.TrySetActiveShaderPass(passData.shader, 0))
            {
                return;
            }

            var tileListSrv = renderCtx.ResourceDatabase.GetBindlessIndex(renderCtx.GetActualBuffer(passData.variantTileList).AsResource(), BindlessAccess.ShaderResource);
            var actualIndirectBuf = renderCtx.GetActualBuffer(passData.indirectArgsBuffer);

            var dispatchVariants = passData.variantRegistry.GetDispatchVariants(PassSemantic.DeferredTexturing);
            for (var i = 0; i < dispatchVariants.Length; i++)
            {
                var v = (uint)dispatchVariants[i].DenseIndex;
                var property = new HiddenDebugClassificationShaderProperties
                {
                    variantTileListIndex = tileListSrv,
                    variantIndex = v,
                    tilesPerRow = passData.tilesPerRow,
                    maxTilesPerVariant = passData.maxTilesPerVariant,
                    renderWidth = passData.renderSize.x,
                    renderHeight = passData.renderSize.y
                };

                renderCtx.SetUserDataWithProperties(in property, target: DataTarget.Graphics);
                renderCtx.ExecuteIndirect(passData.commandSignature, 1, actualIndirectBuf, (ulong)v * INDIRECT_ARGS_STRIDE);
            }
        });
    }

    private void AddTileClassificationPass(RenderGraph rg, Identifier<RGBuffer> visBuffer, Identifier<RGBuffer> visibleMeshlets0, Identifier<RGBuffer> visibleMeshlets1, uint2 screenSize,
        out Identifier<RGBuffer> tileListBuffer, out Identifier<RGBuffer> indirectArgsBuffer)
    {
        var tilesX = (screenSize.x + CLASSIFICATION_TILE_SIZE - 1u) / CLASSIFICATION_TILE_SIZE;
        var tilesY = (screenSize.y + CLASSIFICATION_TILE_SIZE - 1u) / CLASSIFICATION_TILE_SIZE;
        var totalTiles = Math.Max(1u, tilesX * tilesY);
        var maxTilesPerVariant = totalTiles;

        // Build 64-bit mask of active variants that implement PassSemantic.DeferredTexturing
        uint2 deferredVariantMask = default;
        var dispatchVariants = _assetManager.ShaderVariants.GetDispatchVariants(PassSemantic.DeferredTexturing);
        for (var i = 0; i < dispatchVariants.Length; i++)
        {
            var v = (uint)dispatchVariants[i].DenseIndex;
            if (v < 32u)
            {
                deferredVariantMask.x |= (1u << (int)v);
            }
            else if (v < 64u)
            {
                deferredVariantMask.y |= (1u << (int)(v - 32u));
            }
        }

        var countersBuffer = AddClearClassificationCountersPass(rg, MAX_CLASSIFICATION_VARIANTS);
        tileListBuffer = AddTileMaterialClassificationPass(rg, visBuffer, visibleMeshlets0, visibleMeshlets1, countersBuffer, screenSize, maxTilesPerVariant, deferredVariantMask);
        indirectArgsBuffer = AddPrepareDeferredTexturingIndirectArgsPass(rg, countersBuffer, MAX_CLASSIFICATION_VARIANTS);
    }
}
