#define D3D12MA_C_EXPORTS
#include "D3D12MemAlloc.h"
#include "d3d12ma_c.h"

#include <cstddef>

static_assert(sizeof(D3D12MA_ALLOCATION_CALLBACKS) == sizeof(D3D12MA::ALLOCATION_CALLBACKS));
static_assert(sizeof(D3D12MA_ALLOCATION_DESC) == sizeof(D3D12MA::ALLOCATION_DESC));
static_assert(sizeof(D3D12MA_Statistics) == sizeof(D3D12MA::Statistics));
static_assert(sizeof(D3D12MA_DetailedStatistics) == sizeof(D3D12MA::DetailedStatistics));
static_assert(sizeof(D3D12MA_TotalStatistics) == sizeof(D3D12MA::TotalStatistics));
static_assert(sizeof(D3D12MA_Budget) == sizeof(D3D12MA::Budget));
static_assert(sizeof(D3D12MA_VirtualAllocation) == sizeof(D3D12MA::VirtualAllocation));
static_assert(sizeof(D3D12MA_DEFRAGMENTATION_DESC) == sizeof(D3D12MA::DEFRAGMENTATION_DESC));
static_assert(sizeof(D3D12MA_DEFRAGMENTATION_MOVE) == sizeof(D3D12MA::DEFRAGMENTATION_MOVE));
static_assert(sizeof(D3D12MA_DEFRAGMENTATION_PASS_MOVE_INFO) == sizeof(D3D12MA::DEFRAGMENTATION_PASS_MOVE_INFO));
static_assert(sizeof(D3D12MA_DEFRAGMENTATION_STATS) == sizeof(D3D12MA::DEFRAGMENTATION_STATS));
static_assert(sizeof(D3D12MA_POOL_DESC) == sizeof(D3D12MA::POOL_DESC));
static_assert(sizeof(D3D12MA_ALLOCATOR_DESC) == sizeof(D3D12MA::ALLOCATOR_DESC));
static_assert(sizeof(D3D12MA_VIRTUAL_BLOCK_DESC) == sizeof(D3D12MA::VIRTUAL_BLOCK_DESC));
static_assert(sizeof(D3D12MA_VIRTUAL_ALLOCATION_DESC) == sizeof(D3D12MA::VIRTUAL_ALLOCATION_DESC));
static_assert(sizeof(D3D12MA_VIRTUAL_ALLOCATION_INFO) == sizeof(D3D12MA::VIRTUAL_ALLOCATION_INFO));

static D3D12MA::Allocator* Unwrap(D3D12MA_Allocator* p) { return reinterpret_cast<D3D12MA::Allocator*>(p); }
static const D3D12MA::Allocator* Unwrap(const D3D12MA_Allocator* p) { return reinterpret_cast<const D3D12MA::Allocator*>(p); }
static D3D12MA::Allocation* Unwrap(D3D12MA_Allocation* p) { return reinterpret_cast<D3D12MA::Allocation*>(p); }
static const D3D12MA::Allocation* Unwrap(const D3D12MA_Allocation* p) { return reinterpret_cast<const D3D12MA::Allocation*>(p); }
static D3D12MA::Pool* Unwrap(D3D12MA_Pool* p) { return reinterpret_cast<D3D12MA::Pool*>(p); }
static const D3D12MA::Pool* Unwrap(const D3D12MA_Pool* p) { return reinterpret_cast<const D3D12MA::Pool*>(p); }
static D3D12MA::DefragmentationContext* Unwrap(D3D12MA_DefragmentationContext* p) { return reinterpret_cast<D3D12MA::DefragmentationContext*>(p); }
static D3D12MA::VirtualBlock* Unwrap(D3D12MA_VirtualBlock* p) { return reinterpret_cast<D3D12MA::VirtualBlock*>(p); }
static const D3D12MA::VirtualBlock* Unwrap(const D3D12MA_VirtualBlock* p) { return reinterpret_cast<const D3D12MA::VirtualBlock*>(p); }

