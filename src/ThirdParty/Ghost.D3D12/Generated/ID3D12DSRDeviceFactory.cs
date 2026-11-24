using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("F343D1A0-AFE3-439F-B13D-CD87A43B70CA")]
[NativeTypeName("struct ID3D12DSRDeviceFactory : IUnknown")]
public unsafe partial struct ID3D12DSRDeviceFactory : ID3D12DSRDeviceFactory.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12DSRDeviceFactory);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DSRDeviceFactory*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12DSRDeviceFactory*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DSRDeviceFactory*, uint>)(lpVtbl[1]))((ID3D12DSRDeviceFactory*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DSRDeviceFactory*, uint>)(lpVtbl[2]))((ID3D12DSRDeviceFactory*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public HRESULT CreateDSRDevice(ID3D12Device* pD3D12Device, uint NodeMask, [NativeTypeName("const IID &")] Guid* riid, void** ppvDSRDevice)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DSRDeviceFactory*, ID3D12Device*, uint, Guid*, void**, int>)(lpVtbl[3]))((ID3D12DSRDeviceFactory*)Unsafe.AsPointer(ref this), pD3D12Device, NodeMask, riid, ppvDSRDevice);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        HRESULT CreateDSRDevice(ID3D12Device* pD3D12Device, uint NodeMask, [NativeTypeName("const IID &")] Guid* riid, void** ppvDSRDevice);
    }
}
