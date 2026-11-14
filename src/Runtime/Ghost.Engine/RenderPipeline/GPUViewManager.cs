using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.Mathematics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Engine.RenderPipeline;

[GenerateHLSL(PackingRules.Exact, "EngineResources/Shaders/Generated/GhostRenderPipeline.hlsl")]
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct GPUPersistentViewState
{
    [InlineArray(7)]
    public struct SH9Array
    {
        private float4 _element;
    }

    [GenerateAsHLSLType("float4", 7)]
    public SH9Array skyIrradianceSH;   // 112 bytes: 7x float4 (shAr, shAg, shAb, shBr, shBg, shBb, shC)
    public float4 eyeAdaptationState;  // 16 bytes: (exposureScale, prevExposureScale, adaptedLum, targetEV100)
    public float4 reserved0;           // 128 bytes reserved for future persistent view feedback
    public float4 reserved1;
    public float4 reserved2;
    public float4 reserved3;
    public float4 reserved4;
    public float4 reserved5;
    public float4 reserved6;
    public float4 reserved7;
}

public sealed class GPUViewContext : IDisposable
{
    private class ResourceRecord
    {
        public IViewResource Resource
        {
            get;
        }

        public bool Used
        {
            get; set;
        }

        public ResourceRecord(IViewResource resource, bool used)
        {
            Resource = resource;
            Used = used;
        }
    }

    private readonly IResourceAllocator _allocator;
    private readonly IResourceDatabase _database;
    private readonly IPipelineLibrary _pipelineLibrary;
    private readonly ResourceManager _resourceManager;
    private readonly ShaderLibrary _shaderLibrary;

    private readonly Dictionary<Type, ResourceRecord> _resources = new(16);
    private readonly List<Type> _unusedResources = new(16);

    private RenderGraph? _renderGraph;

    private uint2 _renderSize;
    public float4x4 prevViewProjMatrix;
    public float3 prevCameraPosition;
    public ulong prevSkyHash;
    public bool isExposureInitialized;

    public uint ViewId
    {
        get;
    }

    public bool IsActive
    {
        get; private set;
    }

    public RenderGraph RenderGraph
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            Logger.DebugAssert(_renderGraph != null, "RenderGraph is not initialized.");
            return _renderGraph;
        }
    }

    public GPUViewContext(IResourceAllocator allocator, IResourceDatabase database, IPipelineLibrary pipelineLibrary, ResourceManager resourceManager, ShaderLibrary shaderLibrary, uint viewId)
    {
        _allocator = allocator;
        _database = database;
        _pipelineLibrary = pipelineLibrary;
        _resourceManager = resourceManager;
        _shaderLibrary = shaderLibrary;

        ViewId = viewId;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Active()
    {
        IsActive = true;
        _renderGraph ??= new RenderGraph(_database, _allocator, _pipelineLibrary, _resourceManager, _shaderLibrary);
    }

    internal void ReleaseUnusedResource()
    {
        foreach (var kvp in _resources)
        {
            if (!kvp.Value.Used)
            {
                _unusedResources.Add(kvp.Key);
            }
            else
            {
                kvp.Value.Used = false;
            }
        }

        foreach (var type in _unusedResources)
        {
            if (_resources.TryGetValue(type, out var res))
            {
                res.Resource.Dispose();
                _resources.Remove(type);
            }
        }

        _unusedResources.Clear();
    }

    public TResource GetOrCreateResource<TResource, TDesc>(in TDesc desc)
        where TResource : class, IViewResource<TResource, TDesc>
        where TDesc : struct
    {
        TResource resource;
        var type = typeof(TResource);

        ref var res = ref CollectionsMarshal.GetValueRefOrAddDefault(_resources, type, out var exists);
        if (!exists || res == null)
        {
            resource = TResource.Create(_allocator, _database);
            res = new ResourceRecord(resource, true);
        }
        else
        {
            res.Used = true;
            resource = (TResource)res.Resource;
        }

        resource.EnsureResource(desc);
        return resource;
    }

    public bool TryGetResource<TResource>([NotNullWhen(true)] out TResource? resource)
        where TResource : class, IViewResource
    {
        if (!_resources.TryGetValue(typeof(TResource), out var res))
        {
            resource = null;
            return false;
        }

        res.Used = true;
        resource = (TResource)res.Resource;
        return true;
    }

    public void Initialize(uint2 renderSize)
    {
        if (renderSize.x == 0 || renderSize.y == 0)
        {
            return;
        }

        _renderGraph ??= new RenderGraph(_database, _allocator, _pipelineLibrary, _resourceManager, _shaderLibrary);

        if (_renderSize.x != renderSize.x || _renderSize.y != renderSize.y)
        {
            prevViewProjMatrix = default;
            prevCameraPosition = default;
            _renderSize = renderSize;
        }
    }

    public void Dispose()
    {
        foreach (var res in _resources.Values)
        {
            res.Resource.Dispose();
        }

        _resources.Clear();

        _renderGraph?.Dispose();
        _renderGraph = null;

        IsActive = false;
        _renderSize = default;
        prevViewProjMatrix = default;
        prevCameraPosition = default;
        prevSkyHash = 0;
        isExposureInitialized = false;
    }
}

