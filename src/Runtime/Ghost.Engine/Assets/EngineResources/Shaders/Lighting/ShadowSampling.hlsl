#ifndef GHOST_SHADOW_SAMPLING_HLSL
#define GHOST_SHADOW_SAMPLING_HLSL

#include "EngineResources/Shaders/Generated/GPULightData.hlsl"


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

void SampleShadow_ComputeSamples_Tent_5x5(float4 shadowMapTexelSize, float2 coord, out float fetchesWeights[9], out float2 fetchesUV[9])
{
    // shadowMapTexelSize: xy = (1/atlasWidth, 1/atlasHeight), zw = (atlasWidth, atlasHeight)
    float2 tentCenterInTexelSpace = coord * shadowMapTexelSize.zw;
    float2 centerOfFetchesInTexelSpace = floor(tentCenterInTexelSpace + 0.5f);
    float2 offsetFromTentCenterToCenterOfFetches = tentCenterInTexelSpace - centerOfFetchesInTexelSpace;

    float3 texelsWeightsU_A, texelsWeightsU_B;
    float3 texelsWeightsV_A, texelsWeightsV_B;
    SampleShadow_GetTexelWeights_Tent_5x5(offsetFromTentCenterToCenterOfFetches.x, texelsWeightsU_A, texelsWeightsU_B);
    SampleShadow_GetTexelWeights_Tent_5x5(offsetFromTentCenterToCenterOfFetches.y, texelsWeightsV_A, texelsWeightsV_B);

    float3 fetchesWeightsU = float3(texelsWeightsU_A.xz, texelsWeightsU_B.y) + float3(texelsWeightsU_A.y, texelsWeightsU_B.xz);
    float3 fetchesWeightsV = float3(texelsWeightsV_A.xz, texelsWeightsV_B.y) + float3(texelsWeightsV_A.y, texelsWeightsV_B.xz);

    float3 fetchesOffsetsU = float3(texelsWeightsU_A.y, texelsWeightsU_B.xz) / fetchesWeightsU.xyz + float3(-2.5f, -0.5f, 1.5f);
    float3 fetchesOffsetsV = float3(texelsWeightsV_A.y, texelsWeightsV_B.xz) / fetchesWeightsV.xyz + float3(-2.5f, -0.5f, 1.5f);
    fetchesOffsetsU *= shadowMapTexelSize.xxx;
    fetchesOffsetsV *= shadowMapTexelSize.yyy;

    float2 bilinearFetchOrigin = centerOfFetchesInTexelSpace * shadowMapTexelSize.xy;

    fetchesUV[0] = bilinearFetchOrigin + float2(fetchesOffsetsU.x, fetchesOffsetsV.x);
    fetchesUV[1] = bilinearFetchOrigin + float2(fetchesOffsetsU.y, fetchesOffsetsV.x);
    fetchesUV[2] = bilinearFetchOrigin + float2(fetchesOffsetsU.z, fetchesOffsetsV.x);
    fetchesUV[3] = bilinearFetchOrigin + float2(fetchesOffsetsU.x, fetchesOffsetsV.y);
    fetchesUV[4] = bilinearFetchOrigin + float2(fetchesOffsetsU.y, fetchesOffsetsV.y);
    fetchesUV[5] = bilinearFetchOrigin + float2(fetchesOffsetsU.z, fetchesOffsetsV.y);
    fetchesUV[6] = bilinearFetchOrigin + float2(fetchesOffsetsU.x, fetchesOffsetsV.z);
    fetchesUV[7] = bilinearFetchOrigin + float2(fetchesOffsetsU.y, fetchesOffsetsV.z);
    fetchesUV[8] = bilinearFetchOrigin + float2(fetchesOffsetsU.z, fetchesOffsetsV.z);

    fetchesWeights[0] = fetchesWeightsU.x * fetchesWeightsV.x;
    fetchesWeights[1] = fetchesWeightsU.y * fetchesWeightsV.x;
    fetchesWeights[2] = fetchesWeightsU.z * fetchesWeightsV.x;
    fetchesWeights[3] = fetchesWeightsU.x * fetchesWeightsV.y;
    fetchesWeights[4] = fetchesWeightsU.y * fetchesWeightsV.y;
    fetchesWeights[5] = fetchesWeightsU.z * fetchesWeightsV.y;
    fetchesWeights[6] = fetchesWeightsU.x * fetchesWeightsV.z;
    fetchesWeights[7] = fetchesWeightsU.y * fetchesWeightsV.z;
    fetchesWeights[8] = fetchesWeightsU.z * fetchesWeightsV.z;
}

