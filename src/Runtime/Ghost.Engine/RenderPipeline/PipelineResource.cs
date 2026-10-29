using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Engine.Streaming;
using Ghost.Graphics;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;

namespace Ghost.Engine.RenderPipeline;

internal partial class MeshPipelineResource : IPipelineResource
{
    [ResolveAsset("EngineResources/Shaders/MeshPipeline/MeshletCullGraph")]
    public IAssetEntry cullWorkGraphEntry = null!;

    [ResolveAsset("EngineResources/Shaders/MeshPipeline/ShadowCullGraph")]
    public IAssetEntry shadowCullWorkGraphEntry = null!;

    [ResolveAsset("EngineResources/Shaders/MeshPipeline/OccludedMeshletCull")]
    public Handle<ComputeShader> occludedMeshletCullShader;

    [ResolveAsset("EngineResources/Shaders/MeshPipeline/BuildHZB")]
    public Handle<ComputeShader> buildHZBShader;

    [ResolveAsset("EngineResources/Shaders/MeshPipeline/PrepareMeshletIndirectArgs")]
    public Handle<ComputeShader> prepareIndirectArgsShader;

    [ResolveAsset("EngineResources/Shaders/MeshPipeline/PrepareShadowIndirectArgs")]
    public Handle<ComputeShader> prepareShadowIndirectArgsShader;

    [ResolveAsset("EngineResources/Shaders/MeshPipeline/ScatterMeshlets")]
    public Handle<ComputeShader> scatterMeshletsShader;

    [ResolveAsset("EngineResources/Shaders/MeshPipeline/RasterizeSw")]
    public Handle<ComputeShader> rasterizeSwShader;

    [ResolveAsset("EngineResources/Shaders/Blit")]
    public Handle<Shader> blitShader;

    public IWorkGraphProgram? cullWorkGraphProgram;
    public IWorkGraphProgram? shadowCullWorkGraphProgram;

    public void EnsureWorkGraphProgram(RenderEngine renderEngine, AssetManager? assetManager = null, KeywordMask128 keywords = default)
    {
        if (assetManager != null)
        {
            if (cullWorkGraphEntry is WorkGraphAssetEntry wg && !wg.Bytecode.IsEmpty)
            {
                cullWorkGraphProgram = assetManager.ShaderVariants.ResolveWorkGraphProgram(cullWorkGraphEntry, keywords);
            }

            if (shadowCullWorkGraphEntry is WorkGraphAssetEntry shadowWg && !shadowWg.Bytecode.IsEmpty)
            {
                shadowCullWorkGraphProgram = assetManager.ShaderVariants.ResolveWorkGraphProgram(shadowCullWorkGraphEntry, keywords);
            }

            return;
        }

        if (cullWorkGraphProgram == null && cullWorkGraphEntry is WorkGraphAssetEntry wgEntry && !wgEntry.Bytecode.IsEmpty)
        {
            cullWorkGraphProgram = renderEngine.GraphicsEngine.CreateWorkGraphProgram(wgEntry.Bytecode, "MeshletCullGraph");
        }

        if (shadowCullWorkGraphProgram == null && shadowCullWorkGraphEntry is WorkGraphAssetEntry shadowWgEntry && !shadowWgEntry.Bytecode.IsEmpty)
        {
            shadowCullWorkGraphProgram = renderEngine.GraphicsEngine.CreateWorkGraphProgram(shadowWgEntry.Bytecode, "ShadowCullGraph");
        }
    }

    partial void OnDisposing()
    {
        cullWorkGraphProgram?.Dispose();
        cullWorkGraphProgram = null;
        shadowCullWorkGraphProgram?.Dispose();
        shadowCullWorkGraphProgram = null;
    }
}

internal partial class MaterialPipelineResource : IPipelineResource
{
    [ResolveAsset("EngineResources/Shaders/MaterialPipeline/TileMaterialClassification")]
    public Handle<ComputeShader> tileMaterialClassificationShader;

    [ResolveAsset("EngineResources/Shaders/MaterialPipeline/ClearClassificationCounters")]
    public Handle<ComputeShader> clearClassificationCountersShader;

    [ResolveAsset("EngineResources/Shaders/MaterialPipeline/PrepareDeferredTexturingIndirectArgs")]
    public Handle<ComputeShader> prepareDeferredTexturingIndirectArgsShader;

    [ResolveAsset("EngineResources/Shaders/MaterialPipeline/ScatterVariantTiles")]
    public Handle<ComputeShader> scatterVariantTilesShader;

    [ResolveAsset("EngineResources/Shaders/MaterialPipeline/ExportVisibilityDepth")]
    public Handle<ComputeShader> exportVisibilityDepthShader;

    [ResolveAsset("EngineResources/Shaders/MaterialPipeline/ClearVisibilityBuffer")]
    public Handle<ComputeShader> clearVisibilityBufferShader;
}

internal partial class LightingPipelineResource : IPipelineResource
{
    [ResolveAsset("EngineResources/Shaders/Lighting/TileLightCulling")]
    public Handle<ComputeShader> tileLightCullingShader;

    [ResolveAsset("EngineResources/Shaders/Lighting/ClearShadowAtlas")]
    public Handle<ComputeShader> clearShadowAtlasShader;

    [ResolveAsset("EngineResources/Shaders/Lighting/RasterizeShadowSw")]
    public Handle<ComputeShader> rasterizeShadowSwShader;

    [ResolveAsset("EngineResources/Shaders/Lighting/ConvolveSkyIrradianceSH")]
    public Handle<ComputeShader> convolveSkyIrradianceShader;

    [ResolveAsset("EngineResources/Shaders/Lighting/HDRISky")]
    public Handle<Shader> hdriSkyShader;

    [ResolveAsset("EngineResources/Textures/PBR")]
    public Handle<GPUTexture> iblLutTexture;
}
