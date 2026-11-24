using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("16BE35A2-BFD6-49F2-BCAE-EAAE4AFF862D")]
[NativeTypeName("struct ID3D12DebugCommandQueue1 : ID3D12DebugCommandQueue")]
public unsafe partial struct ID3D12DebugCommandQueue1 : ID3D12DebugCommandQueue1.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12DebugCommandQueue1);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DebugCommandQueue1*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12DebugCommandQueue1*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DebugCommandQueue1*, uint>)(lpVtbl[1]))((ID3D12DebugCommandQueue1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DebugCommandQueue1*, uint>)(lpVtbl[2]))((ID3D12DebugCommandQueue1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("BOOL")]
    public int AssertResourceState(ID3D12Resource* pResource, uint Subresource, uint State)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DebugCommandQueue1*, ID3D12Resource*, uint, uint, int>)(lpVtbl[3]))((ID3D12DebugCommandQueue1*)Unsafe.AsPointer(ref this), pResource, Subresource, State);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    public void AssertResourceAccess(ID3D12Resource* pResource, uint Subresource, D3D12_BARRIER_ACCESS Access)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12DebugCommandQueue1*, ID3D12Resource*, uint, D3D12_BARRIER_ACCESS, void>)(lpVtbl[4]))((ID3D12DebugCommandQueue1*)Unsafe.AsPointer(ref this), pResource, Subresource, Access);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    public void AssertTextureLayout(ID3D12Resource* pResource, uint Subresource, D3D12_BARRIER_LAYOUT Layout)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12DebugCommandQueue1*, ID3D12Resource*, uint, D3D12_BARRIER_LAYOUT, void>)(lpVtbl[5]))((ID3D12DebugCommandQueue1*)Unsafe.AsPointer(ref this), pResource, Subresource, Layout);
    }

    public interface Interface : ID3D12DebugCommandQueue.Interface
    {
        [VtblIndex(4)]
        void AssertResourceAccess(ID3D12Resource* pResource, uint Subresource, D3D12_BARRIER_ACCESS Access);

        [VtblIndex(5)]
        void AssertTextureLayout(ID3D12Resource* pResource, uint Subresource, D3D12_BARRIER_LAYOUT Layout);
    }
}
