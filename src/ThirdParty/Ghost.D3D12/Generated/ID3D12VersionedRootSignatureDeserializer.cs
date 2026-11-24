using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("7F91CE67-090C-4BB7-B78E-ED8FF2E31DA0")]
[NativeTypeName("struct ID3D12VersionedRootSignatureDeserializer : IUnknown")]
public unsafe partial struct ID3D12VersionedRootSignatureDeserializer : ID3D12VersionedRootSignatureDeserializer.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12VersionedRootSignatureDeserializer);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12VersionedRootSignatureDeserializer*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12VersionedRootSignatureDeserializer*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12VersionedRootSignatureDeserializer*, uint>)(lpVtbl[1]))((ID3D12VersionedRootSignatureDeserializer*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12VersionedRootSignatureDeserializer*, uint>)(lpVtbl[2]))((ID3D12VersionedRootSignatureDeserializer*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int GetRootSignatureDescAtVersion(D3D_ROOT_SIGNATURE_VERSION convertToVersion, [NativeTypeName("const D3D12_VERSIONED_ROOT_SIGNATURE_DESC **")] D3D12_VERSIONED_ROOT_SIGNATURE_DESC** ppDesc)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12VersionedRootSignatureDeserializer*, D3D_ROOT_SIGNATURE_VERSION, D3D12_VERSIONED_ROOT_SIGNATURE_DESC**, int>)(lpVtbl[3]))((ID3D12VersionedRootSignatureDeserializer*)Unsafe.AsPointer(ref this), convertToVersion, ppDesc);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("const D3D12_VERSIONED_ROOT_SIGNATURE_DESC *")]
    public D3D12_VERSIONED_ROOT_SIGNATURE_DESC* GetUnconvertedRootSignatureDesc()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12VersionedRootSignatureDeserializer*, D3D12_VERSIONED_ROOT_SIGNATURE_DESC*>)(lpVtbl[4]))((ID3D12VersionedRootSignatureDeserializer*)Unsafe.AsPointer(ref this));
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        [return: NativeTypeName("HRESULT")]
        int GetRootSignatureDescAtVersion(D3D_ROOT_SIGNATURE_VERSION convertToVersion, [NativeTypeName("const D3D12_VERSIONED_ROOT_SIGNATURE_DESC **")] D3D12_VERSIONED_ROOT_SIGNATURE_DESC** ppDesc);

        [VtblIndex(4)]
        [return: NativeTypeName("const D3D12_VERSIONED_ROOT_SIGNATURE_DESC *")]
        D3D12_VERSIONED_ROOT_SIGNATURE_DESC* GetUnconvertedRootSignatureDesc();
    }
}
