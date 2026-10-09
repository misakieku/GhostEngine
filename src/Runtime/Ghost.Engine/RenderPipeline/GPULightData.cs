using Ghost.Core.Graphics;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.InteropServices;

namespace Ghost.Engine.RenderPipeline;

/// <summary>
/// GPU-ready memory layout for punctual (point/spot) lights.
/// </summary>
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

    public float spotAngleOffset;   // -cosOuter * spotAngleScale
    public float invRangeSq;        // 1.0 / (range * range)
    public float sourceRadius;      // Specular GGX normalization / contact shadow radius
    public float normalBias;        // normal bias for this shadow view
    public float depthBias;         // depth bias for this shadow view
    public float fadeDistance;      // Distance at which the light starts fading out
}

/// <summary>
/// GPU-ready memory layout for the primary directional light (Sun/Moon).
/// </summary>
[GenerateHLSL(PackingRules.Exact, "EngineResources/Shaders/Generated/GPULightData.hlsl")]
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct GPUDirectionalLight
{
    public float3 directionWS;
    public uint castShadows;
    public float3 color;
    public float shadowBiasMultiplier;
}

/// <summary>
/// GPU-ready memory layout for an individual shadow view (spot light or cubemap face).
/// </summary>
[GenerateHLSL(PackingRules.Exact, "EngineResources/Shaders/Generated/GPULightData.hlsl")]
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct GPUShadowViewData
{
    public float4x4 shadowViewProj;         // World to light clip space
    [GenerateAsHLSLType("float4", 6)]
    public Frustum.PlaneArray planes;       //  Frustum planes for culling
    public float4 tileOffsetScale;          //  xy: normalized atlas offset [0..1], zw: normalized atlas size [0..1]
    public float3 lightPositionWS;          //  for distance/LOD calculation
    public float lodErrorThreshold;         // coarse LOD error threshold for this shadow view

    public float proj11;                    // projection[1][1] for LOD
    public float tileSize;                  // shadow tile size in pixels
}

public struct ShadowData
{
    public float nearPlane;
    public float fadeDistance;
}

public struct PunctualLightRequest
{
    public GPUPunctualLight light;
    public ShadowData shadowData;
}