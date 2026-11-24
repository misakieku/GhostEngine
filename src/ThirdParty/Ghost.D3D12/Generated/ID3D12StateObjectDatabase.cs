using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("C56060B7-B5FC-4135-98E0-A1E9997EACE0")]
[NativeTypeName("struct ID3D12StateObjectDatabase : IUnknown")]
public unsafe partial struct ID3D12StateObjectDatabase : ID3D12StateObjectDatabase.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12StateObjectDatabase);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectDatabase*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12StateObjectDatabase*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectDatabase*, uint>)(lpVtbl[1]))((ID3D12StateObjectDatabase*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectDatabase*, uint>)(lpVtbl[2]))((ID3D12StateObjectDatabase*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int SetApplicationDesc([NativeTypeName("const D3D12_APPLICATION_DESC *")] D3D12_APPLICATION_DESC* pApplicationDesc)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectDatabase*, D3D12_APPLICATION_DESC*, int>)(lpVtbl[3]))((ID3D12StateObjectDatabase*)Unsafe.AsPointer(ref this), pApplicationDesc);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int GetApplicationDesc([NativeTypeName("D3D12ApplicationDescFunc")] delegate* unmanaged[Stdcall]<D3D12_APPLICATION_DESC*, void*, void> CallbackFunc, void* pContext)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectDatabase*, delegate* unmanaged[Stdcall]<D3D12_APPLICATION_DESC*, void*, void>, void*, int>)(lpVtbl[4]))((ID3D12StateObjectDatabase*)Unsafe.AsPointer(ref this), CallbackFunc, pContext);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    [return: NativeTypeName("HRESULT")]
    public int StorePipelineStateDesc([NativeTypeName("const void *")] void* pKey, uint KeySize, uint Version, [NativeTypeName("const D3D12_PIPELINE_STATE_STREAM_DESC *")] D3D12_PIPELINE_STATE_STREAM_DESC* pDesc)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectDatabase*, void*, uint, uint, D3D12_PIPELINE_STATE_STREAM_DESC*, int>)(lpVtbl[5]))((ID3D12StateObjectDatabase*)Unsafe.AsPointer(ref this), pKey, KeySize, Version, pDesc);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    [return: NativeTypeName("HRESULT")]
    public int FindPipelineStateDesc([NativeTypeName("const void *")] void* pKey, uint KeySize, [NativeTypeName("D3D12PipelineStateFunc")] delegate* unmanaged[Stdcall]<void*, uint, uint, D3D12_PIPELINE_STATE_STREAM_DESC*, void*, void> CallbackFunc, void* pContext)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectDatabase*, void*, uint, delegate* unmanaged[Stdcall]<void*, uint, uint, D3D12_PIPELINE_STATE_STREAM_DESC*, void*, void>, void*, int>)(lpVtbl[6]))((ID3D12StateObjectDatabase*)Unsafe.AsPointer(ref this), pKey, KeySize, CallbackFunc, pContext);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    [return: NativeTypeName("HRESULT")]
    public int StoreStateObjectDesc([NativeTypeName("const void *")] void* pKey, uint KeySize, uint Version, [NativeTypeName("const D3D12_STATE_OBJECT_DESC *")] D3D12_STATE_OBJECT_DESC* pDesc, [NativeTypeName("const void *")] void* pStateObjectToGrowFromKey, uint StateObjectToGrowFromKeySize)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectDatabase*, void*, uint, uint, D3D12_STATE_OBJECT_DESC*, void*, uint, int>)(lpVtbl[7]))((ID3D12StateObjectDatabase*)Unsafe.AsPointer(ref this), pKey, KeySize, Version, pDesc, pStateObjectToGrowFromKey, StateObjectToGrowFromKeySize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(8)]
    [return: NativeTypeName("HRESULT")]
    public int FindStateObjectDesc([NativeTypeName("const void *")] void* pKey, uint KeySize, [NativeTypeName("D3D12StateObjectFunc")] delegate* unmanaged[Stdcall]<void*, uint, uint, D3D12_STATE_OBJECT_DESC*, void*, uint, void*, void> CallbackFunc, void* pContext)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectDatabase*, void*, uint, delegate* unmanaged[Stdcall]<void*, uint, uint, D3D12_STATE_OBJECT_DESC*, void*, uint, void*, void>, void*, int>)(lpVtbl[8]))((ID3D12StateObjectDatabase*)Unsafe.AsPointer(ref this), pKey, KeySize, CallbackFunc, pContext);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(9)]
    [return: NativeTypeName("HRESULT")]
    public int FindObjectVersion([NativeTypeName("const void *")] void* pKey, uint KeySize, uint* pVersion)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectDatabase*, void*, uint, uint*, int>)(lpVtbl[9]))((ID3D12StateObjectDatabase*)Unsafe.AsPointer(ref this), pKey, KeySize, pVersion);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        [return: NativeTypeName("HRESULT")]
        int SetApplicationDesc([NativeTypeName("const D3D12_APPLICATION_DESC *")] D3D12_APPLICATION_DESC* pApplicationDesc);

        [VtblIndex(4)]
        [return: NativeTypeName("HRESULT")]
        int GetApplicationDesc([NativeTypeName("D3D12ApplicationDescFunc")] delegate* unmanaged[Stdcall]<D3D12_APPLICATION_DESC*, void*, void> CallbackFunc, void* pContext);

        [VtblIndex(5)]
        [return: NativeTypeName("HRESULT")]
        int StorePipelineStateDesc([NativeTypeName("const void *")] void* pKey, uint KeySize, uint Version, [NativeTypeName("const D3D12_PIPELINE_STATE_STREAM_DESC *")] D3D12_PIPELINE_STATE_STREAM_DESC* pDesc);

        [VtblIndex(6)]
        [return: NativeTypeName("HRESULT")]
        int FindPipelineStateDesc([NativeTypeName("const void *")] void* pKey, uint KeySize, [NativeTypeName("D3D12PipelineStateFunc")] delegate* unmanaged[Stdcall]<void*, uint, uint, D3D12_PIPELINE_STATE_STREAM_DESC*, void*, void> CallbackFunc, void* pContext);

        [VtblIndex(7)]
        [return: NativeTypeName("HRESULT")]
        int StoreStateObjectDesc([NativeTypeName("const void *")] void* pKey, uint KeySize, uint Version, [NativeTypeName("const D3D12_STATE_OBJECT_DESC *")] D3D12_STATE_OBJECT_DESC* pDesc, [NativeTypeName("const void *")] void* pStateObjectToGrowFromKey, uint StateObjectToGrowFromKeySize);

        [VtblIndex(8)]
        [return: NativeTypeName("HRESULT")]
        int FindStateObjectDesc([NativeTypeName("const void *")] void* pKey, uint KeySize, [NativeTypeName("D3D12StateObjectFunc")] delegate* unmanaged[Stdcall]<void*, uint, uint, D3D12_STATE_OBJECT_DESC*, void*, uint, void*, void> CallbackFunc, void* pContext);

        [VtblIndex(9)]
        [return: NativeTypeName("HRESULT")]
        int FindObjectVersion([NativeTypeName("const void *")] void* pKey, uint KeySize, uint* pVersion);
    }
}
