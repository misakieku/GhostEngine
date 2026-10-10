#ifndef GHOST_LIT_DEFERREDLIGHTING_TEMPLATE_HLSL
#define GHOST_LIT_DEFERREDLIGHTING_TEMPLATE_HLSL

#include "Lit/Lit_Common.template.hlsl"
#include "EngineResources/Shaders/Properties.hlsl"
#include "EngineResources/Shaders/MaterialPipeline/ClassificationCommon.hlsl"
#include "EngineResources/Shaders/MaterialPipeline/GBufferPacking.hlsl"
#include "EngineResources/Shaders/Material/Lit/LightLoop.hlsl"
#include "EngineResources/Shaders/Generated/GhostRenderPipeline.hlsl"

groupshared uint s_TileShadingModelMask;

[numthreads(CLASSIFICATION_TILE_SIZE, CLASSIFICATION_TILE_SIZE, 1)]
void CSMain(
    uint3 dispatchThreadId : SV_DispatchThreadID,
    uint3 groupID : SV_GroupID,
    uint3 groupThreadId : SV_GroupThreadID,
    uint groupIndex : SV_GroupIndex)
{
    DeferredLightingShaderProperties props = LoadData<DeferredLightingShaderProperties>(g_PushConstantData.userData0, 0);

    // Shading Model Tile Early-Out Check
    uint tileIndex = groupID.y * props.tilesPerRow + groupID.x;
    if (IS_VALID_BUFFER(props.tileShadingModelMaskBufferIndex))
    {
        ByteAddressBuffer maskBuffer = ResourceDescriptorHeap[props.tileShadingModelMaskBufferIndex];
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
    if (pixelCoord.x >= props.renderWidth || pixelCoord.y >= props.renderHeight)
    {
        return;
    }

    // Sample Depth Buffer & Check Background
    Texture2D<float> depthTexture = ResourceDescriptorHeap[props.depthTextureIndex];
    float depth = depthTexture[pixelCoord];
    if (depth <= 0.0f)
    {
        return;
    }

    // Read G-Buffer

    GBufferOutputs gbuffer = ReadGBuffer(pixelCoord, props.gbuffer0Srv, props.gbuffer1Srv, props.gbuffer2Srv, props.gbuffer3Srv);

    // Verify pixel belongs to this shading model
    uint pixelShadingModel = gbuffer.GetShadingModel();
    if (pixelShadingModel != 0u && pixelShadingModel != SHADING_MODEL_ID)
    {
        return;
    }

    // Reconstruct World Position & View Vector
    float n = g_ViewData.nearClip;
    float f = g_ViewData.farClip;
    float zView = (n * f) / ((f - n) * depth + n);

    float m00 = g_ViewData.projectionMatrix[0][0];
    float m11 = g_ViewData.projectionMatrix[1][1];
    float2 ndc = float2(
        ((float(pixelCoord.x) + 0.5f) * g_ViewData.screenSize.z) * 2.0f - 1.0f,
        1.0f - ((float(pixelCoord.y) + 0.5f) * g_ViewData.screenSize.w) * 2.0f
    );
    float3 posVS = float3((ndc.x / m00) * zView, (ndc.y / m11) * zView, zView);
    float3 positionWS = g_ViewData.cameraPosition + mul(transpose((float3x3)g_ViewData.viewMatrix), posVS);
    float3 V = normalize(g_ViewData.cameraPosition - positionWS);

    // Unpack Surface & BSDF Data
    SurfaceData surface = ExtractSurfaceData(gbuffer);

    DEFERREDLIGHTING_STRATEGY strategy = DEFERREDLIGHTING_STRATEGY::Create();
    BSDFData bsdf = strategy.GetBSDFData(positionWS, gbuffer.GetTangent(surface.normalWS), surface);
    
    ShadingContext shadingCtx;
    shadingCtx.positionWS = positionWS;
    shadingCtx.normalWS = surface.normalWS;
    shadingCtx.positionNDC = ndc;
    shadingCtx.positionSS = pixelCoord;
    shadingCtx.tileCoord = groupID.xy;
    shadingCtx.depth = depth;
    shadingCtx.linearDepth = zView;
    shadingCtx.shadowAtlasIndex = props.shadowAtlasSrv;
    shadingCtx.shadowViewsBufferIndex = props.shadowViewsBufferSrv;
    shadingCtx.shadowIndicesBufferIndex = props.shadowIndicesBufferSrv;

    // Execute Light Loop
    ByteAddressBuffer tileLightList = ResourceDescriptorHeap[props.tileLightListBufferIndex];
    LightLoopOutput light = ExecuteLightLoop<DEFERREDLIGHTING_STRATEGY>(shadingCtx, bsdf, strategy, V, tileLightList, props.tilesPerRow);
    
    float3 finalColor = light.diffuse + light.specular;

    // Output to HDR Color Buffer
    RWTexture2D<float4> litColorTarget = ResourceDescriptorHeap[props.litColorUav];
    litColorTarget[pixelCoord] = float4(finalColor, 1.0f);
}

#endif // GHOST_LIT_DEFERREDLIGHTING_TEMPLATE_HLSL