float SampleShadow_PCF5x5(Texture2D<float> shadowAtlas, SamplerComparisonState samp, float4 tileOffsetScale, float2 atlasUV, float receiverDepth, float atlasSize, float texelSize)
{
    float4 shadowMapTexelSize = float4(texelSize, texelSize, atlasSize, atlasSize);

    float fetchesWeights[9];
    float2 fetchesUV[9];
    SampleShadow_ComputeSamples_Tent_5x5(shadowMapTexelSize, atlasUV, fetchesWeights, fetchesUV);

    // Keep bilinear taps (2x2 footprint) safely inside the atlas tile
    float2 minFetchUV = tileOffsetScale.xy + 0.5f * texelSize;
    float2 maxFetchUV = tileOffsetScale.xy + tileOffsetScale.zw - 0.5f * texelSize;

    float shadow = 0.0f;
    [unroll]
    for (int i = 0; i < 9; ++i)
    {
        float2 sampleUV = clamp(fetchesUV[i], minFetchUV, maxFetchUV);
        shadow += shadowAtlas.SampleCmpLevelZero(samp, sampleUV, receiverDepth) * fetchesWeights[i];
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

float GetPunctualShadowAttenuation(ShadingContext ctx, GPUPunctualLight light, float3 N, float3 L, float4 distance, float3 lightPositonRWS, uint lightIndex)
{
    float shadow = 1.0f;
    int shadowIndex = -1;

    if (IS_VALID_BUFFER(ctx.shadowIndicesBufferIndex))
    {
        shadowIndex = LoadData<int>(ctx.shadowIndicesBufferIndex, lightIndex);
    }

    if (shadowIndex >= 0 && IS_VALID_BUFFER(ctx.shadowAtlasIndex) && IS_VALID_BUFFER(ctx.shadowViewsBufferIndex))
    {
        uint lightType = light.lightTypeAndShadowSize & 0xFu;
        if (lightType == GPULIGHT_TYPE_POINT) // Point Light cubemap face selection
        {
            shadowIndex += GetCubeFaceID(-L);
        }

        GPUShadowSamplingData shadowCull = LoadDataAtOffset<GPUShadowSamplingData>(ctx.shadowViewsBufferIndex, sizeof(GPUShadowViewData) * shadowIndex + sizeof(GPUShadowCullingData));

        float3 biasedPosRWS = ctx.positionRWS + N * shadowCull.normalBias * shadowCull.worldTexelSize * distance.x;
        float4 clipPos = mul(shadowCull.shadowViewProj, float4(biasedPosRWS, 1.0f));

        if (clipPos.w > 0.0001f)
        {
            float3 ndc = clipPos.xyz / clipPos.w;
            float2 uv = ndc.xy * float2(0.5f, -0.5f) + 0.5f;

            float NdotL = saturate(dot(N, L));
            float slope = sqrt(saturate(1.0f - NdotL * NdotL)) / max(NdotL, 0.1f);
            float clampedSlope = min(slope, 2.0f);

            // In Reversed-Z perspective, a depth delta in NDC scales as 1 / (distance^2):
            float ndcDepthScale = abs(shadowCull.shadowProjection.w) * distance.z * distance.z;
            float receiverDepth = ndc.z + (1.0f / 65536.0f) + (shadowCull.slopeBias * clampedSlope * shadowCull.worldTexelSize * ndcDepthScale);

            // Spot lights check cone bounds; Point lights cover all 360 deg, so clamp instead of discarding
            if (all(uv >= 0.0f) && all(uv <= 1.0f))
            {
                uv = saturate(uv);
                float2 atlasUV = shadowCull.tileOffsetScale.xy + uv * shadowCull.tileOffsetScale.zw;
                Texture2D<float> shadowAtlas = GET_TEXTURE2D(ctx.shadowAtlasIndex);

                shadow = SampleShadow_PCF5x5(shadowAtlas, GET_SAMPLER(LinearClampCompareGESampler), shadowCull.tileOffsetScale, atlasUV, receiverDepth, shadowCull.size.x, shadowCull.size.y);
            }
        }
    }

    return shadow;
}

#endif // GHOST_SHADOW_SAMPLING_HLSL
