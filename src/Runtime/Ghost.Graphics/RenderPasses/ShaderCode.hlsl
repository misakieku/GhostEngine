#include "F:/csharp/GhostEngine/src/Runtime//Ghost.Graphics/Shaders/Includes/Properties.hlsl"
#include "F:/csharp/GhostEngine/src/Runtime//Ghost.Graphics/Shaders/Includes/Common.hlsl"

struct PixelInput
{
    float4 position : SV_POSITION;
    float4 color : COLOR;
    float4 uv : TEXCOORD0;
};

[numthreads(3, 1, 1)] // 3 threads per triangle
[OUTPUT_TRIANGLE_TOPOLOGY]
void MSMain(
    uint3 groupThreadID : SV_GroupThreadID,
    uint groupID : SV_GroupID,
    out vertices PixelInput outVerts[3],
    out indices uint3 outTris[1])
{
    uint vertexId = groupThreadID.x;

    PerObjectData perObjectData = LoadData<PerObjectData>(g_PushConstantData.perObjectBuffer, 0);
    Vertex v = LoadVertexData(vertexId, groupID.x, perObjectData.vertexBuffer, perObjectData.indexBuffer);

    SetMeshOutputCounts(3, 1);

    // Write vertex output
    outVerts[vertexId].position = v.position;
    outVerts[vertexId].color = v.color;
    outVerts[vertexId].uv = v.uv;

    // Thread 0 defines topology
    if (vertexId == 0)
    {
        outTris[0] = uint3(0, 1, 2);
    }
}

float4 PSMain(PixelInput input) : SV_TARGET
{
    PerMaterialData perMaterialData = LoadData<PerMaterialData>(g_PushConstantData.perMaterialBuffer, 0);

    float4 color1 = SAMPLE_TEXTURE2D(perMaterialData.texture1, perMaterialData.tex_sampler, input.uv.xy);
    float4 color2 = SAMPLE_TEXTURE2D(perMaterialData.texture2, perMaterialData.tex_sampler, input.uv.xy);
    float4 color3 = SAMPLE_TEXTURE2D(perMaterialData.texture3, perMaterialData.tex_sampler, input.uv.xy);
    float4 color4 = SAMPLE_TEXTURE2D(perMaterialData.texture4, perMaterialData.tex_sampler, input.uv.xy);

    float4 blendedColor = (color1 + color2 + color3 + color4) * 0.25f;
    return perMaterialData.color * blendedColor + input.color;
}
