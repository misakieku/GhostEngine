using Ghost.Graphics.D3D12;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Helpers;
using System.Runtime.InteropServices;
using Win32;
using Win32.Graphics.Direct3D12;
using Win32.Graphics.Dxgi.Common;

namespace Ghost.Graphics.Data;

public unsafe class Texture2D : IDisposable
{
    private UnsafeArray<byte> _cpuData;

    private readonly GraphicsResource _resource;
    private readonly ShaderResourceDescriptor _srvDescriptor;

    private bool _disposed;

    public uint Width
    {
        get;
    }

    public uint Height
    {
        get;
    }

    public Format Format
    {
        get;
    }

    public uint BytesPerPixel
    {
        get;
    }

    public uint Pitch
    {
        get;
    }

    public uint DataSize
    {
        get;
    }

    internal ShaderResourceDescriptor SRVDescriptor => _srvDescriptor;

    public GraphicsResource? Resource => _resource;
    public ReadOnlySpan<byte> CpuData => _cpuData.AsSpan();

    public Texture2D(uint width, uint height, Span<byte> data, Format format)
    {
        Width = width;
        Height = height;
        Format = format;
        BytesPerPixel = GetBytesPerPixel(format);
        Pitch = width * BytesPerPixel;
        DataSize = Pitch * height;

        // Initialize CPU-side data
        _cpuData = new((int)DataSize, Allocator.Persistent);

        if (!data.IsEmpty)
        {
            if (data.Length != DataSize)
            {
                throw new ArgumentException($"Data size mismatch. Expected {DataSize} bytes, got {data.Length} bytes.");
            }

            data.CopyTo(_cpuData.AsSpan());
        }

        _resource = CreateGpuResource();
        _srvDescriptor = CreateShaderResourceView();
    }
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
            _ => throw new NotSupportedException($"Format {format} is not supported.")
        };
    }

    private GraphicsResource CreateGpuResource()
    {
        var heapProperties = new HeapProperties(HeapType.Default);
        var resourceDesc = ResourceDescription.Tex2D(Format, Width, Height);

        ComPtr<ID3D12Resource> textureResource = default;
        GraphicsPipeline.GraphicsDevice.NativeDevice.Ptr->CreateCommittedResource(
            &heapProperties,
            HeapFlags.None,
            &resourceDesc,
            ResourceStates.Common,
            null,
            __uuidof<ID3D12Resource>(),
            textureResource.GetVoidAddressOf()
        );

        return new(textureResource.Move());
    }

    private ShaderResourceDescriptor CreateShaderResourceView()
    {
        var srvDescriptor = GraphicsPipeline.DescriptorAllocator.AllocateSRV();

        // Create the actual SRV
        var srvDesc = new ShaderResourceViewDescription
        {
            Format = Format,
            ViewDimension = SrvDimension.Texture2D,
            Texture2D = new Texture2DSrv { MipLevels = 1 },
            Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING
        };

        GraphicsPipeline.GraphicsDevice.NativeDevice.Ptr->CreateShaderResourceView(_resource.NativeResource.Ptr, &srvDesc, srvDescriptor.CpuHandle);

        return srvDescriptor;
    }

    private void CopyBufferToTexture(CommandList cmd, GraphicsResource srcBuffer, GraphicsResource dstTexture)
    {
        // Calculate the proper footprint for the placed subresource
        var resourceDesc = dstTexture.NativeResource.Ptr->GetDesc();

        PlacedSubresourceFootprint footprint = new()
        {
            Footprint = new SubresourceFootprint
            {
                Format = Format,
                Width = Width,
                Height = Height,
                Depth = 1,
                RowPitch = (uint)((Pitch + 255) & ~255) // Align to 256 bytes
            }
        };

        var srcLocation = new TextureCopyLocation(srcBuffer.NativeResource.Ptr, footprint);
        var dstLocation = new TextureCopyLocation(dstTexture.NativeResource.Ptr, 0);

        cmd.NativeCommandList.Ptr->CopyTextureRegion(&dstLocation, 0, 0, 0, &srcLocation, null);
    }

    /// <summary>
    /// Sets the entire texture data on the CPU side.
    /// </summary>
    /// <param name="data">The texture data to set</param>
    public void SetData(ReadOnlySpan<byte> data)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (data.Length != DataSize)
        {
            throw new ArgumentException($"Data size mismatch. Expected {DataSize} bytes, got {data.Length} bytes.");
        }

        data.CopyTo(_cpuData.AsSpan());
    }

    /// <summary>
    /// Sets the texture data for a specific region on the CPU side.
    /// </summary>
    /// <param name="data">The texture data to set</param>
    /// <param name="x">Starting X coordinate</param>
    /// <param name="y">Starting Y coordinate</param>
    /// <param name="width">Width of the region</param>
    /// <param name="height">Height of the region</param>
    public void SetData(ReadOnlySpan<byte> data, uint x, uint y, uint width, uint height)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (x + width > Width || y + height > Height)
        {
            throw new ArgumentException("Region extends beyond texture bounds.");
        }

        var expectedSize = width * height * BytesPerPixel;
        if (data.Length != expectedSize)
        {
            throw new ArgumentException($"Data size mismatch. Expected {expectedSize} bytes, got {data.Length} bytes.");
        }

        for (uint row = 0; row < height; row++)
        {
            var srcOffset = (int)(row * width * BytesPerPixel);
            var dstOffset = (int)((y + row) * Pitch + x * BytesPerPixel);
            var rowSize = (int)(width * BytesPerPixel);

            data.Slice(srcOffset, rowSize).CopyTo(_cpuData.AsSpan().Slice(dstOffset, rowSize));
        }
    }

    /// <summary>
    /// Sets a single pixel value on the CPU side.
    /// </summary>
    /// <param name="x">X coordinate of the pixel</param>
    /// <param name="y">Y coordinate of the pixel</param>
    /// <param name="color">The color data for the pixel</param>
    public void SetPixel(uint x, uint y, ReadOnlySpan<byte> color)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (x >= Width || y >= Height)
        {
            throw new ArgumentException("Pixel coordinates are outside texture bounds.");
        }

        if (color.Length != BytesPerPixel)
        {
            throw new ArgumentException($"Color data size mismatch. Expected {BytesPerPixel} bytes, got {color.Length} bytes.");
        }

        var offset = (int)(y * Pitch + x * BytesPerPixel);
        color.CopyTo(_cpuData.AsSpan().Slice(offset, (int)BytesPerPixel));
    }

    /// <summary>
    /// Sets a single pixel value using generic color types.
    /// </summary>
    /// <typeparam name="T">The color type (e.g., uint for RGBA32)</typeparam>
    /// <param name="x">X coordinate of the pixel</param>
    /// <param name="y">Y coordinate of the pixel</param>
    /// <param name="color">The color value</param>
    public void SetPixel<T>(uint x, uint y, T color) where T : unmanaged
    {
        if (sizeof(T) != BytesPerPixel)
        {
            throw new ArgumentException($"Color type size mismatch. Expected {BytesPerPixel} bytes, got {sizeof(T)} bytes.");
        }

        var colorSpan = new ReadOnlySpan<byte>(&color, sizeof(T));
        SetPixel(x, y, colorSpan);
    }

    /// <summary>
    /// Gets a single pixel value from the CPU side data.
    /// </summary>
    /// <param name="x">X coordinate of the pixel</param>
    /// <param name="y">Y coordinate of the pixel</param>
    /// <returns>The pixel color data</returns>
    public ReadOnlySpan<byte> GetPixel(uint x, uint y)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (x >= Width || y >= Height)
        {
            throw new ArgumentException("Pixel coordinates are outside texture bounds.");
        }

        var offset = (int)(y * Pitch + x * BytesPerPixel);
        return _cpuData.AsSpan().Slice(offset, (int)BytesPerPixel);
    }

    /// <summary>
    /// Gets a single pixel value as a generic color type.
    /// </summary>
    /// <typeparam name="T">The color type (e.g., uint for RGBA32)</typeparam>
    /// <param name="x">X coordinate of the pixel</param>
    /// <param name="y">Y coordinate of the pixel</param>
    /// <returns>The pixel color value</returns>
    public T GetPixel<T>(uint x, uint y) where T : unmanaged
    {
        if (sizeof(T) != BytesPerPixel)
        {
            throw new ArgumentException($"Color type size mismatch. Expected {BytesPerPixel} bytes, got {sizeof(T)} bytes.");
        }

        var pixelData = GetPixel(x, y);
        return MemoryMarshal.Read<T>(pixelData);
    }

    /// <summary>
    /// Uploads the CPU-side texture data to the GPU resource.
    /// </summary>
    public void UploadTextureData()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        Format.GetSurfaceInfo((int)Width, (int)Height, out var rowPitch, out var slicePitch);
        var initData = new SubresourceData()
        {
            pData = _cpuData.GetUnsafePtr(),
            RowPitch = rowPitch,
            SlicePitch = slicePitch
        };

        using var uploadBatch = new ResourceUploadBatch();
        uploadBatch.Begin();
        uploadBatch.Transition(_resource, ResourceStates.Common, ResourceStates.CopyDest);
        uploadBatch.Upload(_resource, 0, &initData, 1);
        uploadBatch.Transition(_resource, ResourceStates.CopyDest, ResourceStates.PixelShaderResource);
        uploadBatch.WaitForCompletion(uploadBatch.End());
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _cpuData.Dispose();
        _resource.Dispose();

        GraphicsPipeline.DescriptorAllocator.ReleaseSRV(_srvDescriptor);

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}