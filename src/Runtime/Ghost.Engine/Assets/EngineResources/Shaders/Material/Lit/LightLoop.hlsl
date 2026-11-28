#ifndef GHOST_LIGHT_LOOP_HLSL
#define GHOST_LIGHT_LOOP_HLSL

#include "EngineResources/Shaders/Properties.hlsl"
#include "EngineResources/Shaders/Lighting/LightGridCommon.hlsl"
#include "EngineResources/Shaders/Lighting/LightEvaluation.hlsl"
#include "EngineResources/Shaders/Lighting/ShadowSampling.hlsl"
#include "EngineResources/Shaders/Material/Lit/Lit.hlsl"
#include "EngineResources/Shaders/PostProcess/ExposureCommon.hlsl"
#include "EngineResources/Shaders/Generated/GhostRenderPipeline.hlsl"

struct FPTLLightGridPolicy
{
    ByteAddressBuffer tileLightList;
    uint tilesPerRow;

    static FPTLLightGridPolicy Create(ByteAddressBuffer tileLightListBuf, uint tilesPerRowVal)
    {
        FPTLLightGridPolicy policy;
        policy.tileLightList = tileLightListBuf;
        policy.tilesPerRow = tilesPerRowVal;
        return policy;
    }

    bool AllowContactShadows() { return true; }
    bool AllowStochasticRIS() { return true; }
    bool NeedsLightScalarization() { return false; }

    ClusterLightGridEntry GetLightRange(ShadingContext ctx)
    {
        ClusterLightGridEntry range;
        uint tileIndex = ctx.tileCoord.y * tilesPerRow + ctx.tileCoord.x;
        range.offset = tileIndex;
        range.count = min(GetTileLightCount<DWORDS_PER_TILE>(tileLightList, tileIndex), MAX_LIGHTS_PER_TILE);
        return range;
    }

    bool TryGetLightIndex(ClusterLightGridEntry range, uint localOffset, out uint lightIndex)
    {
        TileLightFetchResult fetch = FetchTileLight<DWORDS_PER_TILE>(tileLightList, range.offset, localOffset);
        lightIndex = fetch.lightIndex;
        return fetch.valid;
    }
};

struct ClusteredLightGridPolicy
{
    ByteAddressBuffer clusterLightGrid;
    ByteAddressBuffer clusterLightIndexList;
    uint tilesPerRow;
    uint sliceCount;

    static ClusteredLightGridPolicy Create(ByteAddressBuffer gridBuf, ByteAddressBuffer indexBuf, uint tilesPerRowVal, uint sliceCountVal)
    {
        ClusteredLightGridPolicy policy;
        policy.clusterLightGrid = gridBuf;
        policy.clusterLightIndexList = indexBuf;
        policy.tilesPerRow = tilesPerRowVal;
        policy.sliceCount = sliceCountVal;
        return policy;
    }

    bool AllowContactShadows() { return false; }
    bool AllowStochasticRIS() { return false; }
    bool NeedsLightScalarization() { return true; }

    ClusterLightGridEntry GetLightRange(ShadingContext ctx)
    {
        if (sliceCount == 0u || tilesPerRow == 0u)
        {
            ClusterLightGridEntry emptyRange;
            emptyRange.offset = 0u;
            emptyRange.count = 0u;
            return emptyRange;
        }

        uint sliceZ = DepthToClusterSlice(ctx.linearDepth, g_ViewData.nearClip, g_ViewData.farClip, sliceCount);
        uint clusterIndex = GetClusterIndex(ctx.tileCoord, sliceZ, tilesPerRow, sliceCount);
        return GetClusterLightGridEntry(clusterLightGrid, clusterIndex);
    }

    bool TryGetLightIndex(ClusterLightGridEntry range, uint localOffset, out uint lightIndex)
    {
        lightIndex = FetchClusterLightIndex(clusterLightIndexList, range.offset, localOffset);
        return true;
    }
};

