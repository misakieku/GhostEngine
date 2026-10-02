using Ghost.Core;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Ghost.Graphics.Services;

public partial class ResourceManager
{
    public const ulong DEFAULT_TRANSIENT_PAGE_SIZE = 16 * 1024 * 1024; // 16MB

    [DebuggerDisplay("Heap: {heap}, Offset: {offset}, HeapType: {heapType}, HeapFlags: {heapFlags}")]
    private struct Page
    {
        public Handle<GPUResource> heap;
        public ulong offset;

        public HeapFlags heapFlags;
        public HeapType heapType;
    }

    [DebuggerDisplay("Page Heap: {page.heap}, RetireFrame: {retireFrame}")]
    private struct RetiringPage
    {
        public Page page;
        public ulong retireFrame;
    }

    private UnsafeList<Page> _activePages = new UnsafeList<Page>(8, AllocationHandle.Persistent);
    private UnsafeQueue<Page> _freePages = new UnsafeQueue<Page>(8, AllocationHandle.Persistent);
    private UnsafeQueue<RetiringPage> _retiringPages = new UnsafeQueue<RetiringPage>(8, AllocationHandle.Persistent);

    private UnsafeList<Handle<GPUResource>> _frameTransientResources = new UnsafeList<Handle<GPUResource>>(8, AllocationHandle.Persistent);

    private readonly Lock _transientWriteLock = new Lock();

    private Handle<GPUBuffer> _uploadBuffer;
    private uint _uploadBufferOffset; // We use uint here because the offset is always less than 16MB, which is well within the range of uint.
    private uint _uploadBufferSrvIndex;
    private readonly Lock _uploadBufferLock = new Lock();

    private static bool IsHeapFlagsCompatible(HeapFlags pageHeapFlags, HeapFlags requiredHeapFlags)
    {
        return pageHeapFlags == requiredHeapFlags || pageHeapFlags == HeapFlags.AllowAllBufferAndTexture;
    }

    private void InitializeTransientPool()
    {
        _uploadBufferOffset = 0;
        _uploadBuffer = _resourceAllocator.CreateBuffer(new BufferDesc
        {
            Size = DEFAULT_TRANSIENT_PAGE_SIZE,
            Usage = BufferUsage.Upload | BufferUsage.ShaderResource | BufferUsage.Raw,
            HeapType = HeapType.Upload,
        }, "Transient Upload Buffer");

        _uploadBufferSrvIndex = _resourceDatabase.GetBindlessIndex(_uploadBuffer.AsResource(), BindlessAccess.ShaderResource);
    }

    private bool TryRentReusablePage(HeapType heapType, HeapFlags heapFlags, out Page page)
    {
        var freePageCount = _freePages.Count;
        for (var i = 0; i < freePageCount; i++)
        {
            var candidate = _freePages.Dequeue();
            if (candidate.heapType == heapType && IsHeapFlagsCompatible(candidate.heapFlags, heapFlags))
            {
                candidate.offset = 0;
                page = candidate;
                return true;
            }

            _freePages.Enqueue(candidate);
        }

        page = default;
        return false;
    }

    private Error CreateNewActivePage(HeapType heapType, HeapFlags heapFlags)
    {
        if (TryRentReusablePage(heapType, heapFlags, out var reusablePage))
        {
            _activePages.Add(reusablePage);
            return Error.None;
        }

        var allocationDesc = new AllocationDesc
        {
            Size = DEFAULT_TRANSIENT_PAGE_SIZE,
            Alignment = 65536, // 64KB
            HeapType = heapType,
            HeapFlags = heapFlags,
        };

        var buffer = _resourceAllocator.Allocate(in allocationDesc, $"Page {_activePages.Count + _freePages.Count + _retiringPages.Count}");
        if (buffer.IsInvalid)
        {
            return Error.OutOfMemory;
        }

        _activePages.Add(new Page
        {
            heap = buffer,
            offset = 0,
            heapFlags = heapFlags,
            heapType = heapType,
        });

        return Error.None;
    }

