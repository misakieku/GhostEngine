using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("A7C3F1E8-2B94-4D06-9F5A-E8D1C40B7263")]
[NativeTypeName("struct ID3D12ResourceOpaqueTextureLayout : IUnknown")]
public unsafe partial struct ID3D12ResourceOpaqueTextureLayout : ID3D12ResourceOpaqueTextureLayout.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12ResourceOpaqueTextureLayout);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12ResourceOpaqueTextureLayout*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12ResourceOpaqueTextureLayout*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12ResourceOpaqueTextureLayout*, uint>)(lpVtbl[1]))((ID3D12ResourceOpaqueTextureLayout*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12ResourceOpaqueTextureLayout*, uint>)(lpVtbl[2]))((ID3D12ResourceOpaqueTextureLayout*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int GetOpaqueTextureRowMajorLayoutInfo(uint FirstSubresource, uint NumSubresources, D3D12_OPAQUE_TEXTURE_ROW_MAJOR_LAYOUT_INFO* pLayoutInfo)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12ResourceOpaqueTextureLayout*, uint, uint, D3D12_OPAQUE_TEXTURE_ROW_MAJOR_LAYOUT_INFO*, int>)(lpVtbl[3]))((ID3D12ResourceOpaqueTextureLayout*)Unsafe.AsPointer(ref this), FirstSubresource, NumSubresources, pLayoutInfo);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        [return: NativeTypeName("HRESULT")]
        int GetOpaqueTextureRowMajorLayoutInfo(uint FirstSubresource, uint NumSubresources, D3D12_OPAQUE_TEXTURE_ROW_MAJOR_LAYOUT_INFO* pLayoutInfo);
    }
}
