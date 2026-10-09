using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Engine.ShaderProperties;
using Ghost.Engine.Streaming;
using Ghost.Graphics;
using Ghost.Graphics.Core;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Engine.RenderPipeline;

internal partial class GhostRenderPipeline
{
    private const uint CLASSIFICATION_TILE_SIZE = 16u;
    private const uint MAX_CLASSIFICATION_VARIANTS = 256u;
    private const uint CLASSIFICATION_COUNTER_BUFFER_SIZE = 1028u; // 4 bytes total count + 256 * 4 bytes variant counts
    private const uint INDIRECT_ARGS_STRIDE = 16u;

    private Handle<GPUBuffer> _variantShadingModelsBuffer;
    private uint _variantShadingModelsBufferIndex;

    private unsafe void InitializeClassification(RenderEngine renderEngine, AssetManager assetManager)
    {
        var desc = new BufferDesc
        {
            Size = MAX_CLASSIFICATION_VARIANTS * sizeof(uint),
            Stride = sizeof(uint),
            Usage = BufferUsage.Raw | BufferUsage.ShaderResource,
            HeapType = HeapType.Upload
        };

        _variantShadingModelsBuffer = renderEngine.GraphicsEngine.ResourceAllocator.CreateBuffer(in desc, "VariantShadingModelsBuffer");
        var pMapped = (uint*)renderEngine.GraphicsEngine.ResourceDatabase.MapResource(_variantShadingModelsBuffer.AsResource(), 0, null);
        for (var i = 0; i < MAX_CLASSIFICATION_VARIANTS; i++)
        {
            pMapped[i] = (i < assetManager.ShaderVariants.Count)
                ? assetManager.ShaderVariants.GetVariant(new ShaderVariantIndex(i)).ShadingModelId
                : 0u;
        }
        renderEngine.GraphicsEngine.ResourceDatabase.UnmapResource(_variantShadingModelsBuffer.AsResource(), 0, null);
        _variantShadingModelsBufferIndex = renderEngine.GraphicsEngine.ResourceDatabase.GetBindlessIndex(_variantShadingModelsBuffer.AsResource(), BindlessAccess.ShaderResource);
    }

    private void DisposeClassification()
    {
        if (_variantShadingModelsBuffer.IsValid)
        {
            _renderEngine.GraphicsEngine.ResourceDatabase.ReleaseResource(_variantShadingModelsBuffer.AsResource());
            _variantShadingModelsBuffer = Handle<GPUBuffer>.Invalid;
            _variantShadingModelsBufferIndex = 0;
        }
    }

    private struct ClearClassificationCountersPassData
    {
        public Identifier<RGBuffer> countersBuffer;
        public Handle<ComputeShader> shader;
        public uint maxVariants;
    }

    private struct TileMaterialClassificationPassData
    {
        public Identifier<RGTexture> visBuffer;
        public Identifier<RGBuffer> visibleMeshletsPass1;
        public Identifier<RGBuffer> visibleMeshletsPass2;
        public Identifier<RGBuffer> counterBuffer;
        public Identifier<RGBuffer> unbinnedTiles;
        public Identifier<RGBuffer> tileShadingModelMaskBuffer;
        public Handle<ComputeShader> shader;
        public uint2 renderSize;
        public uint tilesPerRow;
        public uint maxUnbinnedEntries;
        public uint4 deferredMask0;
        public uint4 deferredMask1;
        public uint variantShadingModelsTableBufferIndex;
        public uint2 dispatchGroups;
    }

    private struct PrepareDeferredTexturingIndirectArgsPassData
    {
        public Identifier<RGBuffer> counterBuffer;
        public Identifier<RGBuffer> indirectArgsBuffer;
        public Identifier<RGBuffer> binOffsetsBuffer;
        public Identifier<RGBuffer> binScatterCounters;
        public Handle<ComputeShader> shader;
        public uint maxVariants;
    }

    private struct ScatterVariantTilesPassData
    {
        public Identifier<RGBuffer> unbinnedTilesBuffer;
        public Identifier<RGBuffer> binnedTileListBuffer;
        public Identifier<RGBuffer> binScatterCounters;
        public Identifier<RGBuffer> counterBuffer;
        public Handle<ComputeShader> shader;
        public uint maxTileEntries;
    }

