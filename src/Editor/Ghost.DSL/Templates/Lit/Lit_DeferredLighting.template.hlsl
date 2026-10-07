#ifndef GHOST_LIT_DEFERREDLIGHTING_TEMPLATE_HLSL
#define GHOST_LIT_DEFERREDLIGHTING_TEMPLATE_HLSL

#include "Lit/Lit_Common.template.hlsl"
#include "EngineResources/Shaders/Properties.hlsl"
#include "EngineResources/Shaders/MaterialPipeline/ClassificationCommon.hlsl"
#include "EngineResources/Shaders/Material/Lit/LightLoop.hlsl"

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
    if (props.tileShadingModelMaskBufferIndex != 0xFFFFFFFFu)
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
    Texture2D<float4> gb0Tex = ResourceDescriptorHeap[props.gbuffer0Srv];
    Texture2D<float4> gb1Tex = ResourceDescriptorHeap[props.gbuffer1Srv];
    Texture2D<float4> gb2Tex = ResourceDescriptorHeap[props.gbuffer2Srv];
    Texture2D<float4> gb3Tex = ResourceDescriptorHeap[props.gbuffer3Srv];

    GBufferOutputs gbuffer;
    gbuffer.gbuffer0 = gb0Tex[pixelCoord];
    gbuffer.gbuffer1 = gb1Tex[pixelCoord];
    gbuffer.gbuffer2 = gb2Tex[pixelCoord];
    gbuffer.gbuffer3 = gb3Tex[pixelCoord];

    // Verify pixel belongs to this shading model
    uint pixelShadingModel = (uint)round(gbuffer.gbuffer0.a * 255.0f) & 0x1Fu;
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
    MaterialContext matCtx = (MaterialContext)0;
    matCtx.positionWS = positionWS;
    matCtx.normalWS = surface.normalWS;
    matCtx.tangentWS = float4(1.0f, 0.0f, 0.0f, 1.0f);

    DEFERREDLIGHTING_STRATEGY strategy = DEFERREDLIGHTING_STRATEGY::Create();
    BSDFData bsdf = strategy.GetBSDFData(matCtx, surface);
    
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

