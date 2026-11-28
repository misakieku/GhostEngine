#ifndef GHOST_SHADOW_SAMPLING_HLSL
#define GHOST_SHADOW_SAMPLING_HLSL

#include "EngineResources/Shaders/Common.hlsl"
#include "EngineResources/Shaders/Generated/GPULightData.hlsl"
#include "EngineResources/Shaders/Lighting/ScreenSpaceTracing.hlsl"

typedef float ShadowType;

float SampleShadow_GetTriangleTexelArea(float triangleHeight)
{
    return triangleHeight - 0.5f;
}

void SampleShadow_GetTexelAreas_Tent_3x3(float offset, out float4 computedArea, out float4 computedAreaUncut)
{
    float offset01SquaredHalved = (offset + 0.5f) * (offset + 0.5f) * 0.5f;
    computedAreaUncut.x = computedArea.x = offset01SquaredHalved - offset;
    computedAreaUncut.w = computedArea.w = offset01SquaredHalved;

    computedAreaUncut.y = SampleShadow_GetTriangleTexelArea(1.5f - offset);
    float clampedOffsetLeft = min(offset, 0.0f);
    float areaOfSmallLeftTriangle = clampedOffsetLeft * clampedOffsetLeft;
    computedArea.y = computedAreaUncut.y - areaOfSmallLeftTriangle;

    computedAreaUncut.z = SampleShadow_GetTriangleTexelArea(1.5f + offset);
    float clampedOffsetRight = max(offset, 0.0f);
    float areaOfSmallRightTriangle = clampedOffsetRight * clampedOffsetRight;
    computedArea.z = computedAreaUncut.z - areaOfSmallRightTriangle;
}

void SampleShadow_GetTexelWeights_Tent_5x5(float offset, out float3 texelsWeightsA, out float3 texelsWeightsB)
{
    float4 computedArea_From3texelTriangle;
    float4 computedAreaUncut_From3texelTriangle;
    SampleShadow_GetTexelAreas_Tent_3x3(offset, computedArea_From3texelTriangle, computedAreaUncut_From3texelTriangle);

    // 0.16 is 1.0 / (triangle area = 6.25)
    texelsWeightsA.x = 0.16f * computedArea_From3texelTriangle.x;
    texelsWeightsA.y = 0.16f * computedAreaUncut_From3texelTriangle.y;
    texelsWeightsA.z = 0.16f * (computedArea_From3texelTriangle.y + 1.0f);
    texelsWeightsB.x = 0.16f * (computedArea_From3texelTriangle.z + 1.0f);
    texelsWeightsB.y = 0.16f * computedAreaUncut_From3texelTriangle.z;
    texelsWeightsB.z = 0.16f * computedArea_From3texelTriangle.w;
}

float SampleShadow_PCF5x5(Texture2D<float> shadowAtlas, SamplerComparisonState samp, float4 tileOffsetScale, float2 atlasUV, float receiverDepth, float atlasSize, float texelSize)
{
    float4 shadowMapTexelSize = float4(texelSize, texelSize, atlasSize, atlasSize);
    float2 tentCenterInTexelSpace = atlasUV * shadowMapTexelSize.zw;
    float2 centerOfFetchesInTexelSpace = floor(tentCenterInTexelSpace + 0.5f);
    float2 offsetFromTentCenterToCenterOfFetches = tentCenterInTexelSpace - centerOfFetchesInTexelSpace;

    float3 texelsWeightsU_A, texelsWeightsU_B;
    float3 texelsWeightsV_A, texelsWeightsV_B;
    SampleShadow_GetTexelWeights_Tent_5x5(offsetFromTentCenterToCenterOfFetches.x, texelsWeightsU_A, texelsWeightsU_B);
    SampleShadow_GetTexelWeights_Tent_5x5(offsetFromTentCenterToCenterOfFetches.y, texelsWeightsV_A, texelsWeightsV_B);

    float3 fetchesWeightsU = float3(texelsWeightsU_A.xz, texelsWeightsU_B.y) + float3(texelsWeightsU_A.y, texelsWeightsU_B.xz);
    float3 fetchesWeightsV = float3(texelsWeightsV_A.xz, texelsWeightsV_B.y) + float3(texelsWeightsV_A.y, texelsWeightsV_B.xz);

    float3 fetchesOffsetsU = (float3(texelsWeightsU_A.y, texelsWeightsU_B.xz) / fetchesWeightsU + float3(-2.5f, -0.5f, 1.5f)) * shadowMapTexelSize.xxx;
    float3 fetchesOffsetsV = (float3(texelsWeightsV_A.y, texelsWeightsV_B.xz) / fetchesWeightsV + float3(-2.5f, -0.5f, 1.5f)) * shadowMapTexelSize.yyy;

    float2 bilinearFetchOrigin = centerOfFetchesInTexelSpace * shadowMapTexelSize.xy;
    float2 minFetchUV = tileOffsetScale.xy + 0.5f * texelSize;
    float2 maxFetchUV = tileOffsetScale.xy + tileOffsetScale.zw - 0.5f * texelSize;

    float shadow = 0.0f;
    [unroll]
    for (uint y = 0u; y < 3u; ++y)
    {
        float vCoord = bilinearFetchOrigin.y + fetchesOffsetsV[y];
        float wV = fetchesWeightsV[y];
        [unroll]
        for (uint x = 0u; x < 3u; ++x)
        {
            float2 sampleUV = clamp(float2(bilinearFetchOrigin.x + fetchesOffsetsU[x], vCoord), minFetchUV, maxFetchUV);
            shadow += shadowAtlas.SampleCmpLevelZero(samp, sampleUV, receiverDepth) * (fetchesWeightsU[x] * wV);
        }
    }

    return shadow;
}

