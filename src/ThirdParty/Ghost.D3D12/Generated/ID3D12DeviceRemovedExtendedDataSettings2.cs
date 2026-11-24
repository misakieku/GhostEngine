using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("61552388-01AB-4008-A436-83DB189566EA")]
[NativeTypeName("struct ID3D12DeviceRemovedExtendedDataSettings2 : ID3D12DeviceRemovedExtendedDataSettings1")]
public unsafe partial struct ID3D12DeviceRemovedExtendedDataSettings2 : ID3D12DeviceRemovedExtendedDataSettings2.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12DeviceRemovedExtendedDataSettings2);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceRemovedExtendedDataSettings2*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12DeviceRemovedExtendedDataSettings2*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceRemovedExtendedDataSettings2*, uint>)(lpVtbl[1]))((ID3D12DeviceRemovedExtendedDataSettings2*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceRemovedExtendedDataSettings2*, uint>)(lpVtbl[2]))((ID3D12DeviceRemovedExtendedDataSettings2*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public void SetAutoBreadcrumbsEnablement(D3D12_DRED_ENABLEMENT Enablement)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12DeviceRemovedExtendedDataSettings2*, D3D12_DRED_ENABLEMENT, void>)(lpVtbl[3]))((ID3D12DeviceRemovedExtendedDataSettings2*)Unsafe.AsPointer(ref this), Enablement);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    public void SetPageFaultEnablement(D3D12_DRED_ENABLEMENT Enablement)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12DeviceRemovedExtendedDataSettings2*, D3D12_DRED_ENABLEMENT, void>)(lpVtbl[4]))((ID3D12DeviceRemovedExtendedDataSettings2*)Unsafe.AsPointer(ref this), Enablement);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    public void SetWatsonDumpEnablement(D3D12_DRED_ENABLEMENT Enablement)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12DeviceRemovedExtendedDataSettings2*, D3D12_DRED_ENABLEMENT, void>)(lpVtbl[5]))((ID3D12DeviceRemovedExtendedDataSettings2*)Unsafe.AsPointer(ref this), Enablement);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    public void SetBreadcrumbContextEnablement(D3D12_DRED_ENABLEMENT Enablement)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12DeviceRemovedExtendedDataSettings2*, D3D12_DRED_ENABLEMENT, void>)(lpVtbl[6]))((ID3D12DeviceRemovedExtendedDataSettings2*)Unsafe.AsPointer(ref this), Enablement);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    public void UseMarkersOnlyAutoBreadcrumbs([NativeTypeName("BOOL")] int MarkersOnly)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12DeviceRemovedExtendedDataSettings2*, int, void>)(lpVtbl[7]))((ID3D12DeviceRemovedExtendedDataSettings2*)Unsafe.AsPointer(ref this), MarkersOnly);
    }

    public interface Interface : ID3D12DeviceRemovedExtendedDataSettings1.Interface
    {
        [VtblIndex(7)]
        void UseMarkersOnlyAutoBreadcrumbs([NativeTypeName("BOOL")] int MarkersOnly);
    }
}
