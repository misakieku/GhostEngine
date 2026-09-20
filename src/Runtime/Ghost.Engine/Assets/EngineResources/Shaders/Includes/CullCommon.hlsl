#ifndef GHOST_CULL_COMMON_HLSL
#define GHOST_CULL_COMMON_HLSL

#include "Common.hlsl"

struct FrustumTestResult
{
    bool isVisible;
    bool intersectsNearPlane;
    
    static FrustumTestResult Create(bool visible, bool nearIntersect)
    {
        FrustumTestResult r;
        r.isVisible = visible;
        r.intersectsNearPlane = nearIntersect;
        return r;
    }
};

struct MeshletCandidateRecord
{
    uint instanceIndex;
    uint meshletIndex;
    uint meshletBufferIndex;
};

struct VisibleMeshletEntry
{
    uint instanceIndex;
    uint meshletIndex;
};

struct BBoxFrustumResult
{
    bool isVisible;
    bool clipValid;
    float4 clipMin;
    float4 clipMax;
};

// Transforms an AABB by a 4x4 matrix, returning the tight world AABB
static inline void TransformAABB(float3 minPt, float3 maxPt, float4x4 mat, out float3 outMin, out float3 outMax)
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
#define DEPTH_EPSILON 1e-3f

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

static inline bool IsTriangleOutsideFrustum(float4 h0, float4 h1, float4 h2)
{
    uint cullBits =
        ComputeHomogeneousClipMask(h0) &
        ComputeHomogeneousClipMask(h1) &
        ComputeHomogeneousClipMask(h2);
    return cullBits != 0;
}

#define ACCUMULATE_CLIP_CORNER(P, minXY, maxXY, minZ, maxZ) \
{ \
    float rcpW = rcp((P).w); \
    minXY = min(minXY, (P).xy * rcpW); \
    maxXY = max(maxXY, (P).xy * rcpW); \
    minZ  = min(minZ,  (P).z  * rcpW); \
    maxZ  = max(maxZ,  (P).z  * rcpW); \
}

