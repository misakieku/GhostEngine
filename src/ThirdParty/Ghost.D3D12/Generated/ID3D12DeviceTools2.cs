using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("0EDB4324-20A7-45ED-8016-C317A073BE33")]
[NativeTypeName("struct ID3D12DeviceTools2 : ID3D12DeviceTools1")]
public unsafe partial struct ID3D12DeviceTools2 : ID3D12DeviceTools2.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12DeviceTools2);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceTools2*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12DeviceTools2*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceTools2*, uint>)(lpVtbl[1]))((ID3D12DeviceTools2*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceTools2*, uint>)(lpVtbl[2]))((ID3D12DeviceTools2*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public void SetNextAllocationAddress([NativeTypeName("D3D12_GPU_VIRTUAL_ADDRESS")] ulong nextAllocationVirtualAddress)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12DeviceTools2*, ulong, void>)(lpVtbl[3]))((ID3D12DeviceTools2*)Unsafe.AsPointer(ref this), nextAllocationVirtualAddress);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    public HRESULT GetApplicationSpecificDriverState([NativeTypeName("ID3DBlob **")] ID3D10Blob** ppBlob)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceTools2*, ID3D10Blob**, int>)(lpVtbl[4]))((ID3D12DeviceTools2*)Unsafe.AsPointer(ref this), ppBlob);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    public D3D12_APPLICATION_SPECIFIC_DRIVER_BLOB_STATUS GetApplicationSpecificDriverBlobStatus()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceTools2*, D3D12_APPLICATION_SPECIFIC_DRIVER_BLOB_STATUS>)(lpVtbl[5]))((ID3D12DeviceTools2*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    public HRESULT SetUserDefinedAnnotationMode(D3D12_USER_DEFINED_ANNOTATION_MODE mode)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceTools2*, D3D12_USER_DEFINED_ANNOTATION_MODE, int>)(lpVtbl[6]))((ID3D12DeviceTools2*)Unsafe.AsPointer(ref this), mode);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    public D3D12_USER_DEFINED_ANNOTATION_MODE GetUserDefinedAnnotationMode()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceTools2*, D3D12_USER_DEFINED_ANNOTATION_MODE>)(lpVtbl[7]))((ID3D12DeviceTools2*)Unsafe.AsPointer(ref this));
    }

    public interface Interface : ID3D12DeviceTools1.Interface
    {
        [VtblIndex(6)]
        HRESULT SetUserDefinedAnnotationMode(D3D12_USER_DEFINED_ANNOTATION_MODE mode);

        [VtblIndex(7)]
        D3D12_USER_DEFINED_ANNOTATION_MODE GetUserDefinedAnnotationMode();
    }
}
