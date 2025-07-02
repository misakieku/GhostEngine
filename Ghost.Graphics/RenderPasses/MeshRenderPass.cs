using Ghost.Graphics.Contracts;
using Ghost.Graphics.D3D12;
using Ghost.Graphics.D3D12.Utilities;
using Ghost.Graphics.Data;
using Ghost.Graphics.Utilities;
using System.Drawing;
using Win32;
using Win32.Graphics.Direct3D;
using Win32.Graphics.Direct3D12;
using Win32.Graphics.Dxgi.Common;

namespace Ghost.Graphics.RenderPasses;

internal unsafe class MeshRenderPass : IRenderPass
{
    private const string _HLSL_SOURCE = @"
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
    return float4(1.0, 1.0, 1.0, 1.0);
}
";

    private Mesh? _mesh;

    private ComPtr<ID3D12RootSignature> _rootSignature;
    private ComPtr<ID3D12PipelineState> _pipelineState;

    public void Initialize(ICommandBuffer cmb)
    {
        _mesh = MeshBuilder.CreateCube(0.25f, new(Color.AliceBlue));
        _mesh.UploadMeshData(cmb);

        CreateRootSignature();
        CreatePipelineStateObject();
    }

    private void CreateRootSignature()
    {
        var rootSignatureDesc = new RootSignatureDescription(0u, null)
        {
            Flags = RootSignatureFlags.AllowInputAssemblerInputLayout
        };

        using ComPtr<ID3DBlob> signature = default;
        using ComPtr<ID3DBlob> error = default;

        var hr = D3D12SerializeRootSignature(&rootSignatureDesc, RootSignatureVersion.V1_0, signature.GetAddressOf(), error.GetAddressOf());
        if (hr.Failure)
        {
            var errorMessage = System.Text.Encoding.ASCII.GetString((byte*)error.Get()->GetBufferPointer(), (int)error.Get()->GetBufferSize());
            throw new InvalidOperationException($"Failed to serialize root signature: {errorMessage}");
        }

        GraphicsPipeline.GetGraphicsDevice<D3D12GraphicsDevice>().NativeDevice.Ptr->CreateRootSignature(0, signature.Get()->GetBufferPointer(), signature.Get()->GetBufferSize(), __uuidof<ID3D12RootSignature>(), _rootSignature.GetVoidAddressOf());
    }

    private void CreatePipelineStateObject()
    {
        try
        {
            var vertexShaderBytecode = Shader.CompileShader(_HLSL_SOURCE, "VSMain", "vs_5_0");
            var pixelShaderBytecode = Shader.CompileShader(_HLSL_SOURCE, "PSMain", "ps_5_0");

            fixed (byte* vsPtr = vertexShaderBytecode)
            fixed (byte* psPtr = pixelShaderBytecode)
            {
                var psoDesc = new GraphicsPipelineStateDescription
                {
                    pRootSignature = _rootSignature.Get(),
                    VS = new ShaderBytecode(vsPtr, (nuint)vertexShaderBytecode.Length),
                    PS = new ShaderBytecode(psPtr, (nuint)pixelShaderBytecode.Length),
                    InputLayout = D3D12PipelineResource.InputLayoutDescription,
                    RasterizerState = RasterizerDescription.CullNone,
                    BlendState = BlendDescription.Opaque,
                    DepthStencilState = DepthStencilDescription.Default,
                    SampleMask = uint.MaxValue,
                    PrimitiveTopologyType = PrimitiveTopologyType.Triangle,
                    NumRenderTargets = 1,
                    SampleDesc = new SampleDescription(1, 0),
                    DSVFormat = Format.Unknown,
                };

                psoDesc.RTVFormats[0] = D3D12PipelineResource.SWAP_CHAIN_BACK_BUFFER_FORMAT;

                // Create the PSO
                GraphicsPipeline.GetGraphicsDevice<D3D12GraphicsDevice>().NativeDevice.Ptr->CreateGraphicsPipelineState(&psoDesc, __uuidof<ID3D12PipelineState>(), _pipelineState.GetVoidAddressOf());
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    public void Execute(ICommandBuffer cmb)
    {
        var dx12Cmb = (D3D12CommandBuffer)cmb;
        dx12Cmb.CommandList.Ptr->SetGraphicsRootSignature(_rootSignature.Get());
        dx12Cmb.CommandList.Ptr->SetPipelineState(_pipelineState.Get());

        cmb.DrawMesh(_mesh!);
    }

    public void Dispose()
    {
        _mesh?.Dispose();
        _rootSignature.Dispose();
        _pipelineState.Dispose();
    }
}