    public Handle<GPUTexture> CreateTransientTexture(scoped in TextureDesc desc, string? name = null)
    {
        var isRTOrDS = desc.Usage.HasFlag(TextureUsage.DepthStencil) || desc.Usage.HasFlag(TextureUsage.RenderTarget);
        var size = _resourceAllocator.GetSizeInfo(ResourceDesc.Texture(desc));

        lock (_transientWriteLock)
        {
            if (size.Size > DEFAULT_TRANSIENT_PAGE_SIZE)
            {
                var texHandle = _resourceAllocator.CreateTexture(in desc, name);
                if (texHandle.IsValid)
                {
                    _frameTransientResources.Add(texHandle.AsResource());
                }

                return texHandle;
            }

            var requiredHeapFlags = _renderDevice.DeviceFeture.SupportedFeatures.HasFlag(FeatureSupport.AliasBuffersAndTextures) ?
                HeapFlags.AllowAllBufferAndTexture :
                isRTOrDS ? HeapFlags.AllowOnlyRTAndDS : HeapFlags.AllowOnlyTextures;

            var foundPageIndex = -1;
            var alignedOffset = 0UL;

            for (var i = 0; i < _activePages.Count; i++)
            {
                ref var p = ref _activePages[i];

                if (p.heapType != HeapType.Default)
                {
                    continue;
                }

                if (!IsHeapFlagsCompatible(p.heapFlags, requiredHeapFlags))
                {
                    continue;
                }

                var proposedOffset = (p.offset + (size.Alignment - 1)) & ~(size.Alignment - 1);

                if (proposedOffset + size.Size <= DEFAULT_TRANSIENT_PAGE_SIZE)
                {
                    foundPageIndex = i;
                    alignedOffset = proposedOffset;
                    break;
                }
            }

            if (foundPageIndex == -1)
            {
                var error = CreateNewActivePage(HeapType.Default, requiredHeapFlags);
                if (error != Error.None)
                {
                    Logger.Error($"Failed to create a new page for transient texture: {error}");
                    return Handle<GPUTexture>.Invalid;
                }

                foundPageIndex = _activePages.Count - 1;
                alignedOffset = 0;
            }

            ref var page = ref _activePages[foundPageIndex];

            var handle = _resourceAllocator.CreateTexture(in desc, name, new CreationOptions
            {
                AllocationType = ResourceAllocationType.Suballocation,
                Heap = page.heap,
                Offset = alignedOffset,
            });

            if (handle.IsValid)
            {
                page.offset = alignedOffset + size.Size;
                _frameTransientResources.Add(handle.AsResource());
            }

            return handle;
        }
    }

    public Handle<GPUBuffer> CreateTransientBuffer(scoped in BufferDesc desc, string? name = null)
    {
        var size = _resourceAllocator.GetSizeInfo(ResourceDesc.Buffer(desc));

        lock (_transientWriteLock)
        {
            if (size.Size > DEFAULT_TRANSIENT_PAGE_SIZE)
            {
                var bufHandle = _resourceAllocator.CreateBuffer(in desc, name);
                if (bufHandle.IsValid)
                {
                    _frameTransientResources.Add(bufHandle.AsResource());
                }

                return bufHandle;
            }

            var requiredHeapType = desc.HeapType switch
            {
                HeapType.Upload => HeapType.Upload,
                HeapType.Readback => HeapType.Readback,
                _ => HeapType.Default
            };

            var requiredHeapFlags = _renderDevice.DeviceFeture.SupportedFeatures.HasFlag(FeatureSupport.AliasBuffersAndTextures) ?
                HeapFlags.AllowAllBufferAndTexture : HeapFlags.AllowOnlyBuffers;

            var foundPageIndex = -1;
            var alignedOffset = 0UL;

            for (var i = 0; i < _activePages.Count; i++)
            {
                ref var p = ref _activePages[i];

                if (p.heapType != requiredHeapType)
                {
                    continue;
                }

                if (!IsHeapFlagsCompatible(p.heapFlags, requiredHeapFlags))
                {
                    continue;
                }

                var proposedOffset = (p.offset + (size.Alignment - 1)) & ~(size.Alignment - 1);

                if (proposedOffset + size.Size <= DEFAULT_TRANSIENT_PAGE_SIZE)
                {
                    foundPageIndex = i;
                    alignedOffset = proposedOffset;
                    break;
                }
            }

            if (foundPageIndex == -1)
            {
                var error = CreateNewActivePage(requiredHeapType, requiredHeapFlags);
                if (error != Error.None)
                {
                    Logger.Error($"Failed to create a new page for transient buffer: {error}");
                    return Handle<GPUBuffer>.Invalid;
                }

                foundPageIndex = _activePages.Count - 1;
                alignedOffset = 0;
            }

            ref var page = ref _activePages[foundPageIndex];

            var handle = _resourceAllocator.CreateBuffer(in desc, name, new CreationOptions
            {
                AllocationType = ResourceAllocationType.Suballocation,
                Heap = page.heap,
                Offset = alignedOffset,
            });

            if (handle.IsValid)
            {
                page.offset = alignedOffset + size.Size;
                _frameTransientResources.Add(handle.AsResource());
            }

            return handle;
        }
    }