template<bool usePreVP>
BBoxFrustumResult BBoxIntersectFrustum(float3 bboxMin, float3 bboxMax, float4x4 worldMatrix)
{
    BBoxFrustumResult result = ZERO_INIT(BBoxFrustumResult);

    // 1. Object space center and half-extents
    float3 boxCenter = 0.5f * (bboxMin + bboxMax);
    float3 boxExtent = 0.5f * (bboxMax - bboxMin);

    // 2. Transform to world space using column vectors (GhostEngine is column-major: mul(M, v))
    float3 centerWorld = mul(worldMatrix, float4(boxCenter, 1.0f)).xyz;
    float3 dXWorld = worldMatrix._m00_m10_m20 * boxExtent.x;
    float3 dYWorld = worldMatrix._m01_m11_m21 * boxExtent.y;
    float3 dZWorld = worldMatrix._m02_m12_m22 * boxExtent.z;

    // 3. Transform to clip space using constant buffer view-projection
    float4 cClip, dX, dY, dZ;
    if (usePreVP)
    {
        cClip = mul(g_ViewData.preVPMatrix, float4(centerWorld, 1.0f));
        dX = mul(g_ViewData.preVPMatrix, float4(dXWorld, 0.0f));
        dY = mul(g_ViewData.preVPMatrix, float4(dYWorld, 0.0f));
        dZ = mul(g_ViewData.preVPMatrix, float4(dZWorld, 0.0f));
    }
    else
    {
        cClip = mul(g_ViewData.viewProjectionMatrix, float4(centerWorld, 1.0f));
        dX = mul(g_ViewData.viewProjectionMatrix, float4(dXWorld, 0.0f));
        dY = mul(g_ViewData.viewProjectionMatrix, float4(dYWorld, 0.0f));
        dZ = mul(g_ViewData.viewProjectionMatrix, float4(dZWorld, 0.0f));
    }

    // 4. Frustum culling (6 homogeneous clip-space plane tests in reversed-Z)
    // Left: x + w >= 0
    float rLeft = abs(dX.x + dX.w) + abs(dY.x + dY.w) + abs(dZ.x + dZ.w);
    if ((cClip.x + cClip.w) + rLeft < 0.0f)
        return result;

    // Right: w - x >= 0
    float rRight = abs(dX.w - dX.x) + abs(dY.w - dY.x) + abs(dZ.w - dZ.x);
    if ((cClip.w - cClip.x) + rRight < 0.0f)
        return result;

    // Bottom: y + w >= 0
    float rBottom = abs(dX.y + dX.w) + abs(dY.y + dY.w) + abs(dZ.y + dZ.w);
    if ((cClip.y + cClip.w) + rBottom < 0.0f)
        return result;

    // Top: w - y >= 0
    float rTop = abs(dX.w - dX.y) + abs(dY.w - dY.y) + abs(dZ.w - dZ.y);
    if ((cClip.w - cClip.y) + rTop < 0.0f)
        return result;

    // Far plane (Reversed-Z: z >= 0)
    float rFar = abs(dX.z) + abs(dY.z) + abs(dZ.z);
    if (cClip.z + rFar < 0.0f)
        return result;

    // Near plane (Reversed-Z: w - z >= 0)
    float rNear = abs(dX.w - dX.z) + abs(dY.w - dY.z) + abs(dZ.w - dZ.z);
    if ((cClip.w - cClip.z) + rNear < 0.0f)
        return result;

    // 5. Near plane crossing test (cannot project 2D NDC if any corner is behind eye or penetrates near plane)
    result.isVisible = true;
    float minW = cClip.w - (abs(dX.w) + abs(dY.w) + abs(dZ.w));
    float minNear = (cClip.w - cClip.z) - rNear;
    if (minW <= CULL_EPSILON || minNear < 0.0f)
    {
        result.clipValid = false;
        return result;
    }

    // 6. Project 8 corners to 2D NDC clip bounds and depth
    result.clipValid = true;
    float2 minXY = float2(1e9f, 1e9f);
    float2 maxXY = float2(-1e9f, -1e9f);
    float minZVal = 1e9f;
    float maxZVal = -1e9f;

    // -Z plane
    float4 pZ0 = cClip - dZ;
    float4 pZ0_Y0 = pZ0 - dY;
    ACCUMULATE_CLIP_CORNER(pZ0_Y0 - dX, minXY, maxXY, minZVal, maxZVal);
    ACCUMULATE_CLIP_CORNER(pZ0_Y0 + dX, minXY, maxXY, minZVal, maxZVal);

    float4 pZ0_Y1 = pZ0 + dY;
    ACCUMULATE_CLIP_CORNER(pZ0_Y1 - dX, minXY, maxXY, minZVal, maxZVal);
    ACCUMULATE_CLIP_CORNER(pZ0_Y1 + dX, minXY, maxXY, minZVal, maxZVal);

    // +Z plane
    float4 pZ1 = cClip + dZ;
    float4 pZ1_Y0 = pZ1 - dY;
    ACCUMULATE_CLIP_CORNER(pZ1_Y0 - dX, minXY, maxXY, minZVal, maxZVal);
    ACCUMULATE_CLIP_CORNER(pZ1_Y0 + dX, minXY, maxXY, minZVal, maxZVal);

    float4 pZ1_Y1 = pZ1 + dY;
    ACCUMULATE_CLIP_CORNER(pZ1_Y1 - dX, minXY, maxXY, minZVal, maxZVal);
    ACCUMULATE_CLIP_CORNER(pZ1_Y1 + dX, minXY, maxXY, minZVal, maxZVal);

    // In reversed-Z, minZVal is furthest depth (clipMin.z) and maxZVal is nearest depth (clipMax.z)
    result.clipMin = float4(clamp(minXY, float2(-1.0f, -1.0f), float2(1.0f, 1.0f)), minZVal, 0.0f);
    result.clipMax = float4(clamp(maxXY, float2(-1.0f, -1.0f), float2(1.0f, 1.0f)), maxZVal, 0.0f);

    return result;
}

