using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("1C5789FF-9203-490F-B971-984ACF1EB5FE")]
[NativeTypeName("struct ID3D12ShaderCacheSession1 : ID3D12ShaderCacheSession")]
public unsafe partial struct ID3D12ShaderCacheSession1 : ID3D12ShaderCacheSession1.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12ShaderCacheSession1);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12ShaderCacheSession1*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12ShaderCacheSession1*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12ShaderCacheSession1*, uint>)(lpVtbl[1]))((ID3D12ShaderCacheSession1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12ShaderCacheSession1*, uint>)(lpVtbl[2]))((ID3D12ShaderCacheSession1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int GetPrivateData([NativeTypeName("const GUID &")] Guid* guid, uint* pDataSize, void* pData)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12ShaderCacheSession1*, Guid*, uint*, void*, int>)(lpVtbl[3]))((ID3D12ShaderCacheSession1*)Unsafe.AsPointer(ref this), guid, pDataSize, pData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int SetPrivateData([NativeTypeName("const GUID &")] Guid* guid, uint DataSize, [NativeTypeName("const void *")] void* pData)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12ShaderCacheSession1*, Guid*, uint, void*, int>)(lpVtbl[4]))((ID3D12ShaderCacheSession1*)Unsafe.AsPointer(ref this), guid, DataSize, pData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    [return: NativeTypeName("HRESULT")]
    public int SetPrivateDataInterface([NativeTypeName("const GUID &")] Guid* guid, [NativeTypeName("const IUnknown *")] IUnknown* pData)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12ShaderCacheSession1*, Guid*, IUnknown*, int>)(lpVtbl[5]))((ID3D12ShaderCacheSession1*)Unsafe.AsPointer(ref this), guid, pData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    [return: NativeTypeName("HRESULT")]
    public int SetName([NativeTypeName("LPCWSTR")] char* Name)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12ShaderCacheSession1*, char*, int>)(lpVtbl[6]))((ID3D12ShaderCacheSession1*)Unsafe.AsPointer(ref this), Name);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    [return: NativeTypeName("HRESULT")]
    public int GetDevice([NativeTypeName("const IID &")] Guid* riid, void** ppvDevice)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12ShaderCacheSession1*, Guid*, void**, int>)(lpVtbl[7]))((ID3D12ShaderCacheSession1*)Unsafe.AsPointer(ref this), riid, ppvDevice);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(8)]
    [return: NativeTypeName("HRESULT")]
    public int FindValue([NativeTypeName("const void *")] void* pKey, uint KeySize, void* pValue, uint* pValueSize)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12ShaderCacheSession1*, void*, uint, void*, uint*, int>)(lpVtbl[8]))((ID3D12ShaderCacheSession1*)Unsafe.AsPointer(ref this), pKey, KeySize, pValue, pValueSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(9)]
    [return: NativeTypeName("HRESULT")]
    public int StoreValue([NativeTypeName("const void *")] void* pKey, uint KeySize, [NativeTypeName("const void *")] void* pValue, uint ValueSize)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12ShaderCacheSession1*, void*, uint, void*, uint, int>)(lpVtbl[9]))((ID3D12ShaderCacheSession1*)Unsafe.AsPointer(ref this), pKey, KeySize, pValue, ValueSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(10)]
    public void SetDeleteOnDestroy()
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12ShaderCacheSession1*, void>)(lpVtbl[10]))((ID3D12ShaderCacheSession1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(11)]
    public D3D12_SHADER_CACHE_SESSION_DESC GetDesc()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12ShaderCacheSession1*, D3D12_SHADER_CACHE_SESSION_DESC>)(lpVtbl[11]))((ID3D12ShaderCacheSession1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(12)]
    [return: NativeTypeName("HRESULT")]
    public int FindValue1([NativeTypeName("const D3D12_CACHE_VALUE_KEY *")] D3D12_CACHE_VALUE_KEY* pValueKey, D3D12_CACHE_TYPED_VALUE* pTypedValues, uint NumTypedValues, [NativeTypeName("D3D12CacheSessionAllocationFunc")] delegate* unmanaged[Stdcall]<nuint, void*, void*> pCallbackFunc, void* pContext)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12ShaderCacheSession1*, D3D12_CACHE_VALUE_KEY*, D3D12_CACHE_TYPED_VALUE*, uint, delegate* unmanaged[Stdcall]<nuint, void*, void*>, void*, int>)(lpVtbl[12]))((ID3D12ShaderCacheSession1*)Unsafe.AsPointer(ref this), pValueKey, pTypedValues, NumTypedValues, pCallbackFunc, pContext);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(13)]
    [return: NativeTypeName("HRESULT")]
    public int StoreValue1([NativeTypeName("const D3D12_CACHE_VALUE_KEY *")] D3D12_CACHE_VALUE_KEY* pValueKey, [NativeTypeName("const D3D12_CACHE_TYPED_CONST_VALUE *")] D3D12_CACHE_TYPED_CONST_VALUE* pTypedValues, uint NumTypedValues, D3D12_SHADER_CACHE_STORE_FLAGS Flags)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12ShaderCacheSession1*, D3D12_CACHE_VALUE_KEY*, D3D12_CACHE_TYPED_CONST_VALUE*, uint, D3D12_SHADER_CACHE_STORE_FLAGS, int>)(lpVtbl[13]))((ID3D12ShaderCacheSession1*)Unsafe.AsPointer(ref this), pValueKey, pTypedValues, NumTypedValues, Flags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(14)]
    [return: NativeTypeName("HRESULT")]
    public int GetSessionInfo(D3D12_SHADER_CACHE_SESSION_INFO* pInfo)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12ShaderCacheSession1*, D3D12_SHADER_CACHE_SESSION_INFO*, int>)(lpVtbl[14]))((ID3D12ShaderCacheSession1*)Unsafe.AsPointer(ref this), pInfo);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(15)]
    [return: NativeTypeName("HRESULT")]
    public int TrimTo([NativeTypeName("UINT64")] ulong SizeBytes, D3D12_SHADER_CACHE_TRIM_FLAGS Flags)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12ShaderCacheSession1*, ulong, D3D12_SHADER_CACHE_TRIM_FLAGS, int>)(lpVtbl[15]))((ID3D12ShaderCacheSession1*)Unsafe.AsPointer(ref this), SizeBytes, Flags);
    }

    public interface Interface : ID3D12ShaderCacheSession.Interface
    {
        [VtblIndex(12)]
        [return: NativeTypeName("HRESULT")]
        int FindValue1([NativeTypeName("const D3D12_CACHE_VALUE_KEY *")] D3D12_CACHE_VALUE_KEY* pValueKey, D3D12_CACHE_TYPED_VALUE* pTypedValues, uint NumTypedValues, [NativeTypeName("D3D12CacheSessionAllocationFunc")] delegate* unmanaged[Stdcall]<nuint, void*, void*> pCallbackFunc, void* pContext);

        [VtblIndex(13)]
        [return: NativeTypeName("HRESULT")]
        int StoreValue1([NativeTypeName("const D3D12_CACHE_VALUE_KEY *")] D3D12_CACHE_VALUE_KEY* pValueKey, [NativeTypeName("const D3D12_CACHE_TYPED_CONST_VALUE *")] D3D12_CACHE_TYPED_CONST_VALUE* pTypedValues, uint NumTypedValues, D3D12_SHADER_CACHE_STORE_FLAGS Flags);

        [VtblIndex(14)]
        [return: NativeTypeName("HRESULT")]
        int GetSessionInfo(D3D12_SHADER_CACHE_SESSION_INFO* pInfo);

        [VtblIndex(15)]
        [return: NativeTypeName("HRESULT")]
        int TrimTo([NativeTypeName("UINT64")] ulong SizeBytes, D3D12_SHADER_CACHE_TRIM_FLAGS Flags);
    }
}
