using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12_PIPELINE_STATE_SUBOBJECT_TYPE;

namespace Ghost.D3D12;

[StructLayout(LayoutKind.Explicit, Size = 8)]
public unsafe partial struct CD3DX12_PIPELINE_STATE_STREAM_FLAGS
{
    [FieldOffset(0)]
    private nuint _align;

    [FieldOffset(0)]
    public D3D12_PIPELINE_STATE_SUBOBJECT_TYPE pssType;

    [FieldOffset(4)]
    public D3D12_PIPELINE_STATE_FLAGS pssInner;

    public CD3DX12_PIPELINE_STATE_STREAM_FLAGS(D3D12_PIPELINE_STATE_FLAGS value)
    {
        _align = 0;
        pssType = D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_FLAGS;
        pssInner = value;
    }
}

[StructLayout(LayoutKind.Explicit, Size = 8)]
public unsafe partial struct CD3DX12_PIPELINE_STATE_STREAM_NODE_MASK
{
    [FieldOffset(0)]
    private nuint _align;

    [FieldOffset(0)]
    public D3D12_PIPELINE_STATE_SUBOBJECT_TYPE pssType;

    [FieldOffset(4)]
    public uint pssInner;

    public CD3DX12_PIPELINE_STATE_STREAM_NODE_MASK(uint value)
    {
        _align = 0;
        pssType = D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_NODE_MASK;
        pssInner = value;
    }
}

[StructLayout(LayoutKind.Explicit, Size = 16)]
public unsafe partial struct CD3DX12_PIPELINE_STATE_STREAM_ROOT_SIGNATURE
{
    [FieldOffset(0)]
    public D3D12_PIPELINE_STATE_SUBOBJECT_TYPE pssType;

    [FieldOffset(8)]
    public ID3D12RootSignature* pssInner;

    public CD3DX12_PIPELINE_STATE_STREAM_ROOT_SIGNATURE(ID3D12RootSignature* value)
    {
        pssType = D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_ROOT_SIGNATURE;
        pssInner = value;
    }
}

[StructLayout(LayoutKind.Explicit, Size = 8)]
public unsafe partial struct CD3DX12_PIPELINE_STATE_STREAM_PRIMITIVE_TOPOLOGY
{
    [FieldOffset(0)]
    private nuint _align;

    [FieldOffset(0)]
    public D3D12_PIPELINE_STATE_SUBOBJECT_TYPE pssType;

    [FieldOffset(4)]
    public D3D12_PRIMITIVE_TOPOLOGY_TYPE pssInner;

    public CD3DX12_PIPELINE_STATE_STREAM_PRIMITIVE_TOPOLOGY(D3D12_PRIMITIVE_TOPOLOGY_TYPE value)
    {
        _align = 0;
        pssType = D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_PRIMITIVE_TOPOLOGY;
        pssInner = value;
    }
}

[StructLayout(LayoutKind.Explicit, Size = 24)]
public unsafe partial struct CD3DX12_PIPELINE_STATE_STREAM_PS
{
    [FieldOffset(0)]
    public D3D12_PIPELINE_STATE_SUBOBJECT_TYPE pssType;

    [FieldOffset(8)]
    public D3D12_SHADER_BYTECODE pssInner;

    public CD3DX12_PIPELINE_STATE_STREAM_PS(in D3D12_SHADER_BYTECODE value)
    {
        pssType = D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_PS;
        pssInner = value;
    }
}

[StructLayout(LayoutKind.Explicit, Size = 24)]
public unsafe partial struct CD3DX12_PIPELINE_STATE_STREAM_AS
{
    [FieldOffset(0)]
    public D3D12_PIPELINE_STATE_SUBOBJECT_TYPE pssType;

    [FieldOffset(8)]
    public D3D12_SHADER_BYTECODE pssInner;

    public CD3DX12_PIPELINE_STATE_STREAM_AS(in D3D12_SHADER_BYTECODE value)
    {
        pssType = D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_AS;
        pssInner = value;
    }
}

