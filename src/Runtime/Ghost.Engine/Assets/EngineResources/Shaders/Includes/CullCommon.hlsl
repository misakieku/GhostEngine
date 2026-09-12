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
    // GhostEngine uses column-major matrices (mul(mat, v)); columns are the coordinate frame axes.
    float3 worldExtents = abs(mat._m00_m10_m20) * extents.x +
                          abs(mat._m01_m11_m21) * extents.y +
                          abs(mat._m02_m12_m22) * extents.z;

    outMin = worldCenter - worldExtents;
    outMax = worldCenter + worldExtents;
}

#define CULL_EPSILON 1.2e-07f
#define DEPTH_EPSILON 1.2e-07f

static inline float4 BuildAabbCorner(float3 bboxMin, float3 bboxMax, uint cornerIndex)
{
    bool3 useMax = bool3((cornerIndex & 1u) != 0u, (cornerIndex & 2u) != 0u, (cornerIndex & 4u) != 0u);
    float3 selector = float3(
        useMax.x ? 1.0f : 0.0f,
        useMax.y ? 1.0f : 0.0f,
        useMax.z ? 1.0f : 0.0f);
    return float4(lerp(bboxMin, bboxMax, selector), 1.0f);
}

static inline float4 ProjectToClip(float4 homogeneousPos, out bool validClip)
{
    validClip = (homogeneousPos.w >= CULL_EPSILON);
    return float4(homogeneousPos.xyz * rcp(homogeneousPos.w), homogeneousPos.w);
}

static inline uint ComputeHomogeneousClipMask(float4 homogeneousPos)
{
    uint mask = 0;
    mask |= (homogeneousPos.x < -homogeneousPos.w) ? 1u : 0u;
    mask |= (homogeneousPos.x >  homogeneousPos.w) ? 2u : 0u;
    mask |= (homogeneousPos.y < -homogeneousPos.w) ? 4u : 0u;
    mask |= (homogeneousPos.y >  homogeneousPos.w) ? 8u : 0u;
    // In reversed-Z: 0 <= z <= w (near=1.0, far=0.0)
    mask |= (homogeneousPos.z < 0.0f) ? 16u : 0u;
    mask |= (homogeneousPos.z > homogeneousPos.w) ? 32u : 0u;
    mask |= (homogeneousPos.w <= 0.0f) ? 64u : 0u;
    return mask;
}

// Directly intersects an object-space AABB with camera frustum using worldToClip = mul(viewProj, world)
// Outputs clipMin, clipMax (in NDC [-1, 1], with z in [0, 1]) and clipValid.
// If any corner is behind or crosses the near plane, clipValid is false (cannot be occluded by HZB).
//
// OPTIMIZATION TODO: Can be optimized using UE5 Nanite's delta-corner approach (BoxCullFrustumPerspective)
// by transforming the center and basis delta vectors (DX, DY, DZ) rather than all 8 corners.
// Note: When porting, remember GhostEngine uses column-major matrices (mul(M, v)), so:
//   DX = (2.0f * extent.x) * float4(worldToClip._m00_m10_m20_m30),
//   and depth equation constant term is at viewToClip[2][3] (m_23), not [3][2]!
static inline bool BBoxIntersectFrustum(
    float3 bboxMin,
    float3 bboxMax,
    float4x4 worldMatrix,
    float4x4 viewProjMatrix,
    out float4 clipMinOut,
    out float4 clipMaxOut,
    out bool clipValidOut)
{
    const float4x4 worldToClip = mul(viewProjMatrix, worldMatrix);

    bool validClip = false;
    float4 homogeneousPos = mul(worldToClip, BuildAabbCorner(bboxMin, bboxMax, 0u));
    float4 clipPos = ProjectToClip(homogeneousPos, validClip);

    uint rejectMask = ComputeHomogeneousClipMask(homogeneousPos);
    float4 clipMin = clipPos;
    float4 clipMax = clipPos;
    bool allCornersValid = validClip;

    [unroll]
    for (uint corner = 1u; corner < 8u; ++corner)
    {
        homogeneousPos = mul(worldToClip, BuildAabbCorner(bboxMin, bboxMax, corner));
        clipPos = ProjectToClip(homogeneousPos, validClip);
        rejectMask &= ComputeHomogeneousClipMask(homogeneousPos);

        clipMin = min(clipMin, clipPos);
        clipMax = max(clipMax, clipPos);
        allCornersValid = allCornersValid && validClip;
    }

    clipValidOut = allCornersValid;
    clipMinOut = float4(clamp(clipMin.xy, float2(-1.0f, -1.0f), float2(1.0f, 1.0f)), clipMin.zw);
    clipMaxOut = float4(clamp(clipMax.xy, float2(-1.0f, -1.0f), float2(1.0f, 1.0f)), clipMax.zw);

    return (rejectMask == 0u);
}

