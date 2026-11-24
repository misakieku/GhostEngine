using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("E9EB5314-33AA-42B2-A718-D77F58B1F1C7")]
[NativeTypeName("struct ID3D12SDKConfiguration : IUnknown")]
public unsafe partial struct ID3D12SDKConfiguration : ID3D12SDKConfiguration.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12SDKConfiguration);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12SDKConfiguration*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12SDKConfiguration*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12SDKConfiguration*, uint>)(lpVtbl[1]))((ID3D12SDKConfiguration*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12SDKConfiguration*, uint>)(lpVtbl[2]))((ID3D12SDKConfiguration*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int SetSDKVersion(uint SDKVersion, [NativeTypeName("LPCSTR")] sbyte* SDKPath)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12SDKConfiguration*, uint, sbyte*, int>)(lpVtbl[3]))((ID3D12SDKConfiguration*)Unsafe.AsPointer(ref this), SDKVersion, SDKPath);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        [return: NativeTypeName("HRESULT")]
        int SetSDKVersion(uint SDKVersion, [NativeTypeName("LPCSTR")] sbyte* SDKPath);
    }
}
