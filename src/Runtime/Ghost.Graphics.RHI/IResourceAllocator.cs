using Ghost.Core;

namespace Ghost.Graphics.RHI;

public enum ResourceAllocationType
{
    Default,
    Temporary,
    Suballocation,
}

public struct CreationOptions
{
    public ResourceAllocationType AllocationType
    {
        get; set;
    }

    public Handle<GPUResource> Heap
    {
        get; set;
    }

    public ulong Offset
    {
        get; set;
    }
}

public enum HeapType
{
    Default,
    Upload,
    Readback
}

public enum HeapFlags
{
    None = 0,
    AllowBuffers,
    AllowTextures,
    AllowRTAndDS,
    AlowBufferAndTexture,
}

public struct AllocationDesc
{
    public ulong Size
    {
        get; set;
    }

    public ulong Alignment
    {
        get; set;
    }

    public HeapType HeapType
    {
        get; set;
    }

    public HeapFlags HeapFlags
    {
        get; set;
    }
}

public readonly struct ResourceSizeInfo
{
    public ulong Size
    {
        get; init;
    }

    public ulong Alignment
    {
        get; init;
    }
}

public interface IResourceAllocator : IDisposable
{
    ResourceSizeInfo GetSizeInfo(ResourceDesc desc);

    /// <summary>
    /// Allocates a block of memory on the GPU
    /// </summary>
    /// <param name="desc">Allocation description</param>
    /// <param name="name">Debug name of the allocation</param>
    /// <returns>An <see cref="Handle{GPUResource}"/> point to the allocated memory</returns>
    Handle<GPUResource> Allocate(ref readonly AllocationDesc desc, string name);

    /// <summary>
    /// Creates a texture resource
    /// </summary>
    /// <param name="desc">Texture description</param>
    /// <param name="name">Debug name of the resource</param>
    /// <param name="options">Additional options of the resource allocation</param>
    /// <returns>An <see cref="Handle{Texture}"/> point to the resource</returns>
    Handle<GPUTexture> CreateTexture(ref readonly TextureDesc desc, string name, CreationOptions options = default);

    /// <summary>
    /// Creates a render Target for off-screen rendering
    /// </summary>
    /// <param name="desc">Render Target description</param>
    /// <param name="name">Debug name of the resource</param>
    /// <param name="options">Additional options of the resource allocation</param>
    /// <returns>An <see cref="Handle{Texture}"/> point to the resource</returns>
    Handle<GPUTexture> CreateRenderTarget(ref readonly RenderTargetDesc desc, string name, CreationOptions options = default);

    /// <summary>
    /// Creates a buffer resource
    /// </summary>
    /// <param name="desc">Buffer description</param>
    /// <param name="name">Debug name of the resource</param>
    /// <param name="options">Additional options of the resource allocation</param>
    /// <returns>An <see cref="Handle{GraphicsBuffer}"/> point to the resource</returns>
    Handle<GPUBuffer> CreateBuffer(ref readonly BufferDesc desc, string name, CreationOptions options = default);

    /// <summary>
    /// Creates a temporary upload buffer of the specified size in bytes.
    /// </summary>
    /// <remarks>
    /// This method has been optimized for frequent calls during frame updates. It efficiently manages memory to minimize fragmentation and overhead.
    /// </remarks>
    /// <param name="sizeInBytes">The size of the upload buffer to create, in bytes.</param>
    /// <param name="offset">The offset within the upload buffer where the allocation begins.</param>
    /// <returns>An <see cref="Handle{GraphicsBuffer}"/> pointing to the created upload buffer.</returns>
    Handle<GPUBuffer> CreateTempUploadBuffer(ulong sizeInBytes, out ulong offset);

    /// <summary>
    /// Creates a new sampler object using the specified sampler description.
    /// </summary>
    /// <param name="desc">A read-only reference to a <see cref="SamplerDesc"/> structure that defines the properties of the sampler to be created.</param>
    /// <returns>An <see cref="Identifier{Sampler}"/> that uniquely identifies the created sampler object.</returns>
    Identifier<Sampler> CreateSampler(ref readonly SamplerDesc desc);
}
