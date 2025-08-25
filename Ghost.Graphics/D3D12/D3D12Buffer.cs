using Ghost.Graphics.RHI;
using Win32;
using Win32.Graphics.Direct3D12;

namespace Ghost.Graphics.D3D12;

/// <summary>
/// D3D12 implementation of buffer interface
/// </summary>
internal unsafe class D3D12Buffer : IBuffer
{
    private ComPtr<ID3D12Resource> _resource;
    private ResourceState _currentState;
    private void* _mappedPtr;
    private bool _disposed;

    public BufferUsage Usage
    {
        get;
    }
    public string Name { get; set; } = string.Empty;
    public ulong Size
    {
        get;
    }
    public ResourceState CurrentState => _currentState;

    public ID3D12Resource* NativeResource => _resource.Get();

    public D3D12Buffer(ComPtr<ID3D12Device14> device, BufferDesc desc)
    {
        Usage = desc.Usage;
        Size = desc.Size;
        _currentState = ResourceState.Common;

        CreateBuffer(device, desc);
    }

    private void CreateBuffer(ComPtr<ID3D12Device14> device, BufferDesc desc)
    {
        var resourceDesc = new ResourceDescription
        {
            Dimension = ResourceDimension.Buffer,
            Alignment = 0,
            Width = desc.Size,
            Height = 1,
            DepthOrArraySize = 1,
            MipLevels = 1,
            Format = Win32.Graphics.Dxgi.Common.Format.Unknown,
            SampleDesc = new Win32.Graphics.Dxgi.Common.SampleDescription(1, 0),
            Layout = TextureLayout.RowMajor,
            Flags = ConvertBufferUsage(desc.Usage)
        };

        var heapProps = new HeapProperties
        {
            Type = ConvertMemoryType(desc.MemoryType),
            CPUPageProperty = CpuPageProperty.Unknown,
            MemoryPoolPreference = MemoryPool.Unknown,
            CreationNodeMask = 1,
            VisibleNodeMask = 1
        };

        var initialState = desc.MemoryType switch
        {
            MemoryType.Upload => Win32.Graphics.Direct3D12.ResourceStates.GenericRead,
            MemoryType.Readback => Win32.Graphics.Direct3D12.ResourceStates.CopyDest,
            _ => Win32.Graphics.Direct3D12.ResourceStates.Common
        };

        device.Get()->CreateCommittedResource(
            &heapProps,
            HeapFlags.None,
            &resourceDesc,
            initialState,
            null,
            __uuidof<ID3D12Resource>(),
            _resource.GetVoidAddressOf());
    }

    public void* Map()
    {
        if (_mappedPtr != null)
            return _mappedPtr;

        var range = new Win32.Graphics.Direct3D12.Range { Begin = 0, End = 0 };

        fixed (void** ptr = &_mappedPtr)
        {
            _resource.Get()->Map(0, &range, ptr);
        }
        return _mappedPtr;
    }

    public void Unmap()
    {
        if (_mappedPtr != null)
        {
            _resource.Get()->Unmap(0, null);
            _mappedPtr = null;
        }
    }

    private static HeapType ConvertMemoryType(MemoryType memoryType)
    {
        return memoryType switch
        {
            MemoryType.Default => HeapType.Default,
            MemoryType.Upload => HeapType.Upload,
            MemoryType.Readback => HeapType.Readback,
            _ => throw new ArgumentException($"Unknown memory type: {memoryType}")
        };
    }

    private static ResourceFlags ConvertBufferUsage(BufferUsage usage)
    {
        var flags = ResourceFlags.None;

        if ((usage & BufferUsage.Raw) != 0)
            flags |= ResourceFlags.AllowUnorderedAccess;

        return flags;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        Unmap();
        _resource.Dispose();
        _disposed = true;
    }
}
