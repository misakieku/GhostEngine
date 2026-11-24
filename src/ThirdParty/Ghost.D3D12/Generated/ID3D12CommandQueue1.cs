using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("3A3C3165-0EE7-4B8E-A0AF-6356B4C3BBB9")]
[NativeTypeName("struct ID3D12CommandQueue1 : ID3D12CommandQueue")]
public unsafe partial struct ID3D12CommandQueue1 : ID3D12CommandQueue1.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12CommandQueue1);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12CommandQueue1*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12CommandQueue1*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12CommandQueue1*, uint>)(lpVtbl[1]))((ID3D12CommandQueue1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12CommandQueue1*, uint>)(lpVtbl[2]))((ID3D12CommandQueue1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public HRESULT GetPrivateData([NativeTypeName("const GUID &")] Guid* guid, uint* pDataSize, void* pData)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12CommandQueue1*, Guid*, uint*, void*, int>)(lpVtbl[3]))((ID3D12CommandQueue1*)Unsafe.AsPointer(ref this), guid, pDataSize, pData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    public HRESULT SetPrivateData([NativeTypeName("const GUID &")] Guid* guid, uint DataSize, [NativeTypeName("const void *")] void* pData)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12CommandQueue1*, Guid*, uint, void*, int>)(lpVtbl[4]))((ID3D12CommandQueue1*)Unsafe.AsPointer(ref this), guid, DataSize, pData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    public HRESULT SetPrivateDataInterface([NativeTypeName("const GUID &")] Guid* guid, [NativeTypeName("const IUnknown *")] IUnknown* pData)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12CommandQueue1*, Guid*, IUnknown*, int>)(lpVtbl[5]))((ID3D12CommandQueue1*)Unsafe.AsPointer(ref this), guid, pData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    public HRESULT SetName([NativeTypeName("LPCWSTR")] char* Name)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12CommandQueue1*, char*, int>)(lpVtbl[6]))((ID3D12CommandQueue1*)Unsafe.AsPointer(ref this), Name);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    public HRESULT GetDevice([NativeTypeName("const IID &")] Guid* riid, void** ppvDevice)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12CommandQueue1*, Guid*, void**, int>)(lpVtbl[7]))((ID3D12CommandQueue1*)Unsafe.AsPointer(ref this), riid, ppvDevice);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(8)]
    public void UpdateTileMappings(ID3D12Resource* pResource, uint NumResourceRegions, [NativeTypeName("const D3D12_TILED_RESOURCE_COORDINATE *")] D3D12_TILED_RESOURCE_COORDINATE* pResourceRegionStartCoordinates, [NativeTypeName("const D3D12_TILE_REGION_SIZE *")] D3D12_TILE_REGION_SIZE* pResourceRegionSizes, ID3D12Heap* pHeap, uint NumRanges, [NativeTypeName("const D3D12_TILE_RANGE_FLAGS *")] D3D12_TILE_RANGE_FLAGS* pRangeFlags, [NativeTypeName("const UINT *")] uint* pHeapRangeStartOffsets, [NativeTypeName("const UINT *")] uint* pRangeTileCounts, D3D12_TILE_MAPPING_FLAGS Flags)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12CommandQueue1*, ID3D12Resource*, uint, D3D12_TILED_RESOURCE_COORDINATE*, D3D12_TILE_REGION_SIZE*, ID3D12Heap*, uint, D3D12_TILE_RANGE_FLAGS*, uint*, uint*, D3D12_TILE_MAPPING_FLAGS, void>)(lpVtbl[8]))((ID3D12CommandQueue1*)Unsafe.AsPointer(ref this), pResource, NumResourceRegions, pResourceRegionStartCoordinates, pResourceRegionSizes, pHeap, NumRanges, pRangeFlags, pHeapRangeStartOffsets, pRangeTileCounts, Flags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(9)]
    public void CopyTileMappings(ID3D12Resource* pDstResource, [NativeTypeName("const D3D12_TILED_RESOURCE_COORDINATE *")] D3D12_TILED_RESOURCE_COORDINATE* pDstRegionStartCoordinate, ID3D12Resource* pSrcResource, [NativeTypeName("const D3D12_TILED_RESOURCE_COORDINATE *")] D3D12_TILED_RESOURCE_COORDINATE* pSrcRegionStartCoordinate, [NativeTypeName("const D3D12_TILE_REGION_SIZE *")] D3D12_TILE_REGION_SIZE* pRegionSize, D3D12_TILE_MAPPING_FLAGS Flags)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12CommandQueue1*, ID3D12Resource*, D3D12_TILED_RESOURCE_COORDINATE*, ID3D12Resource*, D3D12_TILED_RESOURCE_COORDINATE*, D3D12_TILE_REGION_SIZE*, D3D12_TILE_MAPPING_FLAGS, void>)(lpVtbl[9]))((ID3D12CommandQueue1*)Unsafe.AsPointer(ref this), pDstResource, pDstRegionStartCoordinate, pSrcResource, pSrcRegionStartCoordinate, pRegionSize, Flags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(10)]
    public void ExecuteCommandLists(uint NumCommandLists, [NativeTypeName("ID3D12CommandList *const *")] ID3D12CommandList** ppCommandLists)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12CommandQueue1*, uint, ID3D12CommandList**, void>)(lpVtbl[10]))((ID3D12CommandQueue1*)Unsafe.AsPointer(ref this), NumCommandLists, ppCommandLists);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(11)]
    public void SetMarker(uint Metadata, [NativeTypeName("const void *")] void* pData, uint Size)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12CommandQueue1*, uint, void*, uint, void>)(lpVtbl[11]))((ID3D12CommandQueue1*)Unsafe.AsPointer(ref this), Metadata, pData, Size);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(12)]
    public void BeginEvent(uint Metadata, [NativeTypeName("const void *")] void* pData, uint Size)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12CommandQueue1*, uint, void*, uint, void>)(lpVtbl[12]))((ID3D12CommandQueue1*)Unsafe.AsPointer(ref this), Metadata, pData, Size);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(13)]
    public void EndEvent()
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12CommandQueue1*, void>)(lpVtbl[13]))((ID3D12CommandQueue1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(14)]
    public HRESULT Signal(ID3D12Fence* pFence, [NativeTypeName("UINT64")] ulong Value)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12CommandQueue1*, ID3D12Fence*, ulong, int>)(lpVtbl[14]))((ID3D12CommandQueue1*)Unsafe.AsPointer(ref this), pFence, Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(15)]
    public HRESULT Wait(ID3D12Fence* pFence, [NativeTypeName("UINT64")] ulong Value)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12CommandQueue1*, ID3D12Fence*, ulong, int>)(lpVtbl[15]))((ID3D12CommandQueue1*)Unsafe.AsPointer(ref this), pFence, Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(16)]
    public HRESULT GetTimestampFrequency([NativeTypeName("UINT64 *")] ulong* pFrequency)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12CommandQueue1*, ulong*, int>)(lpVtbl[16]))((ID3D12CommandQueue1*)Unsafe.AsPointer(ref this), pFrequency);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(17)]
    public HRESULT GetClockCalibration([NativeTypeName("UINT64 *")] ulong* pGpuTimestamp, [NativeTypeName("UINT64 *")] ulong* pCpuTimestamp)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12CommandQueue1*, ulong*, ulong*, int>)(lpVtbl[17]))((ID3D12CommandQueue1*)Unsafe.AsPointer(ref this), pGpuTimestamp, pCpuTimestamp);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(18)]
    public D3D12_COMMAND_QUEUE_DESC GetDesc()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12CommandQueue1*, D3D12_COMMAND_QUEUE_DESC>)(lpVtbl[18]))((ID3D12CommandQueue1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(19)]
    public HRESULT SetProcessPriority(D3D12_COMMAND_QUEUE_PROCESS_PRIORITY Priority)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12CommandQueue1*, D3D12_COMMAND_QUEUE_PROCESS_PRIORITY, int>)(lpVtbl[19]))((ID3D12CommandQueue1*)Unsafe.AsPointer(ref this), Priority);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(20)]
    public HRESULT GetProcessPriority(D3D12_COMMAND_QUEUE_PROCESS_PRIORITY* pOutValue)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12CommandQueue1*, D3D12_COMMAND_QUEUE_PROCESS_PRIORITY*, int>)(lpVtbl[20]))((ID3D12CommandQueue1*)Unsafe.AsPointer(ref this), pOutValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(21)]
    public HRESULT SetGlobalPriority(D3D12_COMMAND_QUEUE_GLOBAL_PRIORITY Priority)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12CommandQueue1*, D3D12_COMMAND_QUEUE_GLOBAL_PRIORITY, int>)(lpVtbl[21]))((ID3D12CommandQueue1*)Unsafe.AsPointer(ref this), Priority);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(22)]
    public HRESULT GetGlobalPriority(D3D12_COMMAND_QUEUE_GLOBAL_PRIORITY* pOutValue)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12CommandQueue1*, D3D12_COMMAND_QUEUE_GLOBAL_PRIORITY*, int>)(lpVtbl[22]))((ID3D12CommandQueue1*)Unsafe.AsPointer(ref this), pOutValue);
    }

    public interface Interface : ID3D12CommandQueue.Interface
    {
        [VtblIndex(19)]
        HRESULT SetProcessPriority(D3D12_COMMAND_QUEUE_PROCESS_PRIORITY Priority);

        [VtblIndex(20)]
        HRESULT GetProcessPriority(D3D12_COMMAND_QUEUE_PROCESS_PRIORITY* pOutValue);

        [VtblIndex(21)]
        HRESULT SetGlobalPriority(D3D12_COMMAND_QUEUE_GLOBAL_PRIORITY Priority);

        [VtblIndex(22)]
        HRESULT GetGlobalPriority(D3D12_COMMAND_QUEUE_GLOBAL_PRIORITY* pOutValue);
    }
}
