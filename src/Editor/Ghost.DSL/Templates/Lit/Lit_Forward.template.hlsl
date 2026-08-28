// ============================================================
// GhostEngine LitTemplate - Forward Pass
// ============================================================

#include "Lit_Common.template.hlsl"

struct PSInput
{
    float4 position : SV_POSITION;
    float3 worldPos : TEXCOORD0;
    float3 normal : NORMAL;
    float2 uv : TEXCOORD1;
    nointerpolation uint meshletID : MESHLET_ID;
    nointerpolation uint materialIndex : MATERIAL_INDEX;
    nointerpolation uint instanceIndex : INSTANCE_INDEX;
};

struct ASPayload
{
    uint meshletIndex;
    uint materialIndex;
    uint instanceIndex;
};

groupshared ASPayload s_Payload;

[numthreads(1, 1, 1)]
void ASMain(uint3 groupID : SV_GroupID)
{
    FrameData frameData = LoadData<FrameData>(g_PushConstantData.frameBuffer, 0);
    InstanceData instanceData = LoadData<InstanceData>(frameData.instanceBuffer, g_PushConstantData.instanceIndex);
    MeshData meshData = LoadData<MeshData>(instanceData.meshBuffer, 0);

    ByteAddressBuffer meshletBuffer = GET_BUFFER(meshData.meshletBuffer);
    Meshlet meshlet = meshletBuffer.Load<Meshlet>(groupID.x * sizeof(Meshlet));

    uint localMaterialIndex = (meshlet.packedCounts >> 16) & 0xFFu;
    s_Payload.meshletIndex = groupID.x;
    s_Payload.instanceIndex = g_PushConstantData.instanceIndex;
    s_Payload.materialIndex = LoadMaterialBindlessIndex(
        frameData.paletteOffsetBuffer,
        frameData.materialIndexBuffer,
        instanceData.materialPaletteIndex,
        localMaterialIndex);

    DispatchMesh(1, 1, 1, s_Payload);
}

[numthreads(128, 1, 1)]
[outputtopology("triangle")]
void MSMain(
    in payload ASPayload asPayload,
    uint3 groupThreadID : SV_GroupThreadID,
    out vertices PSInput outVerts[64],
    out indices uint3 outTris[124])
{
    FrameData frameData = LoadData<FrameData>(g_PushConstantData.frameBuffer, 0);
    InstanceData instanceData = LoadData<InstanceData>(frameData.instanceBuffer, asPayload.instanceIndex);
    MeshData meshData = LoadData<MeshData>(instanceData.meshBuffer, 0);

    ByteAddressBuffer meshletBuffer = GET_BUFFER(meshData.meshletBuffer);
    Meshlet meshlet = meshletBuffer.Load<Meshlet>(asPayload.meshletIndex * sizeof(Meshlet));

    uint vertexCount = meshlet.packedCounts & 0xFFu;
    uint triangleCount = (meshlet.packedCounts >> 8) & 0xFFu;
    SetMeshOutputCounts(vertexCount, triangleCount);

    ByteAddressBuffer meshletVerticesBuffer = GET_BUFFER(meshData.meshletVerticesBuffer);
    ByteAddressBuffer meshletTrianglesBuffer = GET_BUFFER(meshData.meshletTrianglesBuffer);

    if (groupThreadID.x < vertexCount)
    {
        uint vertexIndex = meshletVerticesBuffer.Load((meshlet.vertexOffset + groupThreadID.x) * 4);
        ByteAddressBuffer vertices = GET_BUFFER(meshData.vertexBuffer);
        Vertex v = vertices.Load<Vertex>(vertexIndex * sizeof(Vertex));

        ViewData viewData = LoadData<ViewData>(g_PushConstantData.viewBuffer, 0);

        float4 worldPos = mul(instanceData.localToWorld, float4(v.position.xyz, 1.0f));
        float4 viewPos = mul(viewData.viewMatrix, worldPos);

        outVerts[groupThreadID.x].position = mul(viewData.projectionMatrix, viewPos);
        outVerts[groupThreadID.x].worldPos = worldPos.xyz;
        outVerts[groupThreadID.x].normal = normalize(mul((float3x3)instanceData.localToWorld, v.normal));
        outVerts[groupThreadID.x].uv = v.uv;
        outVerts[groupThreadID.x].meshletID = asPayload.meshletIndex;
        outVerts[groupThreadID.x].materialIndex = asPayload.materialIndex;
        outVerts[groupThreadID.x].instanceIndex = asPayload.instanceIndex;
    }

    if (groupThreadID.x < triangleCount)
    {
        uint packedIndices = meshletTrianglesBuffer.Load((meshlet.triangleOffset + groupThreadID.x) * 4);
        outTris[groupThreadID.x] = uint3(packedIndices & 0xFF, (packedIndices >> 8) & 0xFF, (packedIndices >> 16) & 0xFF);
    }
}

float4 PSMain(PSInput input) : SV_TARGET
{
    Payload payload = (Payload)0;

#ifdef GHOST_HAS_ALPHA_CLIP
    float coverage = GetAlphaCoverage(input.materialIndex, input.uv, payload);
    clip(coverage - 0.5f);
#endif

    MaterialContext ctx = (MaterialContext)0;
    ctx.instanceIndex = input.instanceIndex;
    ctx.materialIndex = input.materialIndex;
    ctx.worldPos = input.worldPos;
    ctx.normalWS = input.normal;
    ctx.uv = input.uv;

    SurfaceData surface;
    GetSurfaceData(ctx, payload, surface);

    // Simple forward directional light placeholder
    float3 N = normalize(surface.normalWS);
    float3 L = normalize(float3(0.577f, 0.577f, 0.577f));
    float NdotL = saturate(dot(N, L));

    float3 color = surface.albedo * (NdotL * 0.8f + 0.2f) + surface.emissive;
    return float4(color, 1.0f);
}
