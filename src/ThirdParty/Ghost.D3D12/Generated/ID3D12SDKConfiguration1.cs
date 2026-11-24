using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("8AAF9303-AD25-48B9-9A57-D9C37E009D9F")]
[NativeTypeName("struct ID3D12SDKConfiguration1 : ID3D12SDKConfiguration")]
public unsafe partial struct ID3D12SDKConfiguration1 : ID3D12SDKConfiguration1.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12SDKConfiguration1);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12SDKConfiguration1*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12SDKConfiguration1*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12SDKConfiguration1*, uint>)(lpVtbl[1]))((ID3D12SDKConfiguration1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12SDKConfiguration1*, uint>)(lpVtbl[2]))((ID3D12SDKConfiguration1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int SetSDKVersion(uint SDKVersion, [NativeTypeName("LPCSTR")] sbyte* SDKPath)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12SDKConfiguration1*, uint, sbyte*, int>)(lpVtbl[3]))((ID3D12SDKConfiguration1*)Unsafe.AsPointer(ref this), SDKVersion, SDKPath);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int CreateDeviceFactory(uint SDKVersion, [NativeTypeName("LPCSTR")] sbyte* SDKPath, [NativeTypeName("const IID &")] Guid* riid, void** ppvFactory)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12SDKConfiguration1*, uint, sbyte*, Guid*, void**, int>)(lpVtbl[4]))((ID3D12SDKConfiguration1*)Unsafe.AsPointer(ref this), SDKVersion, SDKPath, riid, ppvFactory);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    public void FreeUnusedSDKs()
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12SDKConfiguration1*, void>)(lpVtbl[5]))((ID3D12SDKConfiguration1*)Unsafe.AsPointer(ref this));
    }

    public interface Interface : ID3D12SDKConfiguration.Interface
    {
        [VtblIndex(4)]
        [return: NativeTypeName("HRESULT")]
        int CreateDeviceFactory(uint SDKVersion, [NativeTypeName("LPCSTR")] sbyte* SDKPath, [NativeTypeName("const IID &")] Guid* riid, void** ppvFactory);

        [VtblIndex(5)]
        void FreeUnusedSDKs();
    }
}
