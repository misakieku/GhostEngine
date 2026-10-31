#ifndef GHOST_RIS_COMMON_HLSL
#define GHOST_RIS_COMMON_HLSL

#include "EngineResources/Shaders/Common.hlsl"
#include "EngineResources/Shaders/Utilities/SpaceTransforms.hlsl"

struct RisLightSample
{
    uint lightIndex;
    float weight;
};

// Packs light sample into uint2 for compact texture storage (R32G32_UInt)
uint2 PackRisLightSample(uint lightIndex, float weight)
{
    return uint2(lightIndex, asuint(weight));
}

// Unpacks light sample from uint2
RisLightSample UnpackRisLightSample(uint2 packedSample)
{
    RisLightSample s;
    s.lightIndex = packedSample.x;
    s.weight = asfloat(packedSample.y);
    return s;
}

// Reconstructs Camera-Relative World Space position and normalized view vector from device depth
float3 ReconstructPositionRWS(uint2 pixelCoord, float depth, out float zView, out float2 ndc)
{
    float n = g_ViewData.nearClip;
    float f = g_ViewData.farClip;
    zView = (n * f) / ((f - n) * depth + n);

    float m00 = g_ViewData.projectionMatrix[0][0];
    float m11 = g_ViewData.projectionMatrix[1][1];
    ndc = float2(
        ((float(pixelCoord.x) + 0.5f) * g_ViewData.screenSize.z) * 2.0f - 1.0f,
        1.0f - ((float(pixelCoord.y) + 0.5f) * g_ViewData.screenSize.w) * 2.0f
    );
    float3 posVS = float3((ndc.x / m00) * zView, (ndc.y / m11) * zView, zView);
    return TransformViewToWorld(posVS);
}

float Luminance(float3 rgb)
{
    return dot(rgb, float3(0.2126f, 0.7152f, 0.0722f));
}

#endif // GHOST_RIS_COMMON_HLSL
