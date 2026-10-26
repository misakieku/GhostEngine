#ifndef GHOST_STANDARDLIT_HLSL
#define GHOST_STANDARDLIT_HLSL

#include "EngineResources/Shaders/Material/Lit/Lit.hlsl"
#include "EngineResources/Shaders/Material/Lit/BSDF.hlsl"
#include "EngineResources/Shaders/Lighting/SphericalHarmonics.hlsl"

struct BSDFData
{
    uint shadingModel;
    float3 diffuseColor;
    float3 fresnel0;
    float ambientOcclusion;
    float specularOcclusion;
    float3 normalWS;
    float perceptualRoughness;
    float3 tangentWS;
    float roughnessT;
    float roughnessB;
    float3 emissive;
};

struct PreLightData
{
    float NdotV;
    float clampedNdotV;
    float partLambdaV;
    float energyCompensation;
};

struct StandardLitDeferredLighting
{
    static const uint ShadingModelID = 2u;

    static StandardLitDeferredLighting Create()
    {
        return (StandardLitDeferredLighting)0;
    }

    BSDFData GetBSDFData(float3 positionRWS, float3 T, SurfaceData surface)
    {
        BSDFData bsdf = (BSDFData)0;
        bsdf.shadingModel = surface.shadingModel;
        bsdf.diffuseColor = surface.albedo * (1.0f - surface.metallic);
        bsdf.fresnel0 = lerp(float3(0.04f, 0.04f, 0.04f), surface.albedo, surface.metallic);
        bsdf.ambientOcclusion = surface.occlusion;
        bsdf.specularOcclusion = 1.0f;
        bsdf.normalWS = surface.normalWS;
        bsdf.perceptualRoughness = surface.roughness;
        bsdf.tangentWS = T;

        // Convert perceptual roughness to linear roughness for GGX: alpha = roughness^2
        float roughness = max(0.001f, surface.roughness * surface.roughness);
        bsdf.roughnessT = roughness;
        bsdf.roughnessB = roughness;
        bsdf.emissive = surface.emissive;

        return bsdf;
    }

    PreLightData GetPreLightData(ShadingContext ctx, float3 V, inout BSDFData bsdf)
    {
        PreLightData data = (PreLightData)0;
        data.NdotV = dot(bsdf.normalWS, V);
        data.clampedNdotV = ClampNdotV(data.NdotV);
        data.partLambdaV = GetSmithJointGGXPartLambdaV(data.clampedNdotV, bsdf.roughnessT);

        // Precompute multi-scattering energy compensation factor using the split-sum LUT
        float2 envBRDF = float2(0.0f, 0.0f);
        if (IS_VALID_BUFFER(ctx.iblLutTextureIndex))
        {
            envBRDF = SampleTexture2DLevel<float4>(ctx.iblLutTextureIndex, LinearClampSampler, float2(data.clampedNdotV, bsdf.perceptualRoughness), 0.0f).rg;
        }

        float reflectivity = envBRDF.x + envBRDF.y;
        data.energyCompensation = (1.0f / max(reflectivity, 0.001f)) - 1.0f;

        return data;
    }

    bool IsNonZeroBSDF(float3 V, float3 L, BSDFData bsdf)
    {
        float NdotL = dot(bsdf.normalWS, L);
        return NdotL > 0.0f;
    }

    void ClampRoughness(inout BSDFData bsdf, float minRoughness)
    {
        bsdf.roughnessT = max(minRoughness, bsdf.roughnessT);
        bsdf.roughnessB = max(minRoughness, bsdf.roughnessB);
    }

