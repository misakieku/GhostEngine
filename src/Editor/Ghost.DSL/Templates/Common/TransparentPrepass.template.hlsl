// ============================================================
// GhostEngine Unified Transparent Extinction Prepass (Lit / Unlit)
// ============================================================

#ifndef GHOST_TEMPLATE_TRANSPARENT_PREPASS
#define GHOST_TEMPLATE_TRANSPARENT_PREPASS

#include "EngineResources/Shaders/Generated/GhostRenderPipeline.hlsl"

cbuffer PassProperties : register(b3, space0)
{
    TransparentPrepassShaderProperties g_Properties;
};

#if defined(GHOST_TEMPLATE_LIT)
#include "Lit/Lit_Common.template.hlsl"
#elif defined(GHOST_TEMPLATE_UNLIT)
#include "Unlit/Unlit_Common.template.hlsl"
#else
#error "Unsupported template type for transparent prepass."
#endif

#include "EngineResources/Shaders/Mesh.hlsl"
#include "EngineResources/Shaders/Properties.hlsl"
#include "EngineResources/Shaders/Utilities/CullCommon.hlsl"
#include "EngineResources/Shaders/Utilities/SpaceTransforms.hlsl"
#include "EngineResources/Shaders/MaterialPipeline/MaterialEncoding.hlsl"
#include "EngineResources/Shaders/Oit/OitCommon.hlsl"

struct TransparentPrepassPixelInput
{
    float4 position : SV_POSITION;
    float3 positionRWS : TEXCOORD0;
    float3 normalWS : TEXCOORD1;
    float2 uv : TEXCOORD2;
    nointerpolation uint instanceIndex : INSTANCE_ID;
    nointerpolation uint materialBufferIndex : CBUFFER_ID;
    nointerpolation uint chromatic : CHROMATIC_FLAG;
};

struct TransparentPrepassPrimitiveOutput
{
    bool cullPrim : SV_CullPrimitive;
};

#define TRANSPARENT_PREPASS_MS_THREADS 64

struct SharedTransparentPrepassState
{
    float4x4 localToRWS;
    float4x4 localToWorld;
    float4x4 worldViewProj;
    float3 worldBoundsMin;
    float positionQuantStep;
    uint pageBufferIndex;
    uint pageBase;
    uint vertexOffset;
    uint triangleOffset;
    uint packedQuantOrigin0;
    uint packedQuantOrigin1;
    uint packedFormat;
    uint vertexCount;
    uint triangleCount;
    uint instanceIndex;
    uint materialBufferIndex;
    uint chromatic;
    float doubleSided;
};

groupshared SharedTransparentPrepassState s_Meshlet;
groupshared float4 g_VertexPositions[MAX_VERTICES_PER_MESHLET];

