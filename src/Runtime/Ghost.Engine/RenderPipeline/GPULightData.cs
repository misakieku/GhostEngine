using Ghost.Core.Graphics;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.InteropServices;

namespace Ghost.Engine.RenderPipeline;

[GenerateHLSL(PackingRules.Exact, "EngineResources/Shaders/Generated/GPULightData.hlsl")]
public enum GPULightType
{
    Point,
    Spot,
}

[GenerateHLSL(PackingRules.Exact, "EngineResources/Shaders/Generated/GPULightData.hlsl")]
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct GPUPunctualLight
{
    public float3 positionWS;
    public float range;

    public float3 color;
    public uint lightTypeAndFlags;  // bits 0..3: type (0=Point, 1=Spot), bits 4..31: flags

    public float3 directionWS;
    public float spotAngleScale;    // 1.0 / max(0.001, cosInner - cosOuter)

    public float4 size;             // Used by area (X = length or width, Y = height, Z = CosBarnDoorAngle, W = BarnDoorLength) and punctual lights (X = radius)

    public float diffuseDimmer;
    public float specularDimmer;
    public float spotAngleOffset;   // -cosOuter * spotAngleScale
    public float invRangeSq;        // 1.0 / (range * range)

    public float lightDimmer;       // Light intensity scale;
    public float shadowDimmer;      // Shadow intensity scale;
    public float minRoughness;      // This is use to give a small "area" to punctual light, as if we have a light with a radius.
    public float fadeDistance;      // Distance at which the light starts fading out
}

[GenerateHLSL(PackingRules.Exact, "EngineResources/Shaders/Generated/GPULightData.hlsl")]
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct GPUDirectionalLight
{
    public float3 directionWS;
    public uint castShadows;
    public float3 color;
    public float shadowBiasMultiplier;
}

[GenerateHLSL(PackingRules.Exact, "EngineResources/Shaders/Generated/GPULightData.hlsl")]
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct GPUShadowCullingData
{
    [GenerateAsHLSLType("float4", 6)]
    public Frustum.PlaneArray planes;       // Frustum planes for culling
    public float4 size;                     // x: shadow atlas size, y: atlas texel size, z: tile size, w: tile texel size
    public float lodErrorThreshold;         // Coarse LOD error threshold for this shadow view
}

[GenerateHLSL(PackingRules.Exact, "EngineResources/Shaders/Generated/GPULightData.hlsl")]
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct GPUShadowSamplingData
{
    public float4x4 shadowViewProj;         // World to light clip space

    public float4 tileOffsetScale;          // xy: normalized atlas offset [0..1], zw: normalized atlas size [0..1]
    public float4 shadowProjection;         // x: proj.m00, y: proj.m11, z: proj.m22, w: proj.m23 (used for depth reconstruction in shader)
    public float4 size;                     // x: shadow atlas size, y: atlas texel size, z: tile size, w: tile texel size

    public float worldTexelSize;            // World space size of a texel in the shadow map
    public float normalBias;                // Normal bias for this shadow view
    public float slopeBias;                 // Slope based depth bias for this shadow view
}

[GenerateHLSL(PackingRules.Exact, "EngineResources/Shaders/Generated/GPULightData.hlsl")]
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct GPUShadowViewData
{
    public GPUShadowCullingData cullingData;
    public GPUShadowSamplingData samplingData;
}

public struct ShadowData
{
    public float nearPlane;
    public float fadeDistance;
    public float lodErrorScale;
    public float normalBias;
    public float depthBias;
}

public struct PunctualLightRequest
{
    public GPUPunctualLight light;
    public ShadowData shadowData;
}
