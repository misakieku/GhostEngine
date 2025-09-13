using Misaki.HighPerformance.Image;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Helpers;
using Win32.Graphics.Direct3D12;
using Win32.Graphics.Dxgi.Common;

namespace Ghost.Graphics.Data;

/// <summary>
/// Unified texture implementation that supports both bindless and regular descriptor table binding
/// for use with SM 6.6 ResourceDescriptorHeap access and traditional frame buffer textures
/// </summary>
public unsafe class Texture2D : Texture
{
    private UnTypedArray _cpuData;

    public uint Pitch
    {
        get;
    }

    private Texture2D(uint width, uint height, Format format, in TextureHandle handle, in ReadOnlySpan<byte> cpuData, BindlessDescriptor bindlessDescriptor)
        : base(width, height, format, in handle, bindlessDescriptor)
    {
        Pitch = width * BytesPerPixel;

        uint alignment;
        if (BytesPerPixel > 4)
        {
            alignment = (uint)MemoryUtilities.AlignOf<float>();
        }
        else
        {
            alignment = (uint)MemoryUtilities.AlignOf<byte>();
        }

        _cpuData = new UnTypedArray((uint)Size, alignment, ref AllocationManager.PersistentHandle);
        if (!cpuData.IsEmpty)
        {
            if ((uint)cpuData.Length > Size)
            {
                throw new ArgumentException($"Data size mismatch. Expected {Size} bytes, got {cpuData.Length} bytes.");
            }

            _cpuData.CopyFrom(cpuData);
        }
    }

    public static Texture2D Create(uint width, uint height, in ReadOnlySpan<byte> data, Format format)
    {
        var handle = GraphicsPipeline.ResourceAllocator.CreateTexture2D(width, height, 0, format);
        var bindlessDescriptor = CreateBindlessShaderResourceView(handle.ResourceHandle.GetAllocation().Resource, format);

        return new Texture2D(width, height, format, in handle, in data, bindlessDescriptor);
    }

    public static Texture2D FromFile(string filePath)
    {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using var image = ImageResult.FromStream(stream, ColorComponents.RGBA);

        return Create(image.Width, image.Height, image.AsSpan(), Format.R8G8B8A8Unorm);
    }

    /// <summary>
    /// Sets the entire texture data on the CPU side.
    /// </summary>
    /// <param name="data">The texture data to set</param>
    public void SetData<T>(ReadOnlySpan<T> data)
        where T : unmanaged
    {
        ThrowIfDisposed();

        if ((uint)data.Length > Size)
        {
            throw new ArgumentException($"Data size mismatch. Expected {Size} bytes, got {data.Length} bytes.");
        }

        _cpuData.CopyFrom(data);
    }

    /// <summary>
    /// Sets a single pixel value using generic color types.
    /// </summary>
    /// <typeparam name="T">The color type (e.g., uint for RGBA32)</typeparam>
    /// <param name="x">X coordinate of the pixel</param>
    /// <param name="y">Y coordinate of the pixel</param>
    /// <param name="color">The color value</param>
    public void SetPixel<T>(uint x, uint y, T color)
        where T : unmanaged
    {
        if (sizeof(T) != BytesPerPixel)
        {
            throw new ArgumentException($"Color type size mismatch. Expected {BytesPerPixel} bytes, got {sizeof(T)} bytes.");
        }

        var colorSpan = new ReadOnlySpan<byte>(&color, sizeof(T));
        SetPixel(x, y, colorSpan);
    }

    /// <summary>
    /// Sets a single pixel value on the CPU side.
    /// </summary>
    /// <param name="x">X coordinate of the pixel</param>
    /// <param name="y">Y coordinate of the pixel</param>
    /// <param name="color">The color data for the pixel</param>
    public void SetPixel(uint x, uint y, ReadOnlySpan<byte> color)
    {
        ThrowIfDisposed();

        if (x >= Width || y >= Height)
        {
            throw new ArgumentException("Pixel coordinates are outside texture bounds.");
        }

        if (color.Length != BytesPerPixel)
        {
            throw new ArgumentException($"Color data size mismatch. Expected {BytesPerPixel} bytes, got {color.Length} bytes.");
        }

        var offset = y * Pitch + x * BytesPerPixel;
        _cpuData.CopyFrom(color, 0u, offset, BytesPerPixel);
    }

    /// <summary>
    /// Gets a single pixel value as a generic color type.
    /// </summary>
    /// <typeparam name="T">The color type (e.g., uint for RGBA32)</typeparam>
    /// <param name="x">X coordinate of the pixel</param>
    /// <param name="y">Y coordinate of the pixel</param>
    /// <returns>The pixel color value</returns>
    public T GetPixel<T>(uint x, uint y)
        where T : unmanaged
    {
        if (sizeof(T) != BytesPerPixel)
        {
            throw new ArgumentException($"Color type size mismatch. Expected {BytesPerPixel} bytes, got {sizeof(T)} bytes.");
        }

        var pixelData = GetPixel(x, y);
        fixed (byte* pPixel = pixelData)
        {
            return *(T*)pPixel;
        }
    }

    /// <summary>
    /// Gets a single pixel value from the CPU side data.
    /// </summary>
    /// <param name="x">X coordinate of the pixel</param>
    /// <param name="y">Y coordinate of the pixel</param>
    /// <returns>The pixel color data</returns>
    public ReadOnlySpan<byte> GetPixel(uint x, uint y)
    {
        ThrowIfDisposed();

        if (x >= Width || y >= Height)
        {
            throw new ArgumentException("Pixel coordinates are outside texture bounds.");
        }

        var offset = (int)(y * Pitch + x * BytesPerPixel);
        return _cpuData.AsSpan().Slice(offset, (int)BytesPerPixel);
    }

    /// <summary>
    /// Uploads the CPU-side texture data to the GPU resource.
    /// </summary>
    public void UploadTextureData()
    {
        ThrowIfDisposed();

        Format.GetSurfaceInfo((int)Width, (int)Height, out var rowPitch, out var slicePitch);
        var initData = new SubresourceData()
        {
            pData = _cpuData.GetUnsafePtr(),
            RowPitch = rowPitch,
            SlicePitch = slicePitch
        };

        var uploadBatch = GraphicsPipeline.UploadBatch;
        uploadBatch.Transition(NativeResource, ResourceStates.Common, ResourceStates.CopyDest);
        uploadBatch.Upload(NativeResource, 0, &initData, 1);
        uploadBatch.Transition(NativeResource, ResourceStates.CopyDest, ResourceStates.PixelShaderResource);
    }

    public override void Dispose()
    {
        base.Dispose();
        _cpuData.Dispose();
    }
}