namespace Ghost.Graphics.RHI;

/// <summary>
/// D3D12-style descriptor allocator interface
/// </summary>
public interface IDescriptorAllocator : IDisposable
{
    /// <summary>
    /// Allocates a render target view descriptor
    /// </summary>
    /// <returns>RTV descriptor handle</returns>
    DescriptorHandle AllocateRTV();

    /// <summary>
    /// Allocates multiple render target view descriptors
    /// </summary>
    /// <param name="count">Number of descriptors to allocate</param>
    /// <returns>Array of RTV descriptor handles</returns>
    DescriptorHandle[] AllocateRTVs(uint count);

    /// <summary>
    /// Allocates a depth stencil view descriptor
    /// </summary>
    /// <returns>DSV descriptor handle</returns>
    DescriptorHandle AllocateDSV();

    /// <summary>
    /// Allocates a shader resource view descriptor
    /// </summary>
    /// <returns>SRV descriptor handle</returns>
    DescriptorHandle AllocateSRV();

    /// <summary>
    /// Allocates a sampler descriptor
    /// </summary>
    /// <returns>Sampler descriptor handle</returns>
    DescriptorHandle AllocateSampler();

    /// <summary>
    /// Allocates a bindless descriptor for SM 6.6 rendering
    /// </summary>
    /// <returns>Bindless descriptor handle</returns>
    DescriptorHandle AllocateBindless();

    /// <summary>
    /// Releases a render target view descriptor
    /// </summary>
    /// <param name="handle">RTV descriptor to release</param>
    void ReleaseRTV(DescriptorHandle handle);

    /// <summary>
    /// Releases a depth stencil view descriptor
    /// </summary>
    /// <param name="handle">DSV descriptor to release</param>
    void ReleaseDSV(DescriptorHandle handle);

    /// <summary>
    /// Releases a shader resource view descriptor
    /// </summary>
    /// <param name="handle">SRV descriptor to release</param>
    void ReleaseSRV(DescriptorHandle handle);

    /// <summary>
    /// Releases a sampler descriptor
    /// </summary>
    /// <param name="handle">Sampler descriptor to release</param>
    void ReleaseSampler(DescriptorHandle handle);

    /// <summary>
    /// Releases a bindless descriptor
    /// </summary>
    /// <param name="handle">Bindless descriptor to release</param>
    void ReleaseBindless(DescriptorHandle handle);
}

/// <summary>
/// D3D12-style descriptor heap interface
/// </summary>
public interface IDescriptorHeap : IDisposable
{
    /// <summary>
    /// Type of descriptors this heap contains
    /// </summary>
    DescriptorHeapType Type { get; }

    /// <summary>
    /// Maximum number of descriptors in this heap
    /// </summary>
    uint MaxDescriptors { get; }

    /// <summary>
    /// Whether this heap is shader visible
    /// </summary>
    bool IsShaderVisible { get; }

    /// <summary>
    /// Gets a CPU descriptor handle at the specified index
    /// </summary>
    /// <param name="index">Index of the descriptor</param>
    /// <returns>CPU descriptor handle</returns>
    DescriptorHandle GetCPUHandle(uint index);

    /// <summary>
    /// Gets a GPU descriptor handle at the specified index
    /// </summary>
    /// <param name="index">Index of the descriptor</param>
    /// <returns>GPU descriptor handle</returns>
    DescriptorHandle GetGPUHandle(uint index);
}

/// <summary>
/// Descriptor handle for D3D12-style descriptor management
/// </summary>
public struct DescriptorHandle : IEquatable<DescriptorHandle>
{
    public uint Index;
    public bool IsValid;

    public DescriptorHandle(uint index)
    {
        Index = index;
        IsValid = true;
    }

    public static DescriptorHandle Invalid => new() { Index = uint.MaxValue, IsValid = false };

    public bool Equals(DescriptorHandle other) => Index == other.Index && IsValid == other.IsValid;
    public override bool Equals(object? obj) => obj is DescriptorHandle other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Index, IsValid);

    public static bool operator ==(DescriptorHandle left, DescriptorHandle right) => left.Equals(right);
    public static bool operator !=(DescriptorHandle left, DescriptorHandle right) => !left.Equals(right);
}

/// <summary>
/// D3D12 descriptor heap types
/// </summary>
public enum DescriptorHeapType
{
    CBV_SRV_UAV,
    Sampler,
    RTV,
    DSV
}
