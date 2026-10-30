using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Graphics;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Runtime.InteropServices;

namespace Ghost.Engine.Streaming;

public sealed unsafe class MeshletStreamingManager : IDisposable
{
    private struct RegisteredMesh
    {
        public MeshAssetEntry mesh;
        public uint baseGlobalPageOffset;
        public uint pageCount;
    }

    private struct SlotInfo
    {
        public uint globalPageID;
        public ulong lastUsedFrame;
        public bool isPinned;
    }

    private struct PendingRootUpload : IDisposable
    {
        public uint physicalSlot;
        public UnsafeArray<byte> data;

        public void Dispose()
        {
            data.Dispose();
        }
    }

    public const uint DEFAULT_PAGE_POOL_SLOTS = 1024; // 128 MB physical pool (1024 * 128KB)
    public const uint DEFAULT_PAGE_TABLE_CAPACITY = 65536; // 65536 virtual pages
    public const uint DEFAULT_MAX_REQUESTS_PER_FRAME = 1024; // Max page requests reported by GPU
    public const int DEFAULT_MAX_PAGES_PER_FRAME = 8; // 1 MB per frame upload budget

    private readonly IResourceAllocator _resourceAllocator;
    private readonly IResourceDatabase _resourceDatabase;
    private readonly GPUPagePool _pagePool;
    private readonly GPUPageTable _pageTable;
    private readonly Handle<GPUBuffer> _streamingRequestsGpuBuffer;
    private readonly Handle<GPUBuffer> _streamingRequestsBitmaskGpuBuffer;
    private UnsafeArray<Handle<GPUBuffer>> _streamingRequestsReadbackBuffers;
    private UnsafeArray<byte> _zeroBitmaskBuffer;
    private readonly ulong _bitmaskBufferSize;
    private readonly uint _maxRequestsPerFrame;
    private readonly int _maxPagesPerFrame;

    private readonly List<RegisteredMesh> _registeredMeshes = new();
    private UnsafeArray<SlotInfo> _slotInfos;
    private UnsafeQueue<uint> _requestQueue;
    private UnsafeHashSet<uint> _queuedRequests;
    private UnsafeList<PendingRootUpload> _pendingRootUploads;

    private ulong _currentFrame;
    private UnsafeArray<byte> _pageStagingBuffer;
    private bool _pagePoolInShaderResource;
    private bool _streamingRequestsInUav;
    private bool _streamingRequestsBitmaskInUav;
    private bool _disposed;

    public GPUPagePool PagePool => _pagePool;
    public GPUPageTable PageTable => _pageTable;
    public uint PagePoolSrvIndex => _pagePool.BindlessIndex;
    public uint PageTableSrvIndex => _pageTable.BindlessIndex;
    public Handle<GPUBuffer> StreamingRequestsGpuBuffer => _streamingRequestsGpuBuffer;
    public Handle<GPUBuffer> StreamingRequestsBitmaskGpuBuffer => _streamingRequestsBitmaskGpuBuffer;
    public uint MaxStreamingRequestsPerFrame => _maxRequestsPerFrame;
    public int PendingRequestCount => _requestQueue.Count;