template<typename T>
DirectLighting ShadeSurface_Punctual(ShadingContext ctx, BSDFData bsdf, T strategy, PreLightData preLightData, float3 V, uint lightIndex, bool allowContactShadows)
{
    DirectLighting lighting = (DirectLighting)0;

    ByteAddressBuffer lightsBuf = GET_BUFFER(g_FrameData.punctualLightsBuffer);
    uint byteBase = lightIndex * 112u;

    // Load position, range, lightDimmer, and direction first to reject out-of-range/backfacing fragments before fetching full light state.
    float4 posAndRange = asfloat(lightsBuf.Load4(byteBase));
    float lightDimmer = asfloat(lightsBuf.Load(byteBase + 80u));
    if (lightDimmer <= 0.0f)
    {
        return lighting;
    }

    float3 lightPosRWS = TransformAbsoluteWorldToCameraRelative(posAndRange.xyz);
    float3 lightToSurface = ctx.positionRWS - lightPosRWS;
    float3 unL = -lightToSurface;
    float distSq = dot(unL, unL);
    if (distSq >= posAndRange.w * posAndRange.w)
    {
        return lighting;
    }

    float distRcp = rsqrt(max(distSq, 1e-8f));
    float3 L = unL * distRcp;
    if (!strategy.IsNonZeroBSDF(V, L, bsdf))
    {
        return lighting;
    }

    uint4 rawColorAndType = lightsBuf.Load4(byteBase + 16u);
    float3 lightRgb = asfloat(rawColorAndType.xyz);
    uint lightType = rawColorAndType.w & 0xFu;
    float4 dirAndScale = asfloat(lightsBuf.Load4(byteBase + 32u));
    float4 dimmersAndRangeCoeffs = asfloat(lightsBuf.Load4(byteBase + 64u)); // x: diffuseDimmer, y: specularDimmer, z: spotAngleOffset, w: invRangeSq

    float factor = distSq * dimmersAndRangeCoeffs.w;
    float smoothFactor = saturate(1.0f - factor * factor);
    float attenuation = (smoothFactor * smoothFactor) / max(distSq, 0.0001f);
    if (lightType == GPULIGHT_TYPE_SPOT)
    {
        float cosAngle = dot(-L, dirAndScale.xyz);
        float spotAtten = saturate(cosAngle * dirAndScale.w + dimmersAndRangeCoeffs.z);
        attenuation *= spotAtten * spotAtten;
    }

    if (attenuation <= 0.0f)
    {
        return lighting;
    }

    float3 lightColor = lightRgb * (attenuation * lightDimmer);
    float2 shadowAndMinRough = asfloat(lightsBuf.Load2(byteBase + 84u)); // x: shadowDimmer, y: minRoughness
    float sizeX = asfloat(lightsBuf.Load(byteBase + 48u));

    float4 distance = float4(distSq * distRcp, distSq, distRcp, dot(lightToSurface, dirAndScale.xyz));
    ModifyDistancesForFillLighting(distance, sizeX);

    if (shadowAndMinRough.x > 0.0f)
    {
        ShadowType shadow;
        float hitDist = 0.0f;
        uint contactShadow = allowContactShadows ? lightsBuf.Load(byteBase + 96u) : 0u;
        if (allowContactShadows && g_Properties.enableContactShadows != 0u && contactShadow != 0u
            && TraceScreenContactShadow(ctx.positionRWS, ctx.normalWS, ctx.positionSS, g_Properties.depthTextureIndex, L,
            min(distance.x, g_Properties.contactShadowDistance), g_Properties.contactShadowTolerance, hitDist))
        {
            shadow = (ShadowType)0.0f;
        }
        else
        {
            shadow = GetPunctualShadowAttenuation(ctx.positionRWS, bsdf.normalWS, L, distance, lightType,
                lightIndex, g_Properties.shadowIndicesBufferSrv, g_Properties.shadowViewsBufferSrv, g_Properties.shadowAtlasSrv);
        }

        lightColor *= lerp(1.0f, shadow, shadowAndMinRough.x);
    }

    strategy.ClampRoughness(bsdf, shadowAndMinRough.y);

    if (any(lightColor > 0.0f))
    {
        lighting = strategy.EvaluateDirectLighting(bsdf, preLightData, V, L, lightColor);
        lighting.diffuse *= dimmersAndRangeCoeffs.x;
        lighting.specular *= dimmersAndRangeCoeffs.y;
    }

    return lighting;
}

