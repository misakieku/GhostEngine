using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("C700E43F-47A6-4EC6-AD83-F275400231AF")]
[NativeTypeName("struct ID3D12StateObjectDatabase1 : ID3D12StateObjectDatabase")]
public unsafe partial struct ID3D12StateObjectDatabase1 : ID3D12StateObjectDatabase1.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12StateObjectDatabase1);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectDatabase1*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12StateObjectDatabase1*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectDatabase1*, uint>)(lpVtbl[1]))((ID3D12StateObjectDatabase1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectDatabase1*, uint>)(lpVtbl[2]))((ID3D12StateObjectDatabase1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public HRESULT SetApplicationDesc([NativeTypeName("const D3D12_APPLICATION_DESC *")] D3D12_APPLICATION_DESC* pApplicationDesc)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectDatabase1*, D3D12_APPLICATION_DESC*, int>)(lpVtbl[3]))((ID3D12StateObjectDatabase1*)Unsafe.AsPointer(ref this), pApplicationDesc);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    public HRESULT GetApplicationDesc([NativeTypeName("D3D12ApplicationDescFunc")] delegate* unmanaged[Stdcall]<D3D12_APPLICATION_DESC*, void*, void> CallbackFunc, void* pContext)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectDatabase1*, delegate* unmanaged[Stdcall]<D3D12_APPLICATION_DESC*, void*, void>, void*, int>)(lpVtbl[4]))((ID3D12StateObjectDatabase1*)Unsafe.AsPointer(ref this), CallbackFunc, pContext);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    public HRESULT StorePipelineStateDesc([NativeTypeName("const void *")] void* pKey, uint KeySize, uint Version, [NativeTypeName("const D3D12_PIPELINE_STATE_STREAM_DESC *")] D3D12_PIPELINE_STATE_STREAM_DESC* pDesc)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectDatabase1*, void*, uint, uint, D3D12_PIPELINE_STATE_STREAM_DESC*, int>)(lpVtbl[5]))((ID3D12StateObjectDatabase1*)Unsafe.AsPointer(ref this), pKey, KeySize, Version, pDesc);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    public HRESULT FindPipelineStateDesc([NativeTypeName("const void *")] void* pKey, uint KeySize, [NativeTypeName("D3D12PipelineStateFunc")] delegate* unmanaged[Stdcall]<void*, uint, uint, D3D12_PIPELINE_STATE_STREAM_DESC*, void*, void> CallbackFunc, void* pContext)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectDatabase1*, void*, uint, delegate* unmanaged[Stdcall]<void*, uint, uint, D3D12_PIPELINE_STATE_STREAM_DESC*, void*, void>, void*, int>)(lpVtbl[6]))((ID3D12StateObjectDatabase1*)Unsafe.AsPointer(ref this), pKey, KeySize, CallbackFunc, pContext);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    public HRESULT StoreStateObjectDesc([NativeTypeName("const void *")] void* pKey, uint KeySize, uint Version, [NativeTypeName("const D3D12_STATE_OBJECT_DESC *")] D3D12_STATE_OBJECT_DESC* pDesc, [NativeTypeName("const void *")] void* pStateObjectToGrowFromKey, uint StateObjectToGrowFromKeySize)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectDatabase1*, void*, uint, uint, D3D12_STATE_OBJECT_DESC*, void*, uint, int>)(lpVtbl[7]))((ID3D12StateObjectDatabase1*)Unsafe.AsPointer(ref this), pKey, KeySize, Version, pDesc, pStateObjectToGrowFromKey, StateObjectToGrowFromKeySize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(8)]
    public HRESULT FindStateObjectDesc([NativeTypeName("const void *")] void* pKey, uint KeySize, [NativeTypeName("D3D12StateObjectFunc")] delegate* unmanaged[Stdcall]<void*, uint, uint, D3D12_STATE_OBJECT_DESC*, void*, uint, void*, void> CallbackFunc, void* pContext)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectDatabase1*, void*, uint, delegate* unmanaged[Stdcall]<void*, uint, uint, D3D12_STATE_OBJECT_DESC*, void*, uint, void*, void>, void*, int>)(lpVtbl[8]))((ID3D12StateObjectDatabase1*)Unsafe.AsPointer(ref this), pKey, KeySize, CallbackFunc, pContext);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(9)]
    public HRESULT FindObjectVersion([NativeTypeName("const void *")] void* pKey, uint KeySize, uint* pVersion)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectDatabase1*, void*, uint, uint*, int>)(lpVtbl[9]))((ID3D12StateObjectDatabase1*)Unsafe.AsPointer(ref this), pKey, KeySize, pVersion);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(10)]
    public HRESULT EnumeratePipelineStates([NativeTypeName("D3D12EnumeratePipelineStateFunc")] delegate* unmanaged[Stdcall]<D3D12_DATABASE_GROUP_KEY*, uint*, uint, D3D12_DATABASE_PIPELINE_STATE_KEY*, void*, int> CallbackFunc, void* pContext)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectDatabase1*, delegate* unmanaged[Stdcall]<D3D12_DATABASE_GROUP_KEY*, uint*, uint, D3D12_DATABASE_PIPELINE_STATE_KEY*, void*, int>, void*, int>)(lpVtbl[10]))((ID3D12StateObjectDatabase1*)Unsafe.AsPointer(ref this), CallbackFunc, pContext);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(11)]
    public HRESULT EnumerateStateObjects([NativeTypeName("D3D12EnumerateStateObjectFunc")] delegate* unmanaged[Stdcall]<D3D12_DATABASE_GROUP_KEY*, uint*, uint, D3D12_DATABASE_STATE_OBJECT_KEY*, void*, int> CallbackFunc, void* pContext)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectDatabase1*, delegate* unmanaged[Stdcall]<D3D12_DATABASE_GROUP_KEY*, uint*, uint, D3D12_DATABASE_STATE_OBJECT_KEY*, void*, int>, void*, int>)(lpVtbl[11]))((ID3D12StateObjectDatabase1*)Unsafe.AsPointer(ref this), CallbackFunc, pContext);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(12)]
    public HRESULT GetStateObjectDependencies([NativeTypeName("const D3D12_DATABASE_GROUP_KEY *")] D3D12_DATABASE_GROUP_KEY* pGroupKey, [NativeTypeName("const D3D12_DATABASE_STATE_OBJECT_KEY *")] D3D12_DATABASE_STATE_OBJECT_KEY* pStateObjectKey, [NativeTypeName("D3D12StateObjectDependenciesFunc")] delegate* unmanaged[Stdcall]<D3D12_DATABASE_STATE_OBJECT_KEY*, uint, D3D12_DATABASE_STATE_OBJECT_KEY*, void*, void> CallbackFunc, void* pContext)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectDatabase1*, D3D12_DATABASE_GROUP_KEY*, D3D12_DATABASE_STATE_OBJECT_KEY*, delegate* unmanaged[Stdcall]<D3D12_DATABASE_STATE_OBJECT_KEY*, uint, D3D12_DATABASE_STATE_OBJECT_KEY*, void*, void>, void*, int>)(lpVtbl[12]))((ID3D12StateObjectDatabase1*)Unsafe.AsPointer(ref this), pGroupKey, pStateObjectKey, CallbackFunc, pContext);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(13)]
    public HRESULT SetApplicationDesc1([NativeTypeName("const D3D12_APPLICATION_DESC *")] D3D12_APPLICATION_DESC* pApplicationDesc, [NativeTypeName("const GUID &")] Guid* AppId)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectDatabase1*, D3D12_APPLICATION_DESC*, Guid*, int>)(lpVtbl[13]))((ID3D12StateObjectDatabase1*)Unsafe.AsPointer(ref this), pApplicationDesc, AppId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(14)]
    public HRESULT GetApplicationDesc1([NativeTypeName("D3D12ApplicationDescFunc1")] delegate* unmanaged[Stdcall]<D3D12_APPLICATION_DESC*, Guid*, void*, void> CallbackFunc, void* pContext)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectDatabase1*, delegate* unmanaged[Stdcall]<D3D12_APPLICATION_DESC*, Guid*, void*, void>, void*, int>)(lpVtbl[14]))((ID3D12StateObjectDatabase1*)Unsafe.AsPointer(ref this), CallbackFunc, pContext);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(15)]
    public HRESULT FindPipelineStateDesc1([NativeTypeName("const D3D12_DATABASE_GROUP_KEY *")] D3D12_DATABASE_GROUP_KEY* pGroupKey, [NativeTypeName("const D3D12_DATABASE_PIPELINE_STATE_KEY *")] D3D12_DATABASE_PIPELINE_STATE_KEY* pPipelineStateKey, D3D12_FIND_PIPELINE_STATE_DESC_FLAGS Flags, [NativeTypeName("D3D12PipelineStateFunc1")] delegate* unmanaged[Stdcall]<D3D12_DATABASE_GROUP_KEY*, uint*, uint, D3D12_DATABASE_PIPELINE_STATE_KEY*, D3D12_PIPELINE_STATE_STREAM_DESC*, void*, void> CallbackFunc, [NativeTypeName("D3D12CreateRootSignatureFunc")] delegate* unmanaged[Stdcall]<D3D12_DATABASE_ROOT_SIGNATURE_KEY*, void*, nuint, ID3D12RootSignature**, void*, HRESULT> pfnCreateRootSignature, [NativeTypeName("D3D12LookupRootSignatureFunc")] delegate* unmanaged[Stdcall]<D3D12_DATABASE_ROOT_SIGNATURE_KEY*, ID3D12RootSignature**, void*, HRESULT> pfnLookupRootSignature, void* pContext)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectDatabase1*, D3D12_DATABASE_GROUP_KEY*, D3D12_DATABASE_PIPELINE_STATE_KEY*, D3D12_FIND_PIPELINE_STATE_DESC_FLAGS, delegate* unmanaged[Stdcall]<D3D12_DATABASE_GROUP_KEY*, uint*, uint, D3D12_DATABASE_PIPELINE_STATE_KEY*, D3D12_PIPELINE_STATE_STREAM_DESC*, void*, void>, delegate* unmanaged[Stdcall]<D3D12_DATABASE_ROOT_SIGNATURE_KEY*, void*, nuint, ID3D12RootSignature**, void*, HRESULT>, delegate* unmanaged[Stdcall]<D3D12_DATABASE_ROOT_SIGNATURE_KEY*, ID3D12RootSignature**, void*, HRESULT>, void*, int>)(lpVtbl[15]))((ID3D12StateObjectDatabase1*)Unsafe.AsPointer(ref this), pGroupKey, pPipelineStateKey, Flags, CallbackFunc, pfnCreateRootSignature, pfnLookupRootSignature, pContext);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(16)]
    public HRESULT FindStateObjectDesc1([NativeTypeName("const D3D12_DATABASE_GROUP_KEY *")] D3D12_DATABASE_GROUP_KEY* pGroupKey, [NativeTypeName("const D3D12_DATABASE_STATE_OBJECT_KEY *")] D3D12_DATABASE_STATE_OBJECT_KEY* pStateObjectKey, D3D12_FIND_STATE_OBJECT_DESC_FLAGS Flags, [NativeTypeName("D3D12StateObjectFunc1")] delegate* unmanaged[Stdcall]<D3D12_DATABASE_GROUP_KEY*, uint*, uint, D3D12_DATABASE_STATE_OBJECT_KEY*, D3D12_STATE_OBJECT_DESC*, D3D12_DATABASE_STATE_OBJECT_KEY*, D3D12_DATABASE_GROUP_KEY*, void*, void> CallbackFunc, [NativeTypeName("D3D12LookupStateObjectFunc")] delegate* unmanaged[Stdcall]<D3D12_DATABASE_STATE_OBJECT_KEY*, D3D12_DATABASE_GROUP_KEY*, IUnknown**, void*, HRESULT> pfnLookupStateObject, [NativeTypeName("D3D12CreateRootSignatureFunc")] delegate* unmanaged[Stdcall]<D3D12_DATABASE_ROOT_SIGNATURE_KEY*, void*, nuint, ID3D12RootSignature**, void*, HRESULT> pfnCreateRootSignature, [NativeTypeName("D3D12LookupRootSignatureFunc")] delegate* unmanaged[Stdcall]<D3D12_DATABASE_ROOT_SIGNATURE_KEY*, ID3D12RootSignature**, void*, HRESULT> pfnLookupRootSignature, void* pContext)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectDatabase1*, D3D12_DATABASE_GROUP_KEY*, D3D12_DATABASE_STATE_OBJECT_KEY*, D3D12_FIND_STATE_OBJECT_DESC_FLAGS, delegate* unmanaged[Stdcall]<D3D12_DATABASE_GROUP_KEY*, uint*, uint, D3D12_DATABASE_STATE_OBJECT_KEY*, D3D12_STATE_OBJECT_DESC*, D3D12_DATABASE_STATE_OBJECT_KEY*, D3D12_DATABASE_GROUP_KEY*, void*, void>, delegate* unmanaged[Stdcall]<D3D12_DATABASE_STATE_OBJECT_KEY*, D3D12_DATABASE_GROUP_KEY*, IUnknown**, void*, HRESULT>, delegate* unmanaged[Stdcall]<D3D12_DATABASE_ROOT_SIGNATURE_KEY*, void*, nuint, ID3D12RootSignature**, void*, HRESULT>, delegate* unmanaged[Stdcall]<D3D12_DATABASE_ROOT_SIGNATURE_KEY*, ID3D12RootSignature**, void*, HRESULT>, void*, int>)(lpVtbl[16]))((ID3D12StateObjectDatabase1*)Unsafe.AsPointer(ref this), pGroupKey, pStateObjectKey, Flags, CallbackFunc, pfnLookupStateObject, pfnCreateRootSignature, pfnLookupRootSignature, pContext);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(17)]
    public HRESULT GetPipelineStateDescCount(uint* pCount)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectDatabase1*, uint*, int>)(lpVtbl[17]))((ID3D12StateObjectDatabase1*)Unsafe.AsPointer(ref this), pCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(18)]
    public HRESULT GetStateObjectDescCount(uint* pCount)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectDatabase1*, uint*, int>)(lpVtbl[18]))((ID3D12StateObjectDatabase1*)Unsafe.AsPointer(ref this), pCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(19)]
    public HRESULT BeginTransaction(D3D12_TRANSACTION_TYPE Type)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectDatabase1*, D3D12_TRANSACTION_TYPE, int>)(lpVtbl[19]))((ID3D12StateObjectDatabase1*)Unsafe.AsPointer(ref this), Type);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(20)]
    public HRESULT CommitTransaction()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectDatabase1*, int>)(lpVtbl[20]))((ID3D12StateObjectDatabase1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(21)]
    public HRESULT RollbackTransaction()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectDatabase1*, int>)(lpVtbl[21]))((ID3D12StateObjectDatabase1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(22)]
    [return: NativeTypeName("BOOL")]
    public int IsTransactionActive()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectDatabase1*, int>)(lpVtbl[22]))((ID3D12StateObjectDatabase1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(23)]
    public HRESULT Checkpoint(D3D12_CHECKPOINT_MODE CheckpointMode, D3D12_JOURNAL_MODE JournalMode, D3D12_JOURNAL_MODE* pPreviousJournalMode)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectDatabase1*, D3D12_CHECKPOINT_MODE, D3D12_JOURNAL_MODE, D3D12_JOURNAL_MODE*, int>)(lpVtbl[23]))((ID3D12StateObjectDatabase1*)Unsafe.AsPointer(ref this), CheckpointMode, JournalMode, pPreviousJournalMode);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(24)]
    public HRESULT StorePipelineStateDesc1([NativeTypeName("const D3D12_DATABASE_GROUP_KEY *")] D3D12_DATABASE_GROUP_KEY* pGroupKeys, [NativeTypeName("const UINT *")] uint* pVersions, uint NumGroupKeys, [NativeTypeName("const D3D12_PIPELINE_STATE_STREAM_DESC *")] D3D12_PIPELINE_STATE_STREAM_DESC* pDesc)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectDatabase1*, D3D12_DATABASE_GROUP_KEY*, uint*, uint, D3D12_PIPELINE_STATE_STREAM_DESC*, int>)(lpVtbl[24]))((ID3D12StateObjectDatabase1*)Unsafe.AsPointer(ref this), pGroupKeys, pVersions, NumGroupKeys, pDesc);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(25)]
    public HRESULT StoreStateObjectDesc1([NativeTypeName("const D3D12_DATABASE_GROUP_KEY *")] D3D12_DATABASE_GROUP_KEY* pGroupKeys, [NativeTypeName("const UINT *")] uint* pVersions, uint NumGroupKeys, [NativeTypeName("const D3D12_STATE_OBJECT_DESC *")] D3D12_STATE_OBJECT_DESC* pDesc, [NativeTypeName("const D3D12_DATABASE_GROUP_KEY *")] D3D12_DATABASE_GROUP_KEY* pStateObjectToGrowFromGroupKey, IUnknown* pCreatedObject, IUnknown* pStateObjectToGrowFrom)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12StateObjectDatabase1*, D3D12_DATABASE_GROUP_KEY*, uint*, uint, D3D12_STATE_OBJECT_DESC*, D3D12_DATABASE_GROUP_KEY*, IUnknown*, IUnknown*, int>)(lpVtbl[25]))((ID3D12StateObjectDatabase1*)Unsafe.AsPointer(ref this), pGroupKeys, pVersions, NumGroupKeys, pDesc, pStateObjectToGrowFromGroupKey, pCreatedObject, pStateObjectToGrowFrom);
    }

    public interface Interface : ID3D12StateObjectDatabase.Interface
    {
        [VtblIndex(10)]
        HRESULT EnumeratePipelineStates([NativeTypeName("D3D12EnumeratePipelineStateFunc")] delegate* unmanaged[Stdcall]<D3D12_DATABASE_GROUP_KEY*, uint*, uint, D3D12_DATABASE_PIPELINE_STATE_KEY*, void*, int> CallbackFunc, void* pContext);

        [VtblIndex(11)]
        HRESULT EnumerateStateObjects([NativeTypeName("D3D12EnumerateStateObjectFunc")] delegate* unmanaged[Stdcall]<D3D12_DATABASE_GROUP_KEY*, uint*, uint, D3D12_DATABASE_STATE_OBJECT_KEY*, void*, int> CallbackFunc, void* pContext);

        [VtblIndex(12)]
        HRESULT GetStateObjectDependencies([NativeTypeName("const D3D12_DATABASE_GROUP_KEY *")] D3D12_DATABASE_GROUP_KEY* pGroupKey, [NativeTypeName("const D3D12_DATABASE_STATE_OBJECT_KEY *")] D3D12_DATABASE_STATE_OBJECT_KEY* pStateObjectKey, [NativeTypeName("D3D12StateObjectDependenciesFunc")] delegate* unmanaged[Stdcall]<D3D12_DATABASE_STATE_OBJECT_KEY*, uint, D3D12_DATABASE_STATE_OBJECT_KEY*, void*, void> CallbackFunc, void* pContext);

        [VtblIndex(13)]
        HRESULT SetApplicationDesc1([NativeTypeName("const D3D12_APPLICATION_DESC *")] D3D12_APPLICATION_DESC* pApplicationDesc, [NativeTypeName("const GUID &")] Guid* AppId);

        [VtblIndex(14)]
        HRESULT GetApplicationDesc1([NativeTypeName("D3D12ApplicationDescFunc1")] delegate* unmanaged[Stdcall]<D3D12_APPLICATION_DESC*, Guid*, void*, void> CallbackFunc, void* pContext);

        [VtblIndex(15)]
        HRESULT FindPipelineStateDesc1([NativeTypeName("const D3D12_DATABASE_GROUP_KEY *")] D3D12_DATABASE_GROUP_KEY* pGroupKey, [NativeTypeName("const D3D12_DATABASE_PIPELINE_STATE_KEY *")] D3D12_DATABASE_PIPELINE_STATE_KEY* pPipelineStateKey, D3D12_FIND_PIPELINE_STATE_DESC_FLAGS Flags, [NativeTypeName("D3D12PipelineStateFunc1")] delegate* unmanaged[Stdcall]<D3D12_DATABASE_GROUP_KEY*, uint*, uint, D3D12_DATABASE_PIPELINE_STATE_KEY*, D3D12_PIPELINE_STATE_STREAM_DESC*, void*, void> CallbackFunc, [NativeTypeName("D3D12CreateRootSignatureFunc")] delegate* unmanaged[Stdcall]<D3D12_DATABASE_ROOT_SIGNATURE_KEY*, void*, nuint, ID3D12RootSignature**, void*, HRESULT> pfnCreateRootSignature, [NativeTypeName("D3D12LookupRootSignatureFunc")] delegate* unmanaged[Stdcall]<D3D12_DATABASE_ROOT_SIGNATURE_KEY*, ID3D12RootSignature**, void*, HRESULT> pfnLookupRootSignature, void* pContext);

        [VtblIndex(16)]
        HRESULT FindStateObjectDesc1([NativeTypeName("const D3D12_DATABASE_GROUP_KEY *")] D3D12_DATABASE_GROUP_KEY* pGroupKey, [NativeTypeName("const D3D12_DATABASE_STATE_OBJECT_KEY *")] D3D12_DATABASE_STATE_OBJECT_KEY* pStateObjectKey, D3D12_FIND_STATE_OBJECT_DESC_FLAGS Flags, [NativeTypeName("D3D12StateObjectFunc1")] delegate* unmanaged[Stdcall]<D3D12_DATABASE_GROUP_KEY*, uint*, uint, D3D12_DATABASE_STATE_OBJECT_KEY*, D3D12_STATE_OBJECT_DESC*, D3D12_DATABASE_STATE_OBJECT_KEY*, D3D12_DATABASE_GROUP_KEY*, void*, void> CallbackFunc, [NativeTypeName("D3D12LookupStateObjectFunc")] delegate* unmanaged[Stdcall]<D3D12_DATABASE_STATE_OBJECT_KEY*, D3D12_DATABASE_GROUP_KEY*, IUnknown**, void*, HRESULT> pfnLookupStateObject, [NativeTypeName("D3D12CreateRootSignatureFunc")] delegate* unmanaged[Stdcall]<D3D12_DATABASE_ROOT_SIGNATURE_KEY*, void*, nuint, ID3D12RootSignature**, void*, HRESULT> pfnCreateRootSignature, [NativeTypeName("D3D12LookupRootSignatureFunc")] delegate* unmanaged[Stdcall]<D3D12_DATABASE_ROOT_SIGNATURE_KEY*, ID3D12RootSignature**, void*, HRESULT> pfnLookupRootSignature, void* pContext);

        [VtblIndex(17)]
        HRESULT GetPipelineStateDescCount(uint* pCount);

        [VtblIndex(18)]
        HRESULT GetStateObjectDescCount(uint* pCount);

        [VtblIndex(19)]
        HRESULT BeginTransaction(D3D12_TRANSACTION_TYPE Type);

        [VtblIndex(20)]
        HRESULT CommitTransaction();

        [VtblIndex(21)]
        HRESULT RollbackTransaction();

        [VtblIndex(22)]
        [return: NativeTypeName("BOOL")]
        int IsTransactionActive();

        [VtblIndex(23)]
        HRESULT Checkpoint(D3D12_CHECKPOINT_MODE CheckpointMode, D3D12_JOURNAL_MODE JournalMode, D3D12_JOURNAL_MODE* pPreviousJournalMode);

        [VtblIndex(24)]
        HRESULT StorePipelineStateDesc1([NativeTypeName("const D3D12_DATABASE_GROUP_KEY *")] D3D12_DATABASE_GROUP_KEY* pGroupKeys, [NativeTypeName("const UINT *")] uint* pVersions, uint NumGroupKeys, [NativeTypeName("const D3D12_PIPELINE_STATE_STREAM_DESC *")] D3D12_PIPELINE_STATE_STREAM_DESC* pDesc);

        [VtblIndex(25)]
        HRESULT StoreStateObjectDesc1([NativeTypeName("const D3D12_DATABASE_GROUP_KEY *")] D3D12_DATABASE_GROUP_KEY* pGroupKeys, [NativeTypeName("const UINT *")] uint* pVersions, uint NumGroupKeys, [NativeTypeName("const D3D12_STATE_OBJECT_DESC *")] D3D12_STATE_OBJECT_DESC* pDesc, [NativeTypeName("const D3D12_DATABASE_GROUP_KEY *")] D3D12_DATABASE_GROUP_KEY* pStateObjectToGrowFromGroupKey, IUnknown* pCreatedObject, IUnknown* pStateObjectToGrowFrom);
    }
}