    internal MeshletStreamingManager(IResourceAllocator resourceAllocator, IResourceDatabase resourceDatabase, uint frameBufferCount,
        uint pagePoolSlots = DEFAULT_PAGE_POOL_SLOTS, uint pageTableCapacity = DEFAULT_PAGE_TABLE_CAPACITY,
        uint maxRequestsPerFrame = DEFAULT_MAX_REQUESTS_PER_FRAME, int maxPagesPerFrame = DEFAULT_MAX_PAGES_PER_FRAME)
    {
        _resourceAllocator = resourceAllocator;
        _resourceDatabase = resourceDatabase;
        _maxRequestsPerFrame = Math.Max(64u, maxRequestsPerFrame);
        _maxPagesPerFrame = Math.Max(1, maxPagesPerFrame);

        _pagePool = new GPUPagePool(resourceAllocator, resourceDatabase, pagePoolSlots);
        _pageTable = new GPUPageTable(resourceAllocator, resourceDatabase, pageTableCapacity);

        // GPU feedback buffer written by culling shaders (offset 0: uint requestCount, followed by uint pageIDs[])
        var requestBufferSize = (ulong)(4 + _maxRequestsPerFrame * 4);
        var requestDesc = new BufferDesc
        {
            Size = requestBufferSize,
            Stride = 4,
            Usage = BufferUsage.Raw | BufferUsage.ShaderResource | BufferUsage.UnorderedAccess,
            HeapType = HeapType.Default,
        };
        _streamingRequestsGpuBuffer = resourceAllocator.CreateBuffer(in requestDesc, "StreamingRequests_GPU");

        // GPU deduplication bitmask buffer (1 bit per global page ID, initialized to 0)
        _bitmaskBufferSize = (pageTableCapacity + 31u) / 32u * 4u;
        _zeroBitmaskBuffer = new UnsafeArray<byte>((int)_bitmaskBufferSize, AllocationHandle.Persistent, AllocationOption.Clear);

        var bitmaskDesc = new BufferDesc
        {
            Size = _bitmaskBufferSize,
            Stride = 4,
            Usage = BufferUsage.Raw | BufferUsage.ShaderResource | BufferUsage.UnorderedAccess,
            HeapType = HeapType.Default,
        };

        _streamingRequestsBitmaskGpuBuffer = resourceAllocator.CreateBuffer(in bitmaskDesc, "StreamingRequests_Bitmask_GPU");

        // CPU readback ring buffers on HeapType.Readback (never transitioned by barriers)
        var readbackRingLength = Math.Max(2, frameBufferCount);
        _streamingRequestsReadbackBuffers = new UnsafeArray<Handle<GPUBuffer>>((int)readbackRingLength, AllocationHandle.Persistent);
        var readbackDesc = new BufferDesc
        {
            Size = requestBufferSize,
            Stride = 4,
            Usage = BufferUsage.Raw | BufferUsage.ShaderResource,
            HeapType = HeapType.Readback,
        };

        for (var i = 0; i < readbackRingLength; i++)
        {
            _streamingRequestsReadbackBuffers[i] = resourceAllocator.CreateBuffer(in readbackDesc, $"StreamingRequests_Readback_{i}");
        }

        _slotInfos = new UnsafeArray<SlotInfo>((int)_pagePool.SlotCount, AllocationHandle.Persistent);
        for (var i = 0; i < _slotInfos.Length; i++)
        {
            _slotInfos[i] = new SlotInfo
            {
                globalPageID = GPUPageTable.PAGE_NOT_RESIDENT,
                lastUsedFrame = 0,
                isPinned = false
            };
        }

        _requestQueue = new UnsafeQueue<uint>((int)_maxRequestsPerFrame, AllocationHandle.Persistent);
        _queuedRequests = new UnsafeHashSet<uint>((int)_maxRequestsPerFrame, AllocationHandle.Persistent);
        _pendingRootUploads = new UnsafeList<PendingRootUpload>(16, AllocationHandle.Persistent);

        _pageStagingBuffer = new UnsafeArray<byte>(MeshletPageConstants.PAGE_SIZE, AllocationHandle.Persistent);
    }

