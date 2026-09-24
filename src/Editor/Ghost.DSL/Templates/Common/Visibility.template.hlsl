// ============================================================
// GhostEngine Unified Visibility Pass (Lit / Unlit)
// ============================================================

#if defined(GHOST_TEMPLATE_LIT)
#include "Lit/Lit_Common.template.hlsl"
#elif defined(GHOST_TEMPLATE_UNLIT)
#include "Unlit/Unlit_Common.template.hlsl"
#endif

#include "EngineResources/Shaders/Properties.hlsl"
#include "EngineResources/Shaders/MeshPipeline/CullCommon.hlsl"
#include "EngineResources/Shaders/MaterialPipeline/MaterialEncoding.hlsl"
#include "EngineResources/Shaders/MaterialPipeline/VisibilityBufferEncoding.hlsl"

struct VisibilityPixelInput
{
    float4 position : SV_POSITION;
    float2 uv : TEXCOORD0;
    nointerpolation uint visibleMeshletIndex : INSTANCE_ID;
    nointerpolation uint localMaterialIndex : MATERIAL_ID;
    nointerpolation uint materialBufferIndex : CBUFFER_ID;
    nointerpolation uint variantIndex : VARIANT_ID;
};

struct VisibilityPrimitiveOutput
{
    uint primitiveID : SV_PrimitiveID;
    bool cullPrim : SV_CullPrimitive;
};

// Speculative early-Z test via non-atomic 64-bit load
static inline bool VisibilitySpeculativeEarlyZ(uint2 pixelCoord, float depth, uint visBufferIndex, out uint byteAddress, out uint64_t currentPacked)
{
    uint renderWidth = (uint)g_ViewData.screenSize.x;
    byteAddress = ComputePixelByteAddress(pixelCoord, renderWidth);

    RWByteAddressBuffer visBuffer = ResourceDescriptorHeap[visBufferIndex];
    currentPacked = visBuffer.Load<uint64_t>(byteAddress);

    uint currentDepthInt = (uint)(currentPacked >> VBUFFER_DEPTH_SHIFT);
    uint depthInt = asuint(depth);

    // In Reversed-Z, a closer fragment has a strictly greater depth integer
    return (depthInt > currentDepthInt);
}

// Writes visibility buffer entry via 64-bit atomic max
static inline void VisibilityWritePixelAtomic(uint visBufferIndex, uint byteAddress, float depth, uint visibleMeshletIndex, uint primitiveID)
{
    RWByteAddressBuffer visBuffer = ResourceDescriptorHeap[visBufferIndex];
    uint64_t newPacked = PackVisibility64(depth, visibleMeshletIndex, primitiveID);
    visBuffer.InterlockedMax64(byteAddress, newPacked);
}

static inline bool IsFrontFacing(float4 h0, float4 h1, float4 h2)
{
    return determinant(float3x3(h0.xyw, h1.xyw, h2.xyw)) >= 0;
}

static inline float4 GetVertexClipPosition(uint vertexIndex, uint meshletVertexOffset, ByteAddressBuffer meshletVerticesBuffer, ByteAddressBuffer vertexBuffer, float4x4 worldViewProj, out float2 uv)
{
    uint vIdx = meshletVerticesBuffer.Load((meshletVertexOffset + vertexIndex) * 4u);
    Vertex v = vertexBuffer.Load<Vertex>(vIdx * sizeof(Vertex));
    uv = v.uv;
    return mul(worldViewProj, float4(v.position, 1.0f));
}

#define VISIBILITY_MS_THREADS 64