    public Handle<GPUBuffer> CreateTransientUploadBuffer(scoped in BufferDesc desc, out uint offset, out uint srvIndex)
    {
        Logger.DebugAssert(desc.HeapType == HeapType.Upload, "Transient upload buffer must be of HeapType.Upload");

        lock (_uploadBufferLock)
        {
            if (_uploadBufferOffset + desc.Size > DEFAULT_TRANSIENT_PAGE_SIZE)
            {
                var handle = CreateTransientBuffer(desc);
                offset = 0;
                srvIndex = _resourceDatabase.GetBindlessIndex(handle.AsResource());
                return handle;
            }

            offset = _uploadBufferOffset;
            srvIndex = _uploadBufferSrvIndex;

            _uploadBufferOffset += (uint)desc.Size;

            return _uploadBuffer;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Handle<GPUBuffer> CreateTransientUploadBuffer(BufferDesc desc, out uint offset)
    {
        return CreateTransientUploadBuffer(desc, out offset, out _);
    }

    private void EndFramePool(ulong completedFrame)
    {
        // Return swap-displaced handles to the pool only once their frame has retired, so pooled reuse
        // cannot recycle memory still referenced by in-flight work.
        while (_deferredPoolReturns.TryPeek(out var poolReturn) && poolReturn.returnFrame < completedFrame)
        {
            _deferredPoolReturns.Dequeue();
            ReleasePooledResource(poolReturn.handle);
        }

        for (var i = 0; i < _activePages.Count; i++)
        {
            ref var page = ref _activePages[i];
            _retiringPages.Enqueue(new RetiringPage
            {
                page = page,
                retireFrame = _submittedFrame
            });
        }

        _activePages.Clear();

        while (_retiringPages.TryPeek(out var retiringPage) && retiringPage.retireFrame < completedFrame)
        {
            _retiringPages.Dequeue();

            // Reset the page for reuse
            retiringPage.page.offset = 0;
            _freePages.Enqueue(retiringPage.page);
        }

        for (var i = 0; i < _frameTransientResources.Count; i++)
        {
            _resourceDatabase.ReleaseResource(_frameTransientResources[i]);
        }

        _frameTransientResources.Clear();
        _uploadBufferOffset = 0;
    }

    private void DisposeTransientPool()
    {
        foreach (var resource in _frameTransientResources)
        {
            _resourceDatabase.ReleaseResourceImmediately(resource);
        }

        foreach (var page in _activePages)
        {
            _resourceDatabase.ReleaseResourceImmediately(page.heap);
        }

        foreach (var page in _freePages)
        {
            _resourceDatabase.ReleaseResourceImmediately(page.heap);
        }

        foreach (var page in _retiringPages)
        {
            _resourceDatabase.ReleaseResourceImmediately(page.page.heap);
        }

        _resourceDatabase.ReleaseResource(_uploadBuffer.AsResource());

        _activePages.Dispose();
        _freePages.Dispose();
        _retiringPages.Dispose();
        _frameTransientResources.Dispose();
    }
}