    internal uint RegisterMesh(MeshAssetEntry mesh, uint pageCount, ReadOnlySpan<byte> rootPage0Data)
    {
        lock (_registeredMeshes)
        {
            var baseOffset = _pageTable.AllocatePageRange(pageCount);

            if (!_pagePool.TryAllocateSlot(out var rootSlot))
            {
                Logger.Error("GPUPagePool exhausted during Root Page 0 allocation!");
                rootSlot = 0;
            }

            _slotInfos[rootSlot] = new SlotInfo
            {
                globalPageID = baseOffset,
                lastUsedFrame = ulong.MaxValue,
                isPinned = true // Permanently pinned!
            };

            _pageTable.SetPageResidency(baseOffset, rootSlot);

            for (var p = 1u; p < pageCount; p++)
            {
                _pageTable.SetPageResidency(baseOffset + p, GPUPageTable.PAGE_NOT_RESIDENT);
            }

            if (!rootPage0Data.IsEmpty)
            {
                var copy = new UnsafeArray<byte>(rootPage0Data.Length, AllocationHandle.Persistent);
                copy.CopyFrom(rootPage0Data);

                _pendingRootUploads.Add(new PendingRootUpload
                {
                    physicalSlot = rootSlot,
                    data = copy
                });
            }

            _registeredMeshes.Add(new RegisteredMesh
            {
                mesh = mesh,
                baseGlobalPageOffset = baseOffset,
                pageCount = pageCount
            });

            return baseOffset;
        }
    }

    internal void UnregisterMesh(uint baseGlobalPageOffset)
    {
        lock (_registeredMeshes)
        {
            for (var i = 0; i < _registeredMeshes.Count; i++)
            {
                if (_registeredMeshes[i].baseGlobalPageOffset == baseGlobalPageOffset)
                {
                    var entry = _registeredMeshes[i];
                    for (var p = 0u; p < entry.pageCount; p++)
                    {
                        var globalPageID = baseGlobalPageOffset + p;
                        var slot = _pageTable.GetPhysicalSlot(globalPageID);
                        if (slot != GPUPageTable.PAGE_NOT_RESIDENT)
                        {
                            _slotInfos[slot].isPinned = false;
                            _slotInfos[slot].globalPageID = GPUPageTable.PAGE_NOT_RESIDENT;
                            _pagePool.FreeSlot(slot);
                            _pageTable.SetPageResidency(globalPageID, GPUPageTable.PAGE_NOT_RESIDENT);
                        }
                    }

                    _registeredMeshes.RemoveAt(i);
                    break;
                }
            }
        }
    }

    internal void EnqueueRequests(ReadOnlySpan<uint> requestedGlobalPageIDs)
    {
        for (var i = 0; i < requestedGlobalPageIDs.Length; i++)
        {
            var pageID = requestedGlobalPageIDs[i];
            var currentSlot = _pageTable.GetPhysicalSlot(pageID);

            if (currentSlot != GPUPageTable.PAGE_NOT_RESIDENT)
            {
                // Already resident — refresh LRU timestamp
                if (currentSlot < (uint)_slotInfos.Length && !_slotInfos[currentSlot].isPinned)
                {
                    _slotInfos[currentSlot].lastUsedFrame = _currentFrame;
                }
                continue;
            }

            if (_queuedRequests.Add(pageID))
            {
                _requestQueue.Enqueue(pageID);
            }
        }
    }