groupshared float4 g_VertexPositions[MAX_VERTICES_PER_MESHLET];
groupshared float2 g_VertexUVs[MAX_VERTICES_PER_MESHLET];
groupshared uint g_PackedIndices[MAX_TRIANGLES_PER_MESHLET];

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

    uint vertexCount = meshlet.packedCounts & 0xFFu;
    uint triangleCount = (meshlet.packedCounts >> 8) & 0xFFu;
    uint localMaterialIndex = (meshlet.packedCounts >> 16) & 0xFFu;

    uint packedMaterial = LoadMaterialBindlessIndex(g_FrameData.paletteOffsetBuffer,g_FrameData.materialIndexBuffer,instanceData.materialPaletteIndex, localMaterialIndex);

    uint materialBufferIndex = UnpackMaterialMaterialBufferIndex(packedMaterial);
    uint variantIndex = UnpackMaterialVariantIndex(packedMaterial);
    
    MaterialProperties props = LoadData<MaterialProperties>(materialBufferIndex, 0);

    uint targetVariantIndex = g_PushConstantData.userData2;
    bool isVisible = (targetVariantIndex == 0xFFFFFFFF || variantIndex == targetVariantIndex);
    
    vertexCount = isVisible ? vertexCount : 0;
    triangleCount = isVisible ? triangleCount : 0;

    SetMeshOutputCounts(vertexCount, triangleCount);
    
    float4x4 worldViewProj = mul(g_ViewData.viewProjectionMatrix, instanceData.localToWorld);
    
    uint i;
    [unroll(2)]
    for (i = 0; i < MAX_TRIANGLES_PER_MESHLET; i += VISIBILITY_MS_THREADS)
    {
        const uint primId = i + groupThreadID;
        if (primId < triangleCount)
        {
            uint packedIndices = meshletTrianglesBuffer.Load((meshlet.triangleOffset + primId) * 4);
            uint3 indices = uint3(packedIndices & 0xFF, (packedIndices >> 8) & 0xFF, (packedIndices >> 16) & 0xFF);
        
            float2 uv0, uv1, uv2;
            float4 v0 = GetVertexClipPosition(indices.x, meshlet.vertexOffset, meshletVerticesBuffer, vertices, worldViewProj, uv0);
            float4 v1 = GetVertexClipPosition(indices.y, meshlet.vertexOffset, meshletVerticesBuffer, vertices, worldViewProj, uv1);
            float4 v2 = GetVertexClipPosition(indices.z, meshlet.vertexOffset, meshletVerticesBuffer, vertices, worldViewProj, uv2);
        
            g_VertexPositions[indices.x] = v0;
            g_VertexPositions[indices.y] = v1;
            g_VertexPositions[indices.z] = v2;
        
            g_VertexUVs[indices.x] = uv0;
            g_VertexUVs[indices.y] = uv1;
            g_VertexUVs[indices.z] = uv2;
        
            g_PackedIndices[primId] = packedIndices;
        }
    }
    
    GroupMemoryBarrierWithGroupSync();

    if (groupThreadID < vertexCount)
    {
        uint passBit = (g_PushConstantData.userData3 & 1u) << 23u;

        outVerts[groupThreadID].position = g_VertexPositions[groupThreadID];
        outVerts[groupThreadID].uv = g_VertexUVs[groupThreadID];
        outVerts[groupThreadID].visibleMeshletIndex = (groupID & 0x7FFFFFu) | passBit;
        outVerts[groupThreadID].localMaterialIndex = localMaterialIndex;
        outVerts[groupThreadID].materialBufferIndex = materialBufferIndex;
        outVerts[groupThreadID].variantIndex = variantIndex;
    }

    [unroll(2)]
    for (i = groupThreadID; i < triangleCount; i += VISIBILITY_MS_THREADS)
    {
        uint packedIndices = g_PackedIndices[i];
        uint3 indices = uint3(packedIndices & 0xFF, (packedIndices >> 8) & 0xFF, (packedIndices >> 16) & 0xFF);
        
        float4 v0 = g_VertexPositions[indices.x];
        float4 v1 = g_VertexPositions[indices.y];
        float4 v2 = g_VertexPositions[indices.z];
        
        outTris[i] = indices;
        
        outPrims[i].primitiveID = i;
        outPrims[i].cullPrim = props.doubleSidedConstants.w == 0.0f && !IsFrontFacing(v2, v1, v0);
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
    MaterialProperties props = LoadData<MaterialProperties>(input.materialBufferIndex, 0);
    
    if (props.alphaClip)
    {
        float coverage = GetAlphaCoverage(props, input.uv, payload);
        if (coverage < props.alphaClipThreshold)
        {
            discard;
        }
    }

    // 64-bit atomic max write
    VisibilityWritePixelAtomic(visBufferIndex, byteAddress, input.position.z, input.visibleMeshletIndex, primitiveID);
}