extern "C" {

HRESULT D3D12MA_CreateAllocator(const D3D12MA_ALLOCATOR_DESC* pDesc, D3D12MA_Allocator** ppAllocator)
{
    return D3D12MA::CreateAllocator(reinterpret_cast<const D3D12MA::ALLOCATOR_DESC*>(pDesc), reinterpret_cast<D3D12MA::Allocator**>(ppAllocator));
}

HRESULT D3D12MA_CreateVirtualBlock(const D3D12MA_VIRTUAL_BLOCK_DESC* pDesc, D3D12MA_VirtualBlock** ppVirtualBlock)
{
    return D3D12MA::CreateVirtualBlock(reinterpret_cast<const D3D12MA::VIRTUAL_BLOCK_DESC*>(pDesc), reinterpret_cast<D3D12MA::VirtualBlock**>(ppVirtualBlock));
}

HRESULT D3D12MA_Allocator_QueryInterface(D3D12MA_Allocator* pAllocator, const GUID* riid, void** ppvObject)
{
    return Unwrap(pAllocator)->QueryInterface(*riid, ppvObject);
}

ULONG D3D12MA_Allocator_AddRef(D3D12MA_Allocator* pAllocator)
{
    return Unwrap(pAllocator)->AddRef();
}

ULONG D3D12MA_Allocator_Release(D3D12MA_Allocator* pAllocator)
{
    return Unwrap(pAllocator)->Release();
}

const D3D12_FEATURE_DATA_D3D12_OPTIONS* D3D12MA_Allocator_GetD3D12Options(const D3D12MA_Allocator* pAllocator)
{
    return &Unwrap(pAllocator)->GetD3D12Options();
}

BOOL D3D12MA_Allocator_IsUMA(const D3D12MA_Allocator* pAllocator)
{
    return Unwrap(pAllocator)->IsUMA();
}

BOOL D3D12MA_Allocator_IsCacheCoherentUMA(const D3D12MA_Allocator* pAllocator)
{
    return Unwrap(pAllocator)->IsCacheCoherentUMA();
}

BOOL D3D12MA_Allocator_IsGPUUploadHeapSupported(const D3D12MA_Allocator* pAllocator)
{
    return Unwrap(pAllocator)->IsGPUUploadHeapSupported();
}

BOOL D3D12MA_Allocator_IsTightAlignmentSupported(const D3D12MA_Allocator* pAllocator)
{
    return Unwrap(pAllocator)->IsTightAlignmentSupported();
}

UINT64 D3D12MA_Allocator_GetMemoryCapacity(const D3D12MA_Allocator* pAllocator, UINT memorySegmentGroup)
{
    return Unwrap(pAllocator)->GetMemoryCapacity(memorySegmentGroup);
}

HRESULT D3D12MA_Allocator_CreateResource(D3D12MA_Allocator* pAllocator, const D3D12MA_ALLOCATION_DESC* pAllocDesc, const D3D12_RESOURCE_DESC* pResourceDesc, D3D12_RESOURCE_STATES InitialResourceState, const D3D12_CLEAR_VALUE* pOptimizedClearValue, D3D12MA_Allocation** ppAllocation, const GUID* riidResource, void** ppvResource)
{
    return Unwrap(pAllocator)->CreateResource(reinterpret_cast<const D3D12MA::ALLOCATION_DESC*>(pAllocDesc), pResourceDesc, InitialResourceState, pOptimizedClearValue, reinterpret_cast<D3D12MA::Allocation**>(ppAllocation), *riidResource, ppvResource);
}

HRESULT D3D12MA_Allocator_CreateResource2(D3D12MA_Allocator* pAllocator, const D3D12MA_ALLOCATION_DESC* pAllocDesc, const D3D12_RESOURCE_DESC1* pResourceDesc, D3D12_RESOURCE_STATES InitialResourceState, const D3D12_CLEAR_VALUE* pOptimizedClearValue, D3D12MA_Allocation** ppAllocation, const GUID* riidResource, void** ppvResource)
{
    return Unwrap(pAllocator)->CreateResource2(reinterpret_cast<const D3D12MA::ALLOCATION_DESC*>(pAllocDesc), pResourceDesc, InitialResourceState, pOptimizedClearValue, reinterpret_cast<D3D12MA::Allocation**>(ppAllocation), *riidResource, ppvResource);
}

HRESULT D3D12MA_Allocator_CreateResource3(D3D12MA_Allocator* pAllocator, const D3D12MA_ALLOCATION_DESC* pAllocDesc, const D3D12_RESOURCE_DESC1* pResourceDesc, D3D12_BARRIER_LAYOUT InitialLayout, const D3D12_CLEAR_VALUE* pOptimizedClearValue, UINT32 NumCastableFormats, const DXGI_FORMAT* pCastableFormats, D3D12MA_Allocation** ppAllocation, const GUID* riidResource, void** ppvResource)
{
    return Unwrap(pAllocator)->CreateResource3(reinterpret_cast<const D3D12MA::ALLOCATION_DESC*>(pAllocDesc), pResourceDesc, InitialLayout, pOptimizedClearValue, NumCastableFormats, pCastableFormats, reinterpret_cast<D3D12MA::Allocation**>(ppAllocation), *riidResource, ppvResource);
}

HRESULT D3D12MA_Allocator_CreateResource4(D3D12MA_Allocator* pAllocator, const D3D12MA_ALLOCATION_DESC* pAllocDesc, const D3D12_RESOURCE_DESC2* pResourceDesc, D3D12_BARRIER_LAYOUT InitialLayout, const D3D12_CLEAR_VALUE* pOptimizedClearValue, UINT32 NumCastableFormats, const DXGI_FORMAT* pCastableFormats, D3D12MA_Allocation** ppAllocation, const GUID* riidResource, void** ppvResource)
{
    return Unwrap(pAllocator)->CreateResource4(reinterpret_cast<const D3D12MA::ALLOCATION_DESC*>(pAllocDesc), pResourceDesc, InitialLayout, pOptimizedClearValue, NumCastableFormats, pCastableFormats, reinterpret_cast<D3D12MA::Allocation**>(ppAllocation), *riidResource, ppvResource);
}

HRESULT D3D12MA_Allocator_AllocateMemory(D3D12MA_Allocator* pAllocator, const D3D12MA_ALLOCATION_DESC* pAllocDesc, const D3D12_RESOURCE_ALLOCATION_INFO* pAllocInfo, D3D12MA_Allocation** ppAllocation)
{
    return Unwrap(pAllocator)->AllocateMemory(reinterpret_cast<const D3D12MA::ALLOCATION_DESC*>(pAllocDesc), pAllocInfo, reinterpret_cast<D3D12MA::Allocation**>(ppAllocation));
}

HRESULT D3D12MA_Allocator_CreateAliasingResource(D3D12MA_Allocator* pAllocator, D3D12MA_Allocation* pAllocation, UINT64 AllocationLocalOffset, const D3D12_RESOURCE_DESC* pResourceDesc, D3D12_RESOURCE_STATES InitialResourceState, const D3D12_CLEAR_VALUE* pOptimizedClearValue, const GUID* riidResource, void** ppvResource)
{
    return Unwrap(pAllocator)->CreateAliasingResource(Unwrap(pAllocation), AllocationLocalOffset, pResourceDesc, InitialResourceState, pOptimizedClearValue, *riidResource, ppvResource);
}

HRESULT D3D12MA_Allocator_CreateAliasingResource1(D3D12MA_Allocator* pAllocator, D3D12MA_Allocation* pAllocation, UINT64 AllocationLocalOffset, const D3D12_RESOURCE_DESC1* pResourceDesc, D3D12_RESOURCE_STATES InitialResourceState, const D3D12_CLEAR_VALUE* pOptimizedClearValue, const GUID* riidResource, void** ppvResource)
{
    return Unwrap(pAllocator)->CreateAliasingResource1(Unwrap(pAllocation), AllocationLocalOffset, pResourceDesc, InitialResourceState, pOptimizedClearValue, *riidResource, ppvResource);
}

HRESULT D3D12MA_Allocator_CreateAliasingResource2(D3D12MA_Allocator* pAllocator, D3D12MA_Allocation* pAllocation, UINT64 AllocationLocalOffset, const D3D12_RESOURCE_DESC1* pResourceDesc, D3D12_BARRIER_LAYOUT InitialLayout, const D3D12_CLEAR_VALUE* pOptimizedClearValue, UINT32 NumCastableFormats, const DXGI_FORMAT* pCastableFormats, const GUID* riidResource, void** ppvResource)
{
    return Unwrap(pAllocator)->CreateAliasingResource2(Unwrap(pAllocation), AllocationLocalOffset, pResourceDesc, InitialLayout, pOptimizedClearValue, NumCastableFormats, pCastableFormats, *riidResource, ppvResource);
}

HRESULT D3D12MA_Allocator_CreateAliasingResource3(D3D12MA_Allocator* pAllocator, D3D12MA_Allocation* pAllocation, UINT64 AllocationLocalOffset, const D3D12_RESOURCE_DESC2* pResourceDesc, D3D12_BARRIER_LAYOUT InitialLayout, const D3D12_CLEAR_VALUE* pOptimizedClearValue, UINT32 NumCastableFormats, const DXGI_FORMAT* pCastableFormats, const GUID* riidResource, void** ppvResource)
{
    return Unwrap(pAllocator)->CreateAliasingResource3(Unwrap(pAllocation), AllocationLocalOffset, pResourceDesc, InitialLayout, pOptimizedClearValue, NumCastableFormats, pCastableFormats, *riidResource, ppvResource);
}

HRESULT D3D12MA_Allocator_CreatePool(D3D12MA_Allocator* pAllocator, const D3D12MA_POOL_DESC* pPoolDesc, D3D12MA_Pool** ppPool)
{
    return Unwrap(pAllocator)->CreatePool(reinterpret_cast<const D3D12MA::POOL_DESC*>(pPoolDesc), reinterpret_cast<D3D12MA::Pool**>(ppPool));
}

void D3D12MA_Allocator_SetCurrentFrameIndex(D3D12MA_Allocator* pAllocator, UINT frameIndex)
{
    Unwrap(pAllocator)->SetCurrentFrameIndex(frameIndex);
}

void D3D12MA_Allocator_GetBudget(D3D12MA_Allocator* pAllocator, D3D12MA_Budget* pLocalBudget, D3D12MA_Budget* pNonLocalBudget)
{
    Unwrap(pAllocator)->GetBudget(reinterpret_cast<D3D12MA::Budget*>(pLocalBudget), reinterpret_cast<D3D12MA::Budget*>(pNonLocalBudget));
}

void D3D12MA_Allocator_CalculateStatistics(D3D12MA_Allocator* pAllocator, D3D12MA_TotalStatistics* pStats)
{
    Unwrap(pAllocator)->CalculateStatistics(reinterpret_cast<D3D12MA::TotalStatistics*>(pStats));
}

void D3D12MA_Allocator_BuildStatsString(const D3D12MA_Allocator* pAllocator, WCHAR** ppStatsString, BOOL DetailedMap)
{
    Unwrap(pAllocator)->BuildStatsString(ppStatsString, DetailedMap);
}

void D3D12MA_Allocator_FreeStatsString(const D3D12MA_Allocator* pAllocator, WCHAR* pStatsString)
{
    Unwrap(pAllocator)->FreeStatsString(pStatsString);
}

void D3D12MA_Allocator_BeginDefragmentation(D3D12MA_Allocator* pAllocator, const D3D12MA_DEFRAGMENTATION_DESC* pDesc, D3D12MA_DefragmentationContext** ppContext)
{
    Unwrap(pAllocator)->BeginDefragmentation(reinterpret_cast<const D3D12MA::DEFRAGMENTATION_DESC*>(pDesc), reinterpret_cast<D3D12MA::DefragmentationContext**>(ppContext));
}

HRESULT D3D12MA_Allocation_QueryInterface(D3D12MA_Allocation* pAllocation, const GUID* riid, void** ppvObject)
{
    return Unwrap(pAllocation)->QueryInterface(*riid, ppvObject);
}

ULONG D3D12MA_Allocation_AddRef(D3D12MA_Allocation* pAllocation)
{
    return Unwrap(pAllocation)->AddRef();
}

ULONG D3D12MA_Allocation_Release(D3D12MA_Allocation* pAllocation)
{
    return Unwrap(pAllocation)->Release();
}

UINT64 D3D12MA_Allocation_GetOffset(const D3D12MA_Allocation* pAllocation)
{
    return Unwrap(pAllocation)->GetOffset();
}

UINT64 D3D12MA_Allocation_GetAlignment(const D3D12MA_Allocation* pAllocation)
{
    return Unwrap(pAllocation)->GetAlignment();
}

UINT64 D3D12MA_Allocation_GetSize(const D3D12MA_Allocation* pAllocation)
{
    return Unwrap(pAllocation)->GetSize();
}

ID3D12Resource* D3D12MA_Allocation_GetResource(const D3D12MA_Allocation* pAllocation)
{
    return Unwrap(pAllocation)->GetResource();
}

void D3D12MA_Allocation_SetResource(D3D12MA_Allocation* pAllocation, ID3D12Resource* pResource)
{
    Unwrap(pAllocation)->SetResource(pResource);
}

ID3D12Heap* D3D12MA_Allocation_GetHeap(const D3D12MA_Allocation* pAllocation)
{
    return Unwrap(pAllocation)->GetHeap();
}

void D3D12MA_Allocation_SetPrivateData(D3D12MA_Allocation* pAllocation, void* pPrivateData)
{
    Unwrap(pAllocation)->SetPrivateData(pPrivateData);
}

void* D3D12MA_Allocation_GetPrivateData(const D3D12MA_Allocation* pAllocation)
{
    return Unwrap(pAllocation)->GetPrivateData();
}

void D3D12MA_Allocation_SetName(D3D12MA_Allocation* pAllocation, LPCWSTR Name)
{
    Unwrap(pAllocation)->SetName(Name);
}

LPCWSTR D3D12MA_Allocation_GetName(const D3D12MA_Allocation* pAllocation)
{
    return Unwrap(pAllocation)->GetName();
}

HRESULT D3D12MA_Pool_QueryInterface(D3D12MA_Pool* pPool, const GUID* riid, void** ppvObject)
{
    return Unwrap(pPool)->QueryInterface(*riid, ppvObject);
}

ULONG D3D12MA_Pool_AddRef(D3D12MA_Pool* pPool)
{
    return Unwrap(pPool)->AddRef();
}

ULONG D3D12MA_Pool_Release(D3D12MA_Pool* pPool)
{
    return Unwrap(pPool)->Release();
}

void D3D12MA_Pool_GetDesc(const D3D12MA_Pool* pPool, D3D12MA_POOL_DESC* pOutDesc)
{
    D3D12MA::POOL_DESC desc = Unwrap(pPool)->GetDesc();
    *pOutDesc = *reinterpret_cast<const D3D12MA_POOL_DESC*>(&desc);
}

void D3D12MA_Pool_GetStatistics(D3D12MA_Pool* pPool, D3D12MA_Statistics* pStats)
{
    Unwrap(pPool)->GetStatistics(reinterpret_cast<D3D12MA::Statistics*>(pStats));
}

void D3D12MA_Pool_CalculateStatistics(D3D12MA_Pool* pPool, D3D12MA_DetailedStatistics* pStats)
{
    Unwrap(pPool)->CalculateStatistics(reinterpret_cast<D3D12MA::DetailedStatistics*>(pStats));
}

void D3D12MA_Pool_SetName(D3D12MA_Pool* pPool, LPCWSTR Name)
{
    Unwrap(pPool)->SetName(Name);
}

LPCWSTR D3D12MA_Pool_GetName(const D3D12MA_Pool* pPool)
{
    return Unwrap(pPool)->GetName();
}

HRESULT D3D12MA_Pool_BeginDefragmentation(D3D12MA_Pool* pPool, const D3D12MA_DEFRAGMENTATION_DESC* pDesc, D3D12MA_DefragmentationContext** ppContext)
{
    return Unwrap(pPool)->BeginDefragmentation(reinterpret_cast<const D3D12MA::DEFRAGMENTATION_DESC*>(pDesc), reinterpret_cast<D3D12MA::DefragmentationContext**>(ppContext));
}

HRESULT D3D12MA_DefragmentationContext_QueryInterface(D3D12MA_DefragmentationContext* pContext, const GUID* riid, void** ppvObject)
{
    return Unwrap(pContext)->QueryInterface(*riid, ppvObject);
}

ULONG D3D12MA_DefragmentationContext_AddRef(D3D12MA_DefragmentationContext* pContext)
{
    return Unwrap(pContext)->AddRef();
}

ULONG D3D12MA_DefragmentationContext_Release(D3D12MA_DefragmentationContext* pContext)
{
    return Unwrap(pContext)->Release();
}

HRESULT D3D12MA_DefragmentationContext_BeginPass(D3D12MA_DefragmentationContext* pContext, D3D12MA_DEFRAGMENTATION_PASS_MOVE_INFO* pPassInfo)
{
    return Unwrap(pContext)->BeginPass(reinterpret_cast<D3D12MA::DEFRAGMENTATION_PASS_MOVE_INFO*>(pPassInfo));
}

HRESULT D3D12MA_DefragmentationContext_EndPass(D3D12MA_DefragmentationContext* pContext, D3D12MA_DEFRAGMENTATION_PASS_MOVE_INFO* pPassInfo)
{
    return Unwrap(pContext)->EndPass(reinterpret_cast<D3D12MA::DEFRAGMENTATION_PASS_MOVE_INFO*>(pPassInfo));
}

void D3D12MA_DefragmentationContext_GetStats(D3D12MA_DefragmentationContext* pContext, D3D12MA_DEFRAGMENTATION_STATS* pStats)
{
    Unwrap(pContext)->GetStats(reinterpret_cast<D3D12MA::DEFRAGMENTATION_STATS*>(pStats));
}

HRESULT D3D12MA_VirtualBlock_QueryInterface(D3D12MA_VirtualBlock* pBlock, const GUID* riid, void** ppvObject)
{
    return Unwrap(pBlock)->QueryInterface(*riid, ppvObject);
}

ULONG D3D12MA_VirtualBlock_AddRef(D3D12MA_VirtualBlock* pBlock)
{
    return Unwrap(pBlock)->AddRef();
}

ULONG D3D12MA_VirtualBlock_Release(D3D12MA_VirtualBlock* pBlock)
{
    return Unwrap(pBlock)->Release();
}

BOOL D3D12MA_VirtualBlock_IsEmpty(const D3D12MA_VirtualBlock* pBlock)
{
    return Unwrap(pBlock)->IsEmpty();
}

void D3D12MA_VirtualBlock_GetAllocationInfo(const D3D12MA_VirtualBlock* pBlock, D3D12MA_VirtualAllocation allocation, D3D12MA_VIRTUAL_ALLOCATION_INFO* pInfo)
{
    D3D12MA::VirtualAllocation nativeAlloc;
    nativeAlloc.AllocHandle = allocation.AllocHandle;
    Unwrap(pBlock)->GetAllocationInfo(nativeAlloc, reinterpret_cast<D3D12MA::VIRTUAL_ALLOCATION_INFO*>(pInfo));
}

HRESULT D3D12MA_VirtualBlock_Allocate(D3D12MA_VirtualBlock* pBlock, const D3D12MA_VIRTUAL_ALLOCATION_DESC* pDesc, D3D12MA_VirtualAllocation* pAllocation, UINT64* pOffset)
{
    return Unwrap(pBlock)->Allocate(reinterpret_cast<const D3D12MA::VIRTUAL_ALLOCATION_DESC*>(pDesc), reinterpret_cast<D3D12MA::VirtualAllocation*>(pAllocation), pOffset);
}

void D3D12MA_VirtualBlock_FreeAllocation(D3D12MA_VirtualBlock* pBlock, D3D12MA_VirtualAllocation allocation)
{
    D3D12MA::VirtualAllocation nativeAlloc;
    nativeAlloc.AllocHandle = allocation.AllocHandle;
    Unwrap(pBlock)->FreeAllocation(nativeAlloc);
}

void D3D12MA_VirtualBlock_Clear(D3D12MA_VirtualBlock* pBlock)
{
    Unwrap(pBlock)->Clear();
}

void D3D12MA_VirtualBlock_SetAllocationPrivateData(D3D12MA_VirtualBlock* pBlock, D3D12MA_VirtualAllocation allocation, void* pPrivateData)
{
    D3D12MA::VirtualAllocation nativeAlloc;
    nativeAlloc.AllocHandle = allocation.AllocHandle;
    Unwrap(pBlock)->SetAllocationPrivateData(nativeAlloc, pPrivateData);
}

void D3D12MA_VirtualBlock_GetStatistics(const D3D12MA_VirtualBlock* pBlock, D3D12MA_Statistics* pStats)
{
    Unwrap(pBlock)->GetStatistics(reinterpret_cast<D3D12MA::Statistics*>(pStats));
}

void D3D12MA_VirtualBlock_CalculateStatistics(const D3D12MA_VirtualBlock* pBlock, D3D12MA_DetailedStatistics* pStats)
{
    Unwrap(pBlock)->CalculateStatistics(reinterpret_cast<D3D12MA::DetailedStatistics*>(pStats));
}

void D3D12MA_VirtualBlock_BuildStatsString(const D3D12MA_VirtualBlock* pBlock, WCHAR** ppStatsString)
{
    Unwrap(pBlock)->BuildStatsString(ppStatsString);
}

void D3D12MA_VirtualBlock_FreeStatsString(const D3D12MA_VirtualBlock* pBlock, WCHAR* pStatsString)
{
    Unwrap(pBlock)->FreeStatsString(pStatsString);
}

}
