// ============================================================
// GhostEngine Unified Visibility Pass (Lit / Unlit)
// ============================================================

#if defined(GHOST_TEMPLATE_LIT)
#include "Lit/Lit_Common.template.hlsl"
#elif defined(GHOST_TEMPLATE_UNLIT)
#include "Unlit/Unlit_Common.template.hlsl"
#endif

#include "EngineResources/Shaders/Includes/Properties.hlsl"
#include "EngineResources/Shaders/Includes/CullCommon.hlsl"
#include "EngineResources/Shaders/Includes/MaterialEncoding.hlsl"
#include "EngineResources/Shaders/Includes/VisibilityBufferEncoding.hlsl"
#include "EngineResources/Shaders/Includes/VisibilityCommon.hlsl"

#define VISIBILITY_MS_THREADS 64

[numthreads(VISIBILITY_MS_THREADS, 1, 1)]
[outputtopology("triangle")]
void MSMain(
    uint groupID : SV_GroupID,
    uint groupThreadID : SV_GroupThreadID,
    out vertices VisibilityPixelInput outVerts[MAX_VERTICES_PER_MESHLET],
    out indices uint3 outTris[MAX_TRIANGLES_PER_MESHLET],
    out primitives VisibilityPrimitiveOutput outPrims[MAX_TRIANGLES_PER_MESHLET])
{
    uint visibleBufferIndex = g_PushConstantData.userData0;
    StructuredBuffer<VisibleMeshletEntry> visibleMeshlets = ResourceDescriptorHeap[visibleBufferIndex];
    VisibleMeshletEntry visible = visibleMeshlets[groupID];

    InstanceData instanceData = LoadData<InstanceData>(g_FrameData.sceneBuffer, visible.instanceIndex);
    MeshData meshData = LoadData<MeshData>(instanceData.meshBuffer, 0);
    Meshlet meshlet = LoadData<Meshlet>(meshData.meshletBuffer, visible.meshletIndex);
    
    ByteAddressBuffer meshletVerticesBuffer = GET_BUFFER(meshData.meshletVerticesBuffer);
    ByteAddressBuffer meshletTrianglesBuffer = GET_BUFFER(meshData.meshletTrianglesBuffer);
    ByteAddressBuffer vertices = GET_BUFFER(meshData.vertexBuffer);

    uint vertexIndex = meshletVerticesBuffer.Load((meshlet.vertexOffset + groupThreadID) * 4);
    Vertex v = vertices.Load<Vertex>(vertexIndex * 64);

    uint vertexCount = meshlet.packedCounts & 0xFFu;
    uint triangleCount = (meshlet.packedCounts >> 8) & 0xFFu;
    uint localMaterialIndex = (meshlet.packedCounts >> 16) & 0xFFu;

    uint packedMaterial = LoadMaterialBindlessIndex(
        g_FrameData.paletteOffsetBuffer,
        g_FrameData.materialIndexBuffer,
        instanceData.materialPaletteIndex,
        localMaterialIndex);

    uint cbufferIndex = UnpackMaterialCBufferIndex(packedMaterial);
    uint variantIndex = UnpackMaterialVariantIndex(packedMaterial);

    uint targetVariantIndex = g_PushConstantData.userData2;
    bool isVisible = (targetVariantIndex == 0xFFFFFFFF || variantIndex == targetVariantIndex);
    
    vertexCount = isVisible ? vertexCount : 0;
    triangleCount = isVisible ? triangleCount : 0;

    SetMeshOutputCounts(vertexCount, triangleCount);

    if (groupThreadID < vertexCount)
    {
        float4x4 worldViewProj = mul(g_ViewData.viewProjectionMatrix, instanceData.localToWorld);

        outVerts[groupThreadID].position = mul(worldViewProj, float4(v.position, 1.0f));
        outVerts[groupThreadID].uv = v.uv;
        outVerts[groupThreadID].instanceIndex = visible.instanceIndex;
        outVerts[groupThreadID].localMaterialIndex = localMaterialIndex;
        outVerts[groupThreadID].cbufferIndex = cbufferIndex;
        outVerts[groupThreadID].variantIndex = variantIndex;
    }

    [unroll(2)]
    for (uint i = groupThreadID; i < triangleCount; i += VISIBILITY_MS_THREADS)
    {
        uint packedIndices = meshletTrianglesBuffer.Load((meshlet.triangleOffset + i) * 4);
        outTris[i] = uint3(packedIndices & 0xFF, (packedIndices >> 8) & 0xFF, (packedIndices >> 16) & 0xFF);
        outPrims[i].primitiveID = i;
    }
}

void PSMain(VisibilityPixelInput input, uint primitiveID : SV_PrimitiveID)
{
    uint visBufferIndex = g_PushConstantData.userData1;
    uint byteAddress;
    uint64_t currentVal;

    // Speculative early-Z test (non-atomic read)
    if (!VisibilitySpeculativeEarlyZ((uint2)input.position.xy, input.position.z, visBufferIndex, byteAddress, currentVal))
    {
        return;
    }

    // Dynamic alpha-clip hook
    Payload payload = (Payload)0;
    MaterialProperties props = LoadData<MaterialProperties>(input.cbufferIndex, 0);
    
    if (props.alphaClip)
    {
        float coverage = GetAlphaCoverage(props, input.uv, payload);
        if (coverage < props.alphaClipThreshold)
        {
            discard;
        }
    }

    // 64-bit atomic max write
    VisibilityWritePixelAtomic(visBufferIndex, byteAddress, input.position.z, input.instanceIndex, primitiveID);
}