float3 GetCubeFaceViewPos(float3 L, uint faceIdx)
{
    float3 v;
    switch (faceIdx)
    {
        case 0u: v = float3(-L.z, -L.y,  L.x); break; // +X
        case 1u: v = float3( L.z, -L.y, -L.x); break; // -X
        case 2u: v = float3( L.x,  L.z,  L.y); break; // +Y
        case 3u: v = float3( L.x, -L.z, -L.y); break; // -Y
        case 4u: v = float3( L.x, -L.y,  L.z); break; // +Z
        default: v = float3(-L.x, -L.y, -L.z); break; // -Z
    }

    return v;
}

uint GetCubeFaceID(float3 L)
{
    float3 absL = abs(L);
    uint faceIdx;
    if (absL.x > absL.y && absL.x > absL.z)
    {
        faceIdx = (L.x > 0.0f) ? 0u : 1u;
    }
    else if (absL.y > absL.z)
    {
        faceIdx = (L.y > 0.0f) ? 2u : 3u;
    }
    else
    {
        faceIdx = (L.z > 0.0f) ? 4u : 5u;
    }

    return faceIdx;
}

#if defined(_SHADOW_SHADOWMAP_LOW)
struct GPUShadowMatrix
{
    float4x4 shadowViewProj;
};

ShadowType SamplePunctualShadowView(float3 positionRWS, float3 N, float4 distance, float clampedSlope, uint shadowIndex,
    ByteAddressBuffer shadowViewsBuf, Texture2D<float> shadowAtlasTex)
{
    uint sampOffset = sizeof(GPUShadowViewData) * shadowIndex + sizeof(GPUShadowCullingData);
    float4x4 shadowViewProj = shadowViewsBuf.Load<GPUShadowMatrix>(sampOffset).shadowViewProj;
    float4 tailScalars = asfloat(shadowViewsBuf.Load4(sampOffset + 112u)); // x: worldTexelSize, y: normalBias, z: slopeBias, w: shadowFade

    float3 biasedPosRWS = positionRWS + N * (tailScalars.y * distance.x);
    float4 clipPos = mul(shadowViewProj, float4(biasedPosRWS, 1.0f));
    if (clipPos.w <= 0.0001f)
    {
        return (ShadowType)1.0f;
    }

    float3 ndc = clipPos.xyz / clipPos.w;
    float2 uv = ndc.xy * float2(0.5f, -0.5f) + 0.5f;
    if (any(uv < 0.0f) || any(uv > 1.0f))
    {
        return (ShadowType)1.0f;
    }

    float projW = asfloat(shadowViewsBuf.Load(sampOffset + 92u)); // shadowProjection.w
    float4 tileOffsetScale = asfloat(shadowViewsBuf.Load4(sampOffset + 64u));
    float2 atlasSize = asfloat(shadowViewsBuf.Load2(sampOffset + 96u)); // size.xy

    float ndcDepthScale = abs(projW) * distance.z * distance.z;
    float receiverDepth = ndc.z + (1.0f / 65536.0f) + (tailScalars.z * clampedSlope * ndcDepthScale);

    uv = saturate(uv);
    float2 atlasUV = tileOffsetScale.xy + uv * tileOffsetScale.zw;
    ShadowType shadow = SampleShadow_PCF5x5(shadowAtlasTex, GET_SAMPLER(LINEAR_CLAMP_GE_SAMPLER), tileOffsetScale, atlasUV, receiverDepth, atlasSize.x, atlasSize.y);
    return lerp(1.0f, shadow, tailScalars.w);
}

