#ifndef GHOST_LIT_RISRESOLVESHADING_TEMPLATE_HLSL
#define GHOST_LIT_RISRESOLVESHADING_TEMPLATE_HLSL

#include "EngineResources/Shaders/Generated/GhostRenderPipeline.hlsl"

cbuffer PassProperties : register(b3, space0)
{
    RisResolveShadingShaderProperties g_Properties;
};

#include "Lit/Lit_Common.template.hlsl"
#include "EngineResources/Shaders/MaterialPipeline/ClassificationCommon.hlsl"
#include "EngineResources/Shaders/MaterialPipeline/GBufferPacking.hlsl"
#include "EngineResources/Shaders/Lighting/RisCommon.hlsl"
#include "EngineResources/Shaders/Generated/GPULightData.hlsl"
#include "EngineResources/Shaders/Lighting/LightEvaluation.hlsl"

groupshared uint s_TileShadingModelMask;

[numthreads(CLASSIFICATION_TILE_SIZE, CLASSIFICATION_TILE_SIZE, 1)]
void CSMain(
    uint3 dispatchThreadId : SV_DispatchThreadID,
    uint3 groupID : SV_GroupID,
    uint3 groupThreadId : SV_GroupThreadID,
    uint groupIndex : SV_GroupIndex)
{
    // Shading Model Tile Early-Out Check
    uint tileIndex = groupID.y * g_Properties.tilesPerRow + groupID.x;
    if (IS_VALID_BUFFER(g_Properties.tileShadingModelMaskBufferIndex))
    {
        ByteAddressBuffer maskBuffer = GET_BUFFER(g_Properties.tileShadingModelMaskBufferIndex);
        if (groupIndex == 0u)
        {
            s_TileShadingModelMask = maskBuffer.Load(tileIndex * 4u);
        }

        GroupMemoryBarrierWithGroupSync();

        if ((s_TileShadingModelMask & (1u << SHADING_MODEL_ID)) == 0u)
        {
            return;
        }
    }

    uint2 pixelCoord = groupID.xy * CLASSIFICATION_TILE_SIZE + groupThreadId.xy;
    if (pixelCoord.x >= g_Properties.renderWidth || pixelCoord.y >= g_Properties.renderHeight)
    {
        return;
    }

    // Sample Depth Buffer & Check Background
    Texture2D<float> depthTexture = GET_TEXTURE2D(g_Properties.depthTextureIndex);
    float depth = depthTexture[pixelCoord];
    if (depth <= 0.0f)
    {
        return;
    }

    // Read G-Buffer
    GBufferOutputs gbuffer = ReadGBuffer(pixelCoord, g_Properties.gbuffer0Srv, g_Properties.gbuffer1Srv, g_Properties.gbuffer2Srv, g_Properties.gbuffer3Srv);

    // Verify pixel belongs to this shading model
    uint pixelShadingModel = gbuffer.GetShadingModel();
    if (pixelShadingModel != 0u && pixelShadingModel != SHADING_MODEL_ID)
    {
        return;
    }

    RWTexture2D<float4> diffuseUav = GET_TEXTURE2D(g_Properties.risResolvedDiffuseUav);
    RWTexture2D<float4> specularUav = GET_TEXTURE2D(g_Properties.risResolvedSpecularUav);

    if (!IS_VALID_TEXTURE(g_Properties.risLightSamplesIndex) || !IS_VALID_TEXTURE(g_Properties.risShadowMaskIndex) || !IS_VALID_BUFFER(g_FrameData.punctualLightsBuffer))
    {
        diffuseUav[pixelCoord] = 0.0f;
        specularUav[pixelCoord] = 0.0f;
        return;
    }

    // Load shadow mask and RIS sample
    Texture2D<uint2> lightSamplesTex = GET_TEXTURE2D(g_Properties.risLightSamplesIndex);
    Texture2D<float> shadowMaskTex = GET_TEXTURE2D(g_Properties.risShadowMaskIndex);
    RisLightSample sampleData = UnpackRisLightSample(lightSamplesTex[pixelCoord]);
    float shadow = shadowMaskTex[pixelCoord];

    if (shadow <= 0.0f || sampleData.lightIndex == 0xFFFFFFFF || sampleData.weight <= 0.0f)
    {
        diffuseUav[pixelCoord] = 0.0f;
        specularUav[pixelCoord] = 0.0f;
        return;
    }

    // Reconstruct World Position & View Vector
    float zView;
    float2 ndc;
    float3 positionRWS = ReconstructPositionRWS(pixelCoord, depth, zView, ndc);
    float3 V = GetWorldSpaceNormalizeViewDir(positionRWS);

    // Unpack Surface & BSDF Data
    SurfaceData surface = ExtractSurfaceData(gbuffer);
    DEFERREDLIGHTING_STRATEGY strategy = DEFERREDLIGHTING_STRATEGY::Create();
    BSDFData bsdf = strategy.GetBSDFData(positionRWS, gbuffer.GetTangent(surface.normalWS), surface);

    ShadingContext shadingCtx = (ShadingContext)0;
    shadingCtx.positionRWS = positionRWS;
    shadingCtx.normalWS = surface.normalWS;
    shadingCtx.positionNDC = ndc;
    shadingCtx.positionSS = pixelCoord;
    shadingCtx.tileCoord = groupID.xy;
    shadingCtx.depth = depth;
    shadingCtx.linearDepth = zView;

    // Load Punctual Light & Evaluate Geometric Attenuation
    GPUPunctualLight light = LoadData<GPUPunctualLight>(g_FrameData.punctualLightsBuffer, sampleData.lightIndex);
    float3 lightPosRWS = TransformAbsoluteWorldToCameraRelative(light.positionWS);
    float3 unL = lightPosRWS - positionRWS;
    float distSq = dot(unL, unL);
    float rangeFactor = distSq * light.invRangeSq;

    if (rangeFactor < 1.0f && dot(surface.normalWS, unL) > 0.0f)
    {
        float invDist = rsqrt(max(distSq, 1e-8f));
        float3 L = unL * invDist;

        if (strategy.IsNonZeroBSDF(V, L, bsdf))
        {
            float smoothFactor = saturate(1.0f - rangeFactor * rangeFactor);
            float attenuation = (smoothFactor * smoothFactor) / max(distSq, 0.0001f);
            if ((light.lightTypeAndShadowSize & 0xFu) == GPULIGHT_TYPE_SPOT)
            {
                float spotAtten = saturate(dot(-L, light.directionWS) * light.spotAngleScale + light.spotAngleOffset);
                attenuation *= spotAtten * spotAtten;
            }

            float3 lightRadiance = light.color * (attenuation * light.lightDimmer * shadow * sampleData.weight);
            if (any(lightRadiance > 0.0f))
            {
                strategy.ClampRoughness(bsdf, light.minRoughness);
                PreLightData preLightData = strategy.GetPreLightData(shadingCtx, V, bsdf);

                // Diffuse Term: evaluate direct lighting with pure 1.0 diffuse color to isolate irradiance
                BSDFData bsdfUnmodulated = bsdf;
                bsdfUnmodulated.diffuseColor = float3(1.0f, 1.0f, 1.0f);

                DirectLighting direct = strategy.EvaluateDirectLighting(bsdfUnmodulated, preLightData, V, L, lightRadiance);
                direct.diffuse *= light.diffuseDimmer;
                direct.specular *= light.specularDimmer;

                diffuseUav[pixelCoord] = float4(direct.diffuse, 1.0f);
                specularUav[pixelCoord] = float4(direct.specular, 1.0f);
                return;
            }
        }
    }

    diffuseUav[pixelCoord] = 0.0f;
    specularUav[pixelCoord] = 0.0f;
}

#endif // GHOST_LIT_RISRESOLVESHADING_TEMPLATE_HLSL
