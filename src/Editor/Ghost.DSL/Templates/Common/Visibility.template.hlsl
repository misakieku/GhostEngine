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
bool VisibilitySpeculativeEarlyZ(uint2 pixelCoord, float depth, TEXTURE2D visBufferIndex, out uint64_t currentPacked)
{
    RWTexture2D<uint64_t> visBuffer = GET_TEXTURE2D(visBufferIndex);
    currentPacked = visBuffer[pixelCoord];

    uint currentDepthInt = (uint)(currentPacked >> VBUFFER_DEPTH_SHIFT);
    uint depthInt = asuint(depth);

    // In Reversed-Z, a closer fragment has a strictly greater depth integer
    return (depthInt > currentDepthInt);
}

// Writes visibility buffer entry via 64-bit atomic max
void VisibilityWritePixelAtomic(TEXTURE2D visBufferIndex, uint2 pixelCoord, float depth, uint visibleMeshletIndex, uint primitiveID)
{
    RWTexture2D<uint64_t> visBuffer = GET_TEXTURE2D(visBufferIndex);
    uint64_t newPacked = PackVisibility64(depth, visibleMeshletIndex, primitiveID);
    InterlockedMax(visBuffer[pixelCoord], newPacked);
}

#define VISIBILITY_MS_THREADS 64

struct SharedMeshletState
{
    float4x4 worldViewProj;
    uint pageBufferIndex;
    uint pageBase;
    uint vertexOffset;
    uint triangleOffset;
    uint vertexCount;
    uint triangleCount;
    uint visibleMeshletIndex;
    uint localMaterialIndex;
    uint materialBufferIndex;
    uint variantIndex;
    float doubleSided;
};

groupshared SharedMeshletState s_Meshlet;
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
    if (groupThreadID == 0)
    {
        ByteAddressBuffer matBuf = GET_BUFFER(g_FrameData.materialBuffer);

        BYTE_ADDRESS_BUFFER visibleBufferIndex = g_PushConstantData.userData0;
        BYTE_ADDRESS_BUFFER binOffsetsIndex = g_PushConstantData.userData2;
        uint targetVariantIndex = g_PushConstantData.userData3 >> 1u;
        uint passBit = (g_PushConstantData.userData3 & 1u) << 23u;

        ByteAddressBuffer binOffsetsBuffer = GET_BUFFER(binOffsetsIndex);

        uint binStartOffset = binOffsetsBuffer.Load(targetVariantIndex * 4u);
        uint binnedSlot = binStartOffset + groupID;

        StructuredBuffer<VisibleMeshletEntry> visibleMeshlets = GET_BUFFER(visibleBufferIndex);
        VisibleMeshletEntry visible = visibleMeshlets[binnedSlot];

        InstanceData instanceData = LoadData<InstanceData>(g_FrameData.sceneBuffer, visible.instanceIndex);
        MeshData meshData = LoadData<MeshData>(instanceData.meshBuffer, 0);
        Meshlet meshlet = LoadMeshlet(visible.meshletIndex, meshData);

        uint vertexCount = meshlet.packedCounts & 0xFFu;
        uint triangleCount = (meshlet.packedCounts >> 8) & 0xFFu;
        uint localMaterialIndex = (meshlet.packedCounts >> 16) & 0xFFu;

        uint packedMaterial = LoadMaterialBindlessIndex(g_FrameData.paletteOffsetBuffer, g_FrameData.materialIndexBuffer, instanceData.materialPaletteIndex, localMaterialIndex);

        uint materialBufferIndex = UnpackMaterialByteOffset(packedMaterial);
        uint variantIndex = UnpackMaterialVariantIndex(packedMaterial);

        float doubleSided = asfloat(matBuf.Load(materialBufferIndex + 12u));

        float4x4 localToRWS = ApplyCameraTranslationToMatrix(instanceData.localToWorld);
        float4x4 worldViewProj = mul(g_ViewData.viewProjectionMatrix, localToRWS);

        BYTE_ADDRESS_BUFFER pageBufferIndex;
        uint pageBase;
        GetMeshletPageBufferAndBase(meshData, meshlet.pageIndex, pageBufferIndex, pageBase);

        s_Meshlet.worldViewProj = worldViewProj;
        s_Meshlet.pageBufferIndex = pageBufferIndex;
        s_Meshlet.pageBase = pageBase;
        s_Meshlet.vertexOffset = meshlet.vertexOffset;
        s_Meshlet.triangleOffset = meshlet.triangleOffset;
        s_Meshlet.vertexCount = vertexCount;
        s_Meshlet.triangleCount = triangleCount;
        s_Meshlet.visibleMeshletIndex = (binnedSlot & 0x7FFFFFu) | passBit;
        s_Meshlet.localMaterialIndex = localMaterialIndex;
        s_Meshlet.materialBufferIndex = materialBufferIndex;
        s_Meshlet.variantIndex = variantIndex;
        s_Meshlet.doubleSided = doubleSided;
    }

    GroupMemoryBarrierWithGroupSync();

    SetMeshOutputCounts(s_Meshlet.vertexCount, s_Meshlet.triangleCount);

    if (groupThreadID < s_Meshlet.vertexCount)
    {
        ByteAddressBuffer pageBuffer = GET_BUFFER(s_Meshlet.pageBufferIndex);
        uint vOffset = s_Meshlet.pageBase + s_Meshlet.vertexOffset + groupThreadID * PACKED_VERTEX_STRIDE;

        float3 pos = asfloat(pageBuffer.Load3(vOffset));
        float2 uv = UnpackHalf2(pageBuffer.Load(vOffset + 16u));

        float4 clipPos = mul(s_Meshlet.worldViewProj, float4(pos, 1.0f));

        g_VertexPositions[groupThreadID] = clipPos;

        outVerts[groupThreadID].position = clipPos;
        outVerts[groupThreadID].uv = uv;
        outVerts[groupThreadID].visibleMeshletIndex = s_Meshlet.visibleMeshletIndex;
        outVerts[groupThreadID].localMaterialIndex = s_Meshlet.localMaterialIndex;
        outVerts[groupThreadID].materialBufferIndex = s_Meshlet.materialBufferIndex;
        outVerts[groupThreadID].variantIndex = s_Meshlet.variantIndex;
    }

    GroupMemoryBarrierWithGroupSync();

    [unroll(2)]
    for (uint primId = groupThreadID; primId < s_Meshlet.triangleCount; primId += VISIBILITY_MS_THREADS)
    {
        ByteAddressBuffer pageBuffer = GET_BUFFER(s_Meshlet.pageBufferIndex);
        uint packedIndices = pageBuffer.Load(s_Meshlet.pageBase + s_Meshlet.triangleOffset + primId * PACKED_TRIANGLE_STRIDE);
        uint3 indices = uint3(packedIndices & 0xFF, (packedIndices >> 8) & 0xFF, (packedIndices >> 16) & 0xFF);

        float4 v0 = g_VertexPositions[indices.x];
        float4 v1 = g_VertexPositions[indices.y];
        float4 v2 = g_VertexPositions[indices.z];

        outTris[primId] = indices;
        outPrims[primId].primitiveID = primId;

        bool isCulled = false;
        if (s_Meshlet.doubleSided == 0.0f)
        {
            isCulled = !IsTriangleFrontFacing(v0, v1, v2);
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
            ALPHA_STRATEGY strategy = ALPHA_STRATEGY::Create();
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
