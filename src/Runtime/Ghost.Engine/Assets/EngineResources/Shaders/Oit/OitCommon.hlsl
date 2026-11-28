#ifndef GHOST_OIT_COMMON_HLSL
#define GHOST_OIT_COMMON_HLSL

#include "EngineResources/Shaders/Common.hlsl"

#define OIT_WARP_FLAG_OCCUPIED 1u
#define OIT_WARP_FLAG_RANGE_START 2u
#define OIT_WARP_FLAG_RANGE_END 4u

#define OIT_TILE_SCALE 8u
#define OIT_OCC_TILE_SIZE 8u
#define OIT_ZERO_TRANS_QUAD_SIZE 16u
#define OIT_TILE_DWORD_STRIDE 5u
#define OIT_TILE_DWORD_FLAGS 0u
#define OIT_TILE_DWORD_SCALAR_MAX 1u
#define OIT_TILE_DWORD_SCALAR_INV_MIN 2u
#define OIT_TILE_DWORD_RGB_MAX 3u
#define OIT_TILE_DWORD_RGB_INV_MIN 4u

#define OIT_INDIRECT_CLEAR_EXTINCTION 0u
#define OIT_INDIRECT_INTEGRATE_EXTINCTION 16u
#define OIT_INDIRECT_SPAWN_QUADS 32u
#define OIT_INDIRECT_EARLY_DEPTH_PRIME 48u
#define OIT_INDIRECT_RESOLVE 64u
#define OIT_INDIRECT_BUFFER_BYTE_SIZE 80u

#define OIT_EXTINCTION_MAX 5.5412635f // -log(1.0 / 255.0)
#define OIT_INV_EXTINCTION_MAX (1.0f / OIT_EXTINCTION_MAX)
#define OIT_RGB_CHANNEL_MAX 255u

struct OitWarpLutHeader
{
    uint virtualSliceCount;
    uint halvingShift;
    uint physicalSliceCount;
    float linearizationFactor;
    float farPlane;
};

OitWarpLutHeader LoadOitWarpLutHeader(ByteAddressBuffer depthWarpLut)
{
    uint4 raw = depthWarpLut.Load4(0u);
    OitWarpLutHeader header;
    header.virtualSliceCount = raw.x & 0xFFFFu;
    header.halvingShift = raw.x >> 16u;
    header.physicalSliceCount = raw.y;
    header.linearizationFactor = asfloat(raw.z);
    header.farPlane = asfloat(raw.w);
    return header;
}

bool OitTryGetTilePhysicalSliceRange(ByteAddressBuffer tileFlagsBuf, ByteAddressBuffer depthWarpLut, OitWarpLutHeader header, uint tileIndex, uint maxOffsetDword, uint minOffsetDword, out uint startPhys, out uint endPhys)
{
    uint baseByte = tileIndex * (OIT_TILE_DWORD_STRIDE * 4u);
    uint maxPlus1 = tileFlagsBuf.Load(baseByte + maxOffsetDword * 4u);
    uint invMin = tileFlagsBuf.Load(baseByte + minOffsetDword * 4u);
    if (maxPlus1 == 0u || invMin == 0u || header.virtualSliceCount == 0u || header.physicalSliceCount == 0u)
    {
        startPhys = 0u;
        endPhys = 0u;
        return false;
    }

    uint minVirtOrig = 0xFFFFFFFFu - invMin;
    uint maxVirtOrig = maxPlus1 - 1u;
    if (minVirtOrig > maxVirtOrig)
    {
        startPhys = 0u;
        endPhys = 0u;
        return false;
    }

    uint minVirtCompacted = min(minVirtOrig >> header.halvingShift, header.virtualSliceCount - 1u);
    uint maxVirtCompacted = min(maxVirtOrig >> header.halvingShift, header.virtualSliceCount - 1u);

    uint minEntry = depthWarpLut.Load((4u + minVirtCompacted) * 4u);
    uint maxEntry = depthWarpLut.Load((4u + maxVirtCompacted) * 4u);
    if (((minEntry >> 16u) & OIT_WARP_FLAG_OCCUPIED) == 0u || ((maxEntry >> 16u) & OIT_WARP_FLAG_OCCUPIED) == 0u)
    {
        startPhys = 0u;
        endPhys = 0u;
        return false;
    }

    uint physMin = minEntry & 0xFFFFu;
    uint physMax = maxEntry & 0xFFFFu;
    uint lo = min(physMin, physMax);
    uint hi = max(physMin, physMax);

    // Pad by 1 physical slice on both sides for linear splatting (s0 + 1) and FP boundary rounding.
    startPhys = (lo > 0u) ? (lo - 1u) : 0u;
    endPhys = min(hi + 1u, header.physicalSliceCount - 1u);
    return true;
}

