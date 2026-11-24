using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("09E0BF36-54AC-484F-8847-4BAEEAB6053F")]
[NativeTypeName("struct ID3D12DebugCommandList : IUnknown")]
public unsafe partial struct ID3D12DebugCommandList : ID3D12DebugCommandList.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12DebugCommandList);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DebugCommandList*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12DebugCommandList*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DebugCommandList*, uint>)(lpVtbl[1]))((ID3D12DebugCommandList*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DebugCommandList*, uint>)(lpVtbl[2]))((ID3D12DebugCommandList*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("BOOL")]
    public int AssertResourceState(ID3D12Resource* pResource, uint Subresource, uint State)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DebugCommandList*, ID3D12Resource*, uint, uint, int>)(lpVtbl[3]))((ID3D12DebugCommandList*)Unsafe.AsPointer(ref this), pResource, Subresource, State);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int SetFeatureMask(D3D12_DEBUG_FEATURE Mask)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DebugCommandList*, D3D12_DEBUG_FEATURE, int>)(lpVtbl[4]))((ID3D12DebugCommandList*)Unsafe.AsPointer(ref this), Mask);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    public D3D12_DEBUG_FEATURE GetFeatureMask()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DebugCommandList*, D3D12_DEBUG_FEATURE>)(lpVtbl[5]))((ID3D12DebugCommandList*)Unsafe.AsPointer(ref this));
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        [return: NativeTypeName("BOOL")]
        int AssertResourceState(ID3D12Resource* pResource, uint Subresource, uint State);

        [VtblIndex(4)]
        [return: NativeTypeName("HRESULT")]
        int SetFeatureMask(D3D12_DEBUG_FEATURE Mask);

        [VtblIndex(5)]
        D3D12_DEBUG_FEATURE GetFeatureMask();
    }
}
