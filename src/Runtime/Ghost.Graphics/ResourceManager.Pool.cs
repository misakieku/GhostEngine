using Ghost.Core;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Diagnostics;

namespace Ghost.Graphics;

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

    private UnsafeList<Page> _activePages = new UnsafeList<Page>(4, Allocator.Persistent);
    private UnsafeQueue<Page> _freePages = new UnsafeQueue<Page>(4, Allocator.Persistent);
    private UnsafeQueue<RetiringPage> _retiringPages = new UnsafeQueue<RetiringPage>(4, Allocator.Persistent);

    private UnsafeList<Handle<GPUResource>> _frameTransientResources = new UnsafeList<Handle<GPUResource>>(4, Allocator.Persistent);

    private static bool IsHeapFlagsCompatible(HeapFlags pageHeapFlags, HeapFlags requiredHeapFlags)
    {
        return pageHeapFlags == requiredHeapFlags || pageHeapFlags == HeapFlags.AllowAllBufferAndTexture;
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

    public Handle<GPUTexture> CreateTransientTexture(ref readonly TextureDesc desc, string? name = null)
    {
        var isRTOrDS = desc.Usage.HasFlag(TextureUsage.DepthStencil) || desc.Usage.HasFlag(TextureUsage.RenderTarget);
        var size = _resourceAllocator.GetSizeInfo(ResourceDesc.Texture(desc));

        if (size.Size > DEFAULT_TRANSIENT_PAGE_SIZE)
        {
            var texHandle = _resourceAllocator.CreateTexture(in desc, name);
            if (texHandle.IsValid)
            {
                _frameTransientResources.Add(texHandle.AsResource());
            }

            return texHandle;
        }

        var requiredHeapFlags = _renderDevice.FeatureSupport.HasFlag(FeatureSupport.AliasBuffersAndTextures) ?
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
                Debug.Fail($"Failed to create a new page for transient texture: {error}");
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

    public Handle<GPUBuffer> CreateTransientBuffer(ref readonly BufferDesc desc, string? name = null)
    {
        var size = _resourceAllocator.GetSizeInfo(ResourceDesc.Buffer(desc));
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

        var requiredHeapFlags = _renderDevice.FeatureSupport.HasFlag(FeatureSupport.AliasBuffersAndTextures) ?
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
                Debug.Fail($"Failed to create a new page for transient buffer: {error}");
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

    private void EndFramePool(ulong completedFrame)
    {
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
    }

    private void DisposePool()
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

        _activePages.Dispose();
        _freePages.Dispose();
        _retiringPages.Dispose();
        _frameTransientResources.Dispose();
    }
}
