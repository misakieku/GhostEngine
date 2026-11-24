using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("C54A6B66-72DF-4EE8-8BE5-A946A1429214")]
[NativeTypeName("struct ID3D12RootSignature : ID3D12DeviceChild")]
public unsafe partial struct ID3D12RootSignature : ID3D12RootSignature.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12RootSignature);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12RootSignature*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12RootSignature*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12RootSignature*, uint>)(lpVtbl[1]))((ID3D12RootSignature*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12RootSignature*, uint>)(lpVtbl[2]))((ID3D12RootSignature*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int GetPrivateData([NativeTypeName("const GUID &")] Guid* guid, uint* pDataSize, void* pData)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12RootSignature*, Guid*, uint*, void*, int>)(lpVtbl[3]))((ID3D12RootSignature*)Unsafe.AsPointer(ref this), guid, pDataSize, pData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int SetPrivateData([NativeTypeName("const GUID &")] Guid* guid, uint DataSize, [NativeTypeName("const void *")] void* pData)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12RootSignature*, Guid*, uint, void*, int>)(lpVtbl[4]))((ID3D12RootSignature*)Unsafe.AsPointer(ref this), guid, DataSize, pData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    [return: NativeTypeName("HRESULT")]
    public int SetPrivateDataInterface([NativeTypeName("const GUID &")] Guid* guid, [NativeTypeName("const IUnknown *")] IUnknown* pData)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12RootSignature*, Guid*, IUnknown*, int>)(lpVtbl[5]))((ID3D12RootSignature*)Unsafe.AsPointer(ref this), guid, pData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    [return: NativeTypeName("HRESULT")]
    public int SetName([NativeTypeName("LPCWSTR")] char* Name)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12RootSignature*, char*, int>)(lpVtbl[6]))((ID3D12RootSignature*)Unsafe.AsPointer(ref this), Name);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    [return: NativeTypeName("HRESULT")]
    public int GetDevice([NativeTypeName("const IID &")] Guid* riid, void** ppvDevice)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12RootSignature*, Guid*, void**, int>)(lpVtbl[7]))((ID3D12RootSignature*)Unsafe.AsPointer(ref this), riid, ppvDevice);
    }

    public interface Interface : ID3D12DeviceChild.Interface
    {
    }
}