    internal void Update(scoped in RenderContext context, ICommandBuffer commandBuffer)
    {
        _currentFrame++;

        // 1. Asynchronous, zero-stall readback of Frame N-Ring missing page requests
        if (_currentFrame > (ulong)_streamingRequestsReadbackBuffers.Length && _streamingRequestsReadbackBuffers.Length > 0)
        {
            var readbackSlot = context.FrameIndex % _streamingRequestsReadbackBuffers.Length;
            var currentReadback = _streamingRequestsReadbackBuffers[readbackSlot];
            var pData = (uint*)context.ResourceDatabase.MapResource(currentReadback.AsResource(), 0, null);
            if (pData != null)
            {
                var reqCount = Math.Min(pData[0], _maxRequestsPerFrame);
                if (reqCount > 0)
                {
                    EnqueueRequests(new ReadOnlySpan<uint>(pData + 1, (int)reqCount));
                }
                context.ResourceDatabase.UnmapResource(currentReadback.AsResource(), 0, null);
            }
        }

        // 2. Process pending Root Page uploads
        var hadRootUploads = _pendingRootUploads.Count > 0;
        var willUpload = hadRootUploads || _requestQueue.Count > 0;
        if (willUpload && _pagePoolInShaderResource)
        {
            commandBuffer.Barrier(BarrierDesc.Buffer(
                _pagePool.Buffer,
                syncBefore: BarrierSync.AllShading,
                syncAfter: BarrierSync.Copy,
                accessBefore: BarrierAccess.ShaderResource,
                accessAfter: BarrierAccess.CopyDest));
            _pagePoolInShaderResource = false;
        }

        if (hadRootUploads)
        {
            for (var i = 0; i < _pendingRootUploads.Count; i++)
            {
                ref var rootUpload = ref _pendingRootUploads[i];
                var dstOffset = rootUpload.physicalSlot * MeshletPageConstants.PAGE_SIZE;
                UploadBufferRange(context.ResourceManager, context.ResourceDatabase, commandBuffer, _pagePool.Buffer, rootUpload.data.AsSpan(), dstOffset);

                rootUpload.Dispose();
            }

            _pendingRootUploads.Clear();
        }

        // 3. Process streaming page requests up to frame budget
        var pagesUploaded = 0;
        while (_requestQueue.Count > 0 && pagesUploaded < _maxPagesPerFrame)
        {
            var pageID = _requestQueue.Dequeue();
            _queuedRequests.Remove(pageID);

            // Double-check residency
            if (_pageTable.GetPhysicalSlot(pageID) != GPUPageTable.PAGE_NOT_RESIDENT)
            {
                continue;
            }

            if (!TryResolvePage(pageID, out var mesh, out var localPageIndex))
            {
                continue;
            }

            // Allocate or LRU-evict a physical slot
            if (!TryAllocateOrEvictSlot(pageID, out var slot))
            {
                Logger.Warning("Failed to allocate or evict a slot in GPUPagePool!");
                break;
            }

            // Read 128KB page from mesh asset
            var bytesRead = mesh.ReadPage(localPageIndex, _pageStagingBuffer.AsSpan());
            if (bytesRead > 0)
            {
                var dstOffset = slot * MeshletPageConstants.PAGE_SIZE;
                UploadBufferRange(context.ResourceManager, context.ResourceDatabase, commandBuffer, _pagePool.Buffer, _pageStagingBuffer.AsSpan(0, bytesRead), dstOffset);

                _slotInfos[slot].globalPageID = pageID;
                _slotInfos[slot].lastUsedFrame = _currentFrame;
                _slotInfos[slot].isPinned = false;

                _pageTable.SetPageResidency(pageID, slot);
                pagesUploaded++;
            }
        }

        if (pagesUploaded > 0 || hadRootUploads)
        {
            commandBuffer.Barrier(BarrierDesc.Buffer(
                _pagePool.Buffer,
                syncBefore: BarrierSync.Copy,
                syncAfter: BarrierSync.AllShading,
                accessBefore: BarrierAccess.CopyDest,
                accessAfter: BarrierAccess.ShaderResource));
            _pagePoolInShaderResource = true;
        }
        else if (!_pagePoolInShaderResource)
        {
            commandBuffer.Barrier(BarrierDesc.Buffer(
                _pagePool.Buffer,
                syncBefore: BarrierSync.Copy,
                syncAfter: BarrierSync.AllShading,
                accessBefore: BarrierAccess.CopyDest,
                accessAfter: BarrierAccess.ShaderResource));
            _pagePoolInShaderResource = true;
        }

        // 4. Flush dirty residency entries to GPU
        _pageTable.FlushUpdates(context.ResourceManager, context.ResourceDatabase, commandBuffer);

        // 5. Reset GPU streaming request counter and bitmask for the upcoming frame
        if (_streamingRequestsInUav)
        {
            commandBuffer.Barrier(BarrierDesc.Buffer(
                _streamingRequestsGpuBuffer,
                syncBefore: BarrierSync.AllShading,
                syncAfter: BarrierSync.Copy,
                accessBefore: BarrierAccess.UnorderedAccess,
                accessAfter: BarrierAccess.CopyDest));
        }

        UploadBufferRange(context.ResourceManager, context.ResourceDatabase, commandBuffer, _streamingRequestsGpuBuffer, stackalloc uint[] { 0 }, 0);
        commandBuffer.Barrier(BarrierDesc.Buffer(
            _streamingRequestsGpuBuffer,
            syncBefore: BarrierSync.Copy,
            syncAfter: BarrierSync.AllShading,
            accessBefore: BarrierAccess.CopyDest,
            accessAfter: BarrierAccess.UnorderedAccess));
        _streamingRequestsInUav = true;

        if (_streamingRequestsBitmaskInUav)
        {
            commandBuffer.Barrier(BarrierDesc.Buffer(
                _streamingRequestsBitmaskGpuBuffer,
                syncBefore: BarrierSync.AllShading,
                syncAfter: BarrierSync.Copy,
                accessBefore: BarrierAccess.UnorderedAccess,
                accessAfter: BarrierAccess.CopyDest));
        }

        UploadBufferRange(context.ResourceManager, context.ResourceDatabase, commandBuffer, _streamingRequestsBitmaskGpuBuffer, _zeroBitmaskBuffer.AsSpan(), 0);
        commandBuffer.Barrier(BarrierDesc.Buffer(
            _streamingRequestsBitmaskGpuBuffer,
            syncBefore: BarrierSync.Copy,
            syncAfter: BarrierSync.AllShading,
            accessBefore: BarrierAccess.CopyDest,
            accessAfter: BarrierAccess.UnorderedAccess));
        _streamingRequestsBitmaskInUav = true;
    }

