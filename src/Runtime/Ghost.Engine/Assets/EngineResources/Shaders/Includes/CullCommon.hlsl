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

// Fast 6-plane AABB frustum cull in reversed-Z (0 <= z <= w) with ZERO stack arrays / ZERO alloca
static inline FrustumTestResult FrustumCullAABB(
    float3 minPt,
    float3 maxPt,
    float4x4 viewProj)
{
    FrustumTestResult res;
    res.isVisible = true;
    res.intersectsNearPlane = false;

    // In HLSL, viewProj._m00.._m33 maps to clip = mul(viewProj, float4(p, 1.0))
    // Rows contributing to clip.x, clip.y, clip.z, clip.w:
    float4 rowX = float4(viewProj._11, viewProj._12, viewProj._13, viewProj._14);
    float4 rowY = float4(viewProj._21, viewProj._22, viewProj._23, viewProj._24);
    float4 rowZ = float4(viewProj._31, viewProj._32, viewProj._33, viewProj._34);
    float4 rowW = float4(viewProj._41, viewProj._42, viewProj._43, viewProj._44);

    // 6 inward-facing frustum planes: [nx, ny, nz, d] where dot(plane.xyz, pos) + plane.w >= 0 is inside
    // Reversed-Z: Far is z_ndc = 0 (clip.z >= 0), Near is z_ndc = 1 (clip.w - clip.z >= 0)
    float4 planeLeft   = rowW + rowX;
    float4 planeRight  = rowW - rowX;
    float4 planeBottom = rowW + rowY;
    float4 planeTop    = rowW - rowY;
    float4 planeFar    = rowZ;
    float4 planeNear   = rowW - rowZ;

    // Test positive corner (p-vertex) against each plane. If p-vertex is outside, entire AABB is outside.
    float3 pLeft = float3(planeLeft.x > 0.0f ? maxPt.x : minPt.x, planeLeft.y > 0.0f ? maxPt.y : minPt.y, planeLeft.z > 0.0f ? maxPt.z : minPt.z);
    if ((dot(planeLeft.xyz, pLeft) + planeLeft.w) < 0.0f) { res.isVisible = false; return res; }

    float3 pRight = float3(planeRight.x > 0.0f ? maxPt.x : minPt.x, planeRight.y > 0.0f ? maxPt.y : minPt.y, planeRight.z > 0.0f ? maxPt.z : minPt.z);
    if ((dot(planeRight.xyz, pRight) + planeRight.w) < 0.0f) { res.isVisible = false; return res; }

    float3 pBottom = float3(planeBottom.x > 0.0f ? maxPt.x : minPt.x, planeBottom.y > 0.0f ? maxPt.y : minPt.y, planeBottom.z > 0.0f ? maxPt.z : minPt.z);
    if ((dot(planeBottom.xyz, pBottom) + planeBottom.w) < 0.0f) { res.isVisible = false; return res; }

    float3 pTop = float3(planeTop.x > 0.0f ? maxPt.x : minPt.x, planeTop.y > 0.0f ? maxPt.y : minPt.y, planeTop.z > 0.0f ? maxPt.z : minPt.z);
    if ((dot(planeTop.xyz, pTop) + planeTop.w) < 0.0f) { res.isVisible = false; return res; }

    float3 pFar = float3(planeFar.x > 0.0f ? maxPt.x : minPt.x, planeFar.y > 0.0f ? maxPt.y : minPt.y, planeFar.z > 0.0f ? maxPt.z : minPt.z);
    if ((dot(planeFar.xyz, pFar) + planeFar.w) < 0.0f) { res.isVisible = false; return res; }

    float3 pNear = float3(planeNear.x > 0.0f ? maxPt.x : minPt.x, planeNear.y > 0.0f ? maxPt.y : minPt.y, planeNear.z > 0.0f ? maxPt.z : minPt.z);
    if ((dot(planeNear.xyz, pNear) + planeNear.w) < 0.0f) { res.isVisible = false; return res; }

    // Test negative corner (n-vertex) against near plane to check if AABB intersects/crosses near plane
    float3 nNear = float3(planeNear.x > 0.0f ? minPt.x : maxPt.x, planeNear.y > 0.0f ? minPt.y : maxPt.y, planeNear.z > 0.0f ? minPt.z : maxPt.z);
    res.intersectsNearPlane = ((dot(planeNear.xyz, nNear) + planeNear.w) <= 0.0f);

    return res;
}

