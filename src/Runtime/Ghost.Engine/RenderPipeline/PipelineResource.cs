using Ghost.Core;
using Ghost.Engine.Streaming;
using Ghost.Graphics;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;

namespace Ghost.Engine.RenderPipeline;

internal partial class MeshPipelineResource : IPipelineResource
{
    [ResolveAsset("EngineResources/Shaders/MeshPipeline/MeshletCullGraph")]
    public IAssetEntry cullWorkGraphEntry = null!;

    [ResolveAsset("EngineResources/Shaders/MeshPipeline/OccludedMeshletCull")]
    public Handle<ComputeShader> occludedMeshletCullShader;

    [ResolveAsset("EngineResources/Shaders/MeshPipeline/BuildHZB")]
    public Handle<ComputeShader> buildHZBShader;

    [ResolveAsset("EngineResources/Shaders/MeshPipeline/PrepareMeshletIndirectArgs")]
    public Handle<ComputeShader> prepareIndirectArgsShader;

    [ResolveAsset("EngineResources/Shaders/MeshPipeline/ScatterMeshlets")]
    public Handle<ComputeShader> scatterMeshletsShader;

    [ResolveAsset("EngineResources/Shaders/Blit")]
    public Handle<Shader> blitShader;

    public IWorkGraphProgram? cullWorkGraphProgram;

    public void EnsureWorkGraphProgram(RenderEngine renderEngine)
    {
        if (cullWorkGraphProgram != null)
        {
            return;
        }

        if (cullWorkGraphEntry is WorkGraphAssetEntry wgEntry && !wgEntry.Bytecode.IsEmpty)
        {
            cullWorkGraphProgram = renderEngine.GraphicsEngine.CreateWorkGraphProgram(wgEntry.Bytecode, "MeshletCullGraph");
        }
    }

    partial void OnDisposing()
    {
        cullWorkGraphProgram?.Dispose();
        cullWorkGraphProgram = null;
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

    [ResolveAsset("EngineResources/Shaders/DebugClassification")]
    public Handle<Shader> debugClassificationShader;

    [ResolveAsset("EngineResources/Shaders/MaterialPipeline/ExportVisibilityDepth")]
    public Handle<ComputeShader> exportVisibilityDepthShader;

    [ResolveAsset("EngineResources/Shaders/MaterialPipeline/ClearVisibilityBuffer")]
    public Handle<ComputeShader> clearVisibilityBufferShader;
}