[StructLayout(LayoutKind.Explicit, Size = 24)]
public unsafe partial struct CD3DX12_PIPELINE_STATE_STREAM_MS
{
    [FieldOffset(0)]
    public D3D12_PIPELINE_STATE_SUBOBJECT_TYPE pssType;

    [FieldOffset(8)]
    public D3D12_SHADER_BYTECODE pssInner;

    public CD3DX12_PIPELINE_STATE_STREAM_MS(in D3D12_SHADER_BYTECODE value)
    {
        pssType = D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_MS;
        pssInner = value;
    }
}

[StructLayout(LayoutKind.Explicit, Size = 24)]
public unsafe partial struct CD3DX12_PIPELINE_STATE_STREAM_CS
{
    [FieldOffset(0)]
    public D3D12_PIPELINE_STATE_SUBOBJECT_TYPE pssType;

    [FieldOffset(8)]
    public D3D12_SHADER_BYTECODE pssInner;

    public CD3DX12_PIPELINE_STATE_STREAM_CS(in D3D12_SHADER_BYTECODE value)
    {
        pssType = D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_CS;
        pssInner = value;
    }
}

[StructLayout(LayoutKind.Explicit, Size = 336)]
public unsafe partial struct CD3DX12_PIPELINE_STATE_STREAM_BLEND_DESC
{
    [FieldOffset(0)]
    private nuint _align;

    [FieldOffset(0)]
    public D3D12_PIPELINE_STATE_SUBOBJECT_TYPE pssType;

    [FieldOffset(4)]
    public D3D12_BLEND_DESC pssInner;

    public CD3DX12_PIPELINE_STATE_STREAM_BLEND_DESC(in D3D12_BLEND_DESC value)
    {
        _align = 0;
        pssType = D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_BLEND;
        pssInner = value;
    }
}

[StructLayout(LayoutKind.Explicit, Size = 64)]
public unsafe partial struct CD3DX12_PIPELINE_STATE_STREAM_DEPTH_STENCIL1
{
    [FieldOffset(0)]
    private nuint _align;

    [FieldOffset(0)]
    public D3D12_PIPELINE_STATE_SUBOBJECT_TYPE pssType;

    [FieldOffset(4)]
    public D3D12_DEPTH_STENCIL_DESC1 pssInner;

    public CD3DX12_PIPELINE_STATE_STREAM_DEPTH_STENCIL1(in D3D12_DEPTH_STENCIL_DESC desc)
    {
        _align = 0;
        pssType = D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_DEPTH_STENCIL1;
        pssInner = new D3D12_DEPTH_STENCIL_DESC1
        {
            DepthEnable = desc.DepthEnable,
            DepthWriteMask = desc.DepthWriteMask,
            DepthFunc = desc.DepthFunc,
            StencilEnable = desc.StencilEnable,
            StencilReadMask = desc.StencilReadMask,
            StencilWriteMask = desc.StencilWriteMask,
            FrontFace = desc.FrontFace,
            BackFace = desc.BackFace,
            DepthBoundsTestEnable = 0
        };
    }
}

[StructLayout(LayoutKind.Explicit, Size = 8)]
public unsafe partial struct CD3DX12_PIPELINE_STATE_STREAM_DEPTH_STENCIL_FORMAT
{
    [FieldOffset(0)]
    private nuint _align;

    [FieldOffset(0)]
    public D3D12_PIPELINE_STATE_SUBOBJECT_TYPE pssType;

    [FieldOffset(4)]
    public DXGI_FORMAT pssInner;

    public CD3DX12_PIPELINE_STATE_STREAM_DEPTH_STENCIL_FORMAT(DXGI_FORMAT value)
    {
        _align = 0;
        pssType = D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_DEPTH_STENCIL_FORMAT;
        pssInner = value;
    }
}

[StructLayout(LayoutKind.Explicit, Size = 48)]
public unsafe partial struct CD3DX12_PIPELINE_STATE_STREAM_RASTERIZER
{
    [FieldOffset(0)]
    private nuint _align;

    [FieldOffset(0)]
    public D3D12_PIPELINE_STATE_SUBOBJECT_TYPE pssType;

    [FieldOffset(4)]
    public D3D12_RASTERIZER_DESC pssInner;

