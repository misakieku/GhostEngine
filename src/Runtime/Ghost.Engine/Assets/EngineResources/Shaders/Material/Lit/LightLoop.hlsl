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
    
    // Primary Directional Sun Light
    if (g_FrameData.directionalLightBuffer != 0xFFFFFFFFu)
    {
        DirectionalLightData sun = LoadData<DirectionalLightData>(g_FrameData.directionalLightBuffer, 0);
        float3 L = -sun.directionWS;
        if (any(sun.color > 0.0001f) && any(L != 0.0f))
        {
            DirectLighting sunDirect = strategy.EvaluateDirectLighting(bsdf, preLightData, V, normalize(L), sun.color);
            AccumulateDirectLighting(totalLighting, sunDirect);
        }
    }

    // Punctual Lights (Point / Spot) via FPTL Tile List
    if (g_FrameData.punctualLightsBuffer != 0xFFFFFFFFu)
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

            float3 radiance = light.color * attenuation;
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

