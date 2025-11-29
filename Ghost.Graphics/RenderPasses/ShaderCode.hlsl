
#include GENERATED_CODE_PATH
#include "F:/csharp/GhostEngine/Ghost.Shader/BuiltIn/Properties.hlsl"
#include "F:/csharp/GhostEngine/Ghost.Shader/BuiltIn/Common.hlsl"

struct PixelInput
{
    float4 position : SV_POSITION;
    float4 color : COLOR;
    float4 uv : TEXCOORD0;
};

[NumThreads(3, 1, 1)] // 3 threads per triangle
[OutputTopology("triangle")]
void MSMain(
    uint3 groupThreadID : SV_GroupThreadID,
    uint groupID : SV_GroupID,
    out vertices PixelInput outVerts[3],
    out indices uint3 outTris[1])
{
    uint vertexId = groupThreadID.x;
    Vertex v = LoadVertexData(vertexId, groupID.x, g_PerObjectData.vertexBuffer, g_PerObjectData.indexBuffer);

    SetMeshOutputCounts(3, 1);
    //v.position = mul(g_PerViewData.cameraMatrix, mul(g_PerObjectData.localToWorld, v.position));
    
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
    float4 color1 = SAMPLE_TEXTURE2D(g_PerMaterialData.texture1, g_PerMaterialData.tex_sampler, input.uv.xy);
    float4 color2 = SAMPLE_TEXTURE2D(g_PerMaterialData.texture2, g_PerMaterialData.tex_sampler, input.uv.xy);
    float4 color3 = SAMPLE_TEXTURE2D(g_PerMaterialData.texture3, g_PerMaterialData.tex_sampler, input.uv.xy);
    float4 color4 = SAMPLE_TEXTURE2D(g_PerMaterialData.texture4, g_PerMaterialData.tex_sampler, input.uv.xy);
    
    float4 blendedColor = (color1 + color2 + color3 + color4) * 0.25f;
    return g_PerMaterialData.color * blendedColor;
    //return input.color;
}
