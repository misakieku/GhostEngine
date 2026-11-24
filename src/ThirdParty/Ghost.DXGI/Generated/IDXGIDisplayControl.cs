using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXGI.DXGI;

namespace Ghost.DXGI;

[Guid("EA9DBF1A-C88E-4486-854A-98AA0138F30C")]
[NativeTypeName("struct IDXGIDisplayControl : IUnknown")]
public unsafe partial struct IDXGIDisplayControl : IDXGIDisplayControl.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDXGIDisplayControl);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIDisplayControl*, Guid*, void**, int>)(lpVtbl[0]))((IDXGIDisplayControl*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIDisplayControl*, uint>)(lpVtbl[1]))((IDXGIDisplayControl*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIDisplayControl*, uint>)(lpVtbl[2]))((IDXGIDisplayControl*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("BOOL")]
    public int IsStereoEnabled()
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIDisplayControl*, int>)(lpVtbl[3]))((IDXGIDisplayControl*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    public void SetStereoEnabled([NativeTypeName("BOOL")] int enabled)
    {
        ((delegate* unmanaged[MemberFunction]<IDXGIDisplayControl*, int, void>)(lpVtbl[4]))((IDXGIDisplayControl*)Unsafe.AsPointer(ref this), enabled);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        [return: NativeTypeName("BOOL")]
        int IsStereoEnabled();

        [VtblIndex(4)]
        void SetStereoEnabled([NativeTypeName("BOOL")] int enabled);
    }
}
