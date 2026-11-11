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
            ShadowType shadow;
            float hitDist = 0.0f;
            if (g_Properties.enableContactShadows != 0u && light.contactShadow != 0u
                && TraceScreenContactShadow(ctx.positionRWS, ctx.normalWS, ctx.positionSS, g_Properties.depthTextureIndex, vector.L,
                min(vector.distance.x, g_Properties.contactShadowDistance), g_Properties.contactShadowTolerance, hitDist))
            {
                shadow = (ShadowType)0.0f;
            }
            else
            {
                shadow = GetPunctualShadowAttenuation(ctx.positionRWS, bsdf.normalWS, vector.L, vector.distance, light,
                    lightIndex, g_Properties.shadowIndicesBufferSrv, g_Properties.shadowViewsBufferSrv, g_Properties.shadowAtlasSrv);
            }
            
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
    
#if defined(_DIRECTLIGHTING_STANDARD)
    
    // Directional Lights (Multiple, with single primary shadow caster)
    if (IS_VALID_BUFFER(g_FrameData.directionalLightBuffer) && g_FrameData.directionalLightCount > 0u)
    {
        for (uint dirIdx = 0u; dirIdx < g_FrameData.directionalLightCount; ++dirIdx)
        {
            GPUDirectionalLight dirLight = LoadData<GPUDirectionalLight>(g_FrameData.directionalLightBuffer, dirIdx);
            float3 L = -dirLight.directionWS;
            if (dirLight.lightDimmer > 0.0f && any(dirLight.color > 0.0f))
            {
                float3 lightColor = dirLight.color * dirLight.lightDimmer;
                if ((int)dirIdx == g_FrameData.primaryDirectionalLightIndex && dirLight.castShadows != 0u && dirLight.shadowDimmer > 0.0f)
                {
                    ShadowType shadow;
                    float hitDist = 0.0f;
                    if (g_Properties.enableContactShadows != 0u && dirLight.contactShadow != 0u
                        && TraceScreenContactShadow(ctx.positionRWS, ctx.normalWS, ctx.positionSS, g_Properties.depthTextureIndex, L,
                        g_Properties.contactShadowDistance, g_Properties.contactShadowTolerance, hitDist))
                    {
                        shadow = (ShadowType)0.0f;
                    }
                    else
                    {
                        shadow = GetDirectionalShadowAttenuation(
                            ctx.positionRWS, bsdf.normalWS, L,
                            g_Properties.directionalShadowViewsBufferSrv, g_Properties.directionalShadowAtlasSrv,
                            g_Properties.cascadeCount, g_Properties.cascadeSplits, g_Properties.cascadeBlends, g_Properties.maxShadowDistance, g_Properties.shadowFadeRange);
                    }
                    
                    lightColor *= lerp(1.0f, shadow, dirLight.shadowDimmer);
                }

                if (dirLight.angularDiameter > 0.0f)
                {
                    float minRough = saturate(dirLight.angularDiameter * 0.5f);
                    strategy.ClampRoughness(bsdf, minRough * minRough);
                }

                if (any(lightColor > 0.0f))
                {
                    DirectLighting dirDirect = strategy.EvaluateDirectLighting(bsdf, preLightData, V, normalize(L), lightColor);
                    dirDirect.diffuse *= dirLight.diffuseDimmer;
                    dirDirect.specular *= dirLight.specularDimmer;
                    AccumulateDirectLighting(totalLighting, dirDirect);
                }
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
    
    // TODO: Evaluate area lights (rect / disc / tube) via FPTL tile list
#elif defined(_DIRECTLIGHTING_STOCHASTIC)
    // Stochastic lighting (Directional & Punctual) evaluated via decoupled RIS pipeline
    // (RisLightSampling -> RisScreenContactShadow -> RisRayTrace -> RisResolveShading) and composite the results here
    
    if (IS_VALID_TEXTURE(g_Properties.risResolvedDiffuseSrv))
    {
        Texture2D<float4> risDiffTex = GET_TEXTURE2D(g_Properties.risResolvedDiffuseSrv);
        Texture2D<float4> risSpecTex = GET_TEXTURE2D(g_Properties.risResolvedSpecularSrv);
        
        DirectLighting lighting;
        lighting.diffuse = risDiffTex[ctx.positionSS].rgb * bsdf.diffuseColor;
        lighting.specular = risSpecTex[ctx.positionSS].rgb;

        AccumulateDirectLighting(totalLighting, lighting);
    }
#endif

    // Image-Based Lighting (Diffuse SH9 + Specular Split-Sum Cubemap)
    if (IS_VALID_TEXTURE(g_Properties.skyboxTextureSrv) || IS_VALID_BUFFER(g_Properties.persistentViewStateBufferSrv))
    {
        IndirectLighting ibl = strategy.EvaluateIndirectLighting(ctx, bsdf, preLightData, V);
        AccumulateIndirectLighting(totalLighting, ibl);
    }

    LightLoopOutput output = strategy.PostEvaluateBSDF(totalLighting, bsdf, preLightData, V);

    return output;
}

#endif // GHOST_LIGHT_LOOP_HLSL

