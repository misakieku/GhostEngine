using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("82BC481C-6B9B-4030-AEDB-7EE3D1DF1E63")]
[NativeTypeName("struct ID3D12DeviceRemovedExtendedDataSettings : IUnknown")]
public unsafe partial struct ID3D12DeviceRemovedExtendedDataSettings : ID3D12DeviceRemovedExtendedDataSettings.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12DeviceRemovedExtendedDataSettings);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceRemovedExtendedDataSettings*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12DeviceRemovedExtendedDataSettings*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceRemovedExtendedDataSettings*, uint>)(lpVtbl[1]))((ID3D12DeviceRemovedExtendedDataSettings*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceRemovedExtendedDataSettings*, uint>)(lpVtbl[2]))((ID3D12DeviceRemovedExtendedDataSettings*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public void SetAutoBreadcrumbsEnablement(D3D12_DRED_ENABLEMENT Enablement)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12DeviceRemovedExtendedDataSettings*, D3D12_DRED_ENABLEMENT, void>)(lpVtbl[3]))((ID3D12DeviceRemovedExtendedDataSettings*)Unsafe.AsPointer(ref this), Enablement);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    public void SetPageFaultEnablement(D3D12_DRED_ENABLEMENT Enablement)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12DeviceRemovedExtendedDataSettings*, D3D12_DRED_ENABLEMENT, void>)(lpVtbl[4]))((ID3D12DeviceRemovedExtendedDataSettings*)Unsafe.AsPointer(ref this), Enablement);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    public void SetWatsonDumpEnablement(D3D12_DRED_ENABLEMENT Enablement)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12DeviceRemovedExtendedDataSettings*, D3D12_DRED_ENABLEMENT, void>)(lpVtbl[5]))((ID3D12DeviceRemovedExtendedDataSettings*)Unsafe.AsPointer(ref this), Enablement);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        void SetAutoBreadcrumbsEnablement(D3D12_DRED_ENABLEMENT Enablement);

        [VtblIndex(4)]
        void SetPageFaultEnablement(D3D12_DRED_ENABLEMENT Enablement);

        [VtblIndex(5)]
        void SetWatsonDumpEnablement(D3D12_DRED_ENABLEMENT Enablement);
    }
}
