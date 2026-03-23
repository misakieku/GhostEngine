using Misaki.HighPerformance.LowLevel;
using TerraFX.Interop.DirectX;
using Ghost.Graphics.D3D12.Utilities;
using Ghost.Graphics.RHI;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Ghost.Graphics.D3D12;

public unsafe abstract class D3D12Object<T>: IRHIObject
    where T : unmanaged, ID3D12Object.Interface
{
    private UniquePtr<T> _nativeObject;
    private string _name = string.Empty;

    protected T* pNativeObject => _nativeObject.Get();
    public SharedPtr<T> NativeObject => _nativeObject.Share();

    public string Name
    {
        get => _name;
        set
        {
            if (string.Equals(_name, value, StringComparison.Ordinal))
            {
                return;
            }

            _name = value;
            _nativeObject.Get()->SetName(value);
        }
    }

    protected D3D12Object(T* nativeObject)
    {
        _nativeObject.Attach(nativeObject);
    }

    ~D3D12Object()
    {
        Dispose(disposing: false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_nativeObject.Get() == null, this);
    }

    [Conditional("DEBUG")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void AssertNotDisposed()
    {
        Debug.Assert(_nativeObject.Get() != null, "Object has been disposed.");
    }

    protected virtual void Dispose(bool disposing)
    {
    }

    public void Dispose()
    {
        if (_nativeObject.Get() == null)
        {
            return;
        }

        Dispose(disposing: true);

        _nativeObject.Dispose();

        GC.SuppressFinalize(this);
    }
}