ShadowType SampleDirectionalCascade(float3 positionRWS, float3 N, float clampedSlope, uint cascadeIdx,
    ByteAddressBuffer directionalShadowViewsBuf, Texture2D<float> shadowAtlasTex, SamplerComparisonState samp)
{
    uint sampOffset = sizeof(GPUShadowViewData) * cascadeIdx + sizeof(GPUShadowCullingData);
    float4x4 shadowViewProj = directionalShadowViewsBuf.Load<GPUShadowMatrix>(sampOffset).shadowViewProj;
    float2 biasPair = asfloat(directionalShadowViewsBuf.Load2(sampOffset + 116u)); // x: normalBias, y: slopeBias

    float3 biasedPosRWS = positionRWS + N * biasPair.x;
    float4 clipPos = mul(shadowViewProj, float4(biasedPosRWS, 1.0f));
    float2 uv = clipPos.xy * float2(0.5f, -0.5f) + 0.5f;
    if (any(uv < 0.0f) || any(uv > 1.0f))
    {
        return (ShadowType)1.0f;
    }

    float4 tileOffsetScale = asfloat(directionalShadowViewsBuf.Load4(sampOffset + 64u));
    float2 atlasSize = asfloat(directionalShadowViewsBuf.Load2(sampOffset + 96u));
    float receiverDepth = clipPos.z + (1.0f / 65536.0f) + (biasPair.y * clampedSlope);

    float2 atlasUV = tileOffsetScale.xy + uv * tileOffsetScale.zw;
    return SampleShadow_PCF5x5(shadowAtlasTex, samp, tileOffsetScale, atlasUV, receiverDepth, atlasSize.x, atlasSize.y);
}

ShadowType SampleDirectionalCascadeScalarized(float3 positionRWS, float3 N, float clampedSlope, uint cascadeIdx,
    ByteAddressBuffer dirViewsBuf, Texture2D<float> shadowAtlasTex, SamplerComparisonState samp)
{
    if (WaveActiveAllEqual(cascadeIdx))
    {
        return SampleDirectionalCascade(positionRWS, N, clampedSlope, WaveReadLaneFirst(cascadeIdx), dirViewsBuf, shadowAtlasTex, samp);
    }

    ShadowType result = (ShadowType)1.0f;
    bool done = false;
    [loop]
    for (uint iter = 0u; iter < 64u; ++iter)
    {
        uint4 ballot = WaveActiveBallot(!done);
        if (ballot.x == 0u && ballot.y == 0u)
        {
            break;
        }
        uint firstLane = (ballot.x != 0u) ? firstbitlow(ballot.x) : (32u + firstbitlow(ballot.y));
        uint uniformCascade = WaveReadLaneAt(cascadeIdx, firstLane);
        if (!done && cascadeIdx == uniformCascade)
        {
            done = true;
            result = SampleDirectionalCascade(positionRWS, N, clampedSlope, uniformCascade, dirViewsBuf, shadowAtlasTex, samp);
        }
    }
    return result;
}
#endif

ShadowType GetPunctualShadowAttenuation(float3 positionRWS, float3 N, float3 L, float4 distance, uint lightType,
    uint lightIndex, BYTE_ADDRESS_BUFFER shadowIndicesBuffer, BYTE_ADDRESS_BUFFER shadowViewsBuffer, TEXTURE2D shadowAtlas)
{
    ShadowType shadow = (ShadowType)1.0f;
#if defined(_SHADOW_SHADOWMAP_LOW)
    int shadowIndex = -1;

    if (IS_VALID_BUFFER(shadowIndicesBuffer))
    {
        shadowIndex = LoadData<int>(shadowIndicesBuffer, lightIndex);
    }

    if (shadowIndex >= 0 && IS_VALID_TEXTURE(shadowAtlas) && IS_VALID_BUFFER(shadowViewsBuffer))
    {
        if (lightType == GPULIGHT_TYPE_POINT)
        {
            shadowIndex += GetCubeFaceID(-L);
        }

        float NdotL = saturate(dot(N, L));
        float slope = sqrt(saturate(1.0f - NdotL * NdotL)) / max(NdotL, 0.1f);
        float clampedSlope = min(slope, 2.0f);

        ByteAddressBuffer shadowViewsBuf = GET_BUFFER(shadowViewsBuffer);
        Texture2D<float> shadowAtlasTex = GET_TEXTURE2D(shadowAtlas);

        uint targetIdx = (uint)shadowIndex;
        if (WaveActiveAllEqual(targetIdx))
        {
            shadow = SamplePunctualShadowView(positionRWS, N, distance, clampedSlope, WaveReadLaneFirst(targetIdx), shadowViewsBuf, shadowAtlasTex);
        }
        else
        {
            bool scalarDone = false;
            [loop]
            for (uint scalarIter = 0u; scalarIter < 64u; ++scalarIter)
            {
                uint4 ballot = WaveActiveBallot(!scalarDone);
                if (ballot.x == 0u && ballot.y == 0u)
                {
                    break;
                }
                uint firstLane = (ballot.x != 0u) ? firstbitlow(ballot.x) : (32u + firstbitlow(ballot.y));
                uint uniformIdx = WaveReadLaneAt(targetIdx, firstLane);
                if (!scalarDone && targetIdx == uniformIdx)
                {
                    scalarDone = true;
                    shadow = SamplePunctualShadowView(positionRWS, N, distance, clampedSlope, uniformIdx, shadowViewsBuf, shadowAtlasTex);
                }
            }
        }
    }
#elif defined(_SHADOW_RAYTRACED)
#endif

    return shadow;
}

