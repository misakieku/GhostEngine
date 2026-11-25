using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.Buffer;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Runtime.CompilerServices;

namespace Ghost.Graphics.RenderGraphModule;

internal static class RenderGraphObjectPool
{
    private static readonly List<SharedObjectPoolBase> s_allocatedPools = new();

    private class SharedObjectPoolBase
    {
        public SharedObjectPoolBase() { }
        public virtual void Clear() { }
    }

    private class SharedObjectPool<T> : SharedObjectPoolBase
        where T : class, new()
    {
        private static readonly ObjectPool<T> s_pool = AllocatePool();

        private static ObjectPool<T> AllocatePool()
        {
            var newPool = new ObjectPool<T>(() => new T(), null);
            // Storing instance to clear the static pool of the same type if needed
            s_allocatedPools.Add(new SharedObjectPool<T>());
            return newPool;
        }

        public override void Clear()
        {
            s_pool.Reset();
        }

        public static T Rent()
        {
            return s_pool.Rent();
        }

        public static void Return(T toRelease)
        {
            s_pool.Return(toRelease);
        }
    }

    public static T Rent<T>()
        where T : class, new()
    {
        return SharedObjectPool<T>.Rent();
    }

    public static void Return<T>(T obj)
        where T : class, new()
    {
        SharedObjectPool<T>.Return(obj);
    }

    public static void Clear()
    {
        for (var i = 0; i < s_allocatedPools.Count; i++)
        {
            s_allocatedPools[i].Clear();
        }
    }
}

internal record struct RenderGraphResource
{
    public int index;
    public RGResourceType type;

    // Resource descriptors (only one is valid based on type)
    public RGTextureDesc rgTextureDesc;
    public BufferDesc bufferDesc;

    // Resolved dimensions (computed from rgTextureDesc + ViewState for textures)
    public uint resolvedWidth;
    public uint resolvedHeight;

    public bool isImported;
    public bool isAllocated;
    public bool hasInitialBarrierState;
    public bool hasFinalBarrierState;
    public ResourceBarrierData initialBarrierState;
    public ResourceBarrierData finalBarrierState;
    public int firstUsePass;
    public int lastUsePass;
    public int producerHead;
    public int producerTail;
    public int lastProducerPass;
    public int consumerHead;
    public int consumerTail;
    public int lastConsumerPass;
    public int refCount;

    public Handle<GPUResource> backingResource;

    public bool isExtracted;
    public Handle<GPUResource> extractionTarget;
    public ResourceExtractionFlags extractionFlags;

    public Identifier<RGResource> inPlacePredecessor;
    public Identifier<RGResource> inPlaceSuccessor;

    public readonly bool CanBeAliased
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            var canBeAliased = !isImported && !isExtracted;
            if (type == RGResourceType.Buffer)
            {
                canBeAliased &= !bufferDesc.HeapType.HasFlag(HeapType.Upload);
            }

            return canBeAliased;
        }
    }

    public RenderGraphResource()
    {
        firstUsePass = -1;
        lastUsePass = -1;
        producerHead = -1;
        producerTail = -1;
        lastProducerPass = -1;
        consumerHead = -1;
        consumerTail = -1;
        lastConsumerPass = -1;
        backingResource = Handle<GPUResource>.Invalid;
        inPlacePredecessor = Identifier<RGResource>.Invalid;
        inPlaceSuccessor = Identifier<RGResource>.Invalid;
        isAllocated = true;
    }
}

internal sealed class RenderGraphResourceRegistry : IDisposable
{
    internal struct PassLinkNode
    {
        public int passIndex;
        public int next;
    }

    internal readonly struct PassLinkEnumerable
    {
        private readonly UnsafeList<PassLinkNode> _nodes;
        private readonly int _head;

        public PassLinkEnumerable(UnsafeList<PassLinkNode> nodes, int head)
        {
            _nodes = nodes;
            _head = head;
        }

        public Enumerator GetEnumerator() => new(_nodes, _head);

        public struct Enumerator
        {
            private readonly UnsafeList<PassLinkNode> _nodes;
            private int _next;

            public int Current
            {
                get; private set;
            }

            public Enumerator(UnsafeList<PassLinkNode> nodes, int head)
            {
                _nodes = nodes;
                _next = head;
                Current = -1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                if (_next < 0)
                {
                    return false;
                }

                var node = _nodes[_next];
                Current = node.passIndex;
                _next = node.next;
                return true;
            }
        }
    }

