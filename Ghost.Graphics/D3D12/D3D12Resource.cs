using Ghost.Core;
using Ghost.Graphics.Contracts;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Runtime.CompilerServices;
using Win32;
using Win32.Graphics.Direct3D12;

namespace Ghost.Graphics.D3D12;

public unsafe class D3D12Resource : IResource
{
    private ComPtr<ID3D12Resource> _nativeResource;
    private string _name = string.Empty;

    private bool _disposed;

    internal ConstPtr<ID3D12Resource> NativeResource => new(_nativeResource.Get());

    public ulong GPUAddress => _nativeResource.Get()->GetGPUVirtualAddress();

    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            _nativeResource.Get()->SetName(_name);
        }
    }

    public bool TempResource
    {
        get;
    }

    internal D3D12Resource(ComPtr<ID3D12Resource> nativeResource, bool temp)
    {
        _nativeResource = nativeResource;
        TempResource = temp;
    }

    ~D3D12Resource()
    {
        DisposeInternal();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetData<T>(Span<T> data)
        where T : unmanaged
    {
        fixed (T* ptr = data)
        {
            SetData(ptr, (uint)data.Length);
        }
    }

    public unsafe void SetData<T>(T* data, uint length)
        where T : unmanaged
    {
        var size = (uint)(length * sizeof(T));
        SetData((void*)data, size);
    }

    public unsafe void SetData(void* data, uint size)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var range = new Win32.Graphics.Direct3D12.Range(0, size);

        void* mappedPtr;
        var hr = _nativeResource.Get()->Map(0, &range, &mappedPtr);
        if (hr.Failure)
        {
            var message = hr.ToString();
            throw new InvalidOperationException($"Failed to map resource: {message}");
        }

        Unsafe.CopyBlock(mappedPtr, data, size);
        _nativeResource.Get()->Unmap(0, &range);
    }

    public UnsafeArray<T> ReadData<T>(Allocator allocator)
    where T : unmanaged
    {
        var size = (uint)_nativeResource.Get()->GetDesc().Width;
        var data = new UnsafeArray<T>((int)(size / (uint)sizeof(T)), allocator);
        try
        {
            ReadData(data.GetUnsafePtr(), &size);
            return data;
        }
        catch (Exception)
        {
            data.Dispose();
            throw;
        }
    }

    public void ReadData<T>(T* pData, uint* size)
        where T : unmanaged
    {
        ReadData(pData, size);
    }

    public void ReadData(void* pData, uint* size)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var range = new Win32.Graphics.Direct3D12.Range(0, (uint)_nativeResource.Get()->GetDesc().Width);

        void* mappedPtr;
        var hr = _nativeResource.Get()->Map(0, &range, &mappedPtr);
        if (hr.Failure)
        {
            var message = hr.ToString();
            throw new InvalidOperationException($"Failed to map resource: {message}");
        }

        Unsafe.CopyBlock(pData, mappedPtr, (uint)(range.End - range.Begin));
        _nativeResource.Get()->Unmap(0, &range);
        if (size != null)
        {
            *size = (uint)(range.End - range.Begin);
        }
    }

    internal void DisposeInternal()
    {
        if (_disposed)
        {
            return;
        }

        _nativeResource.Dispose();

        _disposed = true;
    }

    public void Dispose()
    {
        if (!TempResource)
        {
            DisposeInternal();
            GC.SuppressFinalize(this);
        }
    }
}