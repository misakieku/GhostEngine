using System.Runtime.CompilerServices;

namespace Ghost.D3D12;

public unsafe partial struct D3DX12_MESH_SHADER_PIPELINE_STATE_DESC
{
    public ID3D12RootSignature* pRootSignature;
    public D3D12_SHADER_BYTECODE AS;
    public D3D12_SHADER_BYTECODE MS;
    public D3D12_SHADER_BYTECODE PS;
    public D3D12_BLEND_DESC BlendState;
    public uint SampleMask;
    public D3D12_RASTERIZER_DESC RasterizerState;
    public D3D12_DEPTH_STENCIL_DESC DepthStencilState;
    public D3D12_PRIMITIVE_TOPOLOGY_TYPE PrimitiveTopologyType;
    public uint NumRenderTargets;
    public _RTVFormats_e__FixedBuffer RTVFormats;
    public DXGI_FORMAT DSVFormat;
    public DXGI_SAMPLE_DESC SampleDesc;
    public uint NodeMask;
    public D3D12_CACHED_PIPELINE_STATE CachedPSO;
    public D3D12_PIPELINE_STATE_FLAGS Flags;

    [InlineArray(8)]
    public partial struct _RTVFormats_e__FixedBuffer
    {
        public DXGI_FORMAT e0;
    }
}
