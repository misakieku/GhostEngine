using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("3FEBD6DD-4973-4787-8194-E45F9E28923E")]
[NativeTypeName("struct ID3D12DebugDevice : IUnknown")]
public unsafe partial struct ID3D12DebugDevice : ID3D12DebugDevice.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12DebugDevice);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DebugDevice*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12DebugDevice*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DebugDevice*, uint>)(lpVtbl[1]))((ID3D12DebugDevice*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DebugDevice*, uint>)(lpVtbl[2]))((ID3D12DebugDevice*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public HRESULT SetFeatureMask(D3D12_DEBUG_FEATURE Mask)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DebugDevice*, D3D12_DEBUG_FEATURE, int>)(lpVtbl[3]))((ID3D12DebugDevice*)Unsafe.AsPointer(ref this), Mask);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    public D3D12_DEBUG_FEATURE GetFeatureMask()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DebugDevice*, D3D12_DEBUG_FEATURE>)(lpVtbl[4]))((ID3D12DebugDevice*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    public HRESULT ReportLiveDeviceObjects(D3D12_RLDO_FLAGS Flags)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DebugDevice*, D3D12_RLDO_FLAGS, int>)(lpVtbl[5]))((ID3D12DebugDevice*)Unsafe.AsPointer(ref this), Flags);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        HRESULT SetFeatureMask(D3D12_DEBUG_FEATURE Mask);

        [VtblIndex(4)]
        D3D12_DEBUG_FEATURE GetFeatureMask();

        [VtblIndex(5)]
        HRESULT ReportLiveDeviceObjects(D3D12_RLDO_FLAGS Flags);
    }
}
