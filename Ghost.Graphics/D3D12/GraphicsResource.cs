using Ghost.Core;
using Ghost.Graphics.Data;
using System.Runtime.CompilerServices;
using Win32.Graphics.Direct3D12;

namespace Ghost.Graphics.D3D12;

public unsafe class GraphicsResource : IDisposable
{
    private readonly ResourceHandle _handle;
    private string _name = string.Empty;

    internal ConstPtr<ID3D12Resource> NativeResource
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_handle.GetAllocation().Resource);
    }

    internal ulong GPUAddress
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => NativeResource.Ptr->GetGPUVirtualAddress();
    }

    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            NativeResource.Ptr->SetName(_name);
        }
    }

    public bool TempResource
    {
        get;
    }

    public ulong Size => _handle.GetAllocation().Size;

    internal GraphicsResource(in ResourceHandle handle, bool tempResource = false)
    {
        _handle = handle;
        TempResource = tempResource;
    }

    ~GraphicsResource()
    {
        DisposeInternal();
    }

    /// <summary>
    /// Throws an exception if the resource has been disposed.
    /// </summary>
    protected void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(!_handle.IsValid, this);
    }

    internal void DisposeInternal()
    {
        _handle.Dispose();
    }

    public virtual void Dispose()
    {
        if (!TempResource)
        {
            DisposeInternal();
        }

        GC.SuppressFinalize(this);
    }
}