#undef ACCUMULATE_CLIP_CORNER

static inline bool SphereIntersectFrustum(float3 center, float radius, float4 planes[6])
{
    [unroll]
    for (uint i = 0u; i < 6u; ++i)
    {
        float distance = dot(planes[i].xyz, center) + planes[i].w;
        if (distance < -radius)
        {
            return false;
        }
    }
    
    return true;
}

static inline bool AABBIntersectFrustum(float3 minPt, float3 maxPt, float4 planes[6])
{
    float3 center = (minPt + maxPt) * 0.5f;
    float3 extents = (maxPt - minPt) * 0.5f;
    
    [unroll]
    for (uint i = 0u; i < 6u; ++i)
    {
        // Calculate the projected radius of the AABB onto the plane normal
        float projRadius = dot(abs(planes[i].xyz), extents);
        if (dot(planes[i].xyz, center) + planes[i].w < -projRadius)
        {
            return false;
        }
    }
    
    return true;
}

// Fast 6-plane AABB frustum cull wrapper
static inline FrustumTestResult FrustumCullAABB(float3 minPt, float3 maxPt, float4x4 viewProjectionMatrix)
{
    float3 boxCenter = 0.5f * (minPt + maxPt);
    float3 boxExtent = 0.5f * (maxPt - minPt);

    float4 cClip = mul(viewProjectionMatrix, float4(boxCenter, 1.0f));
    float4 dX = viewProjectionMatrix._m00_m10_m20_m30 * boxExtent.x;
    float4 dY = viewProjectionMatrix._m01_m11_m21_m31 * boxExtent.y;
    float4 dZ = viewProjectionMatrix._m02_m12_m22_m32 * boxExtent.z;

    float rLeft = abs(dX.x + dX.w) + abs(dY.x + dY.w) + abs(dZ.x + dZ.w);
    if ((cClip.x + cClip.w) + rLeft < 0.0f)
        return FrustumTestResult::Create(false, false);

    float rRight = abs(dX.w - dX.x) + abs(dY.w - dY.x) + abs(dZ.w - dZ.x);
    if ((cClip.w - cClip.x) + rRight < 0.0f)
        return FrustumTestResult::Create(false, false);

    float rBottom = abs(dX.y + dX.w) + abs(dY.y + dY.w) + abs(dZ.y + dZ.w);
    if ((cClip.y + cClip.w) + rBottom < 0.0f)
        return FrustumTestResult::Create(false, false);

    float rTop = abs(dX.w - dX.y) + abs(dY.w - dY.y) + abs(dZ.w - dZ.y);
    if ((cClip.w - cClip.y) + rTop < 0.0f)
        return FrustumTestResult::Create(false, false);

    float rFar = abs(dX.z) + abs(dY.z) + abs(dZ.z);
    if (cClip.z + rFar < 0.0f)
        return FrustumTestResult::Create(false, false);

    float rNear = abs(dX.w - dX.z) + abs(dY.w - dY.z) + abs(dZ.w - dZ.z);
    if ((cClip.w - cClip.z) + rNear < 0.0f)
        return FrustumTestResult::Create(false, false);

    float minW = cClip.w - (abs(dX.w) + abs(dY.w) + abs(dZ.w));
    float minNear = (cClip.w - cClip.z) - rNear;
    bool intersectsNear = (minW <= CULL_EPSILON || minNear < 0.0f);

    return FrustumTestResult::Create(true, intersectsNear);
}