    private readonly IResourceDatabase _database;
    private readonly IResourceAllocator _allocator;
    private readonly ResourceManager _resourceManager;

    private UnsafeList<RenderGraphResource> _resources;
    private UnsafeList<PassLinkNode> _producerNodes;
    private UnsafeList<PassLinkNode> _consumerNodes;
    private UnsafeList<Handle<GPUResource>> _allocatedBackingResources;
#if GHOST_SAFETY_CHECKS
    private readonly Dictionary<int, string> _resourceName;
#endif
    private Handle<GPUResource> _resourceHeap = Handle<GPUResource>.Invalid;
    private ulong _lastHeapSize = 0;

    internal ReadOnlySpan<RenderGraphResource> Resources => _resources;

    public RenderGraphResourceRegistry(IResourceDatabase database, IResourceAllocator allocator, ResourceManager resourceManager)
    {
        _database = database;
        _allocator = allocator;
        _resourceManager = resourceManager;

        _resources = new UnsafeList<RenderGraphResource>(64, AllocationHandle.Persistent);
        _producerNodes = new UnsafeList<PassLinkNode>(128, AllocationHandle.Persistent);
        _consumerNodes = new UnsafeList<PassLinkNode>(256, AllocationHandle.Persistent);
        _allocatedBackingResources = new UnsafeList<Handle<GPUResource>>(32, AllocationHandle.Persistent);
#if GHOST_SAFETY_CHECKS
        _resourceName = new Dictionary<int, string>(64);
#endif
    }

    public int ResourceCount => _resources.Count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddResource(RenderGraphResource resource, string? name)
    {
#if GHOST_SAFETY_CHECKS
        _resourceName[_resources.Count] = name ?? "UnknownResource";
#endif
        _resources.Add(resource);
    }

    public Identifier<RGTexture> ImportTexture(scoped in TextureDesc desc, Handle<GPUTexture> texture, string? name,
        Color128 clearColor = default, float clearDepth = 1.0f, byte clearStencil = 0,
        bool clearAtFirstUse = false, bool discardAtLastUse = false,
        ResourceBarrierData? initialBarrierState = null, ResourceBarrierData? finalBarrierState = null)
    {
        var resource = new RenderGraphResource
        {
            type = RGResourceType.Texture,
            index = _resources.Count,
            rgTextureDesc = new RGTextureDesc
            {
                sizeMode = RGTextureSizeMode.Absolute,
                width = desc.Width,
                height = desc.Height,
                format = desc.Format,
                clearColor = clearColor,
                clearDepth = clearDepth,
                clearStencil = clearStencil,
                clearAtFirstUse = clearAtFirstUse,
                discardAtLastUse = discardAtLastUse,
                dimension = desc.Dimension,
                mipLevels = desc.MipLevels,
                slice = desc.Slice,
                usage = desc.Usage
            },
            isImported = true,
            isAllocated = _database.IsAllocated(texture.AsResource()),
            hasInitialBarrierState = initialBarrierState.HasValue,
            initialBarrierState = initialBarrierState ?? default,
            hasFinalBarrierState = finalBarrierState.HasValue,
            finalBarrierState = finalBarrierState ?? default,
            backingResource = texture.AsResource(),
            resolvedWidth = desc.Width,
            resolvedHeight = desc.Height
        };

        AddResource(resource, name);

        return new Identifier<RGTexture>(resource.index);
    }

    public Identifier<RGTexture> CreateTexture(scoped in RGTextureDesc desc, string? name)
    {
        var resource = new RenderGraphResource
        {
            type = RGResourceType.Texture,
            index = _resources.Count,
            rgTextureDesc = desc,
            isImported = false
        };

        AddResource(resource, name);

        return new Identifier<RGTexture>(resource.index);
    }

    public Identifier<RGBuffer> ImportBuffer(scoped in BufferDesc desc, Handle<GPUBuffer> buffer, string? name, ResourceBarrierData? initialBarrierState = null, ResourceBarrierData? finalBarrierState = null)
    {
        var resource = new RenderGraphResource
        {
            type = RGResourceType.Buffer,
            index = _resources.Count,
            bufferDesc = desc,
            isImported = true,
            isAllocated = _database.IsAllocated(buffer.AsResource()),
            hasInitialBarrierState = initialBarrierState.HasValue,
            initialBarrierState = initialBarrierState ?? default,
            hasFinalBarrierState = finalBarrierState.HasValue,
            finalBarrierState = finalBarrierState ?? default,
            backingResource = buffer.AsResource()
        };

        AddResource(resource, name);

        return new Identifier<RGBuffer>(resource.index);
    }