    internal void RecordReadbackCopy(ICommandBuffer cmd, int frameIndex)
    {
        if (_streamingRequestsReadbackBuffers.Length == 0)
        {
            return;
        }

        var readbackSlot = frameIndex % _streamingRequestsReadbackBuffers.Length;
        var targetReadback = _streamingRequestsReadbackBuffers[readbackSlot];
        var copyBytes = (ulong)(4 + _maxRequestsPerFrame * 4);

        // 1. Transition GPU buffer: UnorderedAccess -> CopySource
        var toCopy = BarrierDesc.Buffer(
            _streamingRequestsGpuBuffer,
            syncBefore: BarrierSync.AllShading,
            syncAfter: BarrierSync.Copy,
            accessBefore: BarrierAccess.UnorderedAccess,
            accessAfter: BarrierAccess.CopySource);
        cmd.Barrier(toCopy);

        // 2. DMA copy into Readback staging buffer (Readback heap has NO transition barriers)
        cmd.CopyBuffer(targetReadback, _streamingRequestsGpuBuffer, 0, 0, copyBytes);

        // 3. Transition GPU buffer back: CopySource -> UnorderedAccess
        var toUav = BarrierDesc.Buffer(
            _streamingRequestsGpuBuffer,
            syncBefore: BarrierSync.Copy,
            syncAfter: BarrierSync.AllShading,
            accessBefore: BarrierAccess.CopySource,
            accessAfter: BarrierAccess.UnorderedAccess);
        cmd.Barrier(toUav);
        _streamingRequestsInUav = true;
    }

