#ifndef GHOST_SHADOWRASTERIZER_HLSL
#define GHOST_SHADOWRASTERIZER_HLSL

#include "EngineResources/Shaders/Properties.hlsl"
#include "EngineResources/Shaders/Mesh.hlsl"
#include "EngineResources/Shaders/Utilities/CullCommon.hlsl"
#include "EngineResources/Shaders/MaterialPipeline/MaterialEncoding.hlsl"

struct ShadowPixelInput
{
    float4 position : SV_POSITION;
    float4 clipDistances : SV_ClipDistance0;
#if SHADOW_PIXEL_STAGE
    float2 uv : TEXCOORD0;
    nointerpolation uint materialBufferIndex : CBUFFER_ID;
#endif
};

struct ShadowPrimitiveOutput
{
    bool cullPrim : SV_CullPrimitive;
};

#define SHADOW_MS_THREADS 64

groupshared float4 g_VertexPositions[MAX_VERTICES_PER_MESHLET];

[numthreads(SHADOW_MS_THREADS, 1, 1)]
[outputtopology("triangle")]
void MSMain(
    uint groupID : SV_GroupID,
    uint groupThreadID : SV_GroupThreadID,
    out vertices ShadowPixelInput outVerts[MAX_VERTICES_PER_MESHLET],
    out indices uint3 outTris[MAX_TRIANGLES_PER_MESHLET],
    out primitives ShadowPrimitiveOutput outPrims[MAX_TRIANGLES_PER_MESHLET])
{
    uint visibleBufferIndex = g_PushConstantData.userData0;
    uint shadowViewsBufferSrv = g_PushConstantData.userData1;
    uint binOffsetsIndex = g_PushConstantData.userData2;
    uint targetVariantIndex = g_PushConstantData.userData3 >> 1u;

    uint binnedSlot = groupID;
    if (IS_VALID_BUFFER(binOffsetsIndex))
    {
        ByteAddressBuffer binOffsetsBuffer = ResourceDescriptorHeap[binOffsetsIndex];
        uint binStartOffset = binOffsetsBuffer.Load(targetVariantIndex * 4u);
        binnedSlot = binStartOffset + groupID;
    }

    StructuredBuffer<UnbinnedMeshletEntry> visibleMeshlets = ResourceDescriptorHeap[visibleBufferIndex];
    UnbinnedMeshletEntry visible = visibleMeshlets[binnedSlot];

    uint shadowViewIndex = visible.variantIndex & 0xFFFFu;
    ShadowViewData shadowView = LoadData<ShadowViewData>(shadowViewsBufferSrv, shadowViewIndex);

    InstanceData instanceData = LoadData<InstanceData>(g_FrameData.sceneBuffer, visible.instanceIndex);
    MeshData meshData = LoadData<MeshData>(instanceData.meshBuffer, 0);
    Meshlet meshlet = LoadMeshlet(visible.meshletIndex, meshData);

    uint vertexCount = meshlet.packedCounts & 0xFFu;
    uint triangleCount = (meshlet.packedCounts >> 8) & 0xFFu;
    
    SetMeshOutputCounts(vertexCount, triangleCount);

#if SHADOW_PIXEL_STAGE
    uint localMaterialIndex = (meshlet.packedCounts >> 16) & 0xFFu;
    uint packedMaterial = LoadMaterialBindlessIndex(g_FrameData.paletteOffsetBuffer, g_FrameData.materialIndexBuffer, instanceData.materialPaletteIndex, localMaterialIndex);
    uint materialBufferIndex = UnpackMaterialByteOffset(packedMaterial);
#else
    uint materialBufferIndex = 0u;
#endif

    ByteAddressBuffer matBuf = GET_BUFFER(g_FrameData.materialBuffer);
    float doubleSided = asfloat(matBuf.Load(materialBufferIndex + 12u));

    float4x4 worldViewProj = mul(shadowView.shadowViewProj, instanceData.localToWorld);

    if (groupThreadID < vertexCount)
    {
        ByteAddressBuffer rawMeshBuffer = GET_BUFFER(meshData.rawBuffer);
        uint vIndex = LoadMeshletVertexIndex(groupThreadID, meshData, meshlet);
        float3 pos = LoadVertexPositionOnly(rawMeshBuffer, meshData.vertexBufferOffset, vIndex);

        float4 origClipPos = mul(worldViewProj, float4(pos, 1.0f));

        outVerts[groupThreadID].clipDistances = float4(
            origClipPos.w + origClipPos.x, // Left
            origClipPos.w - origClipPos.x, // Right
            origClipPos.w + origClipPos.y, // Bottom
            origClipPos.w - origClipPos.y  // Top
        );

        float4 clipPos = origClipPos;
        clipPos.x = clipPos.x * shadowView.tileOffsetScale.z + (2.0f * shadowView.tileOffsetScale.x + shadowView.tileOffsetScale.z - 1.0f) * clipPos.w;
        clipPos.y = clipPos.y * shadowView.tileOffsetScale.w + (1.0f - (2.0f * shadowView.tileOffsetScale.y + shadowView.tileOffsetScale.w)) * clipPos.w;

        g_VertexPositions[groupThreadID] = clipPos;
        outVerts[groupThreadID].position = clipPos;

#if SHADOW_PIXEL_STAGE
        outVerts[groupThreadID].uv = LoadVertexUVOnly(rawMeshBuffer, meshData.vertexBufferOffset, vIndex);
        outVerts[groupThreadID].materialBufferIndex = materialBufferIndex;
#endif
    }

    GroupMemoryBarrierWithGroupSync();

    [unroll(2)]
    for (uint primId = groupThreadID; primId < triangleCount; primId += SHADOW_MS_THREADS)
    {
        uint packedIndices = LoadPackedIndices(primId, meshData, meshlet);
        uint3 indices = uint3(packedIndices & 0xFF, (packedIndices >> 8) & 0xFF, (packedIndices >> 16) & 0xFF);

        float4 v0 = g_VertexPositions[indices.x];
        float4 v1 = g_VertexPositions[indices.y];
        float4 v2 = g_VertexPositions[indices.z];

        outTris[primId] = indices;

        bool isCulled = false;
        if (doubleSided == 0.0f)
        {
            isCulled = !IsTriangleFrontFacingAndVisible(v0, v1, v2);
            if (!isCulled)
            {
                isCulled = IsTriangleOutsideFrustum(v0, v1, v2);
            }
        }

        outPrims[primId].cullPrim = isCulled;
    }
}

#if SHADOW_PIXEL_STAGE
void PSMain(ShadowPixelInput input)
{
    uint targetVariantIndex = g_PushConstantData.userData3 >> 1u;
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
}
#endif

#endif // GHOST_SHADOWRASTERIZER_HLSL