    private Identifier<RGBuffer> AddClearClassificationCountersPass(RenderGraph rg, uint maxVariants)
    {
        using var builder = rg.AddComputeRenderPass<ClearClassificationCountersPassData>("ClearClassificationCounters");

        var countersDesc = new BufferDesc
        {
            Size = CLASSIFICATION_COUNTER_BUFFER_SIZE,
            Stride = 4,
            Usage = BufferUsage.Raw | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource
        };
        var countersBuffer = builder.CreateBuffer(in countersDesc, "ClassificationCounterBuffer");
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

    private Identifier<RGBuffer> AddTileMaterialClassificationPass(RenderGraph rg, Identifier<RGTexture> visBuffer, Identifier<RGBuffer> visibleMeshlets0, Identifier<RGBuffer> visibleMeshlets1, Identifier<RGBuffer> countersBuffer,
        uint2 screenSize, uint maxUnbinnedEntries, uint4 deferredMask0, uint4 deferredMask1, out Identifier<RGBuffer> tileShadingModelMaskBuffer)
    {
        var tilesX = (screenSize.x + CLASSIFICATION_TILE_SIZE - 1u) / CLASSIFICATION_TILE_SIZE;
        var tilesY = (screenSize.y + CLASSIFICATION_TILE_SIZE - 1u) / CLASSIFICATION_TILE_SIZE;
        var totalTiles = Math.Max(1u, tilesX * tilesY);

        using var builder = rg.AddComputeRenderPass<TileMaterialClassificationPassData>("TileMaterialClassification");

        var unbinnedTilesDesc = new BufferDesc
        {
            Size = maxUnbinnedEntries * 8u, // UnbinnedTileEntry { uint tileIndex; uint variantIndex; } = 8 bytes
            Stride = 4,
            Usage = BufferUsage.Raw | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource
        };
        var unbinnedTilesBuffer = builder.CreateBuffer(in unbinnedTilesDesc, "UnbinnedTileEntries");

        var maskBufferDesc = new BufferDesc
        {
            Size = totalTiles * 4u,
            Stride = 4,
            Usage = BufferUsage.Raw | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource
        };
        tileShadingModelMaskBuffer = builder.CreateBuffer(in maskBufferDesc, "TileShadingModelMaskBuffer");

        builder.UseTexture(visBuffer, AccessFlags.Read);
        builder.UseBuffer(visibleMeshlets0, AccessFlags.Read);
        builder.UseBuffer(visibleMeshlets1, AccessFlags.Read);
        builder.UseBuffer(countersBuffer, AccessFlags.ReadWrite);
        builder.UseBuffer(unbinnedTilesBuffer, AccessFlags.Write);
        builder.UseBuffer(tileShadingModelMaskBuffer, AccessFlags.Write);

        builder.SetPassData(new TileMaterialClassificationPassData
        {
            visBuffer = visBuffer,
            visibleMeshletsPass1 = visibleMeshlets0,
            visibleMeshletsPass2 = visibleMeshlets1,
            counterBuffer = countersBuffer,
            unbinnedTiles = unbinnedTilesBuffer,
            tileShadingModelMaskBuffer = tileShadingModelMaskBuffer,
            shader = _materialPipelineResource.tileMaterialClassificationShader,
            renderSize = screenSize,
            tilesPerRow = tilesX,
            maxUnbinnedEntries = maxUnbinnedEntries,
            deferredMask0 = deferredMask0,
            deferredMask1 = deferredMask1,
            variantShadingModelsTableBufferIndex = _variantShadingModelsBufferIndex,
            dispatchGroups = new uint2(tilesX, tilesY)
        });

        builder.SetRenderFunc<TileMaterialClassificationPassData>(static (ref readonly passData, computeCtx) =>
        {
            var visSrv = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualTexture(passData.visBuffer).AsResource(), BindlessAccess.ShaderResource);
            var visibleMeshletsPass1Srv = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.visibleMeshletsPass1).AsResource(), BindlessAccess.ShaderResource);
            var visibleMeshletsPass2Srv = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.visibleMeshletsPass2).AsResource(), BindlessAccess.ShaderResource);
            var counterUav = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.counterBuffer).AsResource(), BindlessAccess.UnorderedAccess);
            var unbinnedUav = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.unbinnedTiles).AsResource(), BindlessAccess.UnorderedAccess);
            var maskUav = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.tileShadingModelMaskBuffer).AsResource(), BindlessAccess.UnorderedAccess);

            var props = new InternalTileMaterialClassificationShaderProperties
            {
                deferredVariantMask0 = passData.deferredMask0,
                deferredVariantMask1 = passData.deferredMask1,
                visBufferIndex = visSrv,
                visibleMeshletsPass1 = visibleMeshletsPass1Srv,
                visibleMeshletsPass2 = visibleMeshletsPass2Srv,
                counterBufferUav = counterUav,
                unbinnedTilesUav = unbinnedUav,
                tileShadingModelMaskBufferUav = maskUav,
                variantShadingModelsTableBufferIndex = passData.variantShadingModelsTableBufferIndex,
                renderWidth = passData.renderSize.x,
                renderHeight = passData.renderSize.y,
                tilesPerRow = passData.tilesPerRow,
                maxUnbinnedEntries = passData.maxUnbinnedEntries
            };

            computeCtx.SetActiveCompute(passData.shader, 0);
            computeCtx.SetUserDataWithProperties(in props);
            computeCtx.DispatchCompute(passData.dispatchGroups.x, passData.dispatchGroups.y, 1);
        });

        return unbinnedTilesBuffer;
    }

    private void AddPrepareDeferredTexturingIndirectArgsPass(RenderGraph rg, Identifier<RGBuffer> countersBuffer, uint maxVariants,
        out Identifier<RGBuffer> indirectArgsBuffer, out Identifier<RGBuffer> binOffsetsBuffer, out Identifier<RGBuffer> binScatterCounters)
    {
        using var builder = rg.AddComputeRenderPass<PrepareDeferredTexturingIndirectArgsPassData>("PrepareDeferredTexturingIndirectArgs");

        var indirectArgsDesc = new BufferDesc
        {
            Size = maxVariants * INDIRECT_ARGS_STRIDE,
            Stride = 4,
            Usage = BufferUsage.IndirectArgument | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource
        };
        indirectArgsBuffer = builder.CreateBuffer(in indirectArgsDesc, "DeferredTexturingIndirectArgs");

        var binOffsetsDesc = new BufferDesc
        {
            Size = maxVariants * 4u,
            Stride = 4,
            Usage = BufferUsage.Raw | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource
        };
        binOffsetsBuffer = builder.CreateBuffer(in binOffsetsDesc, "VariantTileBinOffsets");

        var binScatterCountersDesc = new BufferDesc
        {
            Size = maxVariants * 4u,
            Stride = 4,
            Usage = BufferUsage.Raw | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource
        };
        binScatterCounters = builder.CreateBuffer(in binScatterCountersDesc, "VariantTileBinScatterCounters");

        builder.UseBuffer(countersBuffer, AccessFlags.ReadWrite);
        builder.UseBuffer(indirectArgsBuffer, AccessFlags.Write);
        builder.UseBuffer(binOffsetsBuffer, AccessFlags.Write);
        builder.UseBuffer(binScatterCounters, AccessFlags.Write);

        builder.SetPassData(new PrepareDeferredTexturingIndirectArgsPassData
        {
            counterBuffer = countersBuffer,
            indirectArgsBuffer = indirectArgsBuffer,
            binOffsetsBuffer = binOffsetsBuffer,
            binScatterCounters = binScatterCounters,
            shader = _materialPipelineResource.prepareDeferredTexturingIndirectArgsShader,
            maxVariants = maxVariants
        });

        builder.SetRenderFunc<PrepareDeferredTexturingIndirectArgsPassData>(static (ref readonly passData, computeCtx) =>
        {
            var counterUav = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.counterBuffer).AsResource(), BindlessAccess.UnorderedAccess);
            var indirectUav = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.indirectArgsBuffer).AsResource(), BindlessAccess.UnorderedAccess);
            var binOffsetsUav = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.binOffsetsBuffer).AsResource(), BindlessAccess.UnorderedAccess);
            var binCountersUav = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.binScatterCounters).AsResource(), BindlessAccess.UnorderedAccess);

            var props = new InternalPrepareDeferredTexturingIndirectArgsShaderProperties
            {
                counterBuffer = counterUav,
                indirectArgsBuffer = indirectUav,
                binOffsetsBuffer = binOffsetsUav,
                binScatterCounters = binCountersUav,
                maxVariants = passData.maxVariants
            };

            computeCtx.SetActiveCompute(passData.shader, 0);
            computeCtx.SetUserDataWithProperties(in props);
            computeCtx.DispatchCompute(1, 1, 1);
        });
    }

    private Identifier<RGBuffer> AddScatterVariantTilesPass(RenderGraph rg, Identifier<RGBuffer> unbinnedTilesBuffer, Identifier<RGBuffer> binScatterCounters, Identifier<RGBuffer> counterBuffer, uint maxTileEntries)
    {
        using var builder = rg.AddComputeRenderPass<ScatterVariantTilesPassData>("ScatterVariantTiles");

        var binnedTileListDesc = new BufferDesc
        {
            Size = maxTileEntries * 4u,
            Stride = 4,
            Usage = BufferUsage.Raw | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource
        };
        var binnedTileListBuffer = builder.CreateBuffer(in binnedTileListDesc, "BinnedVariantTileList");

        builder.UseBuffer(unbinnedTilesBuffer, AccessFlags.Read);
        builder.UseBuffer(binnedTileListBuffer, AccessFlags.Write);
        builder.UseBuffer(binScatterCounters, AccessFlags.ReadWrite);
        builder.UseBuffer(counterBuffer, AccessFlags.Read);

        builder.SetPassData(new ScatterVariantTilesPassData
        {
            unbinnedTilesBuffer = unbinnedTilesBuffer,
            binnedTileListBuffer = binnedTileListBuffer,
            binScatterCounters = binScatterCounters,
            counterBuffer = counterBuffer,
            shader = _materialPipelineResource.scatterVariantTilesShader,
            maxTileEntries = maxTileEntries
        });

        builder.SetRenderFunc<ScatterVariantTilesPassData>(static (ref readonly passData, computeCtx) =>
        {
            var unbinnedSrv = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.unbinnedTilesBuffer).AsResource(), BindlessAccess.ShaderResource);
            var binnedUav = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.binnedTileListBuffer).AsResource(), BindlessAccess.UnorderedAccess);
            var countersUav = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.binScatterCounters).AsResource(), BindlessAccess.UnorderedAccess);
            var counterBufUav = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.counterBuffer).AsResource(), BindlessAccess.UnorderedAccess);

            var props = new InternalScatterVariantTilesShaderProperties
            {
                unbinnedTilesBuffer = unbinnedSrv,
                binnedTileListBuffer = binnedUav,
                binScatterCounters = countersUav,
                counterBuffer = counterBufUav,
                maxTileEntries = passData.maxTileEntries
            };

            computeCtx.SetActiveCompute(passData.shader, 0);
            computeCtx.SetUserDataWithProperties(in props);

            var threadGroups = Math.Max(1u, (passData.maxTileEntries + 63u) / 64u);
            computeCtx.DispatchCompute(threadGroups, 1, 1);
        });

        return binnedTileListBuffer;
    }

    private void AddTileClassificationPass(RenderGraph rg, Identifier<RGTexture> visBuffer, Identifier<RGBuffer> visibleMeshlets0, Identifier<RGBuffer> visibleMeshlets1, uint2 screenSize,
        out Identifier<RGBuffer> binnedTileListBuffer, out Identifier<RGBuffer> tileOffsetsBuffer, out Identifier<RGBuffer> indirectArgsBuffer, out Identifier<RGBuffer> tileShadingModelMaskBuffer)
    {
        // TODO: For Dynamic Resolution Scaling (DRS), size UnbinnedTileEntries and BinnedVariantTileList using the maximum
        // resolution (window/backbuffer size) rather than dynamic screenSize so that BufferDesc.Size remains constant and does
        // not invalidate the render graph hash each frame. Passes will still dispatch using dynamic viewport tile counts.
        var tilesX = (screenSize.x + CLASSIFICATION_TILE_SIZE - 1u) / CLASSIFICATION_TILE_SIZE;
        var tilesY = (screenSize.y + CLASSIFICATION_TILE_SIZE - 1u) / CLASSIFICATION_TILE_SIZE;
        var totalTiles = Math.Max(1u, tilesX * tilesY);
        var maxTileEntries = totalTiles * 4u;

        // Build 256-bit mask of active variants that implement PassSemantic.DeferredTexturing
        uint4 deferredMask0 = default;
        uint4 deferredMask1 = default;
        var dispatchVariants = _assetManager.ShaderVariants.GetDispatchVariants(PassSemantic.DeferredTexturing);
        for (var i = 0; i < dispatchVariants.Length; i++)
        {
            var v = (uint)dispatchVariants[i].DenseIndex;
            if (v < 128u)
            {
                var elem = (int)(v / 32u);
                var bit = 1u << (int)(v % 32u);
                switch (elem)
                {
                    case 0: deferredMask0.x |= bit; break;
                    case 1: deferredMask0.y |= bit; break;
                    case 2: deferredMask0.z |= bit; break;
                    case 3: deferredMask0.w |= bit; break;
                }
            }
            else if (v < 256u)
            {
                var rel = v - 128u;
                var elem = (int)(rel / 32u);
                var bit = 1u << (int)(rel % 32u);
                switch (elem)
                {
                    case 0: deferredMask1.x |= bit; break;
                    case 1: deferredMask1.y |= bit; break;
                    case 2: deferredMask1.z |= bit; break;
                    case 3: deferredMask1.w |= bit; break;
                }
            }
        }

        var countersBuffer = AddClearClassificationCountersPass(rg, MAX_CLASSIFICATION_VARIANTS);
        var unbinnedTilesBuffer = AddTileMaterialClassificationPass(rg, visBuffer, visibleMeshlets0, visibleMeshlets1, countersBuffer, screenSize, maxTileEntries, deferredMask0, deferredMask1, out tileShadingModelMaskBuffer);
        AddPrepareDeferredTexturingIndirectArgsPass(rg, countersBuffer, MAX_CLASSIFICATION_VARIANTS, out indirectArgsBuffer, out tileOffsetsBuffer, out var binScatterCounters);
        binnedTileListBuffer = AddScatterVariantTilesPass(rg, unbinnedTilesBuffer, binScatterCounters, countersBuffer, maxTileEntries);
    }
}
