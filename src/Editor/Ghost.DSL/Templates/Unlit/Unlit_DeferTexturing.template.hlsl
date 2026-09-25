#ifndef GHOST_UNLIT_DEFERTEXTURING_HLSL
#define GHOST_UNLIT_DEFERTEXTURING_HLSL

#include "Unlit_Common.template.hlsl"
#include "EngineResources/Shaders/Properties.hlsl"
#include "EngineResources/Shaders/Common.hlsl"
#include "EngineResources/Shaders/MeshPipeline/CullCommon.hlsl"
#include "EngineResources/Shaders/MaterialPipeline/MaterialEncoding.hlsl"
#include "EngineResources/Shaders/MaterialPipeline/VisibilityBufferEncoding.hlsl"
#include "EngineResources/Shaders/MaterialPipeline/ClassificationCommon.hlsl"
#include "EngineResources/Shaders/MaterialPipeline/Barycentrics.hlsl"
#include "EngineResources/Shaders/MaterialPipeline/GBufferPacking.hlsl"

[numthreads(CLASSIFICATION_TILE_SIZE, CLASSIFICATION_TILE_SIZE, 1)]
void CSMain(
    uint3 dispatchThreadId : SV_DispatchThreadID,
    uint3 groupID : SV_GroupID,
    uint3 groupThreadId : SV_GroupThreadID,
    uint groupIndex : SV_GroupIndex)
{
    DeferredTexturingShaderProperties props = LoadData<DeferredTexturingShaderProperties>(g_PushConstantData.userData0, 0);

    // groupID.x is the tile slot in VariantTileList for props.variantIndex
    ByteAddressBuffer tileOffsetsBuffer = ResourceDescriptorHeap[props.tileOffsetsBufferIndex];
    uint tileOffset = tileOffsetsBuffer.Load(props.variantIndex * 4u);

    ByteAddressBuffer tileList = ResourceDescriptorHeap[props.variantTileListIndex];
    uint tileSlot = groupID.x;
    uint tileIndex = tileList.Load((tileOffset + tileSlot) * 4u);
    uint2 pixelCoord = DecodeTilePixelCoord(tileIndex, groupThreadId.xy, props.tilesPerRow);

    if (pixelCoord.x >= props.renderWidth || pixelCoord.y >= props.renderHeight)
    {
        return;
    }

    uint byteAddress = ComputePixelByteAddress(pixelCoord, props.renderWidth);
    ByteAddressBuffer visBuffer = ResourceDescriptorHeap[props.visBufferIndex];
    uint64_t raw = visBuffer.Load<uint64_t>(byteAddress);

    float depth;
    uint visibleMeshletIndex;
    uint primitiveID;
    UnpackVisibility64(raw, depth, visibleMeshletIndex, primitiveID);

    // Reversed-Z: depth <= 0.0f means background/sky
    if (depth <= 0.0f)
    {
        return;
    }

    uint passBit = (visibleMeshletIndex >> 23u) & 1u;
    uint rawMeshletIndex = visibleMeshletIndex & 0x7FFFFFu;

    uint visibleBufferIndex = (passBit == 0u) ? props.visibleMeshletsPass1 : props.visibleMeshletsPass2;
    StructuredBuffer<VisibleMeshletEntry> visibleMeshlets = ResourceDescriptorHeap[visibleBufferIndex];
    VisibleMeshletEntry visible = visibleMeshlets[rawMeshletIndex];

    // Wave scalarization check (idTech 8 Slide 28)
    bool dataIsUniform = WaveActiveAllTrue(visible.instanceIndex == WaveReadLaneFirst(visible.instanceIndex));
    uint instanceIndex = dataIsUniform ? WaveReadLaneFirst(visible.instanceIndex) : visible.instanceIndex;

    InstanceData instanceData = LoadData<InstanceData>(g_FrameData.sceneBuffer, instanceIndex);
    MeshData meshData = LoadData<MeshData>(instanceData.meshBuffer, 0);
    
    Meshlet meshlet = LoadData<Meshlet>(meshData.meshletBuffer, visible.meshletIndex);
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
    
    MaterialProperties matProps = LoadData<MaterialProperties>(attrs.cbufferIndex, 0);
    Payload payload = (Payload)0;
    float4 unlitColor = GetColor(matProps, attrs.uv, payload);
    
    // Pack GBuffer outputs
    GBufferOutputs outputs;
    // GBuffer0: Empty for unlit
    outputs.gbuffer0 = float4(0.0f, 0.0f, 0.0f, 0.0f);
    // GBuffer1: Normal, roughness=1, metallic=0
    outputs.gbuffer1 = float4(OctahedralEncode(attrs.normalWS), 1.0f, 0.0f);
    // GBuffer2: Motion Vectors (xy) + Occlusion (z) + FeatureBitmask (w)
    outputs.gbuffer2 = float4(attrs.motionVectors, 1.0f, 0.0f);
    // GBuffer3: Emissive Color (rgb) + Unlit flag (1.0f)
    outputs.gbuffer3 = float4(unlitColor.rgb, 1.0f);
    
    WriteGBuffer(pixelCoord, outputs, props.gbuffer0Uav, props.gbuffer1Uav, props.gbuffer2Uav, props.gbuffer3Uav);
}

#endif // GHOST_UNLIT_DEFERTEXTURING_HLSL
