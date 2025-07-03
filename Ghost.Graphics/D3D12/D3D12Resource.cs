using Ghost.Core;
using Ghost.Graphics.Contracts;
using System.Runtime.CompilerServices;
using Win32;
using Win32.Graphics.Direct3D12;

namespace Ghost.Graphics.D3D12;

public unsafe class D3D12Resource : IResource
{
    private ComPtr<ID3D12Resource> _nativeResource
    {
        get;
        set;
    }
    private string _name = string.Empty;

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetData<T>(Span<T> data)
        where T : unmanaged
    {
        var size = (uint)(data.Length * sizeof(T));
        var range = new Win32.Graphics.Direct3D12.Range(0, size);

        fixed (T* ptr = data)
        {
            void* mappedPtr;
            var hr = _nativeResource.Get()->Map(0, &range, &mappedPtr);
            if (hr.Failure)
            {
                var message = hr.ToString();
                throw new InvalidOperationException($"Failed to map resource: {message}");
            }
            Unsafe.CopyBlock(mappedPtr, ptr, size);
            _nativeResource.Get()->Unmap(0, &range);
        }
    }

    internal void DisposeInternal()
    {
        var c = _nativeResource.Reset();
    }

    public void Dispose()
    {
        if (!TempResource)
        {
            DisposeInternal();
        }
    }
}