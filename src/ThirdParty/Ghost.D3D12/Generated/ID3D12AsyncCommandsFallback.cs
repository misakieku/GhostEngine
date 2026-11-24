using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("FBDDF9E8-33A3-488C-9775-1A970DC54862")]
[NativeTypeName("struct ID3D12AsyncCommandsFallback : IUnknown")]
public unsafe partial struct ID3D12AsyncCommandsFallback : ID3D12AsyncCommandsFallback.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12AsyncCommandsFallback);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12AsyncCommandsFallback*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12AsyncCommandsFallback*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12AsyncCommandsFallback*, uint>)(lpVtbl[1]))((ID3D12AsyncCommandsFallback*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12AsyncCommandsFallback*, uint>)(lpVtbl[2]))((ID3D12AsyncCommandsFallback*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int SetFallbackLayerEnabled([NativeTypeName("BOOL")] int Enable)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12AsyncCommandsFallback*, int, int>)(lpVtbl[3]))((ID3D12AsyncCommandsFallback*)Unsafe.AsPointer(ref this), Enable);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("BOOL")]
    public int GetFallbackLayerEnabled()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12AsyncCommandsFallback*, int>)(lpVtbl[4]))((ID3D12AsyncCommandsFallback*)Unsafe.AsPointer(ref this));
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        [return: NativeTypeName("HRESULT")]
        int SetFallbackLayerEnabled([NativeTypeName("BOOL")] int Enable);

        [VtblIndex(4)]
        [return: NativeTypeName("BOOL")]
        int GetFallbackLayerEnabled();
    }
}
