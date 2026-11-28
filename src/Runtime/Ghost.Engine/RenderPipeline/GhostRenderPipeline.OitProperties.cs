using Ghost.Core.Graphics;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.InteropServices;

namespace Ghost.Engine.RenderPipeline;

internal partial class GhostRenderPipeline
{
    [GenerateHLSL(PackingRules.Exact, "EngineResources/Shaders/Generated/GhostRenderPipeline.hlsl")]
    [StructLayout(LayoutKind.Sequential)]
    private struct TransparentPrepassShaderProperties
    {
        public uint visibleMeshletsBufferIndex;
        public uint binOffsetsBufferIndex;
        public uint binIndex;
        public uint scalarExtinctionUav;
        public uint rgbExtinctionUav;
        public uint scalarOverflowMinUav;
        public uint rgbOverflowMinUav;
        public uint depthWarpLutSrv;
        public uint oitWidth;
        public uint oitHeight;
        public uint oitPhysicalSliceCount;
        public uint oitVirtualSliceCount;
        public float oitLinearizationFactor;
        public float oitFarPlane;
        public uint _pad0;
        public uint _pad1;
    }

    [GenerateHLSL(PackingRules.Exact, "EngineResources/Shaders/Generated/GhostRenderPipeline.hlsl")]
    [StructLayout(LayoutKind.Sequential)]
    private struct TransparentForwardShaderProperties
    {
        public uint visibleMeshletsBufferIndex;
        public uint binOffsetsBufferIndex;
        public uint binIndex;
        public uint depthTextureIndex;
        public uint clusterLightGridBufferIndex;
        public uint clusterLightIndexBufferIndex;
        public uint clusterTilesX;
        public uint clusterSliceCount;
        public uint oitIntegralLutSrv;
        public uint oitDepthWarpLutSrv;
        public uint oitWidth;
        public uint oitHeight;
        public uint oitPhysicalSliceCount;
        public uint oitVirtualSliceCount;
        public float oitLinearizationFactor;
        public float oitFarPlane;
        public float oitSampleBias;
        public uint shadowAtlasSrv;
        public uint shadowViewsBufferSrv;
        public uint shadowIndicesBufferSrv;
        public uint skyboxTextureSrv;
        public uint iblLutTextureSrv;
        public uint persistentViewStateBufferSrv;
        public uint viewId;
        public uint skyboxMipCount;
        public float4 skyboxTintAndIntensity;
        public float2 skyboxRotationSinCos;
        public uint directionalShadowAtlasSrv;
        public uint directionalShadowViewsBufferSrv;
        public uint cascadeCount;
        public float maxShadowDistance;
        public float shadowFadeRange;
        public float4 cascadeSplits;
        public float4 cascadeBlends;
        public uint enableContactShadows;
        public float contactShadowDistance;
        public float contactShadowTolerance;
        public uint _pad0;
    }
}
