using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXGI.DXGI;

namespace Ghost.DXGI;

[Guid("2633066B-4514-4C7A-8FD8-12EA98059D18")]
[NativeTypeName("struct IDXGIDecodeSwapChain : IUnknown")]
public unsafe partial struct IDXGIDecodeSwapChain : IDXGIDecodeSwapChain.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDXGIDecodeSwapChain);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIDecodeSwapChain*, Guid*, void**, int>)(lpVtbl[0]))((IDXGIDecodeSwapChain*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIDecodeSwapChain*, uint>)(lpVtbl[1]))((IDXGIDecodeSwapChain*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIDecodeSwapChain*, uint>)(lpVtbl[2]))((IDXGIDecodeSwapChain*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int PresentBuffer(uint BufferToPresent, uint SyncInterval, uint Flags)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIDecodeSwapChain*, uint, uint, uint, int>)(lpVtbl[3]))((IDXGIDecodeSwapChain*)Unsafe.AsPointer(ref this), BufferToPresent, SyncInterval, Flags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int SetSourceRect([NativeTypeName("const RECT *")] RECT* pRect)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIDecodeSwapChain*, RECT*, int>)(lpVtbl[4]))((IDXGIDecodeSwapChain*)Unsafe.AsPointer(ref this), pRect);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    [return: NativeTypeName("HRESULT")]
    public int SetTargetRect([NativeTypeName("const RECT *")] RECT* pRect)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIDecodeSwapChain*, RECT*, int>)(lpVtbl[5]))((IDXGIDecodeSwapChain*)Unsafe.AsPointer(ref this), pRect);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    [return: NativeTypeName("HRESULT")]
    public int SetDestSize(uint Width, uint Height)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIDecodeSwapChain*, uint, uint, int>)(lpVtbl[6]))((IDXGIDecodeSwapChain*)Unsafe.AsPointer(ref this), Width, Height);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    [return: NativeTypeName("HRESULT")]
    public int GetSourceRect(RECT* pRect)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIDecodeSwapChain*, RECT*, int>)(lpVtbl[7]))((IDXGIDecodeSwapChain*)Unsafe.AsPointer(ref this), pRect);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(8)]
    [return: NativeTypeName("HRESULT")]
    public int GetTargetRect(RECT* pRect)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIDecodeSwapChain*, RECT*, int>)(lpVtbl[8]))((IDXGIDecodeSwapChain*)Unsafe.AsPointer(ref this), pRect);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(9)]
    [return: NativeTypeName("HRESULT")]
    public int GetDestSize(uint* pWidth, uint* pHeight)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIDecodeSwapChain*, uint*, uint*, int>)(lpVtbl[9]))((IDXGIDecodeSwapChain*)Unsafe.AsPointer(ref this), pWidth, pHeight);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(10)]
    [return: NativeTypeName("HRESULT")]
    public int SetColorSpace(DXGI_MULTIPLANE_OVERLAY_YCbCr_FLAGS ColorSpace)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIDecodeSwapChain*, DXGI_MULTIPLANE_OVERLAY_YCbCr_FLAGS, int>)(lpVtbl[10]))((IDXGIDecodeSwapChain*)Unsafe.AsPointer(ref this), ColorSpace);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(11)]
    public DXGI_MULTIPLANE_OVERLAY_YCbCr_FLAGS GetColorSpace()
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIDecodeSwapChain*, DXGI_MULTIPLANE_OVERLAY_YCbCr_FLAGS>)(lpVtbl[11]))((IDXGIDecodeSwapChain*)Unsafe.AsPointer(ref this));
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        [return: NativeTypeName("HRESULT")]
        int PresentBuffer(uint BufferToPresent, uint SyncInterval, uint Flags);

        [VtblIndex(4)]
        [return: NativeTypeName("HRESULT")]
        int SetSourceRect([NativeTypeName("const RECT *")] RECT* pRect);

        [VtblIndex(5)]
        [return: NativeTypeName("HRESULT")]
        int SetTargetRect([NativeTypeName("const RECT *")] RECT* pRect);

        [VtblIndex(6)]
        [return: NativeTypeName("HRESULT")]
        int SetDestSize(uint Width, uint Height);

        [VtblIndex(7)]
        [return: NativeTypeName("HRESULT")]
        int GetSourceRect(RECT* pRect);

        [VtblIndex(8)]
        [return: NativeTypeName("HRESULT")]
        int GetTargetRect(RECT* pRect);

        [VtblIndex(9)]
        [return: NativeTypeName("HRESULT")]
        int GetDestSize(uint* pWidth, uint* pHeight);

        [VtblIndex(10)]
        [return: NativeTypeName("HRESULT")]
        int SetColorSpace(DXGI_MULTIPLANE_OVERLAY_YCbCr_FLAGS ColorSpace);

        [VtblIndex(11)]
        DXGI_MULTIPLANE_OVERLAY_YCbCr_FLAGS GetColorSpace();
    }
}
