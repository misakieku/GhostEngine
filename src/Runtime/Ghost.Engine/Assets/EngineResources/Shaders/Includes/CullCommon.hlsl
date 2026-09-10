#ifndef GHOST_CULL_COMMON_HLSL
#define GHOST_CULL_COMMON_HLSL

#include "Common.hlsl"

struct FrustumTestResult
{
    bool isVisible;
    bool intersectsNearPlane;
};

// Transforms an AABB by a 4x4 matrix, returning the tight world AABB
static inline void TransformAABB(
    float3 minPt,
    float3 maxPt,
    float4x4 mat,
    out float3 outMin,
    out float3 outMax)
{
    float3 center = (minPt + maxPt) * 0.5f;
    float3 extents = (maxPt - minPt) * 0.5f;

    float3 worldCenter = mul(mat, float4(center, 1.0f)).xyz;
    float3 worldExtents = abs(mat[0].xyz) * extents.x +
                          abs(mat[1].xyz) * extents.y +
                          abs(mat[2].xyz) * extents.z;

    outMin = worldCenter - worldExtents;
    outMax = worldCenter + worldExtents;
}

// Tests an AABB against view-projection frustum in clip space with reversed-Z (0 <= z <= w)
static inline FrustumTestResult FrustumCullAABB(
    float3 minPt,
    float3 maxPt,
    float4x4 viewProj)
{
    FrustumTestResult res;
    res.isVisible = true;
    res.intersectsNearPlane = false;

    float3 corners[8] = {
        float3(minPt.x, minPt.y, minPt.z),
        float3(maxPt.x, minPt.y, minPt.z),
        float3(minPt.x, maxPt.y, minPt.z),
        float3(maxPt.x, maxPt.y, minPt.z),
        float3(minPt.x, minPt.y, maxPt.z),
        float3(maxPt.x, minPt.y, maxPt.z),
        float3(minPt.x, maxPt.y, maxPt.z),
        float3(maxPt.x, maxPt.y, maxPt.z)
    };

    uint maskLeft = 0;
    uint maskRight = 0;
    uint maskBottom = 0;
    uint maskTop = 0;
    uint maskFar = 0;
    uint maskNear = 0;

    for (int i = 0; i < 8; ++i)
    {
        float4 clip = mul(viewProj, float4(corners[i], 1.0f));

        if (clip.w <= 0.0f || clip.z > clip.w)
        {
            res.intersectsNearPlane = true;
        }

        if (clip.x < -clip.w) maskLeft |= (1 << i);
        if (clip.x >  clip.w) maskRight |= (1 << i);
        if (clip.y < -clip.w) maskBottom |= (1 << i);
        if (clip.y >  clip.w) maskTop |= (1 << i);
        if (clip.z < 0.0f)    maskFar |= (1 << i);      // reversed-Z far plane at z_ndc = 0
        if (clip.z >  clip.w) maskNear |= (1 << i);     // reversed-Z near plane at z_ndc = 1
    }

    // If all 8 points are outside any single plane, the box is completely outside
    if (maskLeft == 0xFF || maskRight == 0xFF ||
        maskBottom == 0xFF || maskTop == 0xFF ||
        maskFar == 0xFF || maskNear == 0xFF)
    {
        res.isVisible = false;
        return res;
    }

    return res;
}

