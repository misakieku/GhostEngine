using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("28E2495D-0F64-4AE4-A6EC-129255DC49A8")]
[NativeTypeName("struct ID3D12ShaderCacheSession : ID3D12DeviceChild")]
public unsafe partial struct ID3D12ShaderCacheSession : ID3D12ShaderCacheSession.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12ShaderCacheSession);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12ShaderCacheSession*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12ShaderCacheSession*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12ShaderCacheSession*, uint>)(lpVtbl[1]))((ID3D12ShaderCacheSession*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12ShaderCacheSession*, uint>)(lpVtbl[2]))((ID3D12ShaderCacheSession*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public HRESULT GetPrivateData([NativeTypeName("const GUID &")] Guid* guid, uint* pDataSize, void* pData)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12ShaderCacheSession*, Guid*, uint*, void*, int>)(lpVtbl[3]))((ID3D12ShaderCacheSession*)Unsafe.AsPointer(ref this), guid, pDataSize, pData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    public HRESULT SetPrivateData([NativeTypeName("const GUID &")] Guid* guid, uint DataSize, [NativeTypeName("const void *")] void* pData)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12ShaderCacheSession*, Guid*, uint, void*, int>)(lpVtbl[4]))((ID3D12ShaderCacheSession*)Unsafe.AsPointer(ref this), guid, DataSize, pData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    public HRESULT SetPrivateDataInterface([NativeTypeName("const GUID &")] Guid* guid, [NativeTypeName("const IUnknown *")] IUnknown* pData)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12ShaderCacheSession*, Guid*, IUnknown*, int>)(lpVtbl[5]))((ID3D12ShaderCacheSession*)Unsafe.AsPointer(ref this), guid, pData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    public HRESULT SetName([NativeTypeName("LPCWSTR")] char* Name)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12ShaderCacheSession*, char*, int>)(lpVtbl[6]))((ID3D12ShaderCacheSession*)Unsafe.AsPointer(ref this), Name);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    public HRESULT GetDevice([NativeTypeName("const IID &")] Guid* riid, void** ppvDevice)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12ShaderCacheSession*, Guid*, void**, int>)(lpVtbl[7]))((ID3D12ShaderCacheSession*)Unsafe.AsPointer(ref this), riid, ppvDevice);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(8)]
    public HRESULT FindValue([NativeTypeName("const void *")] void* pKey, uint KeySize, void* pValue, uint* pValueSize)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12ShaderCacheSession*, void*, uint, void*, uint*, int>)(lpVtbl[8]))((ID3D12ShaderCacheSession*)Unsafe.AsPointer(ref this), pKey, KeySize, pValue, pValueSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(9)]
    public HRESULT StoreValue([NativeTypeName("const void *")] void* pKey, uint KeySize, [NativeTypeName("const void *")] void* pValue, uint ValueSize)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12ShaderCacheSession*, void*, uint, void*, uint, int>)(lpVtbl[9]))((ID3D12ShaderCacheSession*)Unsafe.AsPointer(ref this), pKey, KeySize, pValue, ValueSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(10)]
    public void SetDeleteOnDestroy()
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12ShaderCacheSession*, void>)(lpVtbl[10]))((ID3D12ShaderCacheSession*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(11)]
    public D3D12_SHADER_CACHE_SESSION_DESC GetDesc()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12ShaderCacheSession*, D3D12_SHADER_CACHE_SESSION_DESC>)(lpVtbl[11]))((ID3D12ShaderCacheSession*)Unsafe.AsPointer(ref this));
    }

    public interface Interface : ID3D12DeviceChild.Interface
    {
        [VtblIndex(8)]
        HRESULT FindValue([NativeTypeName("const void *")] void* pKey, uint KeySize, void* pValue, uint* pValueSize);

        [VtblIndex(9)]
        HRESULT StoreValue([NativeTypeName("const void *")] void* pKey, uint KeySize, [NativeTypeName("const void *")] void* pValue, uint ValueSize);

        [VtblIndex(10)]
        void SetDeleteOnDestroy();

        [VtblIndex(11)]
        D3D12_SHADER_CACHE_SESSION_DESC GetDesc();
    }
}
