using Ghost.Core.Utilities;
using Ghost.Graphics.D3D12.Utilities;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel;
using System.Runtime.CompilerServices;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Ghost.Graphics.D3D12;

internal abstract class D3D12RHIObject : IRHIObject
{
    public string Name
    {
        get; set;
    } = string.Empty;
}

internal abstract unsafe class D3D12Object<T> : IRHIObject, IDisposable
    where T : unmanaged, ID3D12Object.Interface
{
    private bool _disposed;
    private string _name = string.Empty;

    protected UniquePtr<T> nativeObject;

    protected bool Disposed => _disposed;

    public string Name
    {
        get => _name;
        set
        {
            if (_name == value)
            {
                return;
            }

            _name = value;
            if (nativeObject.Get() != null)
            {
                nativeObject.Get()->SetName(value);
            }
        }
    }

    ~D3D12Object()
    {
        Dispose(false);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        nativeObject.Dispose();

        _disposed = true;
    }
}


internal abstract class IUnknownObject<T> : IRHIObject, IDisposable
    where T : unmanaged, IUnknown.Interface
{
    private bool _disposed;
    private string _name = string.Empty;

    protected UniquePtr<T> nativeObject;

    protected bool Disposed => _disposed;

    public string Name
    {
        get => _name;
        set
        {
            if (_name == value)
            {
                return;
            }

            _name = value;
        }
    }

    ~IUnknownObject()
    {
        Dispose(false);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        nativeObject.Dispose();

        _disposed = true;
    }
}