float OitDepthToVirtualSlice(float viewZ, float linearizationFactor, float farPlane, uint virtualSliceCount)
{
    float a = max(linearizationFactor, 1e-4f);
    float b = max(farPlane, a + 1e-3f);
    float denom = max(log2(b / a + 1.0f), 1e-5f);
    float normZ = saturate(log2(max(viewZ, 0.0f) / a + 1.0f) / denom);
    return normZ * float(virtualSliceCount);
}

float OitVirtualSliceToDepth(float virtualSlice, float linearizationFactor, float farPlane, uint virtualSliceCount)
{
    float a = max(linearizationFactor, 1e-4f);
    float b = max(farPlane, a + 1e-3f);
    float normZ = clamp(virtualSlice / max(float(virtualSliceCount), 1.0f), 0.0f, 1.0f);
    return a * (exp2(normZ * log2(b / a + 1.0f)) - 1.0f);
}

bool OitMapVirtualSliceToPhysical(ByteAddressBuffer depthWarpLut, float virtualSliceF, out float physicalSliceF, out uint slabStartPhys)
{
    OitWarpLutHeader header = LoadOitWarpLutHeader(depthWarpLut);
    if (header.virtualSliceCount == 0u || header.physicalSliceCount == 0u)
    {
        physicalSliceF = 0.0f;
        slabStartPhys = 0u;
        return false;
    }

    float clampedSlice = clamp(virtualSliceF, 0.0f, float(header.virtualSliceCount) - 1e-4f);
    uint sliceIdx = min((uint)floor(clampedSlice), header.virtualSliceCount - 1u);
    float frac = clampedSlice - float(sliceIdx);

    uint entry = depthWarpLut.Load((4u + sliceIdx) * 4u);
    uint phys0 = entry & 0xFFFFu;
    uint flags = (entry >> 16u) & 0xFFu;
    slabStartPhys = (entry >> 24u) & 0xFFu;

    bool isOccupied = (flags & OIT_WARP_FLAG_OCCUPIED) != 0u;
    if (!isOccupied)
    {
        physicalSliceF = float(phys0);
        return false;
    }

    // Snap fractional interpolation at range end so filtering does not bleed across empty gaps.
    float effectiveFrac = ((flags & OIT_WARP_FLAG_RANGE_END) != 0u) ? 0.0f : frac;
    physicalSliceF = min(float(phys0) + effectiveFrac, float(header.physicalSliceCount - 1u));
    return true;
}

bool OitMapVirtualSliceToPhysical(ByteAddressBuffer depthWarpLut, float virtualSliceF, out float physicalSliceF)
{
    uint unusedSlabStart = 0u;
    return OitMapVirtualSliceToPhysical(depthWarpLut, virtualSliceF, physicalSliceF, unusedSlabStart);
}

float OitScalarAlphaToNormalizedExtinction(float alpha)
{
    float transmittance = max(1.0f - saturate(alpha), 1.0f / 255.0f);
    return saturate(-log(transmittance) * OIT_INV_EXTINCTION_MAX);
}

float3 OitRGBTransmittanceToNormalizedExtinction(float3 transmittance)
{
    float3 clampedT = max(saturate(transmittance), float3(1.0f / 255.0f, 1.0f / 255.0f, 1.0f / 255.0f));
    return saturate(-log(clampedT) * OIT_INV_EXTINCTION_MAX);
}

float3 OitAlphaToRawExtinction(float3 alphaRGB)
{
    float3 clampedT = max(1.0f - saturate(alphaRGB), float3(1e-4f, 1e-4f, 1e-4f));
    return -log(clampedT);
}

float3 OitNormalizedExtinctionToTransmittance(float3 normExtinction)
{
    return exp(-saturate(normExtinction) * OIT_EXTINCTION_MAX);
}

