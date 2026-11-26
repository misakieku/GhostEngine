using System.Runtime.InteropServices;

namespace Ghost.D3D12MA;

public static unsafe partial class Api
{
    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern HRESULT D3D12MA_CreateAllocator([NativeTypeName("const D3D12MA_ALLOCATOR_DESC *")] D3D12MA_ALLOCATOR_DESC* pDesc, D3D12MA_Allocator** ppAllocator);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern HRESULT D3D12MA_CreateVirtualBlock([NativeTypeName("const D3D12MA_VIRTUAL_BLOCK_DESC *")] D3D12MA_VIRTUAL_BLOCK_DESC* pDesc, D3D12MA_VirtualBlock** ppVirtualBlock);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern HRESULT D3D12MA_Allocator_QueryInterface(D3D12MA_Allocator* pAllocator, [NativeTypeName("const GUID *")] Guid* riid, void** ppvObject);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("ULONG")]
    public static extern uint D3D12MA_Allocator_AddRef(D3D12MA_Allocator* pAllocator);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("ULONG")]
    public static extern uint D3D12MA_Allocator_Release(D3D12MA_Allocator* pAllocator);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("const D3D12_FEATURE_DATA_D3D12_OPTIONS *")]
    public static extern D3D12_FEATURE_DATA_D3D12_OPTIONS* D3D12MA_Allocator_GetD3D12Options([NativeTypeName("const D3D12MA_Allocator *")] D3D12MA_Allocator* pAllocator);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("BOOL")]
    public static extern int D3D12MA_Allocator_IsUMA([NativeTypeName("const D3D12MA_Allocator *")] D3D12MA_Allocator* pAllocator);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("BOOL")]
    public static extern int D3D12MA_Allocator_IsCacheCoherentUMA([NativeTypeName("const D3D12MA_Allocator *")] D3D12MA_Allocator* pAllocator);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("BOOL")]
    public static extern int D3D12MA_Allocator_IsGPUUploadHeapSupported([NativeTypeName("const D3D12MA_Allocator *")] D3D12MA_Allocator* pAllocator);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("BOOL")]
    public static extern int D3D12MA_Allocator_IsTightAlignmentSupported([NativeTypeName("const D3D12MA_Allocator *")] D3D12MA_Allocator* pAllocator);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("UINT64")]
    public static extern ulong D3D12MA_Allocator_GetMemoryCapacity([NativeTypeName("const D3D12MA_Allocator *")] D3D12MA_Allocator* pAllocator, uint memorySegmentGroup);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern HRESULT D3D12MA_Allocator_CreateResource(D3D12MA_Allocator* pAllocator, [NativeTypeName("const D3D12MA_ALLOCATION_DESC *")] D3D12MA_ALLOCATION_DESC* pAllocDesc, [NativeTypeName("const D3D12_RESOURCE_DESC *")] D3D12_RESOURCE_DESC* pResourceDesc, D3D12_RESOURCE_STATES InitialResourceState, [NativeTypeName("const D3D12_CLEAR_VALUE *")] D3D12_CLEAR_VALUE* pOptimizedClearValue, D3D12MA_Allocation** ppAllocation, [NativeTypeName("const GUID *")] Guid* riidResource, void** ppvResource);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern HRESULT D3D12MA_Allocator_CreateResource2(D3D12MA_Allocator* pAllocator, [NativeTypeName("const D3D12MA_ALLOCATION_DESC *")] D3D12MA_ALLOCATION_DESC* pAllocDesc, [NativeTypeName("const D3D12_RESOURCE_DESC1 *")] D3D12_RESOURCE_DESC1* pResourceDesc, D3D12_RESOURCE_STATES InitialResourceState, [NativeTypeName("const D3D12_CLEAR_VALUE *")] D3D12_CLEAR_VALUE* pOptimizedClearValue, D3D12MA_Allocation** ppAllocation, [NativeTypeName("const GUID *")] Guid* riidResource, void** ppvResource);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern HRESULT D3D12MA_Allocator_CreateResource3(D3D12MA_Allocator* pAllocator, [NativeTypeName("const D3D12MA_ALLOCATION_DESC *")] D3D12MA_ALLOCATION_DESC* pAllocDesc, [NativeTypeName("const D3D12_RESOURCE_DESC1 *")] D3D12_RESOURCE_DESC1* pResourceDesc, D3D12_BARRIER_LAYOUT InitialLayout, [NativeTypeName("const D3D12_CLEAR_VALUE *")] D3D12_CLEAR_VALUE* pOptimizedClearValue, [NativeTypeName("UINT32")] uint NumCastableFormats, [NativeTypeName("const DXGI_FORMAT *")] DXGI_FORMAT* pCastableFormats, D3D12MA_Allocation** ppAllocation, [NativeTypeName("const GUID *")] Guid* riidResource, void** ppvResource);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern HRESULT D3D12MA_Allocator_CreateResource4(D3D12MA_Allocator* pAllocator, [NativeTypeName("const D3D12MA_ALLOCATION_DESC *")] D3D12MA_ALLOCATION_DESC* pAllocDesc, [NativeTypeName("const D3D12_RESOURCE_DESC2 *")] D3D12_RESOURCE_DESC2* pResourceDesc, D3D12_BARRIER_LAYOUT InitialLayout, [NativeTypeName("const D3D12_CLEAR_VALUE *")] D3D12_CLEAR_VALUE* pOptimizedClearValue, [NativeTypeName("UINT32")] uint NumCastableFormats, [NativeTypeName("const DXGI_FORMAT *")] DXGI_FORMAT* pCastableFormats, D3D12MA_Allocation** ppAllocation, [NativeTypeName("const GUID *")] Guid* riidResource, void** ppvResource);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern HRESULT D3D12MA_Allocator_AllocateMemory(D3D12MA_Allocator* pAllocator, [NativeTypeName("const D3D12MA_ALLOCATION_DESC *")] D3D12MA_ALLOCATION_DESC* pAllocDesc, [NativeTypeName("const D3D12_RESOURCE_ALLOCATION_INFO *")] D3D12_RESOURCE_ALLOCATION_INFO* pAllocInfo, D3D12MA_Allocation** ppAllocation);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern HRESULT D3D12MA_Allocator_CreateAliasingResource(D3D12MA_Allocator* pAllocator, D3D12MA_Allocation* pAllocation, [NativeTypeName("UINT64")] ulong AllocationLocalOffset, [NativeTypeName("const D3D12_RESOURCE_DESC *")] D3D12_RESOURCE_DESC* pResourceDesc, D3D12_RESOURCE_STATES InitialResourceState, [NativeTypeName("const D3D12_CLEAR_VALUE *")] D3D12_CLEAR_VALUE* pOptimizedClearValue, [NativeTypeName("const GUID *")] Guid* riidResource, void** ppvResource);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern HRESULT D3D12MA_Allocator_CreateAliasingResource1(D3D12MA_Allocator* pAllocator, D3D12MA_Allocation* pAllocation, [NativeTypeName("UINT64")] ulong AllocationLocalOffset, [NativeTypeName("const D3D12_RESOURCE_DESC1 *")] D3D12_RESOURCE_DESC1* pResourceDesc, D3D12_RESOURCE_STATES InitialResourceState, [NativeTypeName("const D3D12_CLEAR_VALUE *")] D3D12_CLEAR_VALUE* pOptimizedClearValue, [NativeTypeName("const GUID *")] Guid* riidResource, void** ppvResource);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern HRESULT D3D12MA_Allocator_CreateAliasingResource2(D3D12MA_Allocator* pAllocator, D3D12MA_Allocation* pAllocation, [NativeTypeName("UINT64")] ulong AllocationLocalOffset, [NativeTypeName("const D3D12_RESOURCE_DESC1 *")] D3D12_RESOURCE_DESC1* pResourceDesc, D3D12_BARRIER_LAYOUT InitialLayout, [NativeTypeName("const D3D12_CLEAR_VALUE *")] D3D12_CLEAR_VALUE* pOptimizedClearValue, [NativeTypeName("UINT32")] uint NumCastableFormats, [NativeTypeName("const DXGI_FORMAT *")] DXGI_FORMAT* pCastableFormats, [NativeTypeName("const GUID *")] Guid* riidResource, void** ppvResource);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern HRESULT D3D12MA_Allocator_CreateAliasingResource3(D3D12MA_Allocator* pAllocator, D3D12MA_Allocation* pAllocation, [NativeTypeName("UINT64")] ulong AllocationLocalOffset, [NativeTypeName("const D3D12_RESOURCE_DESC2 *")] D3D12_RESOURCE_DESC2* pResourceDesc, D3D12_BARRIER_LAYOUT InitialLayout, [NativeTypeName("const D3D12_CLEAR_VALUE *")] D3D12_CLEAR_VALUE* pOptimizedClearValue, [NativeTypeName("UINT32")] uint NumCastableFormats, [NativeTypeName("const DXGI_FORMAT *")] DXGI_FORMAT* pCastableFormats, [NativeTypeName("const GUID *")] Guid* riidResource, void** ppvResource);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern HRESULT D3D12MA_Allocator_CreatePool(D3D12MA_Allocator* pAllocator, [NativeTypeName("const D3D12MA_POOL_DESC *")] D3D12MA_POOL_DESC* pPoolDesc, D3D12MA_Pool** ppPool);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void D3D12MA_Allocator_SetCurrentFrameIndex(D3D12MA_Allocator* pAllocator, uint frameIndex);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void D3D12MA_Allocator_GetBudget(D3D12MA_Allocator* pAllocator, D3D12MA_Budget* pLocalBudget, D3D12MA_Budget* pNonLocalBudget);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void D3D12MA_Allocator_CalculateStatistics(D3D12MA_Allocator* pAllocator, D3D12MA_TotalStatistics* pStats);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void D3D12MA_Allocator_BuildStatsString([NativeTypeName("const D3D12MA_Allocator *")] D3D12MA_Allocator* pAllocator, [NativeTypeName("WCHAR **")] char** ppStatsString, [NativeTypeName("BOOL")] int DetailedMap);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void D3D12MA_Allocator_FreeStatsString([NativeTypeName("const D3D12MA_Allocator *")] D3D12MA_Allocator* pAllocator, [NativeTypeName("WCHAR *")] char* pStatsString);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void D3D12MA_Allocator_BeginDefragmentation(D3D12MA_Allocator* pAllocator, [NativeTypeName("const D3D12MA_DEFRAGMENTATION_DESC *")] D3D12MA_DEFRAGMENTATION_DESC* pDesc, D3D12MA_DefragmentationContext** ppContext);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern HRESULT D3D12MA_Allocation_QueryInterface(D3D12MA_Allocation* pAllocation, [NativeTypeName("const GUID *")] Guid* riid, void** ppvObject);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("ULONG")]
    public static extern uint D3D12MA_Allocation_AddRef(D3D12MA_Allocation* pAllocation);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("ULONG")]
    public static extern uint D3D12MA_Allocation_Release(D3D12MA_Allocation* pAllocation);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("UINT64")]
    public static extern ulong D3D12MA_Allocation_GetOffset([NativeTypeName("const D3D12MA_Allocation *")] D3D12MA_Allocation* pAllocation);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("UINT64")]
    public static extern ulong D3D12MA_Allocation_GetAlignment([NativeTypeName("const D3D12MA_Allocation *")] D3D12MA_Allocation* pAllocation);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("UINT64")]
    public static extern ulong D3D12MA_Allocation_GetSize([NativeTypeName("const D3D12MA_Allocation *")] D3D12MA_Allocation* pAllocation);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ID3D12Resource* D3D12MA_Allocation_GetResource([NativeTypeName("const D3D12MA_Allocation *")] D3D12MA_Allocation* pAllocation);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void D3D12MA_Allocation_SetResource(D3D12MA_Allocation* pAllocation, ID3D12Resource* pResource);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ID3D12Heap* D3D12MA_Allocation_GetHeap([NativeTypeName("const D3D12MA_Allocation *")] D3D12MA_Allocation* pAllocation);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void D3D12MA_Allocation_SetPrivateData(D3D12MA_Allocation* pAllocation, void* pPrivateData);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void* D3D12MA_Allocation_GetPrivateData([NativeTypeName("const D3D12MA_Allocation *")] D3D12MA_Allocation* pAllocation);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void D3D12MA_Allocation_SetName(D3D12MA_Allocation* pAllocation, [NativeTypeName("LPCWSTR")] char* Name);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("LPCWSTR")]
    public static extern char* D3D12MA_Allocation_GetName([NativeTypeName("const D3D12MA_Allocation *")] D3D12MA_Allocation* pAllocation);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern HRESULT D3D12MA_Pool_QueryInterface(D3D12MA_Pool* pPool, [NativeTypeName("const GUID *")] Guid* riid, void** ppvObject);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("ULONG")]
    public static extern uint D3D12MA_Pool_AddRef(D3D12MA_Pool* pPool);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("ULONG")]
    public static extern uint D3D12MA_Pool_Release(D3D12MA_Pool* pPool);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void D3D12MA_Pool_GetDesc([NativeTypeName("const D3D12MA_Pool *")] D3D12MA_Pool* pPool, D3D12MA_POOL_DESC* pOutDesc);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void D3D12MA_Pool_GetStatistics(D3D12MA_Pool* pPool, D3D12MA_Statistics* pStats);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void D3D12MA_Pool_CalculateStatistics(D3D12MA_Pool* pPool, D3D12MA_DetailedStatistics* pStats);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void D3D12MA_Pool_SetName(D3D12MA_Pool* pPool, [NativeTypeName("LPCWSTR")] char* Name);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("LPCWSTR")]
    public static extern char* D3D12MA_Pool_GetName([NativeTypeName("const D3D12MA_Pool *")] D3D12MA_Pool* pPool);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern HRESULT D3D12MA_Pool_BeginDefragmentation(D3D12MA_Pool* pPool, [NativeTypeName("const D3D12MA_DEFRAGMENTATION_DESC *")] D3D12MA_DEFRAGMENTATION_DESC* pDesc, D3D12MA_DefragmentationContext** ppContext);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern HRESULT D3D12MA_DefragmentationContext_QueryInterface(D3D12MA_DefragmentationContext* pContext, [NativeTypeName("const GUID *")] Guid* riid, void** ppvObject);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("ULONG")]
    public static extern uint D3D12MA_DefragmentationContext_AddRef(D3D12MA_DefragmentationContext* pContext);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("ULONG")]
    public static extern uint D3D12MA_DefragmentationContext_Release(D3D12MA_DefragmentationContext* pContext);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern HRESULT D3D12MA_DefragmentationContext_BeginPass(D3D12MA_DefragmentationContext* pContext, D3D12MA_DEFRAGMENTATION_PASS_MOVE_INFO* pPassInfo);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern HRESULT D3D12MA_DefragmentationContext_EndPass(D3D12MA_DefragmentationContext* pContext, D3D12MA_DEFRAGMENTATION_PASS_MOVE_INFO* pPassInfo);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void D3D12MA_DefragmentationContext_GetStats(D3D12MA_DefragmentationContext* pContext, D3D12MA_DEFRAGMENTATION_STATS* pStats);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern HRESULT D3D12MA_VirtualBlock_QueryInterface(D3D12MA_VirtualBlock* pBlock, [NativeTypeName("const GUID *")] Guid* riid, void** ppvObject);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("ULONG")]
    public static extern uint D3D12MA_VirtualBlock_AddRef(D3D12MA_VirtualBlock* pBlock);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("ULONG")]
    public static extern uint D3D12MA_VirtualBlock_Release(D3D12MA_VirtualBlock* pBlock);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("BOOL")]
    public static extern int D3D12MA_VirtualBlock_IsEmpty([NativeTypeName("const D3D12MA_VirtualBlock *")] D3D12MA_VirtualBlock* pBlock);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void D3D12MA_VirtualBlock_GetAllocationInfo([NativeTypeName("const D3D12MA_VirtualBlock *")] D3D12MA_VirtualBlock* pBlock, D3D12MA_VirtualAllocation allocation, D3D12MA_VIRTUAL_ALLOCATION_INFO* pInfo);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern HRESULT D3D12MA_VirtualBlock_Allocate(D3D12MA_VirtualBlock* pBlock, [NativeTypeName("const D3D12MA_VIRTUAL_ALLOCATION_DESC *")] D3D12MA_VIRTUAL_ALLOCATION_DESC* pDesc, D3D12MA_VirtualAllocation* pAllocation, [NativeTypeName("UINT64 *")] ulong* pOffset);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void D3D12MA_VirtualBlock_FreeAllocation(D3D12MA_VirtualBlock* pBlock, D3D12MA_VirtualAllocation allocation);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void D3D12MA_VirtualBlock_Clear(D3D12MA_VirtualBlock* pBlock);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void D3D12MA_VirtualBlock_SetAllocationPrivateData(D3D12MA_VirtualBlock* pBlock, D3D12MA_VirtualAllocation allocation, void* pPrivateData);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void D3D12MA_VirtualBlock_GetStatistics([NativeTypeName("const D3D12MA_VirtualBlock *")] D3D12MA_VirtualBlock* pBlock, D3D12MA_Statistics* pStats);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void D3D12MA_VirtualBlock_CalculateStatistics([NativeTypeName("const D3D12MA_VirtualBlock *")] D3D12MA_VirtualBlock* pBlock, D3D12MA_DetailedStatistics* pStats);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void D3D12MA_VirtualBlock_BuildStatsString([NativeTypeName("const D3D12MA_VirtualBlock *")] D3D12MA_VirtualBlock* pBlock, [NativeTypeName("WCHAR **")] char** ppStatsString);

    [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void D3D12MA_VirtualBlock_FreeStatsString([NativeTypeName("const D3D12MA_VirtualBlock *")] D3D12MA_VirtualBlock* pBlock, [NativeTypeName("WCHAR *")] char* pStatsString);
}