// Fast 6-plane AABB frustum cull wrapper
static inline FrustumTestResult FrustumCullAABB(
    float3 minPt,
    float3 maxPt,
    float4x4 viewProj)
{
    float4 clipMin, clipMax;
    bool clipValid;
    float4x4 identityMat = float4x4(
        1.0f, 0.0f, 0.0f, 0.0f,
        0.0f, 1.0f, 0.0f, 0.0f,
        0.0f, 0.0f, 1.0f, 0.0f,
        0.0f, 0.0f, 0.0f, 1.0f
    );
    bool inFrustum = BBoxIntersectFrustum(minPt, maxPt, identityMat, viewProj, clipMin, clipMax, clipValid);
    FrustumTestResult res;
    res.isVisible = inFrustum;
    res.intersectsNearPlane = !clipValid;
    return res;
}

static inline int2 GetHZBMipOffset(
    uint lod,
    uint4 hzbOffsets0,
    uint4 hzbOffsets1,
    uint4 hzbOffsets2,
    uint4 hzbOffsets3)
{
    uint packedVal = 0;
    if (lod < 4)
    {
        packedVal = (lod == 0) ? hzbOffsets0.x : ((lod == 1) ? hzbOffsets0.y : ((lod == 2) ? hzbOffsets0.z : hzbOffsets0.w));
    }
    else if (lod < 8)
    {
        uint l = lod - 4;
        packedVal = (l == 0) ? hzbOffsets1.x : ((l == 1) ? hzbOffsets1.y : ((l == 2) ? hzbOffsets1.z : hzbOffsets1.w));
    }
    else if (lod < 12)
    {
        uint l = lod - 8;
        packedVal = (l == 0) ? hzbOffsets2.x : ((l == 1) ? hzbOffsets2.y : ((l == 2) ? hzbOffsets2.z : hzbOffsets2.w));
    }
    else
    {
        uint l = lod - 12;
        packedVal = (l == 0) ? hzbOffsets3.x : ((l == 1) ? hzbOffsets3.y : ((l == 2) ? hzbOffsets3.z : hzbOffsets3.w));
    }
    return int2(packedVal & 0xFFFF, packedVal >> 16);
}

// Nanite's MipLevelForRect for 4x4 footprint
static inline int MipLevelForRect(int4 rectPixels, int desiredFootprintPixels = 4)
{
    const int maxPixelOffset = desiredFootprintPixels - 1; // 3
    const int mipOffset = 1; // (int)log2(4) - 1 = 1

    int2 mipLevelXY = firstbithigh((uint2)max(rectPixels.zw - rectPixels.xy, int2(0, 0)));
    int mipLevel = max(max(mipLevelXY.x, mipLevelXY.y) - mipOffset, 0);

    mipLevel += any((rectPixels.zw >> mipLevel) - (rectPixels.xy >> mipLevel) > maxPixelOffset) ? 1 : 0;
    return mipLevel;
}

