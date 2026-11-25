using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("DBD5AE51-3317-4F0A-ADF9-1D7CEDCAAE0B")]
[NativeTypeName("struct ID3D12DeviceRemovedExtendedDataSettings1 : ID3D12DeviceRemovedExtendedDataSettings")]
public unsafe partial struct ID3D12DeviceRemovedExtendedDataSettings1 : ID3D12DeviceRemovedExtendedDataSettings1.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12DeviceRemovedExtendedDataSettings1);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceRemovedExtendedDataSettings1*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12DeviceRemovedExtendedDataSettings1*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceRemovedExtendedDataSettings1*, uint>)(lpVtbl[1]))((ID3D12DeviceRemovedExtendedDataSettings1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceRemovedExtendedDataSettings1*, uint>)(lpVtbl[2]))((ID3D12DeviceRemovedExtendedDataSettings1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public void SetAutoBreadcrumbsEnablement(D3D12_DRED_ENABLEMENT Enablement)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12DeviceRemovedExtendedDataSettings1*, D3D12_DRED_ENABLEMENT, void>)(lpVtbl[3]))((ID3D12DeviceRemovedExtendedDataSettings1*)Unsafe.AsPointer(ref this), Enablement);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    public void SetPageFaultEnablement(D3D12_DRED_ENABLEMENT Enablement)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12DeviceRemovedExtendedDataSettings1*, D3D12_DRED_ENABLEMENT, void>)(lpVtbl[4]))((ID3D12DeviceRemovedExtendedDataSettings1*)Unsafe.AsPointer(ref this), Enablement);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    public void SetWatsonDumpEnablement(D3D12_DRED_ENABLEMENT Enablement)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12DeviceRemovedExtendedDataSettings1*, D3D12_DRED_ENABLEMENT, void>)(lpVtbl[5]))((ID3D12DeviceRemovedExtendedDataSettings1*)Unsafe.AsPointer(ref this), Enablement);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    public void SetBreadcrumbContextEnablement(D3D12_DRED_ENABLEMENT Enablement)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12DeviceRemovedExtendedDataSettings1*, D3D12_DRED_ENABLEMENT, void>)(lpVtbl[6]))((ID3D12DeviceRemovedExtendedDataSettings1*)Unsafe.AsPointer(ref this), Enablement);
    }

    public interface Interface : ID3D12DeviceRemovedExtendedDataSettings.Interface
    {
        [VtblIndex(6)]
        void SetBreadcrumbContextEnablement(D3D12_DRED_ENABLEMENT Enablement);
    }
}