    public Identifier<RGBuffer> CreateBuffer(scoped in BufferDesc desc, string? name)
    {
        var resource = new RenderGraphResource
        {
            type = RGResourceType.Buffer,
            index = _resources.Count,
            bufferDesc = desc,
            isImported = false
        };

        AddResource(resource, name);

        return new Identifier<RGBuffer>(resource.index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref RenderGraphResource GetResource(Identifier<RGResource> resource)
    {
        return ref _resources[resource.Value];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref RenderGraphResource GetResource(Identifier<RGTexture> texture)
    {
        return ref _resources[texture.Value];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref RenderGraphResource GetResource(Identifier<RGBuffer> buffer)
    {
        return ref _resources[buffer.Value];
    }

    public string GetResourceName(Identifier<RGResource> resource)
    {
#if GHOST_SAFETY_CHECKS
        return _resourceName.GetValueOrDefault(resource.Value, $"UnknownResource");
#else
        return string.Empty;
#endif
    }

    public RGResourceType GetResourceType(Identifier<RGResource> resource)
    {
        return GetResource(resource).type;
    }

    /// <summary>
    /// Gets heap by global index. Use this when iterating over all resources.
    /// </summary>
    public ref RenderGraphResource GetResourceByIndex(int index)
    {
        return ref _resources[index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AppendPassLink(ref UnsafeList<PassLinkNode> nodes, ref int head, ref int tail, ref int lastPass, ref int firstUsePass, ref int lastUsePass, int passIndex)
    {
        if (lastPass == passIndex)
        {
            return;
        }

        if (passIndex < lastPass)
        {
            var cursor = head;
            while (cursor >= 0)
            {
                if (nodes[cursor].passIndex == passIndex)
                {
                    return;
                }

                cursor = nodes[cursor].next;
            }
        }
        else
        {
            lastPass = passIndex;
        }

        var nodeIdx = nodes.Count;
        nodes.Add(new PassLinkNode { passIndex = passIndex, next = -1 });
        if (head < 0)
        {
            head = nodeIdx;
        }
        else
        {
            var prev = nodes[tail];
            prev.next = nodeIdx;
            nodes[tail] = prev;
        }

        tail = nodeIdx;
        firstUsePass = firstUsePass < 0 ? passIndex : Math.Min(firstUsePass, passIndex);
        lastUsePass = Math.Max(lastUsePass, passIndex);
    }

    public void SetProducer(Identifier<RGResource> resourceID, int passIndex)
    {
        ref var resource = ref GetResource(resourceID);
        AppendPassLink(ref _producerNodes, ref resource.producerHead, ref resource.producerTail, ref resource.lastProducerPass, ref resource.firstUsePass, ref resource.lastUsePass, passIndex);
    }

    public void AddConsumer(Identifier<RGResource> resourceID, int passIndex)
    {
        ref var resource = ref GetResource(resourceID);
        AppendPassLink(ref _consumerNodes, ref resource.consumerHead, ref resource.consumerTail, ref resource.lastConsumerPass, ref resource.firstUsePass, ref resource.lastUsePass, passIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PassLinkEnumerable GetProducerPasses(int resourceIndex)
    {
        return new PassLinkEnumerable(_producerNodes, _resources[resourceIndex].producerHead);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PassLinkEnumerable GetConsumerPasses(int resourceIndex)
    {
        return new PassLinkEnumerable(_consumerNodes, _resources[resourceIndex].consumerHead);
    }

    /// <summary>
    /// Resolves texture sizes based on current view state.
    /// Must be called after all resources are created and before compilation.
    /// </summary>
    internal void ResolveTextureSizes(in ViewportState viewport)
    {
        for (var i = 0; i < _resources.Count; i++)
        {
            ref var res = ref _resources[i];
            if (res.type != RGResourceType.Texture || res.isImported)
            {
                continue;
            }

            var desc = res.rgTextureDesc;
            if (desc.sizeMode == RGTextureSizeMode.Absolute)
            {
                res.resolvedWidth = desc.width;
                res.resolvedHeight = desc.height;
            }
            else // Relative
            {
                res.resolvedWidth = (uint)(desc.scaleX * viewport.Size.x);
                res.resolvedHeight = (uint)(desc.scaleY * viewport.Size.y);
            }
        }
    }

    private bool TryReuseImportedResourceInPlace(int extractedIndex, in RenderGraphResourceOrdering ordering)
    {
        ref var extractedRes = ref _resources[extractedIndex];
        if (!extractedRes.isExtracted || extractedRes.extractionTarget.IsInvalid)
        {
            return false;
        }

        for (var i = 0; i < _resources.Count; i++)
        {
            ref var importedRes = ref _resources[i];
            if (!importedRes.isImported || importedRes.backingResource != extractedRes.extractionTarget)
            {
                continue;
            }

            if (!importedRes.isAllocated || !_database.IsAllocated(importedRes.backingResource))
            {
                return false;
            }

            if (importedRes.type != extractedRes.type)
            {
                return false;
            }

            if (extractedRes.type == RGResourceType.Texture)
            {
                if (importedRes.resolvedWidth != extractedRes.resolvedWidth
                    || importedRes.resolvedHeight != extractedRes.resolvedHeight
                    || importedRes.rgTextureDesc.format != extractedRes.rgTextureDesc.format
                    || importedRes.rgTextureDesc.mipLevels != extractedRes.rgTextureDesc.mipLevels
                    || importedRes.rgTextureDesc.dimension != extractedRes.rgTextureDesc.dimension
                    || importedRes.rgTextureDesc.slice != extractedRes.rgTextureDesc.slice)
                {
                    return false;
                }
            }
            else if (extractedRes.type == RGResourceType.Buffer)
            {
                if (importedRes.bufferDesc.Size < extractedRes.bufferDesc.Size
                    || importedRes.bufferDesc.Stride != extractedRes.bufferDesc.Stride)
                {
                    return false;
                }
            }

            if (ordering.ResourceCount > 0)
            {
                var hasUses = ordering.HasUses(i);
                if (hasUses && !ordering.AllUsesHappenBefore(i, extractedIndex))
                {
                    return false;
                }
            }

            extractedRes.backingResource = importedRes.backingResource;
            extractedRes.inPlacePredecessor = new Identifier<RGResource>(i);
            importedRes.inPlaceSuccessor = new Identifier<RGResource>(extractedIndex);
            return true;
        }

        return false;
    }

    public Error AllocateBackingResources(ref AliasingPlan plan, RenderGraphCompilationCache cache, in RenderGraphResourceOrdering ordering = default)
    {
        var reallocateHeap = plan.totalHeapSize > _lastHeapSize;
        if (_allocatedBackingResources.Count > 0 || _resourceHeap.IsValid)
        {
            for (var i = 0; i < _allocatedBackingResources.Count; i++)
            {
                var handle = _allocatedBackingResources[i];
                if (handle.IsValid)
                {
                    _database.ReleaseResource(handle);
                }
            }

            _allocatedBackingResources.Clear();

            if (_resourceHeap.IsValid && reallocateHeap)
            {
                _database.ReleaseResource(_resourceHeap);
                _resourceHeap = Handle<GPUResource>.Invalid;
            }
        }

        if (reallocateHeap && plan.totalHeapSize > 0)
        {
            var allocationDesc = new AllocationDesc
            {
                Size = plan.totalHeapSize,
                Alignment = 65536, // 64KB
                HeapFlags = HeapFlags.AllowAllBufferAndTexture,
                HeapType = HeapType.Default
            };

            _resourceHeap = _allocator.Allocate(in allocationDesc, "RenderGraphResourceHeap");
            if (_resourceHeap.IsInvalid)
            {
                Logger.Debug($"Failed to allocate resource heap of size {plan.totalHeapSize} bytes.");

                _lastHeapSize = 0;
                return Error.InvalidState;
            }

            _lastHeapSize = plan.totalHeapSize;
        }

        for (var i = 0; i < _resources.Count; i++)
        {
            ref var res = ref _resources[i];
            if (res.isImported)
            {
                continue;
            }

            var name = GetResourceName(i);
            if (res.isExtracted)
            {
                if (TryReuseImportedResourceInPlace(i, ordering))
                {
                    cache.UpdateBackingResource(i, res.backingResource);
                    continue;
                }

                if (res.type == RGResourceType.Texture)
                {
                    var textureDesc = res.rgTextureDesc.ToTextureDesc(res.resolvedWidth, res.resolvedHeight);
                    res.backingResource = _resourceManager.CreatePooledTexture(in textureDesc, name).AsResource();
                }
                else if (res.type == RGResourceType.Buffer)
                {
                    res.backingResource = _resourceManager.CreatePooledBuffer(in res.bufferDesc, name).AsResource();
                }
            }
            else if (res.type == RGResourceType.Buffer && res.bufferDesc.HeapType == HeapType.Upload)
            {
                res.backingResource = _allocator.CreateBuffer(in res.bufferDesc, name).AsResource();
                if (res.backingResource.IsInvalid)
                {
                    Logger.Debug($"Failed to allocate upload buffer for resource '{name}' of size {res.bufferDesc.Size} bytes.");
                    return Error.InvalidState;
                }

                _allocatedBackingResources.Add(res.backingResource);
            }
            else
            {
                var placedIndex = plan.GetPlacedResourceIndex(i);
                var placedResult = plan.GetPlacedResource(placedIndex);
                if (placedResult.IsFailure)
                {
                    continue;
                }

                var ops = new CreationOptions
                {
                    Flags = ResourceAllocationFlags.Suballocation,
                    Heap = _resourceHeap,
                    Offset = placedResult.Value.heapOffset,
                };

                if (res.type == RGResourceType.Texture)
                {
                    var textureDesc = res.rgTextureDesc.ToTextureDesc(res.resolvedWidth, res.resolvedHeight);
                    res.backingResource = _allocator.CreateTexture(in textureDesc, name, ops).AsResource();
                }
                else if (res.type == RGResourceType.Buffer)
                {
                    res.backingResource = _allocator.CreateBuffer(in res.bufferDesc, name, ops).AsResource();
                }
                else
                {
                    throw new NotSupportedException();
                }

                if (res.backingResource.IsInvalid)
                {
                    Logger.Debug($"Failed to allocate resource '{name}' of type {res.type}.");
                    return Error.InvalidState;
                }

                _allocatedBackingResources.Add(res.backingResource);
            }

            cache.UpdateBackingResource(i, res.backingResource);
        }

        return Error.None;
    }

    public void RestoreBackingResources(ReadOnlySpan<Handle<GPUResource>> cachedBackingResources)
    {
        for (var i = 0; i < _resources.Count; i++)
        {
            ref var res = ref _resources[i];
            if (res.isImported)
            {
                continue;
            }

            if (res.isExtracted)
            {
                var wasInPlaceReused = cachedBackingResources[i] == res.extractionTarget;
                if (wasInPlaceReused && TryReuseImportedResourceInPlace(i, default))
                {
                    continue;
                }

                var name = GetResourceName(i);

                // Extracted resources need a fresh/pooled handle every frame
                if (res.type == RGResourceType.Texture)
                {
                    var textureDesc = res.rgTextureDesc.ToTextureDesc(res.resolvedWidth, res.resolvedHeight);
                    res.backingResource = _resourceManager.CreatePooledTexture(textureDesc, name).AsResource();
                }
                else if (res.type == RGResourceType.Buffer)
                {
                    res.backingResource = _resourceManager.CreatePooledBuffer(res.bufferDesc, name).AsResource();
                }
            }
            else
            {
                res.backingResource = cachedBackingResources[i];
            }
        }
    }

    public void Reset()
    {
        _resources.Clear();
        _producerNodes.Clear();
        _consumerNodes.Clear();
#if GHOST_SAFETY_CHECKS
        _resourceName.Clear();
#endif
    }

    public void Dispose()
    {
        for (var i = 0; i < _allocatedBackingResources.Count; i++)
        {
            var handle = _allocatedBackingResources[i];
            if (handle.IsValid)
            {
                _database.ReleaseResource(handle);
            }
        }
        _allocatedBackingResources.Dispose();

        _producerNodes.Dispose();
        _consumerNodes.Dispose();
        _resources.Dispose();

        if (_resourceHeap.IsValid)
        {
            _database.ReleaseResource(_resourceHeap);
            _resourceHeap = Handle<GPUResource>.Invalid;
        }
    }
}
