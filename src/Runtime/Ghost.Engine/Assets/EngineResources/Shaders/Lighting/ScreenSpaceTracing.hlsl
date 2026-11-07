#ifndef GHOST_SCREEN_SPACE_TRACING_HLSL
#define GHOST_SCREEN_SPACE_TRACING_HLSL

#include "EngineResources/Shaders/Common.hlsl"
#include "EngineResources/Shaders/Properties.hlsl"
#include "EngineResources/Shaders/Utilities/Random.hlsl"
#include "EngineResources/Shaders/Utilities/SpaceTransforms.hlsl"

// Marches a micro-contact ray through the full-resolution depth buffer.
// Bridges the geometric gap between full-res rendered geometry and shadow maps/proxy BLAS.
bool TraceScreenContactShadow(float3 positionRWS, float3 normalWS, uint2 pixelCoord, TEXTURE2D depthTextureIndex, float3 rayDirWS, float maxDistance, float compareToleranceScale, out float hitDistance, out float traversedDistance)
{
    hitDistance = 0.0f;
    traversedDistance = 0.0f;

    if (!IS_VALID_TEXTURE(depthTextureIndex) || maxDistance <= 0.0f)
    {
        return false;
    }

    float NdotL = saturate(dot(normalWS, rayDirWS));
    if (NdotL <= 0.0f)
    {
        return false;
    }

    float contactDist = maxDistance;

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

    if (pixelDist < 1.0f)
    {
        return false;
    }

    const int NUM_STEPS = 16;
    float step = 1.0f / float(NUM_STEPS);
    // Ensure the first evaluation sample is outside the origin pixel cell without skipping large portions of the ray
    float minStepTime = min(1.0f / max(pixelDist, 1.0f), step * 0.5f);
    step = (1.0f - minStepTime) / float(NUM_STEPS);

    // View-space depth derivative for Device-Z tolerance over the entire contact shadow volume
    float3 rayOriginVS = TransformWorldToView(rayOriginWS);
    float4 rayDepthClip = TransformViewToHClip(rayOriginVS + float3(0.0f, 0.0f, contactDist));
    float3 rayDepth = rayDepthClip.xyz / max(rayDepthClip.w, 1e-5f);
    float maxThickness = abs(rayDepth.z - rayStart.z) * compareToleranceScale;

    // Exact starting depth from depth buffer at pixelCoord
    Texture2D<float> depthTex = GET_TEXTURE2D(depthTextureIndex);
    float startDepth = depthTex[pixelCoord];

    // Low-discrepancy dither
    float dither = ConstructFloat(JenkinsHash(pixelCoord));
    float sampleTime = minStepTime + (dither * 0.5f) * step;
    float lastTraversedTime = 0.0f;
    bool completedAllSteps = true;

    [unroll(NUM_STEPS)]
    for (int i = 0; i < NUM_STEPS; ++i)
    {
        float3 samplePos = rayStart + rayStep * sampleTime;
        float2 sampleUV = samplePos.xy * float2(0.5f, -0.5f) + 0.5f;

        // Off-screen boundary check [0, 1]
        if (any(sampleUV < 0.0f) || any(sampleUV > 1.0f))
        {
            completedAllSteps = false;
            break;
        }

        // Point sampled depth on full-resolution depth buffer
        float sampleDepth = SampleTexture2DLevel<float>(depthTextureIndex, PointClampSampler, sampleUV, 0.0f);

        // Skip sky/background and origin pixel
        if (sampleDepth > 0.0f && sampleDepth != startDepth)
        {
            float depthDiff = samplePos.z - sampleDepth;
            // Ray is behind the surface in Reversed-Z, and within the contact shadow depth volume (prevents false hits from distant foreground objects)
            if (depthDiff < -1e-5f && abs(depthDiff) < maxThickness)
            {
                hitDistance = sampleTime * contactDist;
                traversedDistance = hitDistance;
                return true;
            }
        }

        lastTraversedTime = sampleTime;
        sampleTime += step;
    }

    traversedDistance = (completedAllSteps ? 1.0f : saturate(lastTraversedTime)) * contactDist;
    return false;
}

bool TraceScreenContactShadow(float3 positionRWS, float3 normalWS, uint2 pixelCoord, TEXTURE2D depthTextureIndex, float3 rayDirWS, float maxDistance, float compareToleranceScale, out float hitDistance)
{
    float dummyTraversed;
    return TraceScreenContactShadow(positionRWS, normalWS, pixelCoord, depthTextureIndex, rayDirWS, maxDistance, compareToleranceScale, hitDistance, dummyTraversed);
}

#endif // GHOST_SCREEN_SPACE_TRACING_HLSL
