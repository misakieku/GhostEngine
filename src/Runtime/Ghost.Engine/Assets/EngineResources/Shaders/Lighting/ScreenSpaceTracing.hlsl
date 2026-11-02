#ifndef GHOST_SCREEN_SPACE_TRACING_HLSL
#define GHOST_SCREEN_SPACE_TRACING_HLSL

#include "EngineResources/Shaders/Common.hlsl"
#include "EngineResources/Shaders/Properties.hlsl"
#include "EngineResources/Shaders/Utilities/Random.hlsl"
#include "EngineResources/Shaders/Utilities/SpaceTransforms.hlsl"

// Marches a micro-contact ray through the full-resolution depth buffer.
// Specifically tuned to bridge the geometric gap between full-res rendered meshlets
// and simplified RayTracingProxy BLAS geometry (3-5 cm scale) without terminator acne.
bool TraceScreenContactShadow(float3 positionRWS, float3 normalWS, uint2 pixelCoord, TEXTURE2D depthTextureIndex, float3 rayDirWS, float maxDistance, float compareToleranceScale, out float hitDistance)
{
    hitDistance = 0.0f;

    if (!IS_VALID_TEXTURE(depthTextureIndex) || maxDistance <= 0.0f)
    {
        return false;
    }

    float NdotL = saturate(dot(normalWS, rayDirWS));
    if (NdotL <= 0.0f)
    {
        return false;
    }

    // Clamp contact ray length to micro-scale (max 4cm) to strictly bridge the proxy mesh gap
    float contactDist = min(maxDistance, 0.04f);

    // Dynamic normal bias: small bias to lift the origin off the receiver surface
    float normalBias = 0.001f + 0.002f * (1.0f - saturate(NdotL));
    float3 rayOriginWS = positionRWS + normalWS * normalBias;
    float3 rayEndWS = rayOriginWS + rayDirWS * contactDist;

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

    // Screen-space pixel travel distance
    float2 rayStepPixels = (rayEnd.xy - rayStart.xy) * 0.5f * g_ViewData.screenSize.xy;
    float pixelDist = length(rayStepPixels);

    // TODO: Expose configurable parameters (contact shadow distance, normal bias, dynamic depth-scaling, and smooth distance fade)
    // Reject rays that project to less than 1.5 pixels on screen
    if (pixelDist < 1.5f)
    {
        return false;
    }

    // Analytical step-out: ensure the first evaluation sample is completely outside the origin pixel cell
    float minStepTime = min(1.5f / pixelDist, 0.5f);

    // View-space depth derivative for Device-Z tolerance
    float3 rayOriginVS = TransformWorldToView(rayOriginWS);
    float4 rayDepthClip = TransformViewToHClip(rayOriginVS + float3(0.0f, 0.0f, contactDist));
    float3 rayDepth = rayDepthClip.xyz / max(rayDepthClip.w, 1e-5f);

    const int NUM_STEPS = 8;
    float step = (1.0f - minStepTime) / float(NUM_STEPS);
    float compareTolerance = abs(rayDepth.z - rayStart.z) * (1.0f / float(NUM_STEPS)) * compareToleranceScale;

    // Sample starting depth on full-resolution depth buffer (point sampled)
    float2 startUV = rayStart.xy * float2(0.5f, -0.5f) + 0.5f;
    float startDepth = SampleTexture2DLevel<float>(depthTextureIndex, PointClampSampler, startUV, 0.0f);

    // Low-discrepancy dither
    float dither = ConstructFloat(JenkinsHash(pixelCoord));
    float sampleTime = minStepTime + (dither * 0.5f) * step;

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

        // Bilinear and point filtered depth: picking min in Reversed-Z selects the deeper surface,
        // smoothing staircase pixel boundaries on curved geometry (matching UE5 HZB_TRACE_USE_BILINEAR_SAMPLED_DEPTH).
        float pointDepth = SampleTexture2DLevel<float>(depthTextureIndex, PointClampSampler, sampleUV, 0.0f);
        float linearDepth = SampleTexture2DLevel<float>(depthTextureIndex, LinearClampSampler, sampleUV, 0.0f);
        float sampleDepth = min(pointDepth, linearDepth);

        // Skip sky/background (sampleDepth <= 0.0f in reversed-Z) and origin pixel
        if (sampleDepth > 0.0f && abs(sampleDepth - startDepth) > 0.00005f)
        {
            float depthDiff = samplePos.z - sampleDepth;
            // Ray is behind the surface by less than compareTolerance in Reversed-Z
            if (depthDiff < 0.0f && abs(depthDiff) < compareTolerance)
            {
                hitDistance = sampleTime * contactDist;
                return true;
            }
        }

        sampleTime += step;
    }

    return false;
}

#endif // GHOST_SCREEN_SPACE_TRACING_HLSL