// Nanite's MipLevelForRect adapted for 2x2 footprint
static inline int MipLevelForRect(int4 rectPixels, int desiredFootprintPixels = 2)
{
    const int maxPixelOffset = desiredFootprintPixels - 1; // 1
    const int mipOffset = (desiredFootprintPixels == 2) ? 0 : 1;

    int2 mipLevelXY = firstbithigh((uint2)max(rectPixels.zw - rectPixels.xy, int2(0, 0)));
    int mipLevel = max(max(mipLevelXY.x, mipLevelXY.y) - mipOffset, 0);

    mipLevel += any((rectPixels.zw >> mipLevel) - (rectPixels.xy >> mipLevel) > maxPixelOffset) ? 1 : 0;
    return mipLevel;
}

// Evaluates whether a projected clip-space bounding box is visible against the HZB pyramid
static inline bool HZBVisible(float4 clipMin, float4 clipMax, uint hzbMipCount, uint renderWidth, uint renderHeight, uint hzbTexture, uint hzbBaseWidth, uint hzbBaseHeight)
{
    if (clipMin.x > 1.0f || clipMin.y > 1.0f || clipMax.x < -1.0f || clipMax.y < -1.0f)
    {
        return false;
    }

    uint2 hzbBaseSize = uint2(hzbBaseWidth, hzbBaseHeight);
    if (hzbTexture == 0 || hzbTexture == 0xFFFFFFFF || hzbMipCount == 0 || hzbBaseSize.x == 0 || hzbBaseSize.y == 0)
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

    // Convert from normalized UV to HZB Mip 0 texels:
    float2 hzbSize = float2(hzbBaseSize);
    int4 hzbTexels;
    hzbTexels.xy = max((int2) floor(rectUV.xy * hzbSize), int2(0, 0));
    hzbTexels.zw = min((int2) ceil(rectUV.zw * hzbSize) - 1, int2(hzbBaseSize) - 1);
    hzbTexels.zw = max(hzbTexels.xy, hzbTexels.zw);

    // Determine target mip level for 2x2 footprint (at most 4 texels to sample)
    int hzbLevel = MipLevelForRect(hzbTexels, 2);
    uint hzbMip = min((uint)hzbLevel, hzbMipCount - 1);

    // Transform HZB Mip 0 coordinates to coordinates of selected mip level
    hzbTexels >>= hzbMip;

    int2 mipSize = max(int2(1, 1), int2(hzbBaseSize >> hzbMip));
    hzbTexels.zw = min(hzbTexels.zw, mipSize - 1);
    hzbTexels.xy = min(hzbTexels.xy, hzbTexels.zw);

    int2 minCoord = hzbTexels.xy;
    int2 maxCoord = min(hzbTexels.zw, minCoord + 1);

    Texture2D<float> hzbTex = GET_TEXTURE2D(hzbTexture);

    // 4 point samples (75% reduction in texture instructions vs 16 samples)
    float d00 = hzbTex.mips[hzbMip][int2(minCoord.x, minCoord.y)];
    float d10 = hzbTex.mips[hzbMip][int2(maxCoord.x, minCoord.y)];
    float d01 = hzbTex.mips[hzbMip][int2(minCoord.x, maxCoord.y)];
    float d11 = hzbTex.mips[hzbMip][int2(maxCoord.x, maxCoord.y)];

    float minOccluderDepth = min(min(d00, d10), min(d01, d11));

    // In reversed-Z, clipMax.z is the nearest point of the bounding box to the camera
    // If the object's nearest point is closer than the occluder's furthest point (plus epsilon), it is VISIBLE!
    return (clipMax.z + DEPTH_EPSILON >= minOccluderDepth);
}

// Evaluates screen-space geometric error for meshlet DAG refinement
static inline bool EvaluateLODDetailSufficient(float objectError, float3 sphereCenter, float sphereRadius, float3 cameraPos, float proj11, float screenHeight, float errorThreshold)
{
    float dist = max(length(sphereCenter - cameraPos) - sphereRadius, 0.001f);
    float pixelError = (objectError * proj11 * screenHeight) / (2.0f * dist);
    return pixelError <= errorThreshold;
}

#endif // GHOST_CULL_COMMON_HLSL