template<typename T, typename TLightGrid>
LightLoopOutput ExecuteLightLoop(ShadingContext ctx, BSDFData bsdf, T strategy, TLightGrid lightGrid, float3 V)
{
    AggregateLighting totalLighting = (AggregateLighting)0;
    PreLightData preLightData = strategy.GetPreLightData(ctx, V, bsdf);
    bool allowContactShadows = lightGrid.AllowContactShadows();
    bool useStochasticRIS = false;

#if defined(_DIRECTLIGHTING_STOCHASTIC)
    if (lightGrid.AllowStochasticRIS())
    {
        useStochasticRIS = true;
        // Stochastic lighting (Directional & Punctual) evaluated via decoupled RIS pipeline
        if (IS_VALID_TEXTURE(g_Properties.risResolvedDiffuseSrv))
        {
            Texture2D<float4> risDiffTex = GET_TEXTURE2D(g_Properties.risResolvedDiffuseSrv);
            Texture2D<float4> risSpecTex = GET_TEXTURE2D(g_Properties.risResolvedSpecularSrv);

            DirectLighting lighting;
            lighting.diffuse = risDiffTex[ctx.positionSS].rgb * bsdf.diffuseColor;
            lighting.specular = risSpecTex[ctx.positionSS].rgb;

            AccumulateDirectLighting(totalLighting, lighting);
        }
    }
    else
#endif
    {
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
                        if (allowContactShadows && g_Properties.enableContactShadows != 0u && dirLight.contactShadow != 0u
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

        // Punctual Lights (Point / Spot) via templated light-grid policy
        if (IS_VALID_BUFFER(g_FrameData.punctualLightsBuffer))
        {
            ClusterLightGridEntry lightRange = lightGrid.GetLightRange(ctx);
            bool scalarizeLights = lightGrid.NeedsLightScalarization();
            for (uint lightOffset = 0u; lightOffset < lightRange.count; lightOffset++)
            {
                uint lightIndex = 0u;
                if (!lightGrid.TryGetLightIndex(lightRange, lightOffset, lightIndex))
                {
                    continue;
                }

                if (scalarizeLights)
                {
                    if (WaveActiveAllEqual(lightIndex))
                    {
                        uint uniformLightIndex = WaveReadLaneFirst(lightIndex);
                        DirectLighting lighting = ShadeSurface_Punctual<T>(ctx, bsdf, strategy, preLightData, V, uniformLightIndex, allowContactShadows);
                        AccumulateDirectLighting(totalLighting, lighting);
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
                            uint uniformLightIndex = WaveReadLaneAt(lightIndex, firstLane);
                            if (!scalarDone && lightIndex == uniformLightIndex)
                            {
                                scalarDone = true;
                                DirectLighting lighting = ShadeSurface_Punctual<T>(ctx, bsdf, strategy, preLightData, V, uniformLightIndex, allowContactShadows);
                                AccumulateDirectLighting(totalLighting, lighting);
                            }
                        }
                    }
                }
                else
                {
                    DirectLighting lighting = ShadeSurface_Punctual<T>(ctx, bsdf, strategy, preLightData, V, lightIndex, allowContactShadows);
                    AccumulateDirectLighting(totalLighting, lighting);
                }
            }
        }
    }

    float preExposure = GetViewPreExposure(g_Properties.persistentViewStateBufferSrv, g_Properties.viewId);

    // Image-Based Lighting (Diffuse SH9 + Specular Split-Sum Cubemap)
    if (IS_VALID_TEXTURE(g_Properties.skyboxTextureSrv) || IS_VALID_BUFFER(g_Properties.persistentViewStateBufferSrv))
    {
        IndirectLighting ibl = strategy.EvaluateIndirectLighting(ctx, bsdf, preLightData, V);
        if (useStochasticRIS)
        {
            // RIS direct buffers are already pre-exposed in RisResolveShading
            ibl.diffuse *= preExposure;
            ibl.specularReflected *= preExposure;
            ibl.specularTransmitted *= preExposure;
        }
        AccumulateIndirectLighting(totalLighting, ibl);
    }

    if (useStochasticRIS)
    {
        bsdf.emissive *= preExposure;
    }

    LightLoopOutput output = strategy.PostEvaluateBSDF(totalLighting, bsdf, preLightData, V);

    if (!useStochasticRIS)
    {
        output.diffuse *= preExposure;
        output.specular *= preExposure;
    }

    return output;
}

template<typename T>
LightLoopOutput ExecuteLightLoop(ShadingContext ctx, BSDFData bsdf, T strategy, float3 V, ByteAddressBuffer tileLightList, uint tilesPerRow)
{
    return ExecuteLightLoop<T, FPTLLightGridPolicy>(ctx, bsdf, strategy, FPTLLightGridPolicy::Create(tileLightList, tilesPerRow), V);
}

#endif // GHOST_LIGHT_LOOP_HLSL

