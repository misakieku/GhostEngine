#include "F:/csharp/GhostEngine/src/Runtime//Ghost.Graphics/Shaders/Includes/Properties.hlsl"
#include "F:/csharp/GhostEngine/src/Runtime//Ghost.Graphics/Shaders/Includes/Common.hlsl"

struct PixelInput
{
    float4 position : SV_POSITION;
    float4 color : COLOR;
    float4 uv : TEXCOORD0;
};

[numthreads(128, 1, 1)] // 128 threads to cover max 64 vertices and 124 triangles
[outputtopology("triangle")]
void MSMain(
    uint3 groupThreadID : SV_GroupThreadID,
    uint groupID : SV_GroupID,
    out vertices PixelInput outVerts[64],
    out indices uint3 outTris[124])
{
    PerObjectData perObjectData = LoadData<PerObjectData>(g_PushConstantData.perObjectBuffer, 0);

    ByteAddressBuffer meshletBuffer = GET_BUFFER(perObjectData.meshletBuffer);
    Meshlet m = meshletBuffer.Load<Meshlet>(groupID.x * sizeof(Meshlet));

    uint vertexCount = m.packedCounts & 0xFF;
    uint triangleCount = (m.packedCounts >> 8) & 0xFF;

    SetMeshOutputCounts(vertexCount, triangleCount);

    ByteAddressBuffer meshletVerticesBuffer = GET_BUFFER(perObjectData.meshletVerticesBuffer);
    ByteAddressBuffer meshletTrianglesBuffer = GET_BUFFER(perObjectData.meshletTrianglesBuffer);

    // Write vertex output
    if (groupThreadID.x < vertexCount)
    {
        uint vertexIndex = meshletVerticesBuffer.Load((m.vertexOffset + groupThreadID.x) * 4);
        ByteAddressBuffer vertices = GET_BUFFER(perObjectData.vertexBuffer);
        Vertex v = vertices.Load<Vertex>(vertexIndex * sizeof(Vertex));

        // Basic MVP transform not needed if already in world space, but usually we need localToWorld and ViewProj
        PerViewData perViewData = LoadData<PerViewData>(g_PushConstantData.perViewBuffer, 0);
        PerInstanceData perInstanceData = LoadData<PerInstanceData>(g_PushConstantData.perInstanceBuffer, 0);

        float4 worldPos = mul(perInstanceData.localToWorld, float4(v.position.xyz, 1.0f));
        outVerts[groupThreadID.x].position = mul(perViewData.viewMatrix, worldPos);
        outVerts[groupThreadID.x].position = mul(perViewData.projectionMatrix, outVerts[groupThreadID.x].position);

        outVerts[groupThreadID.x].color = v.color;
        outVerts[groupThreadID.x].uv = v.uv;
    }

    // Write triangle output (1 thread processes 1 triangle)
    if (groupThreadID.x < triangleCount)
    {
        uint triangleIndex = groupThreadID.x;

        // Load the packed 32-bit integer containing the 3 local indices
        uint packedIndices = meshletTrianglesBuffer.Load((m.triangleOffset + triangleIndex) * 4);

        uint i0 = packedIndices & 0xFF;
        uint i1 = (packedIndices >> 8) & 0xFF;
        uint i2 = (packedIndices >> 16) & 0xFF;

        outTris[triangleIndex] = uint3(i0, i1, i2);
    }
}

float4 PSMain(PixelInput input) : SV_TARGET
{
    // PerMaterialData perMaterialData = LoadData<PerMaterialData>(g_PushConstantData.perMaterialBuffer, 0);
    //
    // float4 color1 = SAMPLE_TEXTURE2D(perMaterialData.texture1, perMaterialData.tex_sampler, input.uv.xy);
    // float4 color2 = SAMPLE_TEXTURE2D(perMaterialData.texture2, perMaterialData.tex_sampler, input.uv.xy);
    // float4 color3 = SAMPLE_TEXTURE2D(perMaterialData.texture3, perMaterialData.tex_sampler, input.uv.xy);
    // float4 color4 = SAMPLE_TEXTURE2D(perMaterialData.texture4, perMaterialData.tex_sampler, input.uv.xy);
    //
    // float4 blendedColor = (color1 + color2 + color3 + color4) * 0.25f;
    // return perMaterialData.color * blendedColor + input.color;
}
