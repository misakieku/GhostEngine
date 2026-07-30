using Ghost.Core;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.Buffer;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Runtime.CompilerServices;

namespace Ghost.Graphics.RenderGraphModule;

internal sealed class RenderGraphObjectPool
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

    public T Rent<T>()
        where T : class, new()
    {
        return SharedObjectPool<T>.Rent();
    }

    public void Return<T>(T obj)
        where T : class, new()
    {
        SharedObjectPool<T>.Return(obj);
    }

    public void Clear()
    {
        for (var i = 0; i < s_allocatedPools.Count; i++)
        {
            s_allocatedPools[i].Clear();
        }
    }
}

internal record struct RenderGraphResource : IDisposable
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
    public int firstUsePass;
    public int lastUsePass;
    public int producerPass;
    public UnsafeList<int> consumerPasses;
    public int refCount;

    public Handle<GPUResource> backingResource;

    public bool isExtracted;
    public Handle<GPUResource> extractionTarget;
    public ResourceExtractionFlags extractionFlags;

    public RenderGraphResource(AllocationHandle allocationHandle)
    {
        firstUsePass = -1;
        lastUsePass = -1;
        producerPass = -1;
        consumerPasses = new UnsafeList<int>(4, allocationHandle);
        backingResource = Handle<GPUResource>.Invalid;
    }

    public void Dispose()
    {
        consumerPasses.Dispose();
    }
}

internal sealed class RenderGraphResourceRegistry : IDisposable
{
    private readonly IResourceDatabase _database;
    private readonly IResourceAllocator _allocator;
    private readonly ResourceManager _resourceManager;

    private UnsafeList<RenderGraphResource> _resources;
#if GHOST_SAFETY_CHECKS
    private readonly Dictionary<int, string> _resourceName;
#endif
    private Handle<GPUResource> _resourceHeap = Handle<GPUResource>.Invalid;

    internal ReadOnlySpan<RenderGraphResource> Resources => _resources;

    public RenderGraphResourceRegistry(IResourceDatabase database, IResourceAllocator allocator, ResourceManager resourceManager)
    {
        _database = database;
        _allocator = allocator;
        _resourceManager = resourceManager;

        _resources = new UnsafeList<RenderGraphResource>(64, AllocationHandle.Persistent);
#if GHOST_SAFETY_CHECKS
        _resourceName = new Dictionary<int, string>(64);
#endif
    }

    public int ResourceCount => _resources.Count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddResource(RenderGraphResource resource, string? name)
    {
#if GHOST_SAFETY_CHECKS
        _resourceName[_resources.Count] = name ?? "Unknow";
#endif
        _resources.Add(resource);
    }

    public Identifier<RGTexture> ImportTexture(scoped in TextureDesc desc, Handle<GPUTexture> texture, string? name,
        Color128 clearColor, float clearDepth, byte clearStencil,
        bool clearAtFirstUse, bool discardAtLastUse)
    {
        var resource = new RenderGraphResource(AllocationHandle.Temp)
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
            backingResource = texture.AsResource(),
            resolvedWidth = desc.Width,
            resolvedHeight = desc.Height
        };

        AddResource(resource, name);

