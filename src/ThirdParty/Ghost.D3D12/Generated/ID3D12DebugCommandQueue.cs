using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("09E0BF36-54AC-484F-8847-4BAEEAB6053A")]
[NativeTypeName("struct ID3D12DebugCommandQueue : IUnknown")]
public unsafe partial struct ID3D12DebugCommandQueue : ID3D12DebugCommandQueue.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12DebugCommandQueue);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DebugCommandQueue*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12DebugCommandQueue*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DebugCommandQueue*, uint>)(lpVtbl[1]))((ID3D12DebugCommandQueue*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DebugCommandQueue*, uint>)(lpVtbl[2]))((ID3D12DebugCommandQueue*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("BOOL")]
    public int AssertResourceState(ID3D12Resource* pResource, uint Subresource, uint State)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DebugCommandQueue*, ID3D12Resource*, uint, uint, int>)(lpVtbl[3]))((ID3D12DebugCommandQueue*)Unsafe.AsPointer(ref this), pResource, Subresource, State);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        [return: NativeTypeName("BOOL")]
        int AssertResourceState(ID3D12Resource* pResource, uint Subresource, uint State);
    }
}