[numthreads(TRANSPARENT_PREPASS_MS_THREADS, 1, 1)]
[outputtopology("triangle")]
void MSMain(
    uint groupID : SV_GroupID,
    uint groupThreadID : SV_GroupThreadID,
    out vertices TransparentPrepassPixelInput outVerts[MAX_VERTICES_PER_MESHLET],
    out indices uint3 outTris[MAX_TRIANGLES_PER_MESHLET],
    out primitives TransparentPrepassPrimitiveOutput outPrims[MAX_TRIANGLES_PER_MESHLET])
{
    if (groupThreadID == 0)
    {
        ByteAddressBuffer matBuf = GET_BUFFER(g_FrameData.materialBuffer);
        ByteAddressBuffer binOffsetsBuffer = GET_BUFFER(g_Properties.binOffsetsBufferIndex);
        uint binStartOffset = binOffsetsBuffer.Load(g_Properties.binIndex * 4u);
        uint binnedSlot = binStartOffset + groupID;

        StructuredBuffer<VisibleMeshletEntry> visibleMeshlets = GET_BUFFER(g_Properties.visibleMeshletsBufferIndex);
        VisibleMeshletEntry visible = visibleMeshlets[binnedSlot];

        InstanceData instanceData = LoadData<InstanceData>(g_FrameData.sceneBuffer, visible.instanceIndex);
        MeshData meshData = LoadData<MeshData>(instanceData.meshBuffer, 0);
        Meshlet meshlet = LoadMeshlet(visible.meshletIndex, meshData);

        uint vertexCount = meshlet.packedCounts & 0xFFu;
        uint triangleCount = (meshlet.packedCounts >> 8u) & 0xFFu;
        uint localMaterialIndex = (meshlet.packedCounts >> 16u) & 0xFFu;

        uint packedMaterial = LoadMaterialBindlessIndex(g_FrameData.paletteOffsetBuffer, g_FrameData.materialIndexBuffer, instanceData.materialPaletteIndex, localMaterialIndex);
        uint materialBufferIndex = UnpackMaterialByteOffset(packedMaterial);
        uint chromatic = UnpackMaterialChromaticTransmittance(packedMaterial);
        float doubleSided = asfloat(matBuf.Load(materialBufferIndex + 12u));

        float4x4 localToRWS = ApplyCameraTranslationToMatrix(instanceData.localToWorld);
        float4x4 worldViewProj = mul(g_ViewData.viewProjectionMatrix, localToRWS);

        BYTE_ADDRESS_BUFFER pageBufferIndex;
        uint pageBase;
        GetMeshletPageBufferAndBase(meshData, meshlet.pageIndex, pageBufferIndex, pageBase);

        s_Meshlet.localToRWS = localToRWS;
        s_Meshlet.localToWorld = instanceData.localToWorld;
        s_Meshlet.worldViewProj = worldViewProj;
        s_Meshlet.worldBoundsMin = meshData.worldBoundsMin;
        s_Meshlet.positionQuantStep = meshData.positionQuantStep;
        s_Meshlet.pageBufferIndex = pageBufferIndex;
        s_Meshlet.pageBase = pageBase;
        s_Meshlet.vertexOffset = meshlet.vertexOffset;
        s_Meshlet.triangleOffset = meshlet.triangleOffset;
        s_Meshlet.packedQuantOrigin0 = meshlet.packedQuantOrigin0;
        s_Meshlet.packedQuantOrigin1 = meshlet.packedQuantOrigin1;
        s_Meshlet.packedFormat = meshlet.packedFormat;
        s_Meshlet.vertexCount = vertexCount;
        s_Meshlet.triangleCount = triangleCount;
        s_Meshlet.instanceIndex = visible.instanceIndex;
        s_Meshlet.materialBufferIndex = materialBufferIndex;
        s_Meshlet.chromatic = chromatic;
        s_Meshlet.doubleSided = doubleSided;
    }

    GroupMemoryBarrierWithGroupSync();

    SetMeshOutputCounts(s_Meshlet.vertexCount, s_Meshlet.triangleCount);

    if (groupThreadID < s_Meshlet.vertexCount)
    {
        ByteAddressBuffer pageBuffer = GET_BUFFER(s_Meshlet.pageBufferIndex);

        float3 pos = UnpackMeshletPosition(pageBuffer, s_Meshlet.pageBase, s_Meshlet.vertexOffset, s_Meshlet.packedQuantOrigin0, s_Meshlet.packedQuantOrigin1, s_Meshlet.packedFormat, s_Meshlet.worldBoundsMin, s_Meshlet.positionQuantStep, groupThreadID);

        bool hasColor = HasMeshletVertexColor(s_Meshlet.packedFormat);
        uint attrStride = hasColor ? 12u : 8u;
        uint attrOffset = GetMeshletAttributeBase(s_Meshlet.pageBase, s_Meshlet.packedFormat) + groupThreadID * attrStride;
        uint2 rawAttr = pageBuffer.Load2(attrOffset);

        float3 localNormal = UnpackOctahedralNormal(rawAttr.x);
        float2 uv = UnpackHalf2(rawAttr.y);

        float3 posRWS = mul(s_Meshlet.localToRWS, float4(pos, 1.0f)).xyz;
        float3 normalWS = normalize(mul((float3x3)s_Meshlet.localToWorld, localNormal));
        float4 clipPos = mul(s_Meshlet.worldViewProj, float4(pos, 1.0f));

        g_VertexPositions[groupThreadID] = clipPos;

        outVerts[groupThreadID].position = clipPos;
        outVerts[groupThreadID].positionRWS = posRWS;
        outVerts[groupThreadID].normalWS = normalWS;
        outVerts[groupThreadID].uv = uv;
        outVerts[groupThreadID].instanceIndex = s_Meshlet.instanceIndex;
        outVerts[groupThreadID].materialBufferIndex = s_Meshlet.materialBufferIndex;
        outVerts[groupThreadID].chromatic = s_Meshlet.chromatic;
    }

    GroupMemoryBarrierWithGroupSync();

    [unroll(2)]
    for (uint primId = groupThreadID; primId < s_Meshlet.triangleCount; primId += TRANSPARENT_PREPASS_MS_THREADS)
    {
        ByteAddressBuffer pageBuffer = GET_BUFFER(s_Meshlet.pageBufferIndex);
        uint3 indices = UnpackMeshletTriangleIndices(pageBuffer, s_Meshlet.pageBase, s_Meshlet.triangleOffset, primId);

        float4 v0 = g_VertexPositions[indices.x];
        float4 v1 = g_VertexPositions[indices.y];
        float4 v2 = g_VertexPositions[indices.z];

        outTris[primId] = indices;

        bool isCulled = false;
        if (s_Meshlet.doubleSided == 0.0f)
        {
            isCulled = !IsTriangleFrontFacing(v0, v1, v2);
        }

        if (!isCulled)
        {
            isCulled = IsTriangleOutsideFrustum(v0, v1, v2);
        }

        outPrims[primId].cullPrim = isCulled;
    }
}