// Projects AABB to screen UV rect [uMin, vMin, uMax, vMax] and finds nearest reversed-Z depth analytically
// Evaluates exact mathematical perspective extrema with ZERO loops, ZERO alloca, and only 4 divisions
static inline bool ProjectAABBToScreen(
    float3 minPt,
    float3 maxPt,
    float4x4 viewMat,
    float4x4 projMat,
    out float4 screenRect,
    out float nearestDepth)
{
    float3 center = (minPt + maxPt) * 0.5f;
    float3 extents = (maxPt - minPt) * 0.5f;

    // Transform AABB center and extents to view space
    float3 cv = mul(viewMat, float4(center, 1.0f)).xyz;
    float3 ev = abs(viewMat[0].xyz) * extents.x +
                abs(viewMat[1].xyz) * extents.y +
                abs(viewMat[2].xyz) * extents.z;

    float zMin = cv.z - ev.z;
    float zMax = cv.z + ev.z;

    // If the box extends behind or crosses the near plane, project to full screen
    if (zMin <= 0.001f)
    {
        screenRect = float4(0.0f, 0.0f, 1.0f, 1.0f);
        nearestDepth = 1.0f;
        return false;
    }

    float xMin = cv.x - ev.x;
    float xMax = cv.x + ev.x;
    float yMin = cv.y - ev.y;
    float yMax = cv.y + ev.y;

    // Exact analytical extrema of perspective projection
    float minX_Z = xMin / (xMin >= 0.0f ? zMax : zMin);
    float maxX_Z = xMax / (xMax >= 0.0f ? zMin : zMax);
    float minY_Z = yMin / (yMin >= 0.0f ? zMax : zMin);
    float maxY_Z = yMax / (yMax >= 0.0f ? zMin : zMax);

    float p00 = projMat._11;
    float p11 = projMat._22;

    float ndcMinX = minX_Z * p00;
    float ndcMaxX = maxX_Z * p00;
    float ndcMinY = minY_Z * p11;
    float ndcMaxY = maxY_Z * p11;

    // Map NDC [-1, 1] to UV [0, 1] (Y flipped for DirectX UV)
    screenRect.x = saturate(ndcMinX * 0.5f + 0.5f);
    screenRect.y = saturate(-ndcMaxY * 0.5f + 0.5f);
    screenRect.z = saturate(ndcMaxX * 0.5f + 0.5f);
    screenRect.w = saturate(-ndcMinY * 0.5f + 0.5f);

    // Reversed-Z nearest depth at zMin (projMat._33 = m22, projMat._34 = m23)
    float clipZ = projMat._33 * zMin + projMat._34;
    nearestDepth = saturate(clipZ / zMin);

    return true;
}

// Tests bounding box against HZB mip chain in reversed-Z (conservative occluder depth = min)
// Loads mip texture descriptor directly from propertiesBuffer (offset 40 = hzbMip0) to avoid any local array/alloca
static inline bool HZBOcclusionTest(
    float4 screenRect,
    float nearestDepth,
    uint hzbMipCount,
    uint hzbWidth,
    uint hzbHeight,
    uint propertiesBufferIndex)
{
    float2 size = (screenRect.zw - screenRect.xy) * float2(hzbWidth, hzbHeight);
    float maxDim = max(size.x, size.y);

    // Compute mip level so footprint is ~1-2 texels
    uint mipLevel = (uint)clamp(ceil(log2(max(maxDim, 1.0f))), 0.0f, (float)(hzbMipCount - 1));

    uint mipWidth = max(1u, hzbWidth >> mipLevel);
    uint mipHeight = max(1u, hzbHeight >> mipLevel);

    int2 minCoord = clamp((int2)(screenRect.xy * float2(mipWidth, mipHeight)), int2(0, 0), int2(mipWidth - 1, mipHeight - 1));
    int2 maxCoord = clamp((int2)(screenRect.zw * float2(mipWidth, mipHeight)), int2(0, 0), int2(mipWidth - 1, mipHeight - 1));

    ByteAddressBuffer propsBuf = GET_BUFFER(propertiesBufferIndex);
    // hzbMip0 is at byte offset 40 in InternalMeshletCullGraphShaderProperties
    uint texId = propsBuf.Load(40 + mipLevel * 4);
    if (texId == 0 || texId == 0xFFFFFFFF)
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
