// ============================================================
// GhostEngine Unified Visibility Pass (Lit / Unlit)
// ============================================================

#ifndef GHOST_TEMPLATE_VISIBILITY
#define GHOST_TEMPLATE_VISIBILITY

#if defined(GHOST_TEMPLATE_LIT)
#include "Lit/Lit_Common.template.hlsl"
#elif defined(GHOST_TEMPLATE_UNLIT)
#include "Unlit/Unlit_Common.template.hlsl"
#else
#error "Unsupported template type for visibility."
#endif

#include "EngineResources/Shaders/Mesh.hlsl"
#include "EngineResources/Shaders/Properties.hlsl"
#include "EngineResources/Shaders/Utilities/CullCommon.hlsl"
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
bool VisibilitySpeculativeEarlyZ(uint2 pixelCoord, float depth, uint visBufferIndex, out uint64_t currentPacked)
{
    RWTexture2D<uint64_t> visBuffer = ResourceDescriptorHeap[visBufferIndex];
    currentPacked = visBuffer[pixelCoord];

    uint currentDepthInt = (uint)(currentPacked >> VBUFFER_DEPTH_SHIFT);
    uint depthInt = asuint(depth);

    // In Reversed-Z, a closer fragment has a strictly greater depth integer
    return (depthInt > currentDepthInt);
}

// Writes visibility buffer entry via 64-bit atomic max
void VisibilityWritePixelAtomic(uint visBufferIndex, uint2 pixelCoord, float depth, uint visibleMeshletIndex, uint primitiveID)
{
    RWTexture2D<uint64_t> visBuffer = ResourceDescriptorHeap[visBufferIndex];
    uint64_t newPacked = PackVisibility64(depth, visibleMeshletIndex, primitiveID);
    InterlockedMax(visBuffer[pixelCoord], newPacked);
}

bool IsFrontFacingAndVisible(float4 h0, float4 h1, float4 h2, float subpixelThreshold = 0.0f)
{
    if (min(h0.w, min(h1.w, h2.w)) <= 0.0f)
    {
        return true;
    }

    float2 e0 = h1.xy * h0.w - h0.xy * h1.w;
    float2 e1 = h2.xy * h0.w - h0.xy * h2.w;

    float crossProduct = e0.x * e1.y - e0.y * e1.x;

    return crossProduct > subpixelThreshold;
}

float4 GetVertexClipPosition(uint vertexIndex, in MeshData meshData, in Meshlet meshlet, float4x4 worldViewProj, out float2 uv)
{
    Vertex v = LoadMeshletVertex(vertexIndex, meshData, meshlet);
    uv = v.uv;
    return mul(worldViewProj, float4(v.position, 1.0f));
}

#define VISIBILITY_MS_THREADS 64