    public CD3DX12_PIPELINE_STATE_STREAM_RASTERIZER(in D3D12_RASTERIZER_DESC value)
    {
        _align = 0;
        pssType = D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_RASTERIZER;
        pssInner = value;
    }
}

[StructLayout(LayoutKind.Explicit, Size = 40)]
public unsafe partial struct CD3DX12_PIPELINE_STATE_STREAM_RENDER_TARGET_FORMATS
{
    [FieldOffset(0)]
    private nuint _align;

    [FieldOffset(0)]
    public D3D12_PIPELINE_STATE_SUBOBJECT_TYPE pssType;

    [FieldOffset(4)]
    public D3D12_RT_FORMAT_ARRAY pssInner;

    public CD3DX12_PIPELINE_STATE_STREAM_RENDER_TARGET_FORMATS(in D3DX12_MESH_SHADER_PIPELINE_STATE_DESC._RTVFormats_e__FixedBuffer formats, uint count)
    {
        _align = 0;
        pssType = D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_RENDER_TARGET_FORMATS;
        pssInner = new D3D12_RT_FORMAT_ARRAY
        {
            NumRenderTargets = count
        };

        for (var i = 0; i < 8; i++)
        {
            pssInner.RTFormats[i] = formats[i];
        }
    }
}

[StructLayout(LayoutKind.Explicit, Size = 16)]
public unsafe partial struct CD3DX12_PIPELINE_STATE_STREAM_SAMPLE_DESC
{
    [FieldOffset(0)]
    private nuint _align;

    [FieldOffset(0)]
    public D3D12_PIPELINE_STATE_SUBOBJECT_TYPE pssType;

    [FieldOffset(4)]
    public DXGI_SAMPLE_DESC pssInner;

    public CD3DX12_PIPELINE_STATE_STREAM_SAMPLE_DESC(DXGI_SAMPLE_DESC value)
    {
        _align = 0;
        pssType = D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_SAMPLE_DESC;
        pssInner = value;
    }
}

[StructLayout(LayoutKind.Explicit, Size = 8)]
public unsafe partial struct CD3DX12_PIPELINE_STATE_STREAM_SAMPLE_MASK
{
    [FieldOffset(0)]
    private nuint _align;

    [FieldOffset(0)]
    public D3D12_PIPELINE_STATE_SUBOBJECT_TYPE pssType;

    [FieldOffset(4)]
    public uint pssInner;

    public CD3DX12_PIPELINE_STATE_STREAM_SAMPLE_MASK(uint value)
    {
        _align = 0;
        pssType = D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_SAMPLE_MASK;
        pssInner = value;
    }
}

[StructLayout(LayoutKind.Explicit, Size = 24)]
public unsafe partial struct CD3DX12_PIPELINE_STATE_STREAM_CACHED_PSO
{
    [FieldOffset(0)]
    public D3D12_PIPELINE_STATE_SUBOBJECT_TYPE pssType;

    [FieldOffset(8)]
    public D3D12_CACHED_PIPELINE_STATE pssInner;

    public CD3DX12_PIPELINE_STATE_STREAM_CACHED_PSO(in D3D12_CACHED_PIPELINE_STATE value)
    {
        pssType = D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_CACHED_PSO;
        pssInner = value;
    }
}

[StructLayout(LayoutKind.Explicit, Size = 32)]
public unsafe partial struct CD3DX12_PIPELINE_STATE_STREAM_VIEW_INSTANCING
{
    [FieldOffset(0)]
    public D3D12_PIPELINE_STATE_SUBOBJECT_TYPE pssType;

    [FieldOffset(8)]
    public D3D12_VIEW_INSTANCING_DESC pssInner;

    public static CD3DX12_PIPELINE_STATE_STREAM_VIEW_INSTANCING Default => new()
    {
        pssType = D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_VIEW_INSTANCING,
        pssInner = new D3D12_VIEW_INSTANCING_DESC
        {
            ViewInstanceCount = 0,
            pViewInstanceLocations = null,
            Flags = D3D12_VIEW_INSTANCING_FLAGS.D3D12_VIEW_INSTANCING_FLAG_NONE
        }
    };
}
