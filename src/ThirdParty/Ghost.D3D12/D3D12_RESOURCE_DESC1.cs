using static Ghost.D3D12.D3D12_RESOURCE_DIMENSION;
using static Ghost.D3D12.D3D12_RESOURCE_FLAGS;
using static Ghost.D3D12.D3D12_TEXTURE_LAYOUT;
using static Ghost.DXGI.DXGI_FORMAT;

namespace Ghost.D3D12;

public partial struct D3D12_RESOURCE_DESC1
{
    public static D3D12_RESOURCE_DESC1 Buffer(ulong width, D3D12_RESOURCE_FLAGS flags = D3D12_RESOURCE_FLAG_NONE, ulong alignment = 0)
    {
        return new D3D12_RESOURCE_DESC1
        {
            Dimension = D3D12_RESOURCE_DIMENSION_BUFFER,
            Alignment = alignment,
            Width = width,
            Height = 1,
            DepthOrArraySize = 1,
            MipLevels = 1,
            Format = DXGI_FORMAT_UNKNOWN,
            SampleDesc = new DXGI_SAMPLE_DESC(1, 0),
            Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR,
            Flags = flags,
            SamplerFeedbackMipRegion = default
        };
    }

    public static D3D12_RESOURCE_DESC1 Tex2D(DXGI_FORMAT format, ulong width, uint height, ushort arraySize = 1, ushort mipLevels = 0, uint sampleCount = 1, uint sampleQuality = 0, D3D12_RESOURCE_FLAGS flags = D3D12_RESOURCE_FLAG_NONE, D3D12_TEXTURE_LAYOUT layout = D3D12_TEXTURE_LAYOUT_UNKNOWN, ulong alignment = 0)
    {
        return new D3D12_RESOURCE_DESC1
        {
            Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE2D,
            Alignment = alignment,
            Width = width,
            Height = height,
            DepthOrArraySize = arraySize,
            MipLevels = mipLevels,
            Format = format,
            SampleDesc = new DXGI_SAMPLE_DESC(sampleCount, sampleQuality),
            Layout = layout,
            Flags = flags,
            SamplerFeedbackMipRegion = default
        };
    }

    public static D3D12_RESOURCE_DESC1 Tex3D(DXGI_FORMAT format, ulong width, uint height, ushort depth, ushort mipLevels = 0, D3D12_RESOURCE_FLAGS flags = D3D12_RESOURCE_FLAG_NONE, D3D12_TEXTURE_LAYOUT layout = D3D12_TEXTURE_LAYOUT_UNKNOWN, ulong alignment = 0)
    {
        return new D3D12_RESOURCE_DESC1
        {
            Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE3D,
            Alignment = alignment,
            Width = width,
            Height = height,
            DepthOrArraySize = depth,
            MipLevels = mipLevels,
            Format = format,
            SampleDesc = new DXGI_SAMPLE_DESC(1, 0),
            Layout = layout,
            Flags = flags,
            SamplerFeedbackMipRegion = default
        };
    }

    public readonly ushort ArraySize() => Dimension != D3D12_RESOURCE_DIMENSION_TEXTURE3D ? DepthOrArraySize : (ushort)1;

    public readonly ushort Depth() => Dimension == D3D12_RESOURCE_DIMENSION_TEXTURE3D ? DepthOrArraySize : (ushort)1;
}
