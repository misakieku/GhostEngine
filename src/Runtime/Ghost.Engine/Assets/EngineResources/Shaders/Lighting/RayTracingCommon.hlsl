#ifndef GHOST_RAY_TRACING_COMMON_HLSL
#define GHOST_RAY_TRACING_COMMON_HLSL

#include "EngineResources/Shaders/Common.hlsl"
#include "EngineResources/Shaders/Utilities/SpaceTransforms.hlsl"

// Flags for inline shadow ray traversal: opaque, first hit, skip procedural
#define SHADOW_RAY_QUERY_FLAGS (RAY_FLAG_SKIP_PROCEDURAL_PRIMITIVES | RAY_FLAG_ACCEPT_FIRST_HIT_AND_END_SEARCH | RAY_FLAG_FORCE_OPAQUE)

// Offsets ray origin along surface normal to prevent self-intersection with coarse proxy geometry (UE MegaLights style)
void ApplyPositionBias(inout float3 rayOrigin, float3 rayDirection, float3 worldNormal, float maxNormalBias)
{
    const float minBias = 0.01f;
    const float maxBias = max(minBias, maxNormalBias);
    const float normalBias = lerp(maxBias, minBias, saturate(dot(worldNormal, rayDirection)));
    rayOrigin += worldNormal * normalBias;
}

// Traces an inline ray query against the scene TLAS (in camera-relative world space).
// Uses fast hardware traversal with early out on any committed hit.
// Returns 0.0f if occluded, 1.0f if unoccluded.
float TraceRayQueryShadow(uint tlasIndex, float3 rayOriginRWS, float3 rayDirRWS, float maxDistance, float tMin)
{
    if (!IS_VALID_BUFFER(tlasIndex) || maxDistance <= tMin)
    {
        return 1.0f;
    }

    RayDesc ray;
    ray.Origin = rayOriginRWS;
    ray.Direction = rayDirRWS;
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

    float r, theta;
    if (abs(offset.x) > abs(offset.y))
    {
        r = offset.x;
        theta = (3.14159265f / 4.0f) * (offset.y / offset.x);
    }
    else
    {
        r = offset.y;
        theta = (3.14159265f / 2.0f) - (3.14159265f / 4.0f) * (offset.x / offset.y);
    }

    return float2(r * cos(theta), r * sin(theta));
}

// Computes soft-shadow jittered ray origin, direction, and distance for a spherical punctual light in camera-relative coordinates.
void ComputeSoftShadowRay(float3 surfacePosRWS, float3 lightPosRWS, float lightRadius, float2 rngSample, out float3 outRayDirRWS, out float outRayDist)
{
    float3 toLight = lightPosRWS - surfacePosRWS;
    float dist = length(toLight);
    float3 L = toLight / max(dist, 0.0001f);

    if (lightRadius > 0.001f)
    {
        float2 disc = SampleConcentricDisk(rngSample);

        // Orthonormal tangent basis perpendicular to L
        float s = (L.z >= 0.0f) ? 1.0f : -1.0f;
        float a = -1.0f / (s + L.z);
        float b = L.x * L.y * a;
        float3 tangent = float3(1.0f + s * L.x * L.x * a, s * b, -s * L.x);
        float3 bitangent = float3(b, s + L.y * L.y * a, -L.y);

        float3 jitteredLightPosRWS = lightPosRWS + (tangent * disc.x + bitangent * disc.y) * lightRadius;
        float3 jitteredToLight = jitteredLightPosRWS - surfacePosRWS;
        outRayDist = length(jitteredToLight);
        outRayDirRWS = jitteredToLight / max(outRayDist, 0.0001f);
    }
    else
    {
        outRayDirRWS = L;
        outRayDist = dist;
    }
}

// Computes soft-shadow jittered ray direction and max distance for a directional light.
void ComputeDirectionalSoftShadowRay(float3 surfacePosRWS, float3 directionWS, float angularDiameter, float2 rngSample, float maxShadowDist, out float3 outRayDirRWS, out float outRayDist)
{
    float3 L = -directionWS;
    if (angularDiameter > 0.0001f)
    {
        float2 disc = SampleConcentricDisk(rngSample);

        // Orthonormal tangent basis perpendicular to L
        float s = (L.z >= 0.0f) ? 1.0f : -1.0f;
        float a = -1.0f / (s + L.z);
        float b = L.x * L.y * a;
        float3 tangent = float3(1.0f + s * L.x * L.x * a, s * b, -s * L.x);
        float3 bitangent = float3(b, s + L.y * L.y * a, -L.y);

        float halfAngle = angularDiameter * 0.5f;
        outRayDirRWS = normalize(L + (tangent * disc.x + bitangent * disc.y) * tan(halfAngle));
    }
    else
    {
        outRayDirRWS = L;
    }
    outRayDist = maxShadowDist;
}

#endif // GHOST_RAY_TRACING_COMMON_HLSL
