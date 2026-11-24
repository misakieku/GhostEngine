using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("C4FEC28F-7966-4E95-9F94-F431CB56C3B8")]
[NativeTypeName("struct ID3D12Object : IUnknown")]
public unsafe partial struct ID3D12Object : ID3D12Object.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12Object);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12Object*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12Object*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12Object*, uint>)(lpVtbl[1]))((ID3D12Object*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12Object*, uint>)(lpVtbl[2]))((ID3D12Object*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int GetPrivateData([NativeTypeName("const GUID &")] Guid* guid, uint* pDataSize, void* pData)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12Object*, Guid*, uint*, void*, int>)(lpVtbl[3]))((ID3D12Object*)Unsafe.AsPointer(ref this), guid, pDataSize, pData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int SetPrivateData([NativeTypeName("const GUID &")] Guid* guid, uint DataSize, [NativeTypeName("const void *")] void* pData)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12Object*, Guid*, uint, void*, int>)(lpVtbl[4]))((ID3D12Object*)Unsafe.AsPointer(ref this), guid, DataSize, pData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    [return: NativeTypeName("HRESULT")]
    public int SetPrivateDataInterface([NativeTypeName("const GUID &")] Guid* guid, [NativeTypeName("const IUnknown *")] IUnknown* pData)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12Object*, Guid*, IUnknown*, int>)(lpVtbl[5]))((ID3D12Object*)Unsafe.AsPointer(ref this), guid, pData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    [return: NativeTypeName("HRESULT")]
    public int SetName([NativeTypeName("LPCWSTR")] char* Name)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12Object*, char*, int>)(lpVtbl[6]))((ID3D12Object*)Unsafe.AsPointer(ref this), Name);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        [return: NativeTypeName("HRESULT")]
        int GetPrivateData([NativeTypeName("const GUID &")] Guid* guid, uint* pDataSize, void* pData);

        [VtblIndex(4)]
        [return: NativeTypeName("HRESULT")]
        int SetPrivateData([NativeTypeName("const GUID &")] Guid* guid, uint DataSize, [NativeTypeName("const void *")] void* pData);

        [VtblIndex(5)]
        [return: NativeTypeName("HRESULT")]
        int SetPrivateDataInterface([NativeTypeName("const GUID &")] Guid* guid, [NativeTypeName("const IUnknown *")] IUnknown* pData);

        [VtblIndex(6)]
        [return: NativeTypeName("HRESULT")]
        int SetName([NativeTypeName("LPCWSTR")] char* Name);
    }
}
