#ifndef GHOST_LIGHT_LOOP_HLSL
#define GHOST_LIGHT_LOOP_HLSL

#include "EngineResources/Shaders/Properties.hlsl"
#include "EngineResources/Shaders/Lighting/LightGridCommon.hlsl"
#include "EngineResources/Shaders/Lighting/LightEvaluation.hlsl"
#include "EngineResources/Shaders/Lighting/ShadowSampling.hlsl"
#include "EngineResources/Shaders/Material/Lit/Lit.hlsl"
#include "EngineResources/Shaders/Generated/GhostRenderPipeline.hlsl"

template<typename T>
DirectLighting ShadeSurface_Punctual(ShadingContext ctx, BSDFData bsdf, T strategy, GPUPunctualLight light, PreLightData preLightData, float3 V, uint lightIndex)
{
    DirectLighting lighting = (DirectLighting)0;

    float3 lightPosRWS = TransformAbsoluteWorldToCameraRelative(light.positionWS);
    PunctualLightVector vector = GetPunctualLightVector(ctx.positionRWS, light, lightPosRWS);

    if (light.lightDimmer > 0.0f && strategy.IsNonZeroBSDF(V, vector.L, bsdf))
    {
        float4 lightColor = EvaluateLight_Punctual(light, vector.L, vector.distance);
        lightColor.rgb *= lightColor.a * light.lightDimmer;

        if (lightColor.a > 0.0f && light.shadowDimmer > 0.0f)
        {
            ShadowType shadow = GetPunctualShadowAttenuation(ctx, light, bsdf.normalWS, vector.L, vector.distance, lightPosRWS, lightIndex);
            lightColor.rgb *= lerp(1.0f, shadow, light.shadowDimmer);
        }

        strategy.ClampRoughness(bsdf, light.minRoughness);

        if (any(lightColor.rgb) > 0.0f)
        {
            lighting = strategy.EvaluateDirectLighting(bsdf, preLightData, V, vector.L, lightColor.rgb);
            lighting.diffuse *= light.diffuseDimmer;
            lighting.specular *= light.specularDimmer;
        }
    }

    return lighting;
}

template<typename T>
LightLoopOutput ExecuteLightLoop(ShadingContext ctx, BSDFData bsdf, T strategy, float3 V, ByteAddressBuffer tileLightList, uint tilesPerRow)
{
    AggregateLighting totalLighting = (AggregateLighting)0;
    PreLightData preLightData = strategy.GetPreLightData(ctx, V, bsdf);

    // Directional Lights (Multiple, with single primary shadow caster)
    if (IS_VALID_BUFFER(g_FrameData.directionalLightBuffer) && g_FrameData.directionalLightCount > 0u)
    {
        for (uint dirIdx = 0u; dirIdx < g_FrameData.directionalLightCount; ++dirIdx)
        {
            GPUDirectionalLight dirLight = LoadData<GPUDirectionalLight>(g_FrameData.directionalLightBuffer, dirIdx);
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

            GPUPunctualLight light = LoadData<GPUPunctualLight>(g_FrameData.punctualLightsBuffer, fetch.lightIndex);
            DirectLighting lighting = ShadeSurface_Punctual<T>(ctx, bsdf, strategy, light, preLightData, V, fetch.lightIndex);
            AccumulateDirectLighting(totalLighting, lighting);
        }
    }

    // Image-Based Lighting (Diffuse SH9 + Specular Split-Sum Cubemap)
    if (IS_VALID_BUFFER(ctx.skyboxTextureIndex))
    {
        IndirectLighting ibl = strategy.EvaluateIndirectLighting(ctx, bsdf, preLightData, V);
        AccumulateIndirectLighting(totalLighting, ibl);
    }

    LightLoopOutput output = strategy.PostEvaluateBSDF(totalLighting, bsdf, preLightData, V);

    return output;
}

#endif // GHOST_LIGHT_LOOP_HLSL

