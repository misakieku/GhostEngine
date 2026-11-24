using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("A9B71770-D099-4A65-A698-3DEE10020F88")]
[NativeTypeName("struct ID3D12DebugDevice1 : IUnknown")]
public unsafe partial struct ID3D12DebugDevice1 : ID3D12DebugDevice1.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12DebugDevice1);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DebugDevice1*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12DebugDevice1*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DebugDevice1*, uint>)(lpVtbl[1]))((ID3D12DebugDevice1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DebugDevice1*, uint>)(lpVtbl[2]))((ID3D12DebugDevice1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int SetDebugParameter(D3D12_DEBUG_DEVICE_PARAMETER_TYPE Type, [NativeTypeName("const void *")] void* pData, uint DataSize)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DebugDevice1*, D3D12_DEBUG_DEVICE_PARAMETER_TYPE, void*, uint, int>)(lpVtbl[3]))((ID3D12DebugDevice1*)Unsafe.AsPointer(ref this), Type, pData, DataSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int GetDebugParameter(D3D12_DEBUG_DEVICE_PARAMETER_TYPE Type, void* pData, uint DataSize)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DebugDevice1*, D3D12_DEBUG_DEVICE_PARAMETER_TYPE, void*, uint, int>)(lpVtbl[4]))((ID3D12DebugDevice1*)Unsafe.AsPointer(ref this), Type, pData, DataSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    [return: NativeTypeName("HRESULT")]
    public int ReportLiveDeviceObjects(D3D12_RLDO_FLAGS Flags)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DebugDevice1*, D3D12_RLDO_FLAGS, int>)(lpVtbl[5]))((ID3D12DebugDevice1*)Unsafe.AsPointer(ref this), Flags);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        [return: NativeTypeName("HRESULT")]
        int SetDebugParameter(D3D12_DEBUG_DEVICE_PARAMETER_TYPE Type, [NativeTypeName("const void *")] void* pData, uint DataSize);

        [VtblIndex(4)]
        [return: NativeTypeName("HRESULT")]
        int GetDebugParameter(D3D12_DEBUG_DEVICE_PARAMETER_TYPE Type, void* pData, uint DataSize);

        [VtblIndex(5)]
        [return: NativeTypeName("HRESULT")]
        int ReportLiveDeviceObjects(D3D12_RLDO_FLAGS Flags);
    }
}
