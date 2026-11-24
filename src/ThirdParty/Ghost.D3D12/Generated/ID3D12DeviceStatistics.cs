using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("3D5CA1A8-A39E-4619-95E0-F9B0A40340F5")]
[NativeTypeName("struct ID3D12DeviceStatistics : IUnknown")]
public unsafe partial struct ID3D12DeviceStatistics : ID3D12DeviceStatistics.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12DeviceStatistics);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceStatistics*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12DeviceStatistics*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceStatistics*, uint>)(lpVtbl[1]))((ID3D12DeviceStatistics*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceStatistics*, uint>)(lpVtbl[2]))((ID3D12DeviceStatistics*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public HRESULT GetStateObjectStatistics(D3D12_STATE_OBJECT_STATISTICS* pStatistics)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceStatistics*, D3D12_STATE_OBJECT_STATISTICS*, int>)(lpVtbl[3]))((ID3D12DeviceStatistics*)Unsafe.AsPointer(ref this), pStatistics);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        HRESULT GetStateObjectStatistics(D3D12_STATE_OBJECT_STATISTICS* pStatistics);
    }
}
