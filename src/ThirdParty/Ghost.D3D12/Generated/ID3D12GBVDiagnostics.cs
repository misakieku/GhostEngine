using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("597985AB-9B75-4DBB-BE23-0761195BEBEE")]
[NativeTypeName("struct ID3D12GBVDiagnostics : IUnknown")]
public unsafe partial struct ID3D12GBVDiagnostics : ID3D12GBVDiagnostics.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12GBVDiagnostics);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12GBVDiagnostics*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12GBVDiagnostics*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12GBVDiagnostics*, uint>)(lpVtbl[1]))((ID3D12GBVDiagnostics*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12GBVDiagnostics*, uint>)(lpVtbl[2]))((ID3D12GBVDiagnostics*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int GetGBVEntireSubresourceStatesData(ID3D12Resource* pResource, int* pData, uint DataSize)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12GBVDiagnostics*, ID3D12Resource*, int*, uint, int>)(lpVtbl[3]))((ID3D12GBVDiagnostics*)Unsafe.AsPointer(ref this), pResource, pData, DataSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int GetGBVSubresourceState(ID3D12Resource* pResource, uint Subresource, int* pData)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12GBVDiagnostics*, ID3D12Resource*, uint, int*, int>)(lpVtbl[4]))((ID3D12GBVDiagnostics*)Unsafe.AsPointer(ref this), pResource, Subresource, pData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    [return: NativeTypeName("HRESULT")]
    public int GetGBVResourceUniformState(ID3D12Resource* pResource, int* pData)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12GBVDiagnostics*, ID3D12Resource*, int*, int>)(lpVtbl[5]))((ID3D12GBVDiagnostics*)Unsafe.AsPointer(ref this), pResource, pData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    [return: NativeTypeName("HRESULT")]
    public int GetGBVResourceInfo(ID3D12Resource* pResource, D3D12_RESOURCE_DESC* pResourceDesc, [NativeTypeName("UINT32 *")] uint* pResourceHash, [NativeTypeName("UINT32 *")] uint* pSubresourceStatesByteOffset)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12GBVDiagnostics*, ID3D12Resource*, D3D12_RESOURCE_DESC*, uint*, uint*, int>)(lpVtbl[6]))((ID3D12GBVDiagnostics*)Unsafe.AsPointer(ref this), pResource, pResourceDesc, pResourceHash, pSubresourceStatesByteOffset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    public void GBVReserved0()
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GBVDiagnostics*, void>)(lpVtbl[7]))((ID3D12GBVDiagnostics*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(8)]
    public void GBVReserved1()
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12GBVDiagnostics*, void>)(lpVtbl[8]))((ID3D12GBVDiagnostics*)Unsafe.AsPointer(ref this));
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        [return: NativeTypeName("HRESULT")]
        int GetGBVEntireSubresourceStatesData(ID3D12Resource* pResource, int* pData, uint DataSize);

        [VtblIndex(4)]
        [return: NativeTypeName("HRESULT")]
        int GetGBVSubresourceState(ID3D12Resource* pResource, uint Subresource, int* pData);

        [VtblIndex(5)]
        [return: NativeTypeName("HRESULT")]
        int GetGBVResourceUniformState(ID3D12Resource* pResource, int* pData);

        [VtblIndex(6)]
        [return: NativeTypeName("HRESULT")]
        int GetGBVResourceInfo(ID3D12Resource* pResource, D3D12_RESOURCE_DESC* pResourceDesc, [NativeTypeName("UINT32 *")] uint* pResourceHash, [NativeTypeName("UINT32 *")] uint* pSubresourceStatesByteOffset);

        [VtblIndex(7)]
        void GBVReserved0();

        [VtblIndex(8)]
        void GBVReserved1();
    }
}
