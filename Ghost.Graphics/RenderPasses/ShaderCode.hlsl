cbuffer ConstantBuffer : register(b0)
{
    float4 _Color;
    uint _TextureIndex1;
    uint _TextureIndex2;
    uint _TextureIndex3;
    uint _TextureIndex4;
    uint _VertexBufferIndex;
    uint _IndexBufferIndex;
};

// SM 6.6 approach - direct access to global descriptor heap
SamplerState _MainSampler : register(s0);

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

// Bindless vertex shader that fetches vertex data from bindless buffers
PixelInput VSMain(uint vertexId : SV_VertexID, uint instanceId : SV_InstanceID)
{
    // Get bindless buffers
    ByteAddressBuffer vertexBuffer = ResourceDescriptorHeap[_VertexBufferIndex];
    ByteAddressBuffer indexBuffer = ResourceDescriptorHeap[_IndexBufferIndex];
    
    // For fully bindless rendering, we use instanced drawing where:
    // - Each instance represents a triangle (instanceId = triangle index)
    // - vertexId goes from 0 to 2 (the 3 vertices of the triangle)
    
    // Calculate the index into the index buffer
    uint indexOffset = (instanceId * 3 + vertexId) * 4; // 4 bytes per index (uint32)
    uint vertexIndex = indexBuffer.Load(indexOffset);
    
    // Calculate the offset into the vertex buffer
    uint vertexOffset = vertexIndex * 80; // 80 bytes per vertex (5 * float4)
    
    // Load vertex data from bindless vertex buffer
    Vertex vertex;
    vertex.position = asfloat(vertexBuffer.Load4(vertexOffset + 0));
    vertex.normal = asfloat(vertexBuffer.Load4(vertexOffset + 16));
    vertex.tangent = asfloat(vertexBuffer.Load4(vertexOffset + 32));
    vertex.color = asfloat(vertexBuffer.Load4(vertexOffset + 48));
    vertex.uv = asfloat(vertexBuffer.Load4(vertexOffset + 64));
    
    // Output transformed vertex
    PixelInput output;
    output.position = vertex.position;
    output.color = vertex.color;
    output.uv = vertex.uv;
    return output;
}

float4 PSMain(PixelInput input) : SV_TARGET
{
    // SM 6.6 Modern Bindless Approach:
    // ResourceDescriptorHeap[index] directly accesses any texture in the heap
    Texture2D tex1 = ResourceDescriptorHeap[_TextureIndex1];
    Texture2D tex2 = ResourceDescriptorHeap[_TextureIndex2];
    Texture2D tex3 = ResourceDescriptorHeap[_TextureIndex3];
    Texture2D tex4 = ResourceDescriptorHeap[_TextureIndex4];
    
    // Sample the textures
    float4 color1 = tex1.Sample(_MainSampler, input.uv.xy);
    float4 color2 = tex2.Sample(_MainSampler, input.uv.xy);
    float4 color3 = tex3.Sample(_MainSampler, input.uv.xy);
    float4 color4 = tex4.Sample(_MainSampler, input.uv.xy);
    
    // Blend all textures together (simple average)
    float4 blendedColor = (color1 + color2 + color3 + color4) * 0.25f;
    
    return blendedColor * _Color;
}