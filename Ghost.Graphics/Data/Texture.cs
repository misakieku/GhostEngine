using Ghost.Graphics.D3D12;
using Win32.Graphics.Direct3D12;
using Win32.Graphics.Dxgi.Common;

namespace Ghost.Graphics.Data;

/// <summary>
/// Base class for all texture types in the graphics pipeline.
/// Provides common functionality for texture dimensions, format, and GPU resource management.
/// </summary>
public abstract unsafe class Texture : GraphicsResource
{
    private readonly BindlessDescriptor _bindlessDescriptor;

    /// <summary>
    /// Width of the texture in pixels.
    /// </summary>
    public uint Width
    {
        get;
    }

    /// <summary>
    /// Height of the texture in pixels.
    /// </summary>
    public uint Height
    {
        get;
    }

    /// <summary>
    /// Number of bytes per pixel for the texture format.
    /// </summary>
    public uint BytesPerPixel
    {
        get;
    }

    /// <summary>
    /// Format of the texture.
    /// </summary>
    public Format Format
    {
        get;
    }

    /// <summary>
    /// The index of this texture in the global bindless descriptor heap.
    /// </summary>
    public uint DescriptorIndex => _bindlessDescriptor.Index;

    internal Texture(uint width, uint height, Format format, in TextureHandle handle, BindlessDescriptor bindlessDescriptor)
        : base(handle.ResourceHandle)
    {
        Width = width;
        Height = height;
        BytesPerPixel = GetBytesPerPixel(format);
        Format = format;
        _bindlessDescriptor = bindlessDescriptor;
    }

    /// <summary>
    /// Creates a bindless shader resource view descriptor for the texture.
    /// </summary>
    protected static BindlessDescriptor CreateBindlessShaderResourceView(ID3D12Resource* resource, Format format)
    {
        var device = GraphicsPipeline.GraphicsDevice.NativeDevice.Ptr;
        var bindlessDescriptor = GraphicsPipeline.DescriptorAllocator.AllocateBindless();

        var srvDesc = new ShaderResourceViewDescription
        {
            Format = format,
            ViewDimension = SrvDimension.Texture2D,
            Texture2D = new Texture2DSrv { MipLevels = 1 },
            Shader4ComponentMapping = 0x1688 // D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING
        };

        device->CreateShaderResourceView(resource, &srvDesc, bindlessDescriptor.CpuHandle);
        return bindlessDescriptor;
    }

    /// <summary>
    /// Gets the bytes per pixel for the specified format.
    /// </summary>
    private static uint GetBytesPerPixel(Format format)
    {
        return format switch
        {
            Format.R8G8B8A8Unorm => 4,
            Format.R8G8B8A8UnormSrgb => 4,
            Format.B8G8R8A8Unorm => 4,
            Format.B8G8R8A8UnormSrgb => 4,
            Format.R8G8B8A8Uint => 4,
            Format.R8G8B8A8Sint => 4,
            Format.R8G8B8A8Snorm => 4,
            Format.R8G8Unorm => 2,
            Format.R8G8Uint => 2,
            Format.R8G8Sint => 2,
            Format.R8G8Snorm => 2,
            Format.R8Unorm => 1,
            Format.R8Uint => 1,
            Format.R8Sint => 1,
            Format.R8Snorm => 1,
            Format.A8Unorm => 1,
            Format.R16G16B16A16Float => 8,
            Format.R16G16B16A16Unorm => 8,
            Format.R16G16B16A16Uint => 8,
            Format.R16G16B16A16Sint => 8,
            Format.R16G16B16A16Snorm => 8,
            Format.R32G32B32A32Float => 16,
            Format.R32G32B32A32Uint => 16,
            Format.R32G32B32A32Sint => 16,
            Format.R32G32B32Float => 12,
            Format.R32G32B32Uint => 12,
            Format.R32G32B32Sint => 12,
            Format.R32G32Float => 8,
            Format.R32G32Uint => 8,
            Format.R32G32Sint => 8,
            Format.R32Float => 4,
            Format.R32Uint => 4,
            Format.R32Sint => 4,
            Format.R16G16Float => 4,
            Format.R16G16Unorm => 4,
            Format.R16G16Uint => 4,
            Format.R16G16Sint => 4,
            Format.R16G16Snorm => 4,
            Format.R16Float => 2,
            Format.R16Unorm => 2,
            Format.R16Uint => 2,
            Format.R16Sint => 2,
            Format.R16Snorm => 2,
            Format.D32Float => 4,
            Format.D24UnormS8Uint => 4,
            Format.D16Unorm => 2,
            _ => throw new NotSupportedException($"Format {format} is not supported.")
        };
    }

    /// <summary>
    /// Disposes the texture resources.
    /// </summary>
    public override void Dispose()
    {
        base.Dispose();
        GraphicsPipeline.DescriptorAllocator.ReleaseBindless(_bindlessDescriptor);
    }
}