ShadowType GetPunctualShadowAttenuation(float3 positionRWS, float3 N, float3 L, float4 distance, GPUPunctualLight light,
    uint lightIndex, BYTE_ADDRESS_BUFFER shadowIndicesBuffer, BYTE_ADDRESS_BUFFER shadowViewsBuffer, TEXTURE2D shadowAtlas)
{
    return GetPunctualShadowAttenuation(positionRWS, N, L, distance, light.lightTypeAndShadowSize & 0xFu, lightIndex, shadowIndicesBuffer, shadowViewsBuffer, shadowAtlas);
}

ShadowType GetDirectionalShadowAttenuation(float3 positionRWS, float3 N, float3 L,
    BYTE_ADDRESS_BUFFER directionalShadowViewsBuffer, TEXTURE2D directionalShadowAtlas,
    uint cascadeCount, float4 cascadeSplits, float4 cascadeBlends, float maxShadowDistance, float shadowFadeRange)
{
    ShadowType shadow = (ShadowType)1.0f;
#if defined(_SHADOW_SHADOWMAP_LOW)
    if (!IS_VALID_TEXTURE(directionalShadowAtlas) || !IS_VALID_BUFFER(directionalShadowViewsBuffer) || cascadeCount == 0u)
    {
        return shadow;
    }

    float zView = dot(positionRWS, g_ViewData.cameraDirection);
    if (zView > maxShadowDistance || zView < 0.0f)
    {
        return shadow;
    }

    uint cascadeIdx = 0u;
    [unroll]
    for (uint c = 0u; c < 4u; ++c)
    {
        if (c < cascadeCount && zView > cascadeSplits[c])
        {
            cascadeIdx = c + 1u;
        }
    }

    if (cascadeIdx >= cascadeCount)
    {
        return shadow;
    }

    ByteAddressBuffer dirViewsBuf = GET_BUFFER(directionalShadowViewsBuffer);
    Texture2D<float> shadowAtlasTex = GET_TEXTURE2D(directionalShadowAtlas);
    SamplerComparisonState samp = GET_SAMPLER(LINEAR_CLAMP_GE_SAMPLER);

    float NdotL = saturate(dot(N, L));
    float slope = sqrt(saturate(1.0f - NdotL * NdotL)) / max(NdotL, 0.1f);
    float clampedSlope = min(slope, 2.0f);

    shadow = SampleDirectionalCascadeScalarized(positionRWS, N, clampedSlope, cascadeIdx, dirViewsBuf, shadowAtlasTex, samp);

    if (cascadeIdx + 1u < cascadeCount)
    {
        float splitDist = cascadeSplits[cascadeIdx];
        float blendFactor = cascadeBlends[cascadeIdx];
        float blendRange = splitDist * blendFactor;
        float blendStart = splitDist - blendRange;

        if (zView > blendStart && blendRange > 0.001f)
        {
            float alpha = saturate((zView - blendStart) / blendRange);
            uint nextCascade = cascadeIdx + 1u;
            ShadowType nextShadow = SampleDirectionalCascadeScalarized(positionRWS, N, clampedSlope, nextCascade, dirViewsBuf, shadowAtlasTex, samp);
            shadow = lerp(shadow, nextShadow, alpha);
        }
    }

    if (shadowFadeRange > 0.001f)
    {
        float fadeStart = maxShadowDistance - shadowFadeRange;
        if (zView > fadeStart)
        {
            float fadeAlpha = saturate((zView - fadeStart) / shadowFadeRange);
            shadow = lerp(shadow, 1.0f, fadeAlpha);
        }
    }
#elif defined(_SHADOW_RAYTRACED)
#endif

    return shadow;
}

#endif // GHOST_SHADOW_SAMPLING_HLSL
