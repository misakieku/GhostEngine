using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("DB82BFED-784D-40EA-AEBF-08BCD7F0A9E4")]
[NativeTypeName("struct ID3D12DeviceDumpFile : IUnknown")]
public unsafe partial struct ID3D12DeviceDumpFile : ID3D12DeviceDumpFile.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12DeviceDumpFile);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceDumpFile*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12DeviceDumpFile*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceDumpFile*, uint>)(lpVtbl[1]))((ID3D12DeviceDumpFile*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceDumpFile*, uint>)(lpVtbl[2]))((ID3D12DeviceDumpFile*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public HRESULT ConfigureDumpFile(D3D12_DUMP_FILE_DRIVER_OPTIONS driverOptions)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceDumpFile*, D3D12_DUMP_FILE_DRIVER_OPTIONS, int>)(lpVtbl[3]))((ID3D12DeviceDumpFile*)Unsafe.AsPointer(ref this), driverOptions);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    public void SetDumpFileCallbacks([NativeTypeName("D3D12_DUMPFILEBEGINCALLBACK")] delegate* unmanaged[Cdecl]<ulong, uint> pBeginCallback, [NativeTypeName("D3D12_DUMPFILEENDCALLBACK")] delegate* unmanaged[Cdecl]<char*, void> pEndCallback)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12DeviceDumpFile*, delegate* unmanaged[Cdecl]<ulong, uint>, delegate* unmanaged[Cdecl]<char*, void>, void>)(lpVtbl[4]))((ID3D12DeviceDumpFile*)Unsafe.AsPointer(ref this), pBeginCallback, pEndCallback);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    public HRESULT AddBlobToDumpFile(void* pBlob, uint sizeBytes, [NativeTypeName("UINT64")] ulong metadata)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceDumpFile*, void*, uint, ulong, int>)(lpVtbl[5]))((ID3D12DeviceDumpFile*)Unsafe.AsPointer(ref this), pBlob, sizeBytes, metadata);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    public void RetainDumpFile([NativeTypeName("BOOL")] int retain)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12DeviceDumpFile*, int, void>)(lpVtbl[6]))((ID3D12DeviceDumpFile*)Unsafe.AsPointer(ref this), retain);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    public D3D12_DEVICE_ERROR_CODE GetDeviceErrorCode()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceDumpFile*, D3D12_DEVICE_ERROR_CODE>)(lpVtbl[7]))((ID3D12DeviceDumpFile*)Unsafe.AsPointer(ref this));
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        HRESULT ConfigureDumpFile(D3D12_DUMP_FILE_DRIVER_OPTIONS driverOptions);

        [VtblIndex(4)]
        void SetDumpFileCallbacks([NativeTypeName("D3D12_DUMPFILEBEGINCALLBACK")] delegate* unmanaged[Cdecl]<ulong, uint> pBeginCallback, [NativeTypeName("D3D12_DUMPFILEENDCALLBACK")] delegate* unmanaged[Cdecl]<char*, void> pEndCallback);

        [VtblIndex(5)]
        HRESULT AddBlobToDumpFile(void* pBlob, uint sizeBytes, [NativeTypeName("UINT64")] ulong metadata);

        [VtblIndex(6)]
        void RetainDumpFile([NativeTypeName("BOOL")] int retain);

        [VtblIndex(7)]
        D3D12_DEVICE_ERROR_CODE GetDeviceErrorCode();
    }
}