void OitSplatScalarSlice(RWTexture3D<uint> scalarExtinctionUav, RWTexture2D<uint> scalarOverflowMinUav, uint2 oitCoord, uint sliceIdx, uint uintExt)
{
    if (uintExt == 0u)
    {
        return;
    }

    uint dwordIdx = sliceIdx >> 2u;
    uint wordIdx = sliceIdx & 3u;
    uint packed = (uintExt & 0xFFu) << (wordIdx * 8u);

    uint prevData = 0u;
    InterlockedAdd(scalarExtinctionUav[uint3(oitCoord, dwordIdx)], packed, prevData);

    uint prevVal = (prevData >> (wordIdx * 8u)) & 0xFFu;
    if (prevVal + uintExt > 255u)
    {
        InterlockedMin(scalarOverflowMinUav[oitCoord], sliceIdx);
    }
}

void OitSplatScalarExtinction(
    RWTexture3D<uint> scalarExtinctionUav,
    RWTexture2D<uint> scalarOverflowMinUav,
    uint2 oitCoord,
    float physicalSliceF,
    uint physicalSliceCount,
    float alpha)
{
    float normExt = OitScalarAlphaToNormalizedExtinction(alpha);
    if (normExt <= 0.0f || physicalSliceCount == 0u)
    {
        return;
    }

    uint s0 = min((uint)floor(physicalSliceF), physicalSliceCount - 1u);
    uint s1 = min(s0 + 1u, physicalSliceCount - 1u);
    float frac = saturate(physicalSliceF - float(s0));

    if (s0 == s1 || frac <= 0.001f)
    {
        uint u0 = (uint)(255.0f * normExt + 0.5f);
        OitSplatScalarSlice(scalarExtinctionUav, scalarOverflowMinUav, oitCoord, s0, u0);
        return;
    }

    float ext0 = normExt * (1.0f - frac);
    float ext1 = normExt * frac;
    uint u0 = (uint)(255.0f * ext0 + 0.5f);
    uint u1 = (uint)(255.0f * ext1 + 0.5f);

    uint dword0 = s0 >> 2u;
    uint dword1 = s1 >> 2u;

    if (dword0 == dword1)
    {
        // Merge both adjacent slices into a single atomic when inside the same packed DWORD.
        uint word0 = s0 & 3u;
        uint word1 = s1 & 3u;
        uint packed = ((u0 & 0xFFu) << (word0 * 8u)) | ((u1 & 0xFFu) << (word1 * 8u));
        if (packed != 0u)
        {
            uint prevData = 0u;
            InterlockedAdd(scalarExtinctionUav[uint3(oitCoord, dword0)], packed, prevData);

            uint prev0 = (prevData >> (word0 * 8u)) & 0xFFu;
            uint prev1 = (prevData >> (word1 * 8u)) & 0xFFu;
            if (prev0 + u0 > 255u)
            {
                InterlockedMin(scalarOverflowMinUav[oitCoord], s0);
            }
            if (prev1 + u1 > 255u)
            {
                InterlockedMin(scalarOverflowMinUav[oitCoord], s1);
            }
        }
    }
    else
    {
        OitSplatScalarSlice(scalarExtinctionUav, scalarOverflowMinUav, oitCoord, s0, u0);
        OitSplatScalarSlice(scalarExtinctionUav, scalarOverflowMinUav, oitCoord, s1, u1);
    }
}

void OitSplatRGBSlice(RWTexture3D<uint> rgbExtinctionUav, RWTexture3D<uint> rgbOverflowMinUav, uint2 oitCoord, uint sliceIdx, uint3 uintExtRGB)
{
    uint packed = (min(uintExtRGB.r, 255u) & 0x3FFu)
                | ((min(uintExtRGB.g, 255u) & 0x3FFu) << 10u)
                | ((min(uintExtRGB.b, 255u) & 0x3FFu) << 20u);

    if (packed == 0u)
    {
        return;
    }

    uint prevData = 0u;
    InterlockedAdd(rgbExtinctionUav[uint3(oitCoord, sliceIdx)], packed, prevData);

    uint prevR = prevData & 0x3FFu;
    uint prevG = (prevData >> 10u) & 0x3FFu;
    uint prevB = (prevData >> 20u) & 0x3FFu;

    if (prevR + uintExtRGB.r > OIT_RGB_CHANNEL_MAX)
    {
        InterlockedMin(rgbOverflowMinUav[uint3(oitCoord, 0u)], sliceIdx);
    }
    if (prevG + uintExtRGB.g > OIT_RGB_CHANNEL_MAX)
    {
        InterlockedMin(rgbOverflowMinUav[uint3(oitCoord, 1u)], sliceIdx);
    }
    if (prevB + uintExtRGB.b > OIT_RGB_CHANNEL_MAX)
    {
        InterlockedMin(rgbOverflowMinUav[uint3(oitCoord, 2u)], sliceIdx);
    }
}