// Projects AABB to screen UV rect [uMin, vMin, uMax, vMax] and finds nearest reversed-Z depth (max z)
static inline bool ProjectAABBToScreen(
    float3 minPt,
    float3 maxPt,
    float4x4 viewProj,
    out float4 screenRect,
    out float nearestDepth)
{
    float3 corners[8] = {
        float3(minPt.x, minPt.y, minPt.z),
        float3(maxPt.x, minPt.y, minPt.z),
        float3(minPt.x, maxPt.y, minPt.z),
        float3(maxPt.x, maxPt.y, minPt.z),
        float3(minPt.x, minPt.y, maxPt.z),
        float3(maxPt.x, minPt.y, maxPt.z),
        float3(minPt.x, maxPt.y, maxPt.z),
        float3(maxPt.x, maxPt.y, maxPt.z)
    };

    float2 uvMin = float2(1.0f, 1.0f);
    float2 uvMax = float2(0.0f, 0.0f);
    float maxZ = -1.0f;

    for (int i = 0; i < 8; ++i)
    {
        float4 clip = mul(viewProj, float4(corners[i], 1.0f));
        if (clip.w <= 0.0f)
        {
            // Crosses camera plane, cannot project safely
            screenRect = float4(0.0f, 0.0f, 1.0f, 1.0f);
            nearestDepth = 1.0f;
            return false;
        }

        float3 ndc = clip.xyz / clip.w;
        float2 uv = float2(ndc.x * 0.5f + 0.5f, -ndc.y * 0.5f + 0.5f);
        uvMin = min(uvMin, uv);
        uvMax = max(uvMax, uv);
        maxZ = max(maxZ, ndc.z);
    }

    screenRect = float4(saturate(uvMin), saturate(uvMax));
    nearestDepth = saturate(maxZ);
    return true;
}

// Tests bounding box against HZB mip chain in reversed-Z (conservative occluder depth = min)
static inline bool HZBOcclusionTest(
    float4 screenRect,
    float nearestDepth,
    uint hzbMipCount,
    uint hzbWidth,
    uint hzbHeight,
    uint hzbMips[16])
{
    float2 size = (screenRect.zw - screenRect.xy) * float2(hzbWidth, hzbHeight);
    float maxDim = max(size.x, size.y);

    // Compute mip level so footprint is ~1-2 texels
    uint mipLevel = (uint)clamp(ceil(log2(max(maxDim, 1.0f))), 0.0f, (float)(hzbMipCount - 1));

    uint mipWidth = max(1u, hzbWidth >> mipLevel);
    uint mipHeight = max(1u, hzbHeight >> mipLevel);

    int2 minCoord = clamp((int2)(screenRect.xy * float2(mipWidth, mipHeight)), int2(0, 0), int2(mipWidth - 1, mipHeight - 1));
    int2 maxCoord = clamp((int2)(screenRect.zw * float2(mipWidth, mipHeight)), int2(0, 0), int2(mipWidth - 1, mipHeight - 1));

    uint texId = hzbMips[mipLevel];
    if (texId == 0)
    {
        return false; // HZB not available or empty -> not occluded
    }

    Texture2D<float> hzbTex = GET_TEXTURE2D(texId);

    float d00 = hzbTex.Load(int3(minCoord.x, minCoord.y, 0)).r;
    float d10 = hzbTex.Load(int3(maxCoord.x, minCoord.y, 0)).r;
    float d01 = hzbTex.Load(int3(minCoord.x, maxCoord.y, 0)).r;
    float d11 = hzbTex.Load(int3(maxCoord.x, maxCoord.y, 0)).r;

    // In reversed-Z, the conservative depth for a cluster of texels is the minimum (furthest depth)
    float minOccluderDepth = min(min(d00, d10), min(d01, d11));

    // If the object's nearest point is smaller (further away) than the occluder, it is occluded!
    return nearestDepth < minOccluderDepth;
}

// Evaluates screen-space geometric error for meshlet DAG refinement
static inline bool EvaluateLODDetailSufficient(
    float objectError,
    float3 sphereCenter,
    float sphereRadius,
    float3 cameraPos,
    float proj11,
    float screenHeight,
    float errorThreshold)
{
    float dist = max(length(sphereCenter - cameraPos) - sphereRadius, 0.001f);
    float pixelError = (objectError * proj11 * screenHeight) / (2.0f * dist);
    return pixelError <= errorThreshold;
}

#endif // GHOST_CULL_COMMON_HLSL
