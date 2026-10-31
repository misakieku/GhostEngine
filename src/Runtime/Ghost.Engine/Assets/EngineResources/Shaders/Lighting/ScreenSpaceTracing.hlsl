#ifndef GHOST_SCREEN_SPACE_TRACING_HLSL
#define GHOST_SCREEN_SPACE_TRACING_HLSL

#include "EngineResources/Shaders/Properties.hlsl"
#include "EngineResources/Shaders/Material/Lit/Lit.hlsl"
#include "EngineResources/Shaders/Utilities/Random.hlsl"
#include "EngineResources/Shaders/Generated/GPULightData.hlsl"


// Marches a ray through the full-resolution depth buffer using UE5-style screen-space ray casting.
// Uses point sampling depth equality (sampleDepth != startDepth) for zero self-intersection,
// and depth-adaptive tolerance in Device-Z to maintain sharp contacts up-close and avoid false hits at distance.
// Returns true if an on-screen occluder is found within maxDistance.
bool TraceScreenContactShadow(ShadingContext ctx, float3 rayDirWS, float maxDistance, float compareToleranceScale, out float hitDistance)
{
    hitDistance = 0.0f;

    if (!IS_VALID_BUFFER(ctx.depthTextureIndex) || maxDistance <= 0.0f)
    {
        return false;
    }

    // Dynamic normal bias: small bias to prevent grazing self-intersections
    float NdotL = saturate(dot(ctx.normalWS, rayDirWS));
    float normalBias = 0.0005f + 0.0015f * (1.0f - NdotL);
    float3 rayOriginWS = ctx.positionRWS + ctx.normalWS * normalBias;
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
    float startDepth = SampleTexture2DLevel<float>(ctx.depthTextureIndex, PointClampSampler, startUV, 0.0f);

    // Spatial/temporal dither using Interleaved Gradient Noise
    float dither = InterleavedGradientNoise((float2)ctx.positionSS, 0);
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

        float sampleDepth = SampleTexture2DLevel<float>(ctx.depthTextureIndex, PointClampSampler, sampleUV, 0.0f);

        // Avoid self-intersection with the starting pixel (exact comparison from point sampling)
        // Also skip sky/background (sampleDepth <= 0.0f in reversed-Z)
        if (sampleDepth > 0.0f && sampleDepth != startDepth)
        {
            // In Reversed-Z: closer to camera = higher depth value.
            // If samplePos is behind sampleDepth, samplePos.z < sampleDepth => depthDiff < 0.
            float depthDiff = samplePos.z - sampleDepth;

            // abs(depthDiff + compareTolerance) < compareTolerance is true iff
            // -2 * compareTolerance < depthDiff < 0
            // This tests both: ray is behind surface AND within thickness tolerance!
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

// Marches a ray through the Closest-depth HZB pyramid (.y channel) to find near-field contact occlusions.
// Returns true if a screen-space occluder is found within maxDistance and thickness.
bool TraceScreenHZB(ShadingContext ctx, float3 rayDirWS, float maxDistance, float thickness, out float hitDistance)
{
    hitDistance = 0.0f;

    if (!IS_VALID_BUFFER(ctx.hzbTextureIndex) || ctx.hzbMipCount == 0 || maxDistance <= 0.0f)
    {
        return false;
    }

    if (ctx.hzbBaseSize.x == 0 || ctx.hzbBaseSize.y == 0)
    {
        return false;
    }

    // Dynamic normal bias: larger bias at grazing angles to prevent surface self-shadowing
    float NdotL = saturate(dot(ctx.normalWS, rayDirWS));
    float normalBias = 0.003f + 0.007f * (1.0f - NdotL);
    float3 rayOriginWS = ctx.positionRWS + ctx.normalWS * normalBias;
    float3 rayEndWS = rayOriginWS + rayDirWS * maxDistance;

    // Project start and end positions into homogeneous clip space
    float4 rayStartClip = TransformWorldToHClip(rayOriginWS);
    float4 rayEndClip = TransformWorldToHClip(rayEndWS);

    // If start is behind the near plane, cannot trace
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

    // Convert from clip space to viewport UV [0, 1] and Reversed-Z depth
    float3 rayStartViewport = float3(
        (rayStartClip.xy / rayStartClip.w) * float2(0.5f, -0.5f) + 0.5f,
        rayStartClip.z / rayStartClip.w
    );
    float3 rayEndViewport = float3(
        (rayEndClip.xy / rayEndClip.w) * float2(0.5f, -0.5f) + 0.5f,
        rayEndClip.z / rayEndClip.w
    );

    // Scale viewport UV by hzbUvFactor to map into the active region of the POT-allocated HZB
    float3 rayStartHZB = float3(rayStartViewport.xy * ctx.hzbUvFactor, rayStartViewport.z);
    float3 rayEndHZB = float3(rayEndViewport.xy * ctx.hzbUvFactor, rayEndViewport.z);
    float3 rayDeltaHZB = rayEndHZB - rayStartHZB;

    // Reject rays that project to less than 1 pixel in viewport space
    float screenDist = length((rayEndViewport.xy - rayStartViewport.xy) * g_ViewData.screenSize.xy);
    if (screenDist < 1.0f)
    {
        return false;
    }

    // Clip ray against active HZB bounds [0, ctx.hzbUvFactor] to avoid marching into POT black padding
    float2 tMax = float2(1.0f, 1.0f);
    if (rayDeltaHZB.x > 0.0f) tMax.x = (ctx.hzbUvFactor.x - rayStartHZB.x) / rayDeltaHZB.x;
    else if (rayDeltaHZB.x < 0.0f) tMax.x = -rayStartHZB.x / rayDeltaHZB.x;

    if (rayDeltaHZB.y > 0.0f) tMax.y = (ctx.hzbUvFactor.y - rayStartHZB.y) / rayDeltaHZB.y;
    else if (rayDeltaHZB.y < 0.0f) tMax.y = -rayStartHZB.y / rayDeltaHZB.y;

    float exitT = saturate(min(tMax.x, tMax.y));
    if (exitT <= 0.0f)
    {
        return false;
    }

    rayDeltaHZB *= exitT;
    float effectiveMaxDist = maxDistance * exitT;

    // Calculate step-out time to exit the starting Mip 0 texel (avoids sampling the shading point itself)
    float2 mip0TexelSize = 1.0f / (float2)ctx.hzbBaseSize;
    int2 startTileCoord = int2(floor(rayStartHZB.xy * (float2)ctx.hzbBaseSize));

    float2 planeT = float2(1.0f, 1.0f);
    if (rayDeltaHZB.x > 0.0f) planeT.x = ((float(startTileCoord.x) + 1.0f) * mip0TexelSize.x - rayStartHZB.x) / max(rayDeltaHZB.x, 1e-7f);
    else if (rayDeltaHZB.x < 0.0f) planeT.x = (float(startTileCoord.x) * mip0TexelSize.x - rayStartHZB.x) / min(rayDeltaHZB.x, -1e-7f);

    if (rayDeltaHZB.y > 0.0f) planeT.y = ((float(startTileCoord.y) + 1.0f) * mip0TexelSize.y - rayStartHZB.y) / max(rayDeltaHZB.y, 1e-7f);
    else if (rayDeltaHZB.y < 0.0f) planeT.y = (float(startTileCoord.y) * mip0TexelSize.y - rayStartHZB.y) / min(rayDeltaHZB.y, -1e-7f);

    float stepOutT = saturate(min(planeT.x, planeT.y) + 0.002f);
    if (stepOutT >= 1.0f)
    {
        return false;
    }

    // Interleaved gradient noise for spatial/temporal dither
    float dither = InterleavedGradientNoise((float2)ctx.positionSS, 0);

    const int NUM_STEPS = 16;
    float stepSize = (1.0f - stepOutT) / float(NUM_STEPS);

    float n = g_ViewData.nearClip;
    float f = g_ViewData.farClip;

    float currentT = stepOutT + (dither * 0.75f + 0.125f) * stepSize;

    [unroll(NUM_STEPS)]
    for (int i = 0; i < NUM_STEPS; ++i)
    {
        float3 currentPos = rayStartHZB + rayDeltaHZB * currentT;

        // Ensure we remain strictly inside active HZB bounds
        if (currentPos.x < 0.0f || currentPos.y < 0.0f ||
            currentPos.x >= ctx.hzbUvFactor.x || currentPos.y >= ctx.hzbUvFactor.y)
        {
            break;
        }

        // Contact shadows use Mip 0 for the first 35% of the ray and Mip 1 further out to stay fine-grained
        uint mipLevel = (currentT > 0.35f && ctx.hzbMipCount > 1) ? 1u : 0u;
        float closestDepth = SampleTexture2DLevel<float2>(ctx.hzbTextureIndex, PointClampSampler, currentPos.xy, (float)mipLevel).y;

        // Pull back occluder depth slightly near ray start to avoid grazing self-hits
        float biasedClosestDepth = closestDepth * lerp(0.997f, 1.0f, saturate(currentT * 4.0f));

        // In Reversed-Z: higher depth = closer to camera.
        // If currentPos.z <= biasedClosestDepth, ray is at or behind the sampled closest surface.
        if (currentPos.z <= biasedClosestDepth && closestDepth > 0.0f)
        {
            float rayLinearDepth = (n * f) / max((f - n) * currentPos.z + n, 1e-4f);
            float surfaceLinearDepth = (n * f) / max((f - n) * closestDepth + n, 1e-4f);

            float depthDiff = rayLinearDepth - surfaceLinearDepth;

            // Hit is valid if within thickness tolerance
            if (depthDiff >= 0.0f && depthDiff <= thickness)
            {
                hitDistance = currentT * effectiveMaxDist;
                return true;
            }
        }

        currentT += stepSize;
    }

    return false;
}

// =========================================================================
// Hardware Ray Tracing (DXR 1.1 Inline RayQuery) Shadow Tracing
// =========================================================================

// K: Number of candidate lights randomly sampled and tested for unshadowed contribution
#define RIS_CANDIDATE_LIGHT_COUNT   8u

// Flags for inline shadow ray traversal: opaque, first hit, skip procedural, cull backfaces to avoid self-intersection with coarse proxy geometry
#define SHADOW_RAY_QUERY_FLAGS (RAY_FLAG_SKIP_PROCEDURAL_PRIMITIVES | RAY_FLAG_ACCEPT_FIRST_HIT_AND_END_SEARCH | RAY_FLAG_FORCE_OPAQUE | RAY_FLAG_CULL_BACK_FACING_TRIANGLES)

// Offsets ray origin along surface normal to prevent self-intersection with coarse proxy geometry (UE MegaLights style)
void ApplyPositionBias(inout float3 rayOrigin, float3 rayDirection, float3 worldNormal, float maxNormalBias)
{
    const float minBias = 0.01f;
    const float maxBias = max(minBias, maxNormalBias);
    const float normalBias = lerp(maxBias, minBias, saturate(dot(worldNormal, rayDirection)));
    rayOrigin += worldNormal * normalBias;
}

// Traces an inline ray query against the scene TLAS.
// Uses fast hardware traversal with early out on any committed hit.
// Returns 0.0f if occluded, 1.0f if unoccluded.
float TraceRayQueryShadow(uint tlasIndex, float3 rayOriginWS, float3 rayDirWS, float maxDistance, float tMin)
{
    if (!IS_VALID_BUFFER(tlasIndex) || maxDistance <= tMin)
    {
        return 1.0f;
    }

    RayDesc ray;
    ray.Origin = rayOriginWS;
    ray.Direction = rayDirWS;
    ray.TMin = tMin;
    ray.TMax = maxDistance;

    RaytracingAccelerationStructure sceneTLAS = ResourceDescriptorHeap[tlasIndex];

    RayQuery<SHADOW_RAY_QUERY_FLAGS> q;
    q.TraceRayInline(sceneTLAS, SHADOW_RAY_QUERY_FLAGS, 0xFFu, ray);
    q.Proceed();

    if (q.CommittedStatus() != COMMITTED_NOTHING)
    {
        return 0.0f;
    }

    return 1.0f;
}

// Area-preserving concentric mapping from [0, 1)^2 to the unit disk (Shirley & Chiu 1997).
// Preserves 2D low-discrepancy R2 stratification isotropically in all directions.
float2 SampleConcentricDisk(float2 u)
{
    float2 offset = 2.0f * u - 1.0f;
    if (offset.x == 0.0f && offset.y == 0.0f)
    {
        return float2(0.0f, 0.0f);
    }

    float r;
    float theta;
    if (abs(offset.x) > abs(offset.y))
    {
        r = offset.x;
        theta = 0.7853981634f * (offset.y / offset.x);
    }
    else
    {
        r = offset.y;
        theta = 1.5707963268f - 0.7853981634f * (offset.x / offset.y);
    }

    return r * float2(cos(theta), sin(theta));
}

// Perturbs the shadow ray target within a disc of radius lightRadius facing the receiver.
void ComputeSoftShadowRay(float3 surfacePosWS, float3 lightPosWS, float lightRadius, float2 rngSample, out float3 outRayDirWS, out float outRayDist)
{
    float3 toLight = lightPosWS - surfacePosWS;
    float dist = length(toLight);
    float3 L = toLight / max(dist, 0.0001f);

    if (lightRadius > 0.0001f)
    {
        float2 disc = SampleConcentricDisk(rngSample);

        // Pixar branchless orthonormal basis (Duff et al. 2017) perpendicular to L
        float s = (L.z >= 0.0f) ? 1.0f : -1.0f;
        float a = -1.0f / (s + L.z);
        float b = L.x * L.y * a;
        float3 tangent = float3(1.0f + s * L.x * L.x * a, s * b, -s * L.x);
        float3 bitangent = float3(b, s + L.y * L.y * a, -L.y);

        float3 jitteredLightPosWS = lightPosWS + (tangent * disc.x + bitangent * disc.y) * lightRadius;
        float3 jitteredToLight = jitteredLightPosWS - surfacePosWS;
        outRayDist = length(jitteredToLight);
        outRayDirWS = jitteredToLight / max(outRayDist, 0.0001f);
    }
    else
    {
        outRayDirWS = L;
        outRayDist = dist;
    }
}

// DXR 1.1 Hardware RayQuery against TLAS with adaptive normal bias and soft shadow jitter.
float EvaluatePunctualVisibility_RIS(ShadingContext ctx, GPUPunctualLight light, float3 lightPosRWS, float3 L, float distance, float2 rngSample, float tMin)
{
    float3 surfacePosWS = TransformCameraRelativeToAbsoluteWorld(ctx.positionRWS);
    float3 lightPosWS = light.positionWS;

    float3 rayDirWS;
    float rayDist;
    ComputeSoftShadowRay(surfacePosWS, lightPosWS, light.size.x, rngSample, rayDirWS, rayDist);

    // Apply UE-style normal offset bias to clear coarse proxy mesh mismatch
    float3 rayOriginWS = surfacePosWS;
    ApplyPositionBias(rayOriginWS, rayDirWS, ctx.normalWS, 0.02f);

    return TraceRayQueryShadow(ctx.tlasIndex, rayOriginWS, rayDirWS, rayDist, tMin);
}

#endif // GHOST_SCREEN_SPACE_TRACING_HLSL