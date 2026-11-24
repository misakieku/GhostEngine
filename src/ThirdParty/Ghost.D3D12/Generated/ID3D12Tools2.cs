using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("01D393C5-C9B0-42A1-958C-C26B02D4D097")]
[NativeTypeName("struct ID3D12Tools2 : ID3D12Tools1")]
public unsafe partial struct ID3D12Tools2 : ID3D12Tools2.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12Tools2);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12Tools2*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12Tools2*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12Tools2*, uint>)(lpVtbl[1]))((ID3D12Tools2*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12Tools2*, uint>)(lpVtbl[2]))((ID3D12Tools2*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public void EnableShaderInstrumentation([NativeTypeName("BOOL")] int bEnable)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12Tools2*, int, void>)(lpVtbl[3]))((ID3D12Tools2*)Unsafe.AsPointer(ref this), bEnable);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("BOOL")]
    public int ShaderInstrumentationEnabled()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12Tools2*, int>)(lpVtbl[4]))((ID3D12Tools2*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    public HRESULT ReserveGPUVARangesAtCreate(D3D12_GPU_VIRTUAL_ADDRESS_RANGE* pRanges, uint uiNumRanges)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12Tools2*, D3D12_GPU_VIRTUAL_ADDRESS_RANGE*, uint, int>)(lpVtbl[5]))((ID3D12Tools2*)Unsafe.AsPointer(ref this), pRanges, uiNumRanges);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    public void ClearReservedGPUVARangesList()
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12Tools2*, void>)(lpVtbl[6]))((ID3D12Tools2*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    public HRESULT SetApplicationSpecificDriverState(IUnknown* pAdapter, [NativeTypeName("ID3DBlob *")] ID3D10Blob* pBlob)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12Tools2*, IUnknown*, ID3D10Blob*, int>)(lpVtbl[7]))((ID3D12Tools2*)Unsafe.AsPointer(ref this), pAdapter, pBlob);
    }

    public interface Interface : ID3D12Tools1.Interface
    {
        [VtblIndex(7)]
        HRESULT SetApplicationSpecificDriverState(IUnknown* pAdapter, [NativeTypeName("ID3DBlob *")] ID3D10Blob* pBlob);
    }
}
