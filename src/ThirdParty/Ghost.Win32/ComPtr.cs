using System.Runtime.CompilerServices;

namespace Ghost.Win32;

public unsafe struct ComPtr<T> : IDisposable
    where T : unmanaged, IUnknown.Interface
{
    private T* _ptr;

    public ComPtr(T* ptr)
    {
        _ptr = ptr;
        InternalAddRef();
    }

    public static implicit operator T*(ComPtr<T> other) => other.Get();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly T* Get() => _ptr;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T** GetAddressOf() => (T**)Unsafe.AsPointer(ref this);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T** ReleaseAndGetAddressOf()
    {
        Dispose();
        return (T**)Unsafe.AsPointer(ref this);
    }

    public void Attach(T* other)
    {
        if (_ptr != null)
        {
            _ptr->Release();
        }

        _ptr = other;
    }

    public T* Detach()
    {
        var ptr = _ptr;
        _ptr = null;
        return ptr;
    }

    public void Dispose()
    {
        var ptr = _ptr;
        if (ptr != null)
        {
            _ptr = null;
            ptr->Release();
        }
    }

    private void InternalAddRef()
    {
        if (_ptr != null)
        {
            _ptr->AddRef();
        }
    }
}
