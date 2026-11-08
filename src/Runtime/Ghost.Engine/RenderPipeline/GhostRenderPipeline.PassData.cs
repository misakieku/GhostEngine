using Ghost.Core;
using Ghost.Graphics.RenderGraphModule;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Engine.RenderPipeline;

internal partial class GhostRenderPipeline
{
    private struct GeometryVisibilityData
    {
        public Identifier<RGTexture> visBuffer;
        public Identifier<RGBuffer> visibleMeshlets0;
        public Identifier<RGBuffer> visibleMeshlets1;
    }

    private struct TileClassificationData
    {
        public Identifier<RGBuffer> tileListBuffer;
        public Identifier<RGBuffer> tileOffsetsBuffer;
        public Identifier<RGBuffer> indirectArgsBuffer;
        public Identifier<RGBuffer> tileShadingModelMaskBuffer;
    }

    private struct GBufferPassData
    {
        public GBufferResources gbuffer;
        public Identifier<RGBuffer> tileLightList;
    }

    private struct ShadowMapPassData
    {
        public Identifier<RGTexture> shadowAtlas;
        public uint shadowViewsBufferSrv;
        public uint shadowIndicesBufferSrv;
        public Identifier<RGTexture> directionalShadowAtlas;
        public uint directionalShadowViewsBufferSrv;
        public uint cascadeCount;
        public float4 cascadeSplits;
        public float4 cascadeBlends;
        public float maxShadowDistance;
        public float shadowFadeRange;
    }

    private struct RisPassData
    {
        public Identifier<RGTexture> resolvedDiffuse;
        public Identifier<RGTexture> resolvedSpecular;
    }
}
