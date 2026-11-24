using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXGI.DXGI;

namespace Ghost.DXGI;

[Guid("C5A05F0C-16F2-4ADF-9F4D-A8C4D58AC550")]
[NativeTypeName("struct IDXGIDebug1 : IDXGIDebug")]
public unsafe partial struct IDXGIDebug1 : IDXGIDebug1.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDXGIDebug1);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIDebug1*, Guid*, void**, int>)(lpVtbl[0]))((IDXGIDebug1*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIDebug1*, uint>)(lpVtbl[1]))((IDXGIDebug1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIDebug1*, uint>)(lpVtbl[2]))((IDXGIDebug1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int ReportLiveObjects(Guid apiid, DXGI_DEBUG_RLO_FLAGS flags)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIDebug1*, Guid, DXGI_DEBUG_RLO_FLAGS, int>)(lpVtbl[3]))((IDXGIDebug1*)Unsafe.AsPointer(ref this), apiid, flags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    public void EnableLeakTrackingForThread()
    {
        ((delegate* unmanaged[MemberFunction]<IDXGIDebug1*, void>)(lpVtbl[4]))((IDXGIDebug1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    public void DisableLeakTrackingForThread()
    {
        ((delegate* unmanaged[MemberFunction]<IDXGIDebug1*, void>)(lpVtbl[5]))((IDXGIDebug1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    [return: NativeTypeName("BOOL")]
    public int IsLeakTrackingEnabledForThread()
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIDebug1*, int>)(lpVtbl[6]))((IDXGIDebug1*)Unsafe.AsPointer(ref this));
    }

    public interface Interface : IDXGIDebug.Interface
    {
        [VtblIndex(4)]
        void EnableLeakTrackingForThread();

        [VtblIndex(5)]
        void DisableLeakTrackingForThread();

        [VtblIndex(6)]
        [return: NativeTypeName("BOOL")]
        int IsLeakTrackingEnabledForThread();
    }
}