groupshared float4 g_VertexPositions[MAX_VERTICES_PER_MESHLET];

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
    uint binOffsetsIndex = g_PushConstantData.userData2;
    uint targetVariantIndex = g_PushConstantData.userData3 >> 1u;
    uint passBit = (g_PushConstantData.userData3 & 1u) << 23u;

    ByteAddressBuffer binOffsetsBuffer = ResourceDescriptorHeap[binOffsetsIndex];
    uint binStartOffset = binOffsetsBuffer.Load(targetVariantIndex * 4u);
    uint binnedSlot = binStartOffset + groupID;

    StructuredBuffer<VisibleMeshletEntry> visibleMeshlets = ResourceDescriptorHeap[visibleBufferIndex];
    VisibleMeshletEntry visible = visibleMeshlets[binnedSlot];

    InstanceData instanceData = LoadData<InstanceData>(g_FrameData.sceneBuffer, visible.instanceIndex);
    MeshData meshData = LoadData<MeshData>(instanceData.meshBuffer, 0);
    Meshlet meshlet = LoadMeshlet(visible.meshletIndex, meshData);

    uint vertexCount = meshlet.packedCounts & 0xFFu;
    uint triangleCount = (meshlet.packedCounts >> 8) & 0xFFu;
    uint localMaterialIndex = (meshlet.packedCounts >> 16) & 0xFFu;

    uint packedMaterial = LoadMaterialBindlessIndex(g_FrameData.paletteOffsetBuffer,g_FrameData.materialIndexBuffer,instanceData.materialPaletteIndex, localMaterialIndex);

    uint materialBufferIndex = UnpackMaterialByteOffset(packedMaterial);
    uint variantIndex = UnpackMaterialVariantIndex(packedMaterial);
    
    ByteAddressBuffer matBuf = GET_BUFFER(g_FrameData.materialBuffer);
    float doubleSided = asfloat(matBuf.Load(materialBufferIndex + 12u));

    SetMeshOutputCounts(vertexCount, triangleCount);
    
    float4x4 worldViewProj = mul(g_ViewData.viewProjectionMatrix, instanceData.localToWorld);

    if (groupThreadID < vertexCount)
    {
        Vertex v = LoadMeshletVertex(groupThreadID, meshData, meshlet);
        
        float2 uv = v.uv;
        float4 clipPos = mul(worldViewProj, float4(v.position, 1.0f));
        
        g_VertexPositions[groupThreadID] = clipPos;

        outVerts[groupThreadID].position = clipPos;
        outVerts[groupThreadID].uv = uv;
        outVerts[groupThreadID].visibleMeshletIndex = (binnedSlot & 0x7FFFFFu) | passBit;
        outVerts[groupThreadID].localMaterialIndex = localMaterialIndex;
        outVerts[groupThreadID].materialBufferIndex = materialBufferIndex;
        outVerts[groupThreadID].variantIndex = variantIndex;
    }

    GroupMemoryBarrierWithGroupSync();

    [unroll(2)]
    for (uint primId = groupThreadID; primId < triangleCount; primId += VISIBILITY_MS_THREADS)
    {
        uint packedIndices = LoadPackedIndices(primId, meshData, meshlet);
        uint3 indices = uint3(packedIndices & 0xFF, (packedIndices >> 8) & 0xFF, (packedIndices >> 16) & 0xFF);

        float4 v0 = g_VertexPositions[indices.x];
        float4 v1 = g_VertexPositions[indices.y];
        float4 v2 = g_VertexPositions[indices.z];

        outTris[primId] = indices;
        outPrims[primId].primitiveID = primId;

        bool isCulled = false;
        if (doubleSided == 0.0f)
        {
            isCulled = !IsFrontFacingAndVisible(v2, v1, v0);
            if (!isCulled)
            {
                isCulled = IsTriangleOutsideFrustum(v0, v1, v2);
            }
        }
        
        outPrims[primId].cullPrim = isCulled;
    }
}

void PSMain(VisibilityPixelInput input, uint primitiveID : SV_PrimitiveID)
{
    uint visBufferIndex = g_PushConstantData.userData1;
    uint targetVariantIndex = g_PushConstantData.userData3 >> 1u;
    uint2 pixelCoord = (uint2)input.position.xy;
    uint64_t currentVal;

    // Speculative early-Z test (non-atomic read)
    if (!VisibilitySpeculativeEarlyZ(pixelCoord, input.position.z, visBufferIndex, currentVal))
    {
        return;
    }

    if (targetVariantIndex > 0u)
    {
        ByteAddressBuffer matBuf = GET_BUFFER(g_FrameData.materialBuffer);
        uint alphaClip = matBuf.Load(input.materialBufferIndex + 16u);
        if (alphaClip != 0u)
        {
            float alphaClipThreshold = asfloat(matBuf.Load(input.materialBufferIndex + 20u));
            VBUFFER_STRATEGY strategy = VBUFFER_STRATEGY::Create();
            float coverage = strategy.GetAlphaCoverage(input.materialBufferIndex, input.uv);
            if (coverage < alphaClipThreshold)
            {
                discard;
            }
        }
    }

    // 64-bit atomic max write
    VisibilityWritePixelAtomic(visBufferIndex, pixelCoord, input.position.z, input.visibleMeshletIndex, primitiveID);
}

#endif // GHOST_TEMPLATE_VISIBILITY