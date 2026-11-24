using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("E4FBC019-DD3C-43E1-8F32-7F649575F0A0")]
[NativeTypeName("struct ID3D12Tools1 : ID3D12Tools")]
public unsafe partial struct ID3D12Tools1 : ID3D12Tools1.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12Tools1);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12Tools1*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12Tools1*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12Tools1*, uint>)(lpVtbl[1]))((ID3D12Tools1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12Tools1*, uint>)(lpVtbl[2]))((ID3D12Tools1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public void EnableShaderInstrumentation([NativeTypeName("BOOL")] int bEnable)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12Tools1*, int, void>)(lpVtbl[3]))((ID3D12Tools1*)Unsafe.AsPointer(ref this), bEnable);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("BOOL")]
    public int ShaderInstrumentationEnabled()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12Tools1*, int>)(lpVtbl[4]))((ID3D12Tools1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    public HRESULT ReserveGPUVARangesAtCreate(D3D12_GPU_VIRTUAL_ADDRESS_RANGE* pRanges, uint uiNumRanges)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12Tools1*, D3D12_GPU_VIRTUAL_ADDRESS_RANGE*, uint, int>)(lpVtbl[5]))((ID3D12Tools1*)Unsafe.AsPointer(ref this), pRanges, uiNumRanges);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    public void ClearReservedGPUVARangesList()
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12Tools1*, void>)(lpVtbl[6]))((ID3D12Tools1*)Unsafe.AsPointer(ref this));
    }

    public interface Interface : ID3D12Tools.Interface
    {
        [VtblIndex(5)]
        HRESULT ReserveGPUVARangesAtCreate(D3D12_GPU_VIRTUAL_ADDRESS_RANGE* pRanges, uint uiNumRanges);

        [VtblIndex(6)]
        void ClearReservedGPUVARangesList();
    }
}
