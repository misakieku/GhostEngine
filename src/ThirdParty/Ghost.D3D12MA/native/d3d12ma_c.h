#pragma once

#if defined(D3D12MA_C_EXPORTS)
    #define D3D12MA_C_API __declspec(dllexport)
#else
    #define D3D12MA_C_API __declspec(dllimport)
#endif

#include <d3d12.h>
#include <dxgi1_4.h>

#ifdef __cplusplus
extern "C" {
#endif

typedef struct D3D12MA_Allocator D3D12MA_Allocator;
typedef struct D3D12MA_Allocation D3D12MA_Allocation;
typedef struct D3D12MA_Pool D3D12MA_Pool;
typedef struct D3D12MA_DefragmentationContext D3D12MA_DefragmentationContext;
typedef struct D3D12MA_VirtualBlock D3D12MA_VirtualBlock;

typedef UINT64 D3D12MA_AllocHandle;

typedef void* (*D3D12MA_ALLOCATE_FUNC_PTR)(size_t Size, size_t Alignment, void* pPrivateData);
typedef void (*D3D12MA_FREE_FUNC_PTR)(void* pMemory, void* pPrivateData);

typedef struct D3D12MA_ALLOCATION_CALLBACKS
{
    D3D12MA_ALLOCATE_FUNC_PTR pAllocate;
    D3D12MA_FREE_FUNC_PTR pFree;
    void* pPrivateData;
} D3D12MA_ALLOCATION_CALLBACKS;

typedef enum D3D12MA_ALLOCATION_FLAGS
{
    D3D12MA_ALLOCATION_FLAG_NONE = 0,
    D3D12MA_ALLOCATION_FLAG_COMMITTED = 0x1,
    D3D12MA_ALLOCATION_FLAG_NEVER_ALLOCATE = 0x2,
    D3D12MA_ALLOCATION_FLAG_WITHIN_BUDGET = 0x4,
    D3D12MA_ALLOCATION_FLAG_UPPER_ADDRESS = 0x8,
    D3D12MA_ALLOCATION_FLAG_CAN_ALIAS = 0x10,
    D3D12MA_ALLOCATION_FLAG_STRATEGY_MIN_MEMORY = 0x00010000,
    D3D12MA_ALLOCATION_FLAG_STRATEGY_MIN_TIME = 0x00020000,
    D3D12MA_ALLOCATION_FLAG_STRATEGY_MIN_OFFSET = 0x0004000,
    D3D12MA_ALLOCATION_FLAG_STRATEGY_BEST_FIT = D3D12MA_ALLOCATION_FLAG_STRATEGY_MIN_MEMORY,
    D3D12MA_ALLOCATION_FLAG_STRATEGY_FIRST_FIT = D3D12MA_ALLOCATION_FLAG_STRATEGY_MIN_TIME,
    D3D12MA_ALLOCATION_FLAG_STRATEGY_MASK =
        D3D12MA_ALLOCATION_FLAG_STRATEGY_MIN_MEMORY |
        D3D12MA_ALLOCATION_FLAG_STRATEGY_MIN_TIME |
        D3D12MA_ALLOCATION_FLAG_STRATEGY_MIN_OFFSET
} D3D12MA_ALLOCATION_FLAGS;

typedef struct D3D12MA_ALLOCATION_DESC
{
    D3D12MA_ALLOCATION_FLAGS Flags;
    D3D12_HEAP_TYPE HeapType;
    D3D12_HEAP_FLAGS ExtraHeapFlags;
    D3D12MA_Pool* CustomPool;
    void* pPrivateData;
} D3D12MA_ALLOCATION_DESC;

typedef struct D3D12MA_Statistics
{
    UINT BlockCount;
    UINT AllocationCount;
    UINT64 BlockBytes;
    UINT64 AllocationBytes;
} D3D12MA_Statistics;

typedef struct D3D12MA_DetailedStatistics
{
    D3D12MA_Statistics Stats;
    UINT UnusedRangeCount;
    UINT64 AllocationSizeMin;
    UINT64 AllocationSizeMax;
    UINT64 UnusedRangeSizeMin;
    UINT64 UnusedRangeSizeMax;
} D3D12MA_DetailedStatistics;

typedef struct D3D12MA_TotalStatistics
{
    D3D12MA_DetailedStatistics HeapType[5];
    D3D12MA_DetailedStatistics MemorySegmentGroup[2];
    D3D12MA_DetailedStatistics Total;
} D3D12MA_TotalStatistics;

typedef struct D3D12MA_Budget
{
    D3D12MA_Statistics Stats;
    UINT64 UsageBytes;
    UINT64 BudgetBytes;
} D3D12MA_Budget;

typedef struct D3D12MA_VirtualAllocation
{
    D3D12MA_AllocHandle AllocHandle;
} D3D12MA_VirtualAllocation;

typedef enum D3D12MA_DEFRAGMENTATION_FLAGS
{
    D3D12MA_DEFRAGMENTATION_FLAG_ALGORITHM_FAST = 0x1,
    D3D12MA_DEFRAGMENTATION_FLAG_ALGORITHM_BALANCED = 0x2,
    D3D12MA_DEFRAGMENTATION_FLAG_ALGORITHM_FULL = 0x4,
    D3D12MA_DEFRAGMENTATION_FLAG_ALGORITHM_MASK =
        D3D12MA_DEFRAGMENTATION_FLAG_ALGORITHM_FAST |
        D3D12MA_DEFRAGMENTATION_FLAG_ALGORITHM_BALANCED |
        D3D12MA_DEFRAGMENTATION_FLAG_ALGORITHM_FULL
} D3D12MA_DEFRAGMENTATION_FLAGS;

typedef struct D3D12MA_DEFRAGMENTATION_DESC
{
    D3D12MA_DEFRAGMENTATION_FLAGS Flags;
    UINT64 MaxBytesPerPass;
    UINT32 MaxAllocationsPerPass;
} D3D12MA_DEFRAGMENTATION_DESC;

typedef enum D3D12MA_DEFRAGMENTATION_MOVE_OPERATION
{
    D3D12MA_DEFRAGMENTATION_MOVE_OPERATION_COPY = 0,
    D3D12MA_DEFRAGMENTATION_MOVE_OPERATION_IGNORE = 1,
    D3D12MA_DEFRAGMENTATION_MOVE_OPERATION_DESTROY = 2
} D3D12MA_DEFRAGMENTATION_MOVE_OPERATION;

typedef struct D3D12MA_DEFRAGMENTATION_MOVE
{
    D3D12MA_DEFRAGMENTATION_MOVE_OPERATION Operation;
    D3D12MA_Allocation* pSrcAllocation;
    D3D12MA_Allocation* pDstTmpAllocation;
} D3D12MA_DEFRAGMENTATION_MOVE;

typedef struct D3D12MA_DEFRAGMENTATION_PASS_MOVE_INFO
{
    UINT32 MoveCount;
    D3D12MA_DEFRAGMENTATION_MOVE* pMoves;
} D3D12MA_DEFRAGMENTATION_PASS_MOVE_INFO;

typedef struct D3D12MA_DEFRAGMENTATION_STATS
{
    UINT64 BytesMoved;
    UINT64 BytesFreed;
    UINT32 AllocationsMoved;
    UINT32 HeapsFreed;
} D3D12MA_DEFRAGMENTATION_STATS;

typedef enum D3D12MA_POOL_FLAGS
{
    D3D12MA_POOL_FLAG_NONE = 0,
    D3D12MA_POOL_FLAG_ALGORITHM_LINEAR = 0x1,
    D3D12MA_POOL_FLAG_MSAA_TEXTURES_ALWAYS_COMMITTED = 0x2,
    D3D12MA_POOL_FLAG_ALWAYS_COMMITTED = 0x4,
    D3D12MA_POOL_FLAG_DONT_USE_TIGHT_ALIGNMENT = 0x8,
    D3D12MA_POOL_FLAG_ALGORITHM_MASK = D3D12MA_POOL_FLAG_ALGORITHM_LINEAR
} D3D12MA_POOL_FLAGS;

typedef struct D3D12MA_POOL_DESC
{
    D3D12MA_POOL_FLAGS Flags;
    D3D12_HEAP_PROPERTIES HeapProperties;
    D3D12_HEAP_FLAGS HeapFlags;
    UINT64 BlockSize;
    UINT MinBlockCount;
    UINT MaxBlockCount;
    UINT64 MinAllocationAlignment;
    ID3D12ProtectedResourceSession* pProtectedSession;
    D3D12_RESIDENCY_PRIORITY ResidencyPriority;
} D3D12MA_POOL_DESC;

typedef enum D3D12MA_ALLOCATOR_FLAGS
{
    D3D12MA_ALLOCATOR_FLAG_NONE = 0,
    D3D12MA_ALLOCATOR_FLAG_SINGLETHREADED = 0x1,
    D3D12MA_ALLOCATOR_FLAG_ALWAYS_COMMITTED = 0x2,
    D3D12MA_ALLOCATOR_FLAG_DEFAULT_POOLS_NOT_ZEROED = 0x4,
    D3D12MA_ALLOCATOR_FLAG_MSAA_TEXTURES_ALWAYS_COMMITTED = 0x8,
    D3D12MA_ALLOCATOR_FLAG_DONT_PREFER_SMALL_BUFFERS_COMMITTED = 0x10,
    D3D12MA_ALLOCATOR_FLAG_DONT_USE_TIGHT_ALIGNMENT = 0x20
} D3D12MA_ALLOCATOR_FLAGS;

typedef struct D3D12MA_ALLOCATOR_DESC
{
    D3D12MA_ALLOCATOR_FLAGS Flags;
    ID3D12Device* pDevice;
    UINT64 PreferredBlockSize;
    const D3D12MA_ALLOCATION_CALLBACKS* pAllocationCallbacks;
    IDXGIAdapter* pAdapter;
} D3D12MA_ALLOCATOR_DESC;

typedef enum D3D12MA_VIRTUAL_BLOCK_FLAGS
{
    D3D12MA_VIRTUAL_BLOCK_FLAG_NONE = 0,
    D3D12MA_VIRTUAL_BLOCK_FLAG_ALGORITHM_LINEAR = D3D12MA_POOL_FLAG_ALGORITHM_LINEAR,
    D3D12MA_VIRTUAL_BLOCK_FLAG_ALGORITHM_MASK = D3D12MA_POOL_FLAG_ALGORITHM_MASK
} D3D12MA_VIRTUAL_BLOCK_FLAGS;

typedef struct D3D12MA_VIRTUAL_BLOCK_DESC
{
    D3D12MA_VIRTUAL_BLOCK_FLAGS Flags;
    UINT64 Size;
    const D3D12MA_ALLOCATION_CALLBACKS* pAllocationCallbacks;
} D3D12MA_VIRTUAL_BLOCK_DESC;

typedef enum D3D12MA_VIRTUAL_ALLOCATION_FLAGS
{
    D3D12MA_VIRTUAL_ALLOCATION_FLAG_NONE = 0,
    D3D12MA_VIRTUAL_ALLOCATION_FLAG_UPPER_ADDRESS = D3D12MA_ALLOCATION_FLAG_UPPER_ADDRESS,
    D3D12MA_VIRTUAL_ALLOCATION_FLAG_STRATEGY_MIN_MEMORY = D3D12MA_ALLOCATION_FLAG_STRATEGY_MIN_MEMORY,
    D3D12MA_VIRTUAL_ALLOCATION_FLAG_STRATEGY_MIN_TIME = D3D12MA_ALLOCATION_FLAG_STRATEGY_MIN_TIME,
    D3D12MA_VIRTUAL_ALLOCATION_FLAG_STRATEGY_MIN_OFFSET = D3D12MA_ALLOCATION_FLAG_STRATEGY_MIN_OFFSET,
    D3D12MA_VIRTUAL_ALLOCATION_FLAG_STRATEGY_MASK = D3D12MA_ALLOCATION_FLAG_STRATEGY_MASK
} D3D12MA_VIRTUAL_ALLOCATION_FLAGS;

typedef struct D3D12MA_VIRTUAL_ALLOCATION_DESC
{
    D3D12MA_VIRTUAL_ALLOCATION_FLAGS Flags;
    UINT64 Size;
    UINT64 Alignment;
    void* pPrivateData;
} D3D12MA_VIRTUAL_ALLOCATION_DESC;

typedef struct D3D12MA_VIRTUAL_ALLOCATION_INFO
{
    UINT64 Offset;
    UINT64 Size;
    void* pPrivateData;
} D3D12MA_VIRTUAL_ALLOCATION_INFO;

D3D12MA_C_API HRESULT D3D12MA_CreateAllocator(const D3D12MA_ALLOCATOR_DESC* pDesc, D3D12MA_Allocator** ppAllocator);
D3D12MA_C_API HRESULT D3D12MA_CreateVirtualBlock(const D3D12MA_VIRTUAL_BLOCK_DESC* pDesc, D3D12MA_VirtualBlock** ppVirtualBlock);

D3D12MA_C_API HRESULT D3D12MA_Allocator_QueryInterface(D3D12MA_Allocator* pAllocator, const GUID* riid, void** ppvObject);
D3D12MA_C_API ULONG D3D12MA_Allocator_AddRef(D3D12MA_Allocator* pAllocator);
D3D12MA_C_API ULONG D3D12MA_Allocator_Release(D3D12MA_Allocator* pAllocator);
D3D12MA_C_API const D3D12_FEATURE_DATA_D3D12_OPTIONS* D3D12MA_Allocator_GetD3D12Options(const D3D12MA_Allocator* pAllocator);
D3D12MA_C_API BOOL D3D12MA_Allocator_IsUMA(const D3D12MA_Allocator* pAllocator);
D3D12MA_C_API BOOL D3D12MA_Allocator_IsCacheCoherentUMA(const D3D12MA_Allocator* pAllocator);
D3D12MA_C_API BOOL D3D12MA_Allocator_IsGPUUploadHeapSupported(const D3D12MA_Allocator* pAllocator);
D3D12MA_C_API BOOL D3D12MA_Allocator_IsTightAlignmentSupported(const D3D12MA_Allocator* pAllocator);
D3D12MA_C_API UINT64 D3D12MA_Allocator_GetMemoryCapacity(const D3D12MA_Allocator* pAllocator, UINT memorySegmentGroup);
D3D12MA_C_API HRESULT D3D12MA_Allocator_CreateResource(D3D12MA_Allocator* pAllocator, const D3D12MA_ALLOCATION_DESC* pAllocDesc, const D3D12_RESOURCE_DESC* pResourceDesc, D3D12_RESOURCE_STATES InitialResourceState, const D3D12_CLEAR_VALUE* pOptimizedClearValue, D3D12MA_Allocation** ppAllocation, const GUID* riidResource, void** ppvResource);
D3D12MA_C_API HRESULT D3D12MA_Allocator_CreateResource2(D3D12MA_Allocator* pAllocator, const D3D12MA_ALLOCATION_DESC* pAllocDesc, const D3D12_RESOURCE_DESC1* pResourceDesc, D3D12_RESOURCE_STATES InitialResourceState, const D3D12_CLEAR_VALUE* pOptimizedClearValue, D3D12MA_Allocation** ppAllocation, const GUID* riidResource, void** ppvResource);
D3D12MA_C_API HRESULT D3D12MA_Allocator_CreateResource3(D3D12MA_Allocator* pAllocator, const D3D12MA_ALLOCATION_DESC* pAllocDesc, const D3D12_RESOURCE_DESC1* pResourceDesc, D3D12_BARRIER_LAYOUT InitialLayout, const D3D12_CLEAR_VALUE* pOptimizedClearValue, UINT32 NumCastableFormats, const DXGI_FORMAT* pCastableFormats, D3D12MA_Allocation** ppAllocation, const GUID* riidResource, void** ppvResource);
D3D12MA_C_API HRESULT D3D12MA_Allocator_CreateResource4(D3D12MA_Allocator* pAllocator, const D3D12MA_ALLOCATION_DESC* pAllocDesc, const D3D12_RESOURCE_DESC2* pResourceDesc, D3D12_BARRIER_LAYOUT InitialLayout, const D3D12_CLEAR_VALUE* pOptimizedClearValue, UINT32 NumCastableFormats, const DXGI_FORMAT* pCastableFormats, D3D12MA_Allocation** ppAllocation, const GUID* riidResource, void** ppvResource);
D3D12MA_C_API HRESULT D3D12MA_Allocator_AllocateMemory(D3D12MA_Allocator* pAllocator, const D3D12MA_ALLOCATION_DESC* pAllocDesc, const D3D12_RESOURCE_ALLOCATION_INFO* pAllocInfo, D3D12MA_Allocation** ppAllocation);
D3D12MA_C_API HRESULT D3D12MA_Allocator_CreateAliasingResource(D3D12MA_Allocator* pAllocator, D3D12MA_Allocation* pAllocation, UINT64 AllocationLocalOffset, const D3D12_RESOURCE_DESC* pResourceDesc, D3D12_RESOURCE_STATES InitialResourceState, const D3D12_CLEAR_VALUE* pOptimizedClearValue, const GUID* riidResource, void** ppvResource);
D3D12MA_C_API HRESULT D3D12MA_Allocator_CreateAliasingResource1(D3D12MA_Allocator* pAllocator, D3D12MA_Allocation* pAllocation, UINT64 AllocationLocalOffset, const D3D12_RESOURCE_DESC1* pResourceDesc, D3D12_RESOURCE_STATES InitialResourceState, const D3D12_CLEAR_VALUE* pOptimizedClearValue, const GUID* riidResource, void** ppvResource);
D3D12MA_C_API HRESULT D3D12MA_Allocator_CreateAliasingResource2(D3D12MA_Allocator* pAllocator, D3D12MA_Allocation* pAllocation, UINT64 AllocationLocalOffset, const D3D12_RESOURCE_DESC1* pResourceDesc, D3D12_BARRIER_LAYOUT InitialLayout, const D3D12_CLEAR_VALUE* pOptimizedClearValue, UINT32 NumCastableFormats, const DXGI_FORMAT* pCastableFormats, const GUID* riidResource, void** ppvResource);
D3D12MA_C_API HRESULT D3D12MA_Allocator_CreateAliasingResource3(D3D12MA_Allocator* pAllocator, D3D12MA_Allocation* pAllocation, UINT64 AllocationLocalOffset, const D3D12_RESOURCE_DESC2* pResourceDesc, D3D12_BARRIER_LAYOUT InitialLayout, const D3D12_CLEAR_VALUE* pOptimizedClearValue, UINT32 NumCastableFormats, const DXGI_FORMAT* pCastableFormats, const GUID* riidResource, void** ppvResource);
D3D12MA_C_API HRESULT D3D12MA_Allocator_CreatePool(D3D12MA_Allocator* pAllocator, const D3D12MA_POOL_DESC* pPoolDesc, D3D12MA_Pool** ppPool);
D3D12MA_C_API void D3D12MA_Allocator_SetCurrentFrameIndex(D3D12MA_Allocator* pAllocator, UINT frameIndex);
D3D12MA_C_API void D3D12MA_Allocator_GetBudget(D3D12MA_Allocator* pAllocator, D3D12MA_Budget* pLocalBudget, D3D12MA_Budget* pNonLocalBudget);
D3D12MA_C_API void D3D12MA_Allocator_CalculateStatistics(D3D12MA_Allocator* pAllocator, D3D12MA_TotalStatistics* pStats);
D3D12MA_C_API void D3D12MA_Allocator_BuildStatsString(const D3D12MA_Allocator* pAllocator, WCHAR** ppStatsString, BOOL DetailedMap);
D3D12MA_C_API void D3D12MA_Allocator_FreeStatsString(const D3D12MA_Allocator* pAllocator, WCHAR* pStatsString);
D3D12MA_C_API void D3D12MA_Allocator_BeginDefragmentation(D3D12MA_Allocator* pAllocator, const D3D12MA_DEFRAGMENTATION_DESC* pDesc, D3D12MA_DefragmentationContext** ppContext);

D3D12MA_C_API HRESULT D3D12MA_Allocation_QueryInterface(D3D12MA_Allocation* pAllocation, const GUID* riid, void** ppvObject);
D3D12MA_C_API ULONG D3D12MA_Allocation_AddRef(D3D12MA_Allocation* pAllocation);
D3D12MA_C_API ULONG D3D12MA_Allocation_Release(D3D12MA_Allocation* pAllocation);
D3D12MA_C_API UINT64 D3D12MA_Allocation_GetOffset(const D3D12MA_Allocation* pAllocation);
D3D12MA_C_API UINT64 D3D12MA_Allocation_GetAlignment(const D3D12MA_Allocation* pAllocation);
D3D12MA_C_API UINT64 D3D12MA_Allocation_GetSize(const D3D12MA_Allocation* pAllocation);
D3D12MA_C_API ID3D12Resource* D3D12MA_Allocation_GetResource(const D3D12MA_Allocation* pAllocation);
D3D12MA_C_API void D3D12MA_Allocation_SetResource(D3D12MA_Allocation* pAllocation, ID3D12Resource* pResource);
D3D12MA_C_API ID3D12Heap* D3D12MA_Allocation_GetHeap(const D3D12MA_Allocation* pAllocation);
D3D12MA_C_API void D3D12MA_Allocation_SetPrivateData(D3D12MA_Allocation* pAllocation, void* pPrivateData);
D3D12MA_C_API void* D3D12MA_Allocation_GetPrivateData(const D3D12MA_Allocation* pAllocation);
D3D12MA_C_API void D3D12MA_Allocation_SetName(D3D12MA_Allocation* pAllocation, LPCWSTR Name);
D3D12MA_C_API LPCWSTR D3D12MA_Allocation_GetName(const D3D12MA_Allocation* pAllocation);

D3D12MA_C_API HRESULT D3D12MA_Pool_QueryInterface(D3D12MA_Pool* pPool, const GUID* riid, void** ppvObject);
D3D12MA_C_API ULONG D3D12MA_Pool_AddRef(D3D12MA_Pool* pPool);
D3D12MA_C_API ULONG D3D12MA_Pool_Release(D3D12MA_Pool* pPool);
D3D12MA_C_API void D3D12MA_Pool_GetDesc(const D3D12MA_Pool* pPool, D3D12MA_POOL_DESC* pOutDesc);
D3D12MA_C_API void D3D12MA_Pool_GetStatistics(D3D12MA_Pool* pPool, D3D12MA_Statistics* pStats);
D3D12MA_C_API void D3D12MA_Pool_CalculateStatistics(D3D12MA_Pool* pPool, D3D12MA_DetailedStatistics* pStats);
D3D12MA_C_API void D3D12MA_Pool_SetName(D3D12MA_Pool* pPool, LPCWSTR Name);
D3D12MA_C_API LPCWSTR D3D12MA_Pool_GetName(const D3D12MA_Pool* pPool);
D3D12MA_C_API HRESULT D3D12MA_Pool_BeginDefragmentation(D3D12MA_Pool* pPool, const D3D12MA_DEFRAGMENTATION_DESC* pDesc, D3D12MA_DefragmentationContext** ppContext);

D3D12MA_C_API HRESULT D3D12MA_DefragmentationContext_QueryInterface(D3D12MA_DefragmentationContext* pContext, const GUID* riid, void** ppvObject);
D3D12MA_C_API ULONG D3D12MA_DefragmentationContext_AddRef(D3D12MA_DefragmentationContext* pContext);
D3D12MA_C_API ULONG D3D12MA_DefragmentationContext_Release(D3D12MA_DefragmentationContext* pContext);
D3D12MA_C_API HRESULT D3D12MA_DefragmentationContext_BeginPass(D3D12MA_DefragmentationContext* pContext, D3D12MA_DEFRAGMENTATION_PASS_MOVE_INFO* pPassInfo);
D3D12MA_C_API HRESULT D3D12MA_DefragmentationContext_EndPass(D3D12MA_DefragmentationContext* pContext, D3D12MA_DEFRAGMENTATION_PASS_MOVE_INFO* pPassInfo);
D3D12MA_C_API void D3D12MA_DefragmentationContext_GetStats(D3D12MA_DefragmentationContext* pContext, D3D12MA_DEFRAGMENTATION_STATS* pStats);

D3D12MA_C_API HRESULT D3D12MA_VirtualBlock_QueryInterface(D3D12MA_VirtualBlock* pBlock, const GUID* riid, void** ppvObject);
D3D12MA_C_API ULONG D3D12MA_VirtualBlock_AddRef(D3D12MA_VirtualBlock* pBlock);
D3D12MA_C_API ULONG D3D12MA_VirtualBlock_Release(D3D12MA_VirtualBlock* pBlock);
D3D12MA_C_API BOOL D3D12MA_VirtualBlock_IsEmpty(const D3D12MA_VirtualBlock* pBlock);
D3D12MA_C_API void D3D12MA_VirtualBlock_GetAllocationInfo(const D3D12MA_VirtualBlock* pBlock, D3D12MA_VirtualAllocation allocation, D3D12MA_VIRTUAL_ALLOCATION_INFO* pInfo);
D3D12MA_C_API HRESULT D3D12MA_VirtualBlock_Allocate(D3D12MA_VirtualBlock* pBlock, const D3D12MA_VIRTUAL_ALLOCATION_DESC* pDesc, D3D12MA_VirtualAllocation* pAllocation, UINT64* pOffset);
D3D12MA_C_API void D3D12MA_VirtualBlock_FreeAllocation(D3D12MA_VirtualBlock* pBlock, D3D12MA_VirtualAllocation allocation);
D3D12MA_C_API void D3D12MA_VirtualBlock_Clear(D3D12MA_VirtualBlock* pBlock);
D3D12MA_C_API void D3D12MA_VirtualBlock_SetAllocationPrivateData(D3D12MA_VirtualBlock* pBlock, D3D12MA_VirtualAllocation allocation, void* pPrivateData);
D3D12MA_C_API void D3D12MA_VirtualBlock_GetStatistics(const D3D12MA_VirtualBlock* pBlock, D3D12MA_Statistics* pStats);
D3D12MA_C_API void D3D12MA_VirtualBlock_CalculateStatistics(const D3D12MA_VirtualBlock* pBlock, D3D12MA_DetailedStatistics* pStats);
D3D12MA_C_API void D3D12MA_VirtualBlock_BuildStatsString(const D3D12MA_VirtualBlock* pBlock, WCHAR** ppStatsString);
D3D12MA_C_API void D3D12MA_VirtualBlock_FreeStatsString(const D3D12MA_VirtualBlock* pBlock, WCHAR* pStatsString);

#ifdef __cplusplus
}
#endif