internal sealed class GPUViewManager : IDisposable
{
    public const int MAX_VIEWS = 32;
    private readonly GPUViewContext[] _views = new GPUViewContext[MAX_VIEWS];
    private readonly Stack<uint> _freeIndices = new(MAX_VIEWS);
    private readonly IResourceDatabase _database;

    private readonly Lock _lock = new();

    public Handle<GPUBuffer> PersistentViewStateBuffer
    {
        get; private set;
    }

    public GPUViewManager(IResourceAllocator allocator, IResourceDatabase database, IPipelineLibrary pipelineLibrary, ResourceManager resourceManager, ShaderLibrary shaderLibrary)
    {
        _database = database;

        var desc = new BufferDesc
        {
            Size = (nuint)(MAX_VIEWS * Marshal.SizeOf<GPUPersistentViewState>()),
            Stride = 4,
            Usage = BufferUsage.Raw | BufferUsage.ShaderResource | BufferUsage.UnorderedAccess,
            HeapType = HeapType.Default
        };

        PersistentViewStateBuffer = allocator.CreateBuffer(in desc, "Persistent View State Buffer");

        for (uint i = 0; i < MAX_VIEWS; i++)
        {
            _views[i] = new GPUViewContext(allocator, database, pipelineLibrary, resourceManager, shaderLibrary, i);
            _freeIndices.Push(MAX_VIEWS - 1 - i);
        }
    }

    public uint AllocateView()
    {
        lock (_lock)
        {
            if (_freeIndices.TryPop(out var id))
            {
                _views[id].Active();
                return id;
            }

            throw new InvalidOperationException($"Exceeded maximum concurrent GPU views ({MAX_VIEWS}).");
        }
    }

    public void ReleaseView(uint viewId)
    {
        lock (_lock)
        {
            if (viewId < MAX_VIEWS && _views[viewId].IsActive)
            {
                _views[viewId].Dispose();
                _freeIndices.Push(viewId);
            }
        }
    }

    public GPUViewContext GetView(uint viewId)
    {
        if (viewId >= MAX_VIEWS)
        {
            throw new ArgumentOutOfRangeException(nameof(viewId), $"Invalid viewId {viewId}.");
        }

        return _views[viewId];
    }

    public void EndFrame()
    {
        lock (_lock)
        {
            foreach (var view in _views)
            {
                if (view.IsActive)
                {
                    view.ReleaseUnusedResource();
                }
            }
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (PersistentViewStateBuffer.IsValid)
            {
                _database.ReleaseResource(PersistentViewStateBuffer.AsResource());
                PersistentViewStateBuffer = Handle<GPUBuffer>.Invalid;
            }

            for (var i = 0; i < MAX_VIEWS; i++)
            {
                _views[i].Dispose();
            }
        }
    }
}