void OitSplatRGBExtinction(
    RWTexture3D<uint> rgbExtinctionUav,
    RWTexture3D<uint> rgbOverflowMinUav,
    uint2 oitCoord,
    float physicalSliceF,
    uint physicalSliceCount,
    float3 alphaRGB)
{
    float3 normExt = OitRGBTransmittanceToNormalizedExtinction(1.0f - saturate(alphaRGB));
    if (all(normExt <= 0.0f) || physicalSliceCount == 0u)
    {
        return;
    }

    uint s0 = min((uint)floor(physicalSliceF), physicalSliceCount - 1u);
    uint s1 = min(s0 + 1u, physicalSliceCount - 1u);
    float frac = saturate(physicalSliceF - float(s0));

    if (s0 == s1 || frac <= 0.001f)
    {
        uint3 u0 = (uint3)(255.0f * normExt + 0.5f);
        OitSplatRGBSlice(rgbExtinctionUav, rgbOverflowMinUav, oitCoord, s0, u0);
        return;
    }

    uint3 u0 = (uint3)(255.0f * (normExt * (1.0f - frac)) + 0.5f);
    uint3 u1 = (uint3)(255.0f * (normExt * frac) + 0.5f);
    OitSplatRGBSlice(rgbExtinctionUav, rgbOverflowMinUav, oitCoord, s0, u0);
    OitSplatRGBSlice(rgbExtinctionUav, rgbOverflowMinUav, oitCoord, s1, u1);
}

float3 OitSampleTransmittanceWeight(
    TEXTURE3D integralLutSrv,
    BYTE_ADDRESS_BUFFER depthWarpLutSrv,
    float2 screenUV,
    float viewZ,
    float sampleBias)
{
    if (!IS_VALID_TEXTURE(integralLutSrv) || !IS_VALID_BUFFER(depthWarpLutSrv))
    {
        return float3(1.0f, 1.0f, 1.0f);
    }

    ByteAddressBuffer warpLut = GET_BUFFER(depthWarpLutSrv);
    OitWarpLutHeader header = LoadOitWarpLutHeader(warpLut);
    if (header.virtualSliceCount == 0u || header.physicalSliceCount == 0u)
    {
        return float3(1.0f, 1.0f, 1.0f);
    }

    float virtualSliceF = OitDepthToVirtualSlice(viewZ, header.linearizationFactor, header.farPlane, header.virtualSliceCount);
    float physicalSliceF = 0.0f;
    uint slabStartPhys = 0u;
    if (!OitMapVirtualSliceToPhysical(warpLut, virtualSliceF, physicalSliceF, slabStartPhys))
    {
        return float3(1.0f, 1.0f, 1.0f);
    }

    // Clamp negative bias at the slab boundary so prior disjoint slabs are never skipped.
    float biasedSlice = max(physicalSliceF + sampleBias, float(slabStartPhys) - 1.0f);
    if (biasedSlice < 0.0f)
    {
        return float3(1.0f, 1.0f, 1.0f);
    }

    Texture3D<float4> integralLut = GET_TEXTURE3D(integralLutSrv);
    uint lutW = 1u;
    uint lutH = 1u;
    uint lutD = 1u;
    integralLut.GetDimensions(lutW, lutH, lutD);

    float maxPhysSlice = max(float(header.physicalSliceCount) - 1.0f, 0.0f);
    float uvwZ = (min(biasedSlice, maxPhysSlice) + 0.5f) / float(max(lutD, 1u));
    SamplerState samp = GET_SAMPLER(LINEAR_CLAMP_SAMPLER);
    return saturate(integralLut.SampleLevel(samp, float3(screenUV, uvwZ), 0.0f).rgb);
}

float OitDeviceDepthToViewDepth(float deviceDepth, float nearClip, float farClip)
{
    float n = max(nearClip, 1e-4f);
    float f = max(farClip, n + 1e-3f);
    return (n * f) / max((f - n) * deviceDepth + n, 1e-6f);
}

float OitViewDepthToDeviceDepth(float viewZ, float nearClip, float farClip)
{
    float n = max(nearClip, 1e-4f);
    float f = max(farClip, n + 1e-3f);
    if (viewZ >= f)
    {
        return 0.0f;
    }
    float clampedZ = max(viewZ, n);
    return saturate((n * (f - clampedZ)) / max((f - n) * clampedZ, 1e-6f));
}

#endif // GHOST_OIT_COMMON_HLSL
