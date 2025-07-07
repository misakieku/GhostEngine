using Ghost.Graphics.Contracts;
using Ghost.Graphics.Data;
using Ghost.Graphics.Shading;
using Ghost.Graphics.Utilities;
using System.Drawing;
using System.Numerics;

namespace Ghost.Graphics.RenderPasses;

internal unsafe class MeshRenderPass : IRenderPass
{
    private const string _HLSL_SOURCE = @"
cbuffer ConstantBuffer : register(b0)
{
    float4 _Color;
};

struct VertexInput
{
    float3 position : POSITION;
    float4 color    : COLOR;
};

struct PixelInput
{
    float4 position : SV_POSITION;
    float4 color    : COLOR;
};

PixelInput VSMain(VertexInput input)
{
    PixelInput output;
    output.position = float4(input.position, 1.0f);
    output.color = input.color;
    return output;
}

float4 PSMain(PixelInput input) : SV_TARGET
{
    return float4(_Color.xyz, 1.0);
}
";

    private Mesh? _mesh;
    private Shader? _shader;
    private Material? _material;

    public void Initialize(ICommandBuffer cmb)
    {
        _mesh = MeshBuilder.CreateCube(0.25f);
        _mesh.UploadMeshData(cmb);

        _shader = new(_HLSL_SOURCE);
        _material = new(_shader);

        var color = new Vector4(Color.Brown.R / 255f, Color.Brown.G / 255f, Color.Brown.B / 255f, 1.0f);
        _material.SetVector("_Color", ref color);
    }

    public void Execute(ICommandBuffer cmb)
    {
        cmb.DrawMesh(_mesh!, _material!);
    }

    public void Dispose()
    {
        _mesh?.Dispose();
        _shader?.Dispose();
        _material?.Dispose();
    }
}
