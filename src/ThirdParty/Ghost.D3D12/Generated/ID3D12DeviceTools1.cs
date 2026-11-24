using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("E30E9FC7-E641-4D6E-8A81-9DD9206EC47A")]
[NativeTypeName("struct ID3D12DeviceTools1 : ID3D12DeviceTools")]
public unsafe partial struct ID3D12DeviceTools1 : ID3D12DeviceTools1.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12DeviceTools1);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceTools1*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12DeviceTools1*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceTools1*, uint>)(lpVtbl[1]))((ID3D12DeviceTools1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceTools1*, uint>)(lpVtbl[2]))((ID3D12DeviceTools1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public void SetNextAllocationAddress([NativeTypeName("D3D12_GPU_VIRTUAL_ADDRESS")] ulong nextAllocationVirtualAddress)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12DeviceTools1*, ulong, void>)(lpVtbl[3]))((ID3D12DeviceTools1*)Unsafe.AsPointer(ref this), nextAllocationVirtualAddress);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int GetApplicationSpecificDriverState([NativeTypeName("ID3DBlob **")] ID3D10Blob** ppBlob)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceTools1*, ID3D10Blob**, int>)(lpVtbl[4]))((ID3D12DeviceTools1*)Unsafe.AsPointer(ref this), ppBlob);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    public D3D12_APPLICATION_SPECIFIC_DRIVER_BLOB_STATUS GetApplicationSpecificDriverBlobStatus()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceTools1*, D3D12_APPLICATION_SPECIFIC_DRIVER_BLOB_STATUS>)(lpVtbl[5]))((ID3D12DeviceTools1*)Unsafe.AsPointer(ref this));
    }

    public interface Interface : ID3D12DeviceTools.Interface
    {
        [VtblIndex(4)]
        [return: NativeTypeName("HRESULT")]
        int GetApplicationSpecificDriverState([NativeTypeName("ID3DBlob **")] ID3D10Blob** ppBlob);

        [VtblIndex(5)]
        D3D12_APPLICATION_SPECIFIC_DRIVER_BLOB_STATUS GetApplicationSpecificDriverBlobStatus();
    }
}
