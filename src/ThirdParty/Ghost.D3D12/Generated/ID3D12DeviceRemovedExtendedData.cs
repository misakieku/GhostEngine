using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("98931D33-5AE8-4791-AA3C-1A73A2934E71")]
[NativeTypeName("struct ID3D12DeviceRemovedExtendedData : IUnknown")]
public unsafe partial struct ID3D12DeviceRemovedExtendedData : ID3D12DeviceRemovedExtendedData.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12DeviceRemovedExtendedData);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceRemovedExtendedData*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12DeviceRemovedExtendedData*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceRemovedExtendedData*, uint>)(lpVtbl[1]))((ID3D12DeviceRemovedExtendedData*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceRemovedExtendedData*, uint>)(lpVtbl[2]))((ID3D12DeviceRemovedExtendedData*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int GetAutoBreadcrumbsOutput(D3D12_DRED_AUTO_BREADCRUMBS_OUTPUT* pOutput)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceRemovedExtendedData*, D3D12_DRED_AUTO_BREADCRUMBS_OUTPUT*, int>)(lpVtbl[3]))((ID3D12DeviceRemovedExtendedData*)Unsafe.AsPointer(ref this), pOutput);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int GetPageFaultAllocationOutput(D3D12_DRED_PAGE_FAULT_OUTPUT* pOutput)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceRemovedExtendedData*, D3D12_DRED_PAGE_FAULT_OUTPUT*, int>)(lpVtbl[4]))((ID3D12DeviceRemovedExtendedData*)Unsafe.AsPointer(ref this), pOutput);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        [return: NativeTypeName("HRESULT")]
        int GetAutoBreadcrumbsOutput(D3D12_DRED_AUTO_BREADCRUMBS_OUTPUT* pOutput);

        [VtblIndex(4)]
        [return: NativeTypeName("HRESULT")]
        int GetPageFaultAllocationOutput(D3D12_DRED_PAGE_FAULT_OUTPUT* pOutput);
    }
}
