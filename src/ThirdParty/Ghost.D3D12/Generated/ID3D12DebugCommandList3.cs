using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("197D5E15-4D37-4D34-AF78-724CD70FDB1F")]
[NativeTypeName("struct ID3D12DebugCommandList3 : ID3D12DebugCommandList2")]
public unsafe partial struct ID3D12DebugCommandList3 : ID3D12DebugCommandList3.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12DebugCommandList3);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DebugCommandList3*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12DebugCommandList3*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DebugCommandList3*, uint>)(lpVtbl[1]))((ID3D12DebugCommandList3*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DebugCommandList3*, uint>)(lpVtbl[2]))((ID3D12DebugCommandList3*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("BOOL")]
    public int AssertResourceState(ID3D12Resource* pResource, uint Subresource, uint State)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DebugCommandList3*, ID3D12Resource*, uint, uint, int>)(lpVtbl[3]))((ID3D12DebugCommandList3*)Unsafe.AsPointer(ref this), pResource, Subresource, State);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    public HRESULT SetFeatureMask(D3D12_DEBUG_FEATURE Mask)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DebugCommandList3*, D3D12_DEBUG_FEATURE, int>)(lpVtbl[4]))((ID3D12DebugCommandList3*)Unsafe.AsPointer(ref this), Mask);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    public D3D12_DEBUG_FEATURE GetFeatureMask()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DebugCommandList3*, D3D12_DEBUG_FEATURE>)(lpVtbl[5]))((ID3D12DebugCommandList3*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    public HRESULT SetDebugParameter(D3D12_DEBUG_COMMAND_LIST_PARAMETER_TYPE Type, [NativeTypeName("const void *")] void* pData, uint DataSize)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DebugCommandList3*, D3D12_DEBUG_COMMAND_LIST_PARAMETER_TYPE, void*, uint, int>)(lpVtbl[6]))((ID3D12DebugCommandList3*)Unsafe.AsPointer(ref this), Type, pData, DataSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    public HRESULT GetDebugParameter(D3D12_DEBUG_COMMAND_LIST_PARAMETER_TYPE Type, void* pData, uint DataSize)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DebugCommandList3*, D3D12_DEBUG_COMMAND_LIST_PARAMETER_TYPE, void*, uint, int>)(lpVtbl[7]))((ID3D12DebugCommandList3*)Unsafe.AsPointer(ref this), Type, pData, DataSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(8)]
    public void AssertResourceAccess(ID3D12Resource* pResource, uint Subresource, D3D12_BARRIER_ACCESS Access)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12DebugCommandList3*, ID3D12Resource*, uint, D3D12_BARRIER_ACCESS, void>)(lpVtbl[8]))((ID3D12DebugCommandList3*)Unsafe.AsPointer(ref this), pResource, Subresource, Access);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(9)]
    public void AssertTextureLayout(ID3D12Resource* pResource, uint Subresource, D3D12_BARRIER_LAYOUT Layout)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12DebugCommandList3*, ID3D12Resource*, uint, D3D12_BARRIER_LAYOUT, void>)(lpVtbl[9]))((ID3D12DebugCommandList3*)Unsafe.AsPointer(ref this), pResource, Subresource, Layout);
    }

    public interface Interface : ID3D12DebugCommandList2.Interface
    {
        [VtblIndex(8)]
        void AssertResourceAccess(ID3D12Resource* pResource, uint Subresource, D3D12_BARRIER_ACCESS Access);

        [VtblIndex(9)]
        void AssertTextureLayout(ID3D12Resource* pResource, uint Subresource, D3D12_BARRIER_LAYOUT Layout);
    }
}