void PSMain(TransparentPrepassPixelInput input, bool isFrontFace : SV_IsFrontFace)
{
    uint2 oitCoord = (uint2)input.position.xy;
    if (oitCoord.x >= g_Properties.oitWidth || oitCoord.y >= g_Properties.oitHeight)
    {
        return;
    }

    MaterialContext ctx = (MaterialContext)0;
    ctx.instanceIndex = input.instanceIndex;
    ctx.materialIndex = input.materialBufferIndex;
    ctx.positionRWS = input.positionRWS;
    ctx.normalWS = normalize(input.normalWS);
    ctx.tangentWS = float4(1.0f, 0.0f, 0.0f, 1.0f);
    ctx.uv = input.uv;
    ctx.ddx_uv = ddx(input.uv);
    ctx.ddy_uv = ddy(input.uv);
    ctx.isFrontFace = isFrontFace;

    MaterialProperties matProps = LoadMaterialData<MaterialProperties>(input.materialBufferIndex);
    TRANSPARENCY_STRATEGY strategy = TRANSPARENCY_STRATEGY::Create();
    TransparencyData tData = strategy.GetTransparencyData(ctx, matProps);

    if (tData.opacity <= 0.001f)
    {
        return;
    }

    if (!IS_VALID_BUFFER(g_Properties.depthWarpLutSrv))
    {
        return;
    }

    ByteAddressBuffer depthWarpLut = GET_BUFFER(g_Properties.depthWarpLutSrv);
    OitWarpLutHeader header = LoadOitWarpLutHeader(depthWarpLut);
    if (header.virtualSliceCount == 0u || header.physicalSliceCount == 0u)
    {
        return;
    }

    float viewZ = dot(input.positionRWS, g_ViewData.cameraDirection);
    if (viewZ <= 0.0f)
    {
        return;
    }

    float virtualSliceF = OitDepthToVirtualSlice(viewZ, header.linearizationFactor, header.farPlane, header.virtualSliceCount);
    float physicalSliceF = 0.0f;
    if (!OitMapVirtualSliceToPhysical(depthWarpLut, virtualSliceF, physicalSliceF))
    {
        return;
    }

    if (input.chromatic == 0u)
    {
        if (IS_VALID_TEXTURE(g_Properties.scalarExtinctionUav) && IS_VALID_TEXTURE(g_Properties.scalarOverflowMinUav))
        {
            RWTexture3D<uint> scalarExtUav = GET_TEXTURE3D(g_Properties.scalarExtinctionUav);
            RWTexture2D<uint> scalarOverflowUav = GET_TEXTURE2D(g_Properties.scalarOverflowMinUav);
            OitSplatScalarExtinction(scalarExtUav, scalarOverflowUav, oitCoord, physicalSliceF, header.physicalSliceCount, tData.opacity);
        }
    }
    else
    {
        if (IS_VALID_TEXTURE(g_Properties.rgbExtinctionUav) && IS_VALID_TEXTURE(g_Properties.rgbOverflowMinUav))
        {
            float3 alphaRGB = saturate(tData.opacity * (1.0f - saturate(tData.transmittanceColor)));
            RWTexture3D<uint> rgbExtUav = GET_TEXTURE3D(g_Properties.rgbExtinctionUav);
            RWTexture3D<uint> rgbOverflowUav = GET_TEXTURE3D(g_Properties.rgbOverflowMinUav);
            OitSplatRGBExtinction(rgbExtUav, rgbOverflowUav, oitCoord, physicalSliceF, header.physicalSliceCount, alphaRGB);
        }
    }
}

#endif // GHOST_TEMPLATE_TRANSPARENT_PREPASS