        return new Identifier<RGTexture>(resource.index);
    }

    public Identifier<RGTexture> CreateTexture(scoped in RGTextureDesc desc, string? name)
    {
        var resource = new RenderGraphResource(AllocationHandle.Temp)
        {
            type = RGResourceType.Texture,
            index = _resources.Count,
            rgTextureDesc = desc,
            isImported = false
        };

        AddResource(resource, name);

        return new Identifier<RGTexture>(resource.index);
    }

    public Identifier<RGBuffer> ImportBuffer(scoped in BufferDesc desc, Handle<GPUBuffer> buffer, string? name)
    {
        var resource = new RenderGraphResource(AllocationHandle.Temp)
        {
            type = RGResourceType.Buffer,
            index = _resources.Count,
            bufferDesc = desc,
            isImported = true,
            backingResource = buffer.AsResource()
        };

        AddResource(resource, name);

        return new Identifier<RGBuffer>(resource.index);
    }

    public Identifier<RGBuffer> CreateBuffer(scoped in BufferDesc desc, string? name)
    {
        var resource = new RenderGraphResource(AllocationHandle.Temp)
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
#if GHOST_SAFETY_CHECKS || GHOST_EDITOR
        return _resourceName.GetValueOrDefault(resource.Value, $"Resource_{resource.Value}");
#else
        return $"Resource_{resource.Value}";
#endif
    }

    /// <summary>
    /// Gets heap by global index. Use this when iterating over all resources.
    /// </summary>
    public ref RenderGraphResource GetResourceByIndex(int index)
    {
        return ref _resources[index];
    }

    public void SetProducer(Identifier<RGResource> resourceID, int passIndex)
    {
        ref var resource = ref GetResource(resourceID);
        resource.producerPass = passIndex;
        if (resource.firstUsePass < 0)
        {
            resource.firstUsePass = passIndex;
        }
    }

    public void AddConsumer(Identifier<RGResource> resourceID, int passIndex)
    {
        ref var resource = ref GetResource(resourceID);
        resource.consumerPasses.Add(passIndex);
        resource.lastUsePass = passIndex;
        if (resource.firstUsePass < 0)
        {
            resource.firstUsePass = passIndex;
        }
    }

    /// <summary>
    /// Resolves texture sizes based on current view state.
    /// Must be called after all resources are created and before compilation.
    /// </summary>
    internal void ResolveTextureSizes(in ViewState viewState)
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
                res.resolvedWidth = (uint)(desc.scaleX * viewState.viewportWidth);
                res.resolvedHeight = (uint)(desc.scaleY * viewState.viewportHeight);
            }
        }
    }

    public Error AllocateBackingResources(AliasingPlan plan, RenderGraphCompilationCache cache)
    {
        if (_resourceHeap.IsValid)
        {
            foreach (var res in _resources)
            {
                if (res.isImported || res.backingResource.IsInvalid)
                {
                    continue;
                }

                _database.ReleaseResource(res.backingResource);
            }

            _database.ReleaseResource(_resourceHeap);
            _resourceHeap = Handle<GPUResource>.Invalid;
        }

        if (plan.totalHeapSize > 0)
        {
            var allocationDesc = new AllocationDesc
            {
                Size = plan.totalHeapSize + 65536, // Add 64KB padding to avoid potential overflows
                Alignment = 65536, // 64KB
                HeapFlags = HeapFlags.AllowAllBufferAndTexture,
                HeapType = HeapType.Default
            };

            _resourceHeap = _allocator.Allocate(in allocationDesc, "RenderGraphResourceHeap");
            if (_resourceHeap.IsInvalid)
            {
                return Error.InvalidState;
            }
        }

        for (var i = 0; i < _resources.Count; i++)
        {
            ref var res = ref _resources[i];
            if (res.isImported)
            {
                continue;
            }

            if (res.isExtracted)
            {
                if (res.type == RGResourceType.Texture)
                {
                    var textureDesc = res.rgTextureDesc.ToTextureDesc(res.resolvedWidth, res.resolvedHeight);
                    res.backingResource = _resourceManager.CreatePooledTexture(in textureDesc).AsResource();
                }
                else if (res.type == RGResourceType.Buffer)
                {
                    res.backingResource = _resourceManager.CreatePooledBuffer(in res.bufferDesc).AsResource();
                }
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
                    AllocationType = ResourceAllocationType.Suballocation,
                    Heap = _resourceHeap,
                    Offset = placedResult.Value.heapOffset,
                };

                var name = GetResourceName(i);

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
                    return Error.InvalidState;
                }
            }

            cache.UpdateBackingResource(i, res.backingResource);
        }

        return Error.None;
    }

    public void RestoreBackingResources(scoped in UnsafeList<Handle<GPUResource>> cachedBackingResources)
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
                // Extracted resources need a fresh/pooled handle every frame
                if (res.type == RGResourceType.Texture)
                {
                    var textureDesc = res.rgTextureDesc.ToTextureDesc(res.resolvedWidth, res.resolvedHeight);
                    res.backingResource = _resourceManager.CreatePooledTexture(textureDesc).AsResource();
                }
                else if (res.type == RGResourceType.Buffer)
                {
                    res.backingResource = _resourceManager.CreatePooledBuffer(res.bufferDesc).AsResource();
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
        foreach (ref var res in _resources)
        {
            res.Dispose();
        }

        _resources.Clear();
#if GHOST_SAFETY_CHECKS
        _resourceName.Clear();
#endif
    }

    public void Dispose()
    {
        foreach (ref var res in _resources)
        {
            if (!res.isImported)
            {
                _database.ReleaseResource(res.backingResource);
            }

            res.Dispose();
        }

        _resources.Dispose();

        _database.ReleaseResource(_resourceHeap);
        _resourceHeap = Handle<GPUResource>.Invalid;
    }
}
