#ifndef GHOST_TEMPLATE_DEFERTEXTURING
#define GHOST_TEMPLATE_DEFERTEXTURING

#if defined(GHOST_TEMPLATE_LIT)
#include "Lit/Lit_Common.template.hlsl"
#elif defined(GHOST_TEMPLATE_UNLIT)
#include "Unlit/Unlit_Common.template.hlsl"
#else
#error "Unsupported template type for deferred texturing"
#endif

#include "EngineResources/Shaders/Mesh.hlsl"
#include "EngineResources/Shaders/Properties.hlsl"
#include "EngineResources/Shaders/Utilities/Color.hlsl"
#include "EngineResources/Shaders/Utilities/CullCommon.hlsl"
#include "EngineResources/Shaders/MaterialPipeline/MaterialEncoding.hlsl"
#include "EngineResources/Shaders/MaterialPipeline/VisibilityBufferEncoding.hlsl"
#include "EngineResources/Shaders/MaterialPipeline/ClassificationCommon.hlsl"
#include "EngineResources/Shaders/MaterialPipeline/Barycentrics.hlsl"
#include "EngineResources/Shaders/MaterialPipeline/GBufferPacking.hlsl"
#include "EngineResources/Shaders/Generated/GhostRenderPipeline.hlsl"

[numthreads(CLASSIFICATION_TILE_SIZE, CLASSIFICATION_TILE_SIZE, 1)]
void CSMain(
    uint3 dispatchThreadId : SV_DispatchThreadID,
    uint3 groupID : SV_GroupID,
    uint3 groupThreadId : SV_GroupThreadID,
    uint groupIndex : SV_GroupIndex)
{
    DeferredTexturingShaderProperties props = LoadData<DeferredTexturingShaderProperties>(g_PushConstantData.userData0, 0);

    // groupID.x is the tile slot in VariantTileList for props.variantIndex
    ByteAddressBuffer tileOffsetsBuffer = GET_BUFFER(props.tileOffsetsBufferIndex);
    uint tileOffset = tileOffsetsBuffer.Load(props.variantIndex * 4u);

    ByteAddressBuffer tileList = GET_BUFFER(props.variantTileListIndex);
    uint tileSlot = groupID.x;
    uint tileIndex = tileList.Load((tileOffset + tileSlot) * 4u);
    uint2 pixelCoord = DecodeTilePixelCoord(tileIndex, groupThreadId.xy, props.tilesPerRow);

    if (pixelCoord.x >= props.renderWidth || pixelCoord.y >= props.renderHeight)
    {
        return;
    }

    Texture2D<uint2> visBuffer = GET_TEXTURE2D(props.visBufferIndex);
    uint2 raw = visBuffer[pixelCoord];

    float depth;
    uint visibleMeshletIndex;
    uint primitiveID;
    UnpackVisibility2(raw, depth, visibleMeshletIndex, primitiveID);

    // Reversed-Z: depth <= 0.0f means background/sky
    if (depth <= 0.0f)
    {
        return;
    }

    uint passBit = (visibleMeshletIndex >> 23u) & 1u;
    uint rawMeshletIndex = visibleMeshletIndex & 0x7FFFFFu;

    STRUCT_BUFFER visibleBufferIndex = (passBit == 0u) ? props.visibleMeshletsPass1 : props.visibleMeshletsPass2;
    StructuredBuffer<VisibleMeshletEntry> visibleMeshlets = GET_BUFFER(visibleBufferIndex);
    VisibleMeshletEntry visible = visibleMeshlets[rawMeshletIndex];

    // Wave scalarization check (idTech 8 Slide 28)
    bool dataIsUniform = WaveActiveAllTrue(visible.instanceIndex == WaveReadLaneFirst(visible.instanceIndex));
    uint instanceIndex = dataIsUniform ? WaveReadLaneFirst(visible.instanceIndex) : visible.instanceIndex;

    InstanceData instanceData = LoadData<InstanceData>(g_FrameData.sceneBuffer, instanceIndex);
    MeshData meshData = LoadData<MeshData>(instanceData.meshBuffer, 0);
    Meshlet meshlet = LoadMeshlet(visible.meshletIndex, meshData);
    uint localMaterialIndex = (meshlet.packedCounts >> 16u) & 0xFFu;

    uint packedMaterial = LoadMaterialBindlessIndex(g_FrameData.paletteOffsetBuffer, g_FrameData.materialIndexBuffer, instanceData.materialPaletteIndex, localMaterialIndex);

    uint candidateVariant = UnpackMaterialVariantIndex(packedMaterial);

    // Check if pixel actually belongs to this shader variant
    if (candidateVariant != props.variantIndex)
    {
        return;
    }

    // Evaluate barycentrics and interpolated vertex attributes
    InterpolatedAttributes attrs = EvaluateBarycentricsAndDerivatives(pixelCoord, depth, instanceIndex, visible.meshletIndex, primitiveID, meshData);

    MaterialContext ctx;
    ctx.instanceIndex = instanceIndex;
    ctx.materialIndex = attrs.cbufferIndex;
    ctx.positionWS = attrs.positionWS;
    ctx.normalWS = attrs.normalWS;
    ctx.tangentWS = attrs.tangentWS;
    ctx.uv = attrs.uv;
    ctx.ddx_uv = attrs.ddx_uv;
    ctx.ddy_uv = attrs.ddy_uv;

    MaterialProperties matProps = LoadMaterialData<MaterialProperties>(attrs.cbufferIndex);
    DEFERREDTEXTURING_STRATEGY strategy = DEFERREDTEXTURING_STRATEGY::Create();
    SurfaceData surface = strategy.GetSurfaceData(ctx, matProps);

    float3 srgbAlbedo = LinearToSRGB(surface.albedo);
    
    GBufferOutputs outputs = (GBufferOutputs)0;
#if defined(GHOST_TEMPLATE_LIT)
    outputs.SetBaseColor(srgbAlbedo);
    outputs.SetMetallic(surface.metallic);
    outputs.SetNormal(surface.normalWS);
    outputs.SetRoughness(surface.roughness);
    outputs.SetOcclusion(surface.occlusion);
    outputs.SetShadingModel(SHADING_MODEL_ID);
    outputs.SetTangent(surface.normalWS, attrs.tangentWS.xyz);
    outputs.SetEmissive(surface.emissive);
#elif defined(GHOST_TEMPLATE_UNLIT)
    outputs.SetBaseColor(srgbAlbedo);
    outputs.SetNormal(attrs.normalWS);
    outputs.SetTangent(attrs.normalWS, attrs.tangentWS.xyz);
    outputs.SetEmissive(surface.emissive);
#else
    #error "Unsupported template type for deferred texturing"
#endif

    WriteGBuffer(pixelCoord, outputs, props.gbuffer0Uav, props.gbuffer1Uav, props.gbuffer2Uav, props.gbuffer3Uav);
    
    RWTexture2D<float2> motionVectorBuffer = GET_TEXTURE2D(props.motionVectorUav);
    motionVectorBuffer[pixelCoord] = attrs.motionVectors;
}

#endif // GHOST_TEMPLATE_DEFERTEXTURING
