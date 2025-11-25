
#include GENERATED_CODE_PATH
#include "F:/csharp/GhostEngine/Ghost.Shader/BuiltIn/Properties.hlsl"

struct Vertex
{
    float4 position;
    float4 normal;
    float4 tangent;
    float4 color;
    float4 uv;
};

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
#if 0
    // Fetch bindless buffers
    ByteAddressBuffer vertexBuffer = ResourceDescriptorHeap[g_PerObjectData.vertexBuffer];
    ByteAddressBuffer indexBuffer = ResourceDescriptorHeap[g_PerObjectData.indexBuffer];

    // Compute the triangle’s vertex indices
    uint vertexId = groupThreadID.x;
    uint indexOffset = (groupID.x * 3 + vertexId) * 4; // uint32 index
    uint vertexIndex = indexBuffer.Load(indexOffset);

    // Load vertex attributes
    uint vertexOffset = vertexIndex * 80; // 80 bytes per vertex
    Vertex v;
    v.position = asfloat(vertexBuffer.Load4(vertexOffset + 0));
    v.normal = asfloat(vertexBuffer.Load4(vertexOffset + 16));
    v.tangent = asfloat(vertexBuffer.Load4(vertexOffset + 32));
    v.color = asfloat(vertexBuffer.Load4(vertexOffset + 48));
    v.uv = asfloat(vertexBuffer.Load4(vertexOffset + 64));

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
#else
    // 1. Tell the hardware how much data to expect
    SetMeshOutputCounts(3, 1);

    // 2. Hardcoded Clip Space Positions (X, Y, Z, W)
    // Visible range: X[-1, 1], Y[-1, 1], Z[0, 1]
    // W must be 1.0
    float4 positions[3] =
    {
        float4(0.0f, 0.5f, 0.5f, 1.0f), // Top
        float4(0.5f, -0.5f, 0.5f, 1.0f), // Bottom Right
        float4(-0.5f, -0.5f, 0.5f, 1.0f) // Bottom Left
    };

    float4 colors[3] =
    {
        float4(g_PerObjectData.vertexBuffer, 0.0f, 0.0f, 1.0f), // Red
        float4(0.0f, g_PerObjectData.indexBuffer, 0.0f, 1.0f), // Green
        float4(0.0f, 0.0f, 0.0f, 1.0f) // Blue
    };

    uint gtid = groupThreadID.x;
    
    // 3. Write Vertex Data (Parallel)
    outVerts[gtid].position = positions[gtid];
    outVerts[gtid].color = colors[gtid];

    // 4. Write Index Data (Only 1st thread needs to do this)
    if (gtid == 0)
    {
        // Clockwise winding (Standard for DX12)
        outTris[0] = uint3(0, 1, 2);
    }
#endif
}

float4 PSMain(PixelInput input) : SV_TARGET
{
    //float4 color1 = SAMPLE_TEXTURE2D_BINDLESS(g_PerMaterialData.texture1, 0, input.uv.xy);
    //float4 color2 = SAMPLE_TEXTURE2D_BINDLESS(g_PerMaterialData.texture2, 0, input.uv.xy);
    //float4 color3 = SAMPLE_TEXTURE2D_BINDLESS(g_PerMaterialData.texture3, 0, input.uv.xy);
    //float4 color4 = SAMPLE_TEXTURE2D_BINDLESS(g_PerMaterialData.texture4, 0, input.uv.xy);
    
    //float4 blendedColor = (color1 + color2 + color3 + color4) * 0.25f;
    return g_PerMaterialData.color + input.color;;
    //return input.color;
}
