using Ghost.Graphics.Contracts;
using Ghost.Graphics.D3D12;
using Ghost.Graphics.Data;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Utilities;
using System.Numerics;

namespace Ghost.Graphics.RenderPasses;

/// <summary>
/// Simplified bindless mesh render pass using high-level bindless APIs with fully bindless vertex/index buffer access
/// </summary>
internal unsafe class MeshRenderPass : IRenderPass
{
    private const string _HLSL_SOURCE = @"
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
    float4 color    : COLOR;
    float4 uv       : TEXCOORD0;
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
";

    // High-level bindless objects
    private MeshClass? _mesh;
    private Shader? _shader;
    private MaterialClass? _material;
    private Texture2D[]? _textures;

    // Texture file paths for this demo
    private readonly string[] _textureFiles = [
        "C:/Users/Misaki/Downloads/Im/Icon.png",
        "C:/Users/Misaki/Downloads/Im/Backdrop.jpg",
        "C:/Users/Misaki/Downloads/Im/101167591_p0.png",
        "C:/Users/Misaki/Downloads/Im/yande.re 1134666 blue_archive nakamasa_ichika sugarhigh.jpg"
    ];

    public void Initialize(ICommandBuffer cmd)
    {
        _mesh = MeshBuilder.CreateCube(0.75f);
        _mesh.UploadMeshData();

        _shader = new ShaderData(_HLSL_SOURCE);
        _material = new Material(_shader);

        _textures = new Texture2D[_textureFiles.Length];
        for (var i = 0; i < _textureFiles.Length; i++)
        {
            _textures[i] = Texture2D.FromFile(_textureFiles[i]);
            _textures[i].UploadTextureData();
        }

        _material.SetVector("_Color", new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
        for (var i = 0; i < _textures.Length; i++)
        {
            var texture = _textures[i];
            _material.SetTexture($"_TextureIndex{i + 1}", texture);
        }

        _material.SetMeshBuffer(_mesh);
        _material.UploadMaterialData();
    }

    public void Execute(ICommandBuffer cmd)
    {
        cmd.DrawMesh(_mesh!, _material!);
    }

    public void Dispose()
    {
        _mesh?.Dispose();
        _shader?.Dispose();
        _material?.Dispose();

        if (_textures != null)
        {
            foreach (var texture in _textures)
            {
                texture?.Dispose();
            }
        }
    }
}