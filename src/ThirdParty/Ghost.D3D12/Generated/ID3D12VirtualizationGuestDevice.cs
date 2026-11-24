using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("BC66D368-7373-4943-8757-FC87DC79E476")]
[NativeTypeName("struct ID3D12VirtualizationGuestDevice : IUnknown")]
public unsafe partial struct ID3D12VirtualizationGuestDevice : ID3D12VirtualizationGuestDevice.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12VirtualizationGuestDevice);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12VirtualizationGuestDevice*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12VirtualizationGuestDevice*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12VirtualizationGuestDevice*, uint>)(lpVtbl[1]))((ID3D12VirtualizationGuestDevice*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12VirtualizationGuestDevice*, uint>)(lpVtbl[2]))((ID3D12VirtualizationGuestDevice*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int ShareWithHost(ID3D12DeviceChild* pObject, [NativeTypeName("HANDLE *")] void** pHandle)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12VirtualizationGuestDevice*, ID3D12DeviceChild*, void**, int>)(lpVtbl[3]))((ID3D12VirtualizationGuestDevice*)Unsafe.AsPointer(ref this), pObject, pHandle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int CreateFenceFd(ID3D12Fence* pFence, [NativeTypeName("UINT64")] ulong FenceValue, int* pFenceFd)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12VirtualizationGuestDevice*, ID3D12Fence*, ulong, int*, int>)(lpVtbl[4]))((ID3D12VirtualizationGuestDevice*)Unsafe.AsPointer(ref this), pFence, FenceValue, pFenceFd);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        [return: NativeTypeName("HRESULT")]
        int ShareWithHost(ID3D12DeviceChild* pObject, [NativeTypeName("HANDLE *")] void** pHandle);

        [VtblIndex(4)]
        [return: NativeTypeName("HRESULT")]
        int CreateFenceFd(ID3D12Fence* pFence, [NativeTypeName("UINT64")] ulong FenceValue, int* pFenceFd);
    }
}
