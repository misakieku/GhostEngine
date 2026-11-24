using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("ED342442-6343-4E16-BB82-A3A577874E56")]
[NativeTypeName("struct ID3D12DeviceConfiguration1 : ID3D12DeviceConfiguration")]
public unsafe partial struct ID3D12DeviceConfiguration1 : ID3D12DeviceConfiguration1.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12DeviceConfiguration1);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceConfiguration1*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12DeviceConfiguration1*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceConfiguration1*, uint>)(lpVtbl[1]))((ID3D12DeviceConfiguration1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceConfiguration1*, uint>)(lpVtbl[2]))((ID3D12DeviceConfiguration1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public D3D12_DEVICE_CONFIGURATION_DESC GetDesc()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceConfiguration1*, D3D12_DEVICE_CONFIGURATION_DESC>)(lpVtbl[3]))((ID3D12DeviceConfiguration1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int GetEnabledExperimentalFeatures(Guid* pGuids, uint NumGuids)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceConfiguration1*, Guid*, uint, int>)(lpVtbl[4]))((ID3D12DeviceConfiguration1*)Unsafe.AsPointer(ref this), pGuids, NumGuids);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    [return: NativeTypeName("HRESULT")]
    public int SerializeVersionedRootSignature([NativeTypeName("const D3D12_VERSIONED_ROOT_SIGNATURE_DESC *")] D3D12_VERSIONED_ROOT_SIGNATURE_DESC* pDesc, [NativeTypeName("ID3DBlob **")] ID3D10Blob** ppResult, [NativeTypeName("ID3DBlob **")] ID3D10Blob** ppError)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceConfiguration1*, D3D12_VERSIONED_ROOT_SIGNATURE_DESC*, ID3D10Blob**, ID3D10Blob**, int>)(lpVtbl[5]))((ID3D12DeviceConfiguration1*)Unsafe.AsPointer(ref this), pDesc, ppResult, ppError);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    [return: NativeTypeName("HRESULT")]
    public int CreateVersionedRootSignatureDeserializer([NativeTypeName("const void *")] void* pBlob, [NativeTypeName("SIZE_T")] nuint Size, [NativeTypeName("const IID &")] Guid* riid, void** ppvDeserializer)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceConfiguration1*, void*, nuint, Guid*, void**, int>)(lpVtbl[6]))((ID3D12DeviceConfiguration1*)Unsafe.AsPointer(ref this), pBlob, Size, riid, ppvDeserializer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    [return: NativeTypeName("HRESULT")]
    public int CreateVersionedRootSignatureDeserializerFromSubobjectInLibrary([NativeTypeName("const void *")] void* pLibraryBlob, [NativeTypeName("SIZE_T")] nuint Size, [NativeTypeName("LPCWSTR")] char* RootSignatureSubobjectName, [NativeTypeName("const IID &")] Guid* riid, void** ppvDeserializer)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceConfiguration1*, void*, nuint, char*, Guid*, void**, int>)(lpVtbl[7]))((ID3D12DeviceConfiguration1*)Unsafe.AsPointer(ref this), pLibraryBlob, Size, RootSignatureSubobjectName, riid, ppvDeserializer);
    }

    public interface Interface : ID3D12DeviceConfiguration.Interface
    {
        [VtblIndex(7)]
        [return: NativeTypeName("HRESULT")]
        int CreateVersionedRootSignatureDeserializerFromSubobjectInLibrary([NativeTypeName("const void *")] void* pLibraryBlob, [NativeTypeName("SIZE_T")] nuint Size, [NativeTypeName("LPCWSTR")] char* RootSignatureSubobjectName, [NativeTypeName("const IID &")] Guid* riid, void** ppvDeserializer);
    }
}
