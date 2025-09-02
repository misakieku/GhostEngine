using Ghost.Graphics.Data;
using Ghost.Graphics.RHI;
using Win32;
using Win32.Graphics.Direct3D12;

namespace Ghost.Graphics.D3D12;

/// <summary>
/// D3D12 implementation of buffer interface using resource handles
/// </summary>
internal unsafe class D3D12Buffer : IBuffer
{
    private readonly BufferHandle _handle;
    private readonly ComPtr<ID3D12Resource> _externalResource; // For externally managed resources
    private ResourceState _currentState;
    private void* _mappedPtr;
    private bool _disposed;

    public BufferUsage Usage
    {
        get;
    }

    public MemoryType MemoryType
    {
        get;
    }

    public string Name
    {
        get => field;
        set
        {
            field = value;
            NativeResource->SetName(field);
        }
    } = string.Empty;

    public ulong Size
    {
        get;
    }

    public ResourceState CurrentState => _currentState;

    public ID3D12Resource* NativeResource => _handle.ResourceHandle.GetAllocation().Resource;

    /// <summary>
    /// Constructor for wrapping existing D3D12 resources
    /// </summary>
    public D3D12Buffer(ComPtr<ID3D12Resource> resource, ulong size, BufferUsage usage, MemoryType memoryType)
    {
        _handle = BufferHandle.Invalid;
        _externalResource = resource.Move();

        Size = size;
        Usage = usage;
        MemoryType = memoryType;
        _currentState = ResourceState.Common;
    }

    /// <summary>
    /// Constructor for allocator-managed buffers
    /// </summary>
    public D3D12Buffer(BufferHandle handle, ref readonly BufferDesc desc)
    {
        _handle = handle;

        Size = desc.Size;
        Usage = desc.Usage;
        MemoryType = desc.MemoryType;
        _currentState = ResourceState.Common;
    }

    public void* Map()
    {
        if (_mappedPtr != null)
            return _mappedPtr;

        if (MemoryType != MemoryType.Upload && MemoryType != MemoryType.Readback)
        {
            throw new InvalidOperationException("Only upload and readback buffers can be mapped");
        }

        var range = new Win32.Graphics.Direct3D12.Range { Begin = 0, End = 0 };
        fixed (void** ptr = &_mappedPtr)
        {
            NativeResource->Map(0, &range, ptr);
        }
        return _mappedPtr;
    }

    public void Unmap()
    {
        if (_mappedPtr != null)
        {
            NativeResource->Unmap(0, null);
            _mappedPtr = null;
        }
    }

    public void SetCurrentState(ResourceState state)
    {
        _currentState = state;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        Unmap();

        if (_handle.IsValid)
        {
            _handle.Dispose();
        }
        else
        {
            // Release external resource
            _externalResource.Dispose();
        }

        _disposed = true;
    }
}
