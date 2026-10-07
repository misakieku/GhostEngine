#ifndef GHOST_LIGHT_LOOP_HLSL
#define GHOST_LIGHT_LOOP_HLSL

#include "EngineResources/Shaders/Properties.hlsl"
#include "EngineResources/Shaders/Lighting/LightGridCommon.hlsl"
#include "EngineResources/Shaders/Material/Lit/Lit.hlsl"

#ifndef DWORDS_PER_TILE
#define DWORDS_PER_TILE 32u
#endif

#ifndef MAX_LIGHTS_PER_TILE
#define MAX_LIGHTS_PER_TILE 63u
#endif

template<typename T>
LightLoopOutput ExecuteLightLoop(in ShadingContext ctx, in BSDFData bsdf, inout T strategy, float3 V, ByteAddressBuffer tileLightList, uint tilesPerRow)
{
    AggregateLighting totalLighting = (AggregateLighting)0;
    PreLightData preLightData = strategy.GetPreLightData(ctx, V, bsdf);
    
    // Directional Lights (Multiple, with single primary shadow caster)
    if (IS_VALID_BUFFER(g_FrameData.directionalLightBuffer) && g_FrameData.directionalLightCount > 0u)
    {
        for (uint dirIdx = 0u; dirIdx < g_FrameData.directionalLightCount; ++dirIdx)
        {
            DirectionalLightData dirLight = LoadData<DirectionalLightData>(g_FrameData.directionalLightBuffer, dirIdx);
            float3 L = -dirLight.directionWS;
            if (any(dirLight.color > 0.0001f) && any(L != 0.0f))
            {
                float shadow = 1.0f;
                if ((int)dirIdx == g_FrameData.primaryDirectionalLightIndex && dirLight.castShadows != 0u)
                {
                    // Primary directional shadow evaluation (CSM placeholder)
                }

                DirectLighting dirDirect = strategy.EvaluateDirectLighting(bsdf, preLightData, V, normalize(L), dirLight.color * shadow);
                AccumulateDirectLighting(totalLighting, dirDirect);
            }
        }
    }

    // Punctual Lights (Point / Spot) via FPTL Tile List
    if (IS_VALID_BUFFER(g_FrameData.punctualLightsBuffer))
    {
        uint tileIndex = ctx.tileCoord.y * tilesPerRow + ctx.tileCoord.x;
        uint lightCount = min(GetTileLightCount<DWORDS_PER_TILE>(tileLightList, tileIndex), MAX_LIGHTS_PER_TILE);

        for (uint lightOffset = 0u; lightOffset < lightCount; lightOffset++)
        {
            TileLightFetchResult fetch = FetchTileLight<DWORDS_PER_TILE>(tileLightList, tileIndex, lightOffset);
            if (!fetch.valid)
            {
                continue;
            }

            PunctualLightData light = LoadData<PunctualLightData>(g_FrameData.punctualLightsBuffer, fetch.lightIndex);

            float3 toLight = light.positionWS - ctx.positionWS;
            float distSq = dot(toLight, toLight);
            float dist = sqrt(distSq);
            float3 L = (dist > 0.0001f) ? (toLight / dist) : float3(0.0f, 1.0f, 0.0f);

            // Smooth windowed distance attenuation (Karis / Frostbite)
            float factor = distSq * light.invRangeSq;
            float smoothFactor = saturate(1.0f - factor * factor);
            float attenuation = (smoothFactor * smoothFactor) / max(distSq, 0.0001f);

            // Spot cone attenuation
            uint lightType = light.lightTypeAndFlags & 0xFu;
            if (lightType == 1u) // Spot = 1
            {
                float cosAngle = dot(-L, light.directionWS);
                float spotAtten = saturate(cosAngle * light.spotAngleScale + light.spotAngleOffset);
                attenuation *= spotAtten * spotAtten;
            }

            float shadow = 1.0f;
            int shadowIndex = -1;
            if (IS_VALID_BUFFER(ctx.shadowIndicesBufferIndex))
            {
                shadowIndex = LoadData<int>(ctx.shadowIndicesBufferIndex, fetch.lightIndex);
            }
            
            // TODO: Better shadow sampling.
            if (shadowIndex >= 0 && IS_VALID_BUFFER(ctx.shadowAtlasIndex) && IS_VALID_BUFFER(ctx.shadowViewsBufferIndex))
            {
                uint viewIdx = (uint)shadowIndex;
                ShadowViewData sViewBase = LoadData<ShadowViewData>(ctx.shadowViewsBufferIndex, viewIdx);
                float3 biasedPosWS = ctx.positionWS + ctx.normalWS * light.normalBias;

                if (lightType == 0u) // Point Light cubemap face selection
                {
                    float3 L_cube = biasedPosWS - light.positionWS;
                    float3 absL = abs(L_cube);
                    uint faceIdx = 0u;
                    if (absL.x >= absL.y && absL.x >= absL.z)
                    {
                        faceIdx = (L_cube.x > 0.0f) ? 0u : 1u;
                    }
                    else if (absL.y >= absL.x && absL.y >= absL.z)
                    {
                        faceIdx = (L_cube.y > 0.0f) ? 2u : 3u;
                    }
                    else
                    {
                        faceIdx = (L_cube.z > 0.0f) ? 4u : 5u;
                    }
                    viewIdx += faceIdx;
                }

                ShadowViewData sView = sViewBase;
                if (lightType == 0u)
                {
                    sView = LoadData<ShadowViewData>(ctx.shadowViewsBufferIndex, viewIdx);
                }
                
                float4 clipPos = mul(sView.shadowViewProj, float4(biasedPosWS, 1.0f));
                if (clipPos.w > 0.0001f)
                {
                    float3 ndc = clipPos.xyz / clipPos.w;
                    float2 uv = ndc.xy * float2(0.5f, -0.5f) + 0.5f;
                    if (all(uv >= 0.0f) && all(uv <= 1.0f))
                    {
                        float2 atlasUV = sView.tileOffsetScale.xy + uv * sView.tileOffsetScale.zw;
                        Texture2D<float> shadowAtlas = ResourceDescriptorHeap[ctx.shadowAtlasIndex];
                        
                        float receiverDepth = ndc.z + light.depthBias;

                        uint2 atlasDim;
                        shadowAtlas.GetDimensions(atlasDim.x, atlasDim.y);
                        float2 pixelPos = atlasUV * float2(atlasDim);
                        int2 baseCoord = int2(floor(pixelPos - 0.5f));
                        float2 f = frac(pixelPos - 0.5f);

                        int2 tileMinPixels = int2(sView.tileOffsetScale.xy * float2(atlasDim));
                        int2 tileMaxPixels = tileMinPixels + int2(sView.tileOffsetScale.zw * float2(atlasDim)) - 1;

                        int2 c00 = clamp(baseCoord + int2(0, 0), tileMinPixels, tileMaxPixels);
                        int2 c10 = clamp(baseCoord + int2(1, 0), tileMinPixels, tileMaxPixels);
                        int2 c01 = clamp(baseCoord + int2(0, 1), tileMinPixels, tileMaxPixels);
                        int2 c11 = clamp(baseCoord + int2(1, 1), tileMinPixels, tileMaxPixels);

                        float d00 = shadowAtlas.Load(int3(c00, 0));
                        float d10 = shadowAtlas.Load(int3(c10, 0));
                        float d01 = shadowAtlas.Load(int3(c01, 0));
                        float d11 = shadowAtlas.Load(int3(c11, 0));

                        // Reversed-Z: closer depth is greater. If receiverDepth >= storedDepth, fragment is lit.
                        float s00 = (receiverDepth >= d00) ? 1.0f : 0.0f;
                        float s10 = (receiverDepth >= d10) ? 1.0f : 0.0f;
                        float s01 = (receiverDepth >= d01) ? 1.0f : 0.0f;
                        float s11 = (receiverDepth >= d11) ? 1.0f : 0.0f;

                        shadow = lerp(lerp(s00, s10, f.x), lerp(s01, s11, f.x), f.y);
                    }
                }
            }

            float3 radiance = light.color * (attenuation * shadow);
            if (any(radiance > 0.0001f))
            {
                DirectLighting punctualDirect = strategy.EvaluateDirectLighting(bsdf, preLightData, V, L, radiance);
                AccumulateDirectLighting(totalLighting, punctualDirect);
            }
        }
    }
    
    // TODO: Add support for area lights, image-based lighting, and other light types.
    
    LightLoopOutput output = strategy.PostEvaluateBSDF(totalLighting, bsdf, preLightData, V);

    return output;
}

#endif // GHOST_LIGHT_LOOP_HLSL

