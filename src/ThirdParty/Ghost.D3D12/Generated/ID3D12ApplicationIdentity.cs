using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("82DC6C85-727B-4A8D-9169-DB6CE3E975A0")]
[NativeTypeName("struct ID3D12ApplicationIdentity : IUnknown")]
public unsafe partial struct ID3D12ApplicationIdentity : ID3D12ApplicationIdentity.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12ApplicationIdentity);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12ApplicationIdentity*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12ApplicationIdentity*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12ApplicationIdentity*, uint>)(lpVtbl[1]))((ID3D12ApplicationIdentity*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12ApplicationIdentity*, uint>)(lpVtbl[2]))((ID3D12ApplicationIdentity*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int SetApplicationIdentity([NativeTypeName("const D3D12_APPLICATION_DESC *")] D3D12_APPLICATION_DESC* pDesc, [NativeTypeName("const GUID &")] Guid* AppId)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12ApplicationIdentity*, D3D12_APPLICATION_DESC*, Guid*, int>)(lpVtbl[3]))((ID3D12ApplicationIdentity*)Unsafe.AsPointer(ref this), pDesc, AppId);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        [return: NativeTypeName("HRESULT")]
        int SetApplicationIdentity([NativeTypeName("const D3D12_APPLICATION_DESC *")] D3D12_APPLICATION_DESC* pDesc, [NativeTypeName("const GUID &")] Guid* AppId);
    }
}
