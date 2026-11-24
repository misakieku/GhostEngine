using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("102CA951-311B-4B01-B11F-ECB83E061B37")]
[NativeTypeName("struct ID3D12DebugCommandList1 : IUnknown")]
public unsafe partial struct ID3D12DebugCommandList1 : ID3D12DebugCommandList1.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12DebugCommandList1);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DebugCommandList1*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12DebugCommandList1*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DebugCommandList1*, uint>)(lpVtbl[1]))((ID3D12DebugCommandList1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DebugCommandList1*, uint>)(lpVtbl[2]))((ID3D12DebugCommandList1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("BOOL")]
    public int AssertResourceState(ID3D12Resource* pResource, uint Subresource, uint State)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DebugCommandList1*, ID3D12Resource*, uint, uint, int>)(lpVtbl[3]))((ID3D12DebugCommandList1*)Unsafe.AsPointer(ref this), pResource, Subresource, State);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int SetDebugParameter(D3D12_DEBUG_COMMAND_LIST_PARAMETER_TYPE Type, [NativeTypeName("const void *")] void* pData, uint DataSize)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DebugCommandList1*, D3D12_DEBUG_COMMAND_LIST_PARAMETER_TYPE, void*, uint, int>)(lpVtbl[4]))((ID3D12DebugCommandList1*)Unsafe.AsPointer(ref this), Type, pData, DataSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    [return: NativeTypeName("HRESULT")]
    public int GetDebugParameter(D3D12_DEBUG_COMMAND_LIST_PARAMETER_TYPE Type, void* pData, uint DataSize)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DebugCommandList1*, D3D12_DEBUG_COMMAND_LIST_PARAMETER_TYPE, void*, uint, int>)(lpVtbl[5]))((ID3D12DebugCommandList1*)Unsafe.AsPointer(ref this), Type, pData, DataSize);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        [return: NativeTypeName("BOOL")]
        int AssertResourceState(ID3D12Resource* pResource, uint Subresource, uint State);

        [VtblIndex(4)]
        [return: NativeTypeName("HRESULT")]
        int SetDebugParameter(D3D12_DEBUG_COMMAND_LIST_PARAMETER_TYPE Type, [NativeTypeName("const void *")] void* pData, uint DataSize);

        [VtblIndex(5)]
        [return: NativeTypeName("HRESULT")]
        int GetDebugParameter(D3D12_DEBUG_COMMAND_LIST_PARAMETER_TYPE Type, void* pData, uint DataSize);
    }
}
