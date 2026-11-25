using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("86CA3B85-49AD-4B6E-AED5-EDDB18540F41")]
[NativeTypeName("struct ID3D12ManualWriteTrackingResource : IUnknown")]
public unsafe partial struct ID3D12ManualWriteTrackingResource : ID3D12ManualWriteTrackingResource.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12ManualWriteTrackingResource);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12ManualWriteTrackingResource*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12ManualWriteTrackingResource*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12ManualWriteTrackingResource*, uint>)(lpVtbl[1]))((ID3D12ManualWriteTrackingResource*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12ManualWriteTrackingResource*, uint>)(lpVtbl[2]))((ID3D12ManualWriteTrackingResource*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public void TrackWrite(uint Subresource, [NativeTypeName("const D3D12_RANGE *")] D3D12_RANGE* pWrittenRange)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12ManualWriteTrackingResource*, uint, D3D12_RANGE*, void>)(lpVtbl[3]))((ID3D12ManualWriteTrackingResource*)Unsafe.AsPointer(ref this), Subresource, pWrittenRange);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        void TrackWrite(uint Subresource, [NativeTypeName("const D3D12_RANGE *")] D3D12_RANGE* pWrittenRange);
    }
}
