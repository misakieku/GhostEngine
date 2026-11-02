#ifndef GHOST_SCREEN_SPACE_TRACING_HLSL
#define GHOST_SCREEN_SPACE_TRACING_HLSL

#include "EngineResources/Shaders/Common.hlsl"
#include "EngineResources/Shaders/Properties.hlsl"
#include "EngineResources/Shaders/Utilities/Random.hlsl"
#include "EngineResources/Shaders/Utilities/SpaceTransforms.hlsl"

// Marches a ray through the full-resolution depth buffer using UE5-style screen-space ray casting.
// Uses point sampling depth equality (sampleDepth != startDepth) for zero self-intersection,
// and depth-adaptive tolerance in Device-Z to maintain sharp contacts up-close and avoid false hits at distance.
// Returns true if an on-screen occluder is found within maxDistance.
bool TraceScreenContactShadow(float3 positionRWS, float3 normalWS, uint2 pixelCoord, TEXTURE2D depthTextureIndex, float3 rayDirWS, float maxDistance, float compareToleranceScale, out float hitDistance)
{
    hitDistance = 0.0f;

    if (!IS_VALID_TEXTURE(depthTextureIndex) || maxDistance <= 0.0f)
    {
        return false;
    }

    // Dynamic normal bias: small bias to prevent grazing self-intersections
    float NdotL = saturate(dot(normalWS, rayDirWS));
    float normalBias = 0.0005f + 0.0015f * (1.0f - NdotL);
    float3 rayOriginWS = positionRWS + normalWS * normalBias;
    float3 rayEndWS = rayOriginWS + rayDirWS * maxDistance;

    // Project start and end positions into homogeneous clip space
    float4 rayStartClip = TransformWorldToHClip(rayOriginWS);
    float4 rayEndClip = TransformWorldToHClip(rayEndWS);

    if (rayStartClip.w <= 0.0f)
    {
        return false;
    }

    // Clip ray against near plane if necessary
    float nearPlane = g_ViewData.nearClip;
    if (rayEndClip.w < nearPlane)
    {
        float t = (rayStartClip.w - nearPlane) / max(rayStartClip.w - rayEndClip.w, 1e-5f);
        rayEndClip = lerp(rayStartClip, rayEndClip, saturate(t));
    }

    // Perspective divide to get NDC coordinates [-1, 1] and Reversed-Z [1, 0]
    float3 rayStart = rayStartClip.xyz / rayStartClip.w;
    float3 rayEnd = rayEndClip.xyz / rayEndClip.w;
    float3 rayStep = rayEnd - rayStart;

    // Reject rays that project to less than 1 pixel on screen
    float screenDist = length((rayEnd.xy - rayStart.xy) * 0.5f * g_ViewData.screenSize.xy);
    if (screenDist < 1.0f)
    {
        return false;
    }

    // View-space depth derivative for Device-Z tolerance
    float3 rayOriginVS = TransformWorldToView(rayOriginWS);
    float4 rayDepthClip = TransformViewToHClip(rayOriginVS + float3(0.0f, 0.0f, maxDistance));
    float3 rayDepth = rayDepthClip.xyz / max(rayDepthClip.w, 1e-5f);

    const int NUM_STEPS = 12;
    float step = 1.0f / float(NUM_STEPS);
    float compareTolerance = abs(rayDepth.z - rayStart.z) * step * compareToleranceScale;

    // Sample starting depth on full-resolution depth buffer (point sampled)
    float2 startUV = rayStart.xy * float2(0.5f, -0.5f) + 0.5f;
    float startDepth = SampleTexture2DLevel<float>(depthTextureIndex, PointClampSampler, startUV, 0.0f);

    // Spatial/temporal dither using Interleaved Gradient Noise
    float dither = InterleavedGradientNoise((float2)pixelCoord, 0);
    float stepOffset = dither - 0.5f;
    float sampleTime = stepOffset * step + step;

    [unroll(NUM_STEPS)]
    for (int i = 0; i < NUM_STEPS; ++i)
    {
        float3 samplePos = rayStart + rayStep * sampleTime;
        float2 sampleUV = samplePos.xy * float2(0.5f, -0.5f) + 0.5f;

        // Off-screen boundary check [0, 1]
        if (any(sampleUV < 0.0f) || any(sampleUV > 1.0f))
        {
            break;
        }

        float sampleDepth = SampleTexture2DLevel<float>(depthTextureIndex, PointClampSampler, sampleUV, 0.0f);

        // Avoid self-intersection with the starting pixel (exact comparison from point sampling)
        // Also skip sky/background (sampleDepth <= 0.0f in reversed-Z)
        if (sampleDepth > 0.0f && sampleDepth != startDepth)
        {
            float depthDiff = samplePos.z - sampleDepth;
            if (abs(depthDiff + compareTolerance) < compareTolerance)
            {
                hitDistance = sampleTime * maxDistance;
                return true;
            }
        }

        sampleTime += step;
    }

    return false;
}

#endif // GHOST_SCREEN_SPACE_TRACING_HLSL
