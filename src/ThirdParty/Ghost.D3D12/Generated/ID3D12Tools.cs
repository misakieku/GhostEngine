using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("7071E1F0-E84B-4B33-974F-12FA49DE65C5")]
[NativeTypeName("struct ID3D12Tools : IUnknown")]
public unsafe partial struct ID3D12Tools : ID3D12Tools.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12Tools);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12Tools*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12Tools*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12Tools*, uint>)(lpVtbl[1]))((ID3D12Tools*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12Tools*, uint>)(lpVtbl[2]))((ID3D12Tools*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public void EnableShaderInstrumentation([NativeTypeName("BOOL")] int bEnable)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12Tools*, int, void>)(lpVtbl[3]))((ID3D12Tools*)Unsafe.AsPointer(ref this), bEnable);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("BOOL")]
    public int ShaderInstrumentationEnabled()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12Tools*, int>)(lpVtbl[4]))((ID3D12Tools*)Unsafe.AsPointer(ref this));
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        void EnableShaderInstrumentation([NativeTypeName("BOOL")] int bEnable);

        [VtblIndex(4)]
        [return: NativeTypeName("BOOL")]
        int ShaderInstrumentationEnabled();
    }
}