    private static void UploadBufferRange<T>(ResourceManager resourceManager, IResourceDatabase resourceDatabase, ICommandBuffer cmd, Handle<GPUBuffer> buffer, ReadOnlySpan<T> data, uint byteOffset)
        where T : unmanaged
    {
        if (data.IsEmpty)
        {
            return;
        }

        var sizeInBytes = (nuint)(data.Length * sizeof(T));
        var uploadDesc = new BufferDesc
        {
            Size = sizeInBytes,
            Usage = BufferUsage.Upload,
            HeapType = HeapType.Upload,
        };

        var uploadHandle = resourceManager.CreateTransientUploadBuffer(uploadDesc, out var srcOffset);
        fixed (T* pData = data)
        {
            var mappedData = resourceDatabase.MapResource(uploadHandle.AsResource(), 0, null);
            MemoryUtility.MemCpy((byte*)mappedData + srcOffset, pData, sizeInBytes);
            resourceDatabase.UnmapResource(uploadHandle.AsResource(), 0, null);
        }

        cmd.CopyBuffer(buffer, uploadHandle, byteOffset, srcOffset, sizeInBytes);
    }

    private bool TryResolvePage(uint globalPageID, out MeshAssetEntry mesh, out uint localPageIndex)
    {
        lock (_registeredMeshes)
        {
            for (var i = 0; i < _registeredMeshes.Count; i++)
            {
                ref readonly var entry = ref CollectionsMarshal.AsSpan(_registeredMeshes)[i];
                if (globalPageID >= entry.baseGlobalPageOffset &&
                    globalPageID < entry.baseGlobalPageOffset + entry.pageCount)
                {
                    mesh = entry.mesh;
                    localPageIndex = globalPageID - entry.baseGlobalPageOffset;
                    return true;
                }
            }
        }

        mesh = null!;
        localPageIndex = 0;
        return false;
    }

    private bool TryAllocateOrEvictSlot(uint newGlobalPageID, out uint physicalSlot)
    {
        // 1. Try allocate from free slots
        if (_pagePool.TryAllocateSlot(out physicalSlot))
        {
            return true;
        }

        // 2. Pool full: Find least-recently-used unpinned slot
        var oldestFrame = ulong.MaxValue;
        var victimSlot = uint.MaxValue;

        for (var i = 0u; i < (uint)_slotInfos.Length; i++)
        {
            if (_slotInfos[i].isPinned)
            {
                continue; // Never evict Root Page 0!
            }

            if (_slotInfos[i].lastUsedFrame < oldestFrame)
            {
                oldestFrame = _slotInfos[i].lastUsedFrame;
                victimSlot = i;
            }
        }

        if (victimSlot == uint.MaxValue)
        {
            physicalSlot = uint.MaxValue;
            return false;
        }

        // Evict victim slot
        var evictedPageID = _slotInfos[victimSlot].globalPageID;
        if (evictedPageID != GPUPageTable.PAGE_NOT_RESIDENT)
        {
            _pageTable.SetPageResidency(evictedPageID, GPUPageTable.PAGE_NOT_RESIDENT);
        }

        physicalSlot = victimSlot;
        return true;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        for (var i = 0; i < _pendingRootUploads.Count; i++)
        {
            _pendingRootUploads[i].Dispose();
        }

        if (_streamingRequestsGpuBuffer.IsValid)
        {
            _resourceDatabase.ReleaseResource(_streamingRequestsGpuBuffer.AsResource());
        }

        if (_streamingRequestsBitmaskGpuBuffer.IsValid)
        {
            _resourceDatabase.ReleaseResource(_streamingRequestsBitmaskGpuBuffer.AsResource());
        }

        for (var i = 0; i < _streamingRequestsReadbackBuffers.Length; i++)
        {
            if (_streamingRequestsReadbackBuffers[i].IsValid)
            {
                _resourceDatabase.ReleaseResource(_streamingRequestsReadbackBuffers[i].AsResource());
            }
        }

        _registeredMeshes.Clear();

        _pagePool.Dispose();
        _pageTable.Dispose();

        _slotInfos.Dispose();
        _requestQueue.Dispose();
        _queuedRequests.Dispose();
        _pendingRootUploads.Dispose();
        _pageStagingBuffer.Dispose();
        _zeroBitmaskBuffer.Dispose();
        _streamingRequestsReadbackBuffers.Dispose();
    }
}