// Evaluates whether a projected clip-space bounding box is visible against the HZB pyramid atlas
static inline bool HZBVisible(
    float4 clipMin,
    float4 clipMax,
    uint hzbMipCount,
    uint renderWidth,
    uint renderHeight,
    uint hzbAtlas,
    uint4 hzbOffsets0,
    uint4 hzbOffsets1,
    uint4 hzbOffsets2,
    uint4 hzbOffsets3)
{
    if (clipMin.x > 1.0f || clipMin.y > 1.0f || clipMax.x < -1.0f || clipMax.y < -1.0f)
    {
        return false;
    }

    if (hzbAtlas == 0 || hzbAtlas == 0xFFFFFFFF || hzbMipCount == 0)
    {
        return true;
    }

    // Map NDC [-1, 1] to UV [0, 1] with inverted Y:
    float4 rectUV = saturate(float4(clipMin.xy, clipMax.xy) * float2(0.5f, -0.5f).xyxy + 0.5f).xwzy;

    // In Nanite: calculate pixel footprint in FULL render resolution, testing pixel center overlap
    int4 screenViewRect = int4(0, 0, (int)renderWidth, (int)renderHeight);
    float2 viewSize = float2(renderWidth, renderHeight);
    int4 pixels = int4(rectUV * viewSize.xyxy + float4(0.5f, 0.5f, -0.5f, -0.5f));
    pixels.xy = max(pixels.xy, screenViewRect.xy);
    pixels.zw = min(pixels.zw, screenViewRect.zw - 1);

    // If rectangle doesn't even overlap a pixel center, it cannot produce any fragments
    if (any(pixels.zw < pixels.xy))
    {
        return false;
    }

    // Convert from full-res screen pixels to HZB Mip 0 texels (half resolution):
    int4 hzbTexels = int4(pixels.xy, max(pixels.xy, pixels.zw)) >> 1;

    // Determine target mip level for 4x4 footprint
    int hzbLevel = MipLevelForRect(hzbTexels, 4);
    uint hzbMip = min((uint)hzbLevel, hzbMipCount - 1);

    // Transform HZB Mip 0 coordinates to coordinates of selected mip level
    hzbTexels >>= hzbMip;

    int2 mipSize = max(int2(1, 1), int2((int)renderWidth >> (int)(hzbMip + 1), (int)renderHeight >> (int)(hzbMip + 1)));
    hzbTexels.zw = min(hzbTexels.zw, mipSize - 1);
    hzbTexels.xy = min(hzbTexels.xy, hzbTexels.zw);

    int4 xCoords = min(hzbTexels.x + int4(0, 1, 2, 3), hzbTexels.z);
    int4 yCoords = min(hzbTexels.y + int4(0, 1, 2, 3), hzbTexels.w);

    int2 mipOffset = GetHZBMipOffset(hzbMip, hzbOffsets0, hzbOffsets1, hzbOffsets2, hzbOffsets3);
    Texture2D<float> hzbAtlasTex = GET_TEXTURE2D(hzbAtlas);

    float4 row0 = float4(
        hzbAtlasTex.Load(int3(mipOffset + int2(xCoords.x, yCoords.x), 0)).r,
        hzbAtlasTex.Load(int3(mipOffset + int2(xCoords.y, yCoords.x), 0)).r,
        hzbAtlasTex.Load(int3(mipOffset + int2(xCoords.z, yCoords.x), 0)).r,
        hzbAtlasTex.Load(int3(mipOffset + int2(xCoords.w, yCoords.x), 0)).r
    );
    float4 row1 = float4(
        hzbAtlasTex.Load(int3(mipOffset + int2(xCoords.x, yCoords.y), 0)).r,
        hzbAtlasTex.Load(int3(mipOffset + int2(xCoords.y, yCoords.y), 0)).r,
        hzbAtlasTex.Load(int3(mipOffset + int2(xCoords.z, yCoords.y), 0)).r,
        hzbAtlasTex.Load(int3(mipOffset + int2(xCoords.w, yCoords.y), 0)).r
    );
    float4 row2 = float4(
        hzbAtlasTex.Load(int3(mipOffset + int2(xCoords.x, yCoords.z), 0)).r,
        hzbAtlasTex.Load(int3(mipOffset + int2(xCoords.y, yCoords.z), 0)).r,
        hzbAtlasTex.Load(int3(mipOffset + int2(xCoords.z, yCoords.z), 0)).r,
        hzbAtlasTex.Load(int3(mipOffset + int2(xCoords.w, yCoords.z), 0)).r
    );
    float4 row3 = float4(
        hzbAtlasTex.Load(int3(mipOffset + int2(xCoords.x, yCoords.w), 0)).r,
        hzbAtlasTex.Load(int3(mipOffset + int2(xCoords.y, yCoords.w), 0)).r,
        hzbAtlasTex.Load(int3(mipOffset + int2(xCoords.z, yCoords.w), 0)).r,
        hzbAtlasTex.Load(int3(mipOffset + int2(xCoords.w, yCoords.w), 0)).r
    );

    float4 minRow = min(min(row0, row1), min(row2, row3));
    float minOccluderDepth = min(min(minRow.x, minRow.y), min(minRow.z, minRow.w));

    // In reversed-Z, clipMax.z is the nearest point of the bounding box to the camera
    // If the object's nearest point is closer than the occluder's furthest point (plus epsilon), it is VISIBLE!
    return (clipMax.z + DEPTH_EPSILON >= minOccluderDepth);
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