    DirectLighting EvaluateDirectLighting(BSDFData bsdf, PreLightData preLightData, float3 V, float3 L, float3 lightColor)
    {
        DirectLighting direct = (DirectLighting)0;

        float NdotL = dot(bsdf.normalWS, L);
        if (NdotL <= 0.0f)
        {
            return direct;
        }

        float clampedNdotL = saturate(NdotL);
        float3 H = normalize(L + V);
        float NdotH = saturate(dot(bsdf.normalWS, H));
        float LdotH = saturate(dot(L, H));
        float LdotV = dot(L, V);

        // 1. Specular Term: Schlick Fresnel * Height-Correlated Smith-Joint GGX
        float3 F = F_Schlick(bsdf.fresnel0, LdotH);
        float DV = DV_SmithJointGGX(NdotH, clampedNdotL, preLightData.clampedNdotV, bsdf.roughnessT, preLightData.partLambdaV);
        float3 specTerm = F * DV;

        // 2. Diffuse Term: Disney Burley Diffuse
        float diffTerm = DisneyDiffuse(preLightData.clampedNdotV, clampedNdotL, LdotV, bsdf.perceptualRoughness);

        direct.diffuse = bsdf.diffuseColor * diffTerm * clampedNdotL * lightColor;
        direct.specular = specTerm * clampedNdotL * lightColor;

        return direct;
    }

    IndirectLighting EvaluateIndirectLighting(ShadingContext ctx, BSDFData bsdf, PreLightData preLightData, float3 V)
    {
        IndirectLighting indirect = (IndirectLighting)0;

        // 1. Diffuse IBL via Spherical Harmonics L2
        if (IS_VALID_BUFFER(ctx.persistentViewStateBufferIndex))
        {
            PackedSH9 sh = LoadViewSH9(ctx.persistentViewStateBufferIndex, ctx.viewId);
            float3 diffuseIrradiance = EvaluateSH9Irradiance(sh, bsdf.normalWS);
            indirect.diffuse = diffuseIrradiance * bsdf.diffuseColor * bsdf.ambientOcclusion;
        }

        // 2. Specular IBL via GGX prefiltered cubemap + Split-Sum BRDF LUT
        if (IS_VALID_BUFFER(ctx.skyboxTextureIndex))
        {
            float3 R = reflect(-V, bsdf.normalWS);
            float3 rotatedR = RotateDirectionY(R, ctx.skyboxRotationSinCos);

            float mip = bsdf.perceptualRoughness * max(0.0f, float(ctx.skyboxMipCount) - 1.0f);
            float3 prefilteredSpec = SampleTextureCubeLevel<float4>(ctx.skyboxTextureIndex, TrilinearClampSampler, rotatedR, mip).rgb;
            prefilteredSpec *= ctx.skyboxTintAndIntensity.rgb * ctx.skyboxTintAndIntensity.w;

            float2 envBRDF = float2(0.0f, 0.0f);
            if (IS_VALID_BUFFER(ctx.iblLutTextureIndex))
            {
                // PBR.dds contains BRDF LUT in RG channels: x = scale, y = bias
                envBRDF = SampleTexture2DLevel<float4>(ctx.iblLutTextureIndex, LinearClampSampler, float2(preLightData.clampedNdotV, bsdf.perceptualRoughness), 0.0f).rg;
            }

            float3 specularColor = prefilteredSpec * (bsdf.fresnel0 * envBRDF.x + envBRDF.y) * bsdf.specularOcclusion;
            indirect.specularReflected = specularColor;
        }

        return indirect;
    }

    LightLoopOutput PostEvaluateBSDF(AggregateLighting lighting, BSDFData bsdf, PreLightData preLightData, float3 V)
    {
        LightLoopOutput output;
        output.diffuse = lighting.direct.diffuse + lighting.indirect.diffuse + bsdf.emissive;

        // Rescale total specular to account for multiple scattering energy compensation
        float3 totalSpecular = lighting.direct.specular + lighting.indirect.specularReflected;
        totalSpecular *= (1.0f + bsdf.fresnel0 * preLightData.energyCompensation);

        output.specular = totalSpecular;
        return output;
    }
};

#endif // GHOST_STANDARDLIT_HLSL
