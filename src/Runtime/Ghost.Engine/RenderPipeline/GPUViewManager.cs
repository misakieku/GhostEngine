using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Engine.Volumes;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.LowLevel.Buffer;
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
    private readonly IResourceAllocator _allocator;
    private readonly IResourceDatabase _database;
    private readonly IPipelineLibrary _pipelineLibrary;
    private readonly ResourceManager _resourceManager;
    private readonly ShaderLibrary _shaderLibrary;

    private readonly Dictionary<Type, IViewHistory> _histories = new();
    private RenderGraph? _renderGraph;

    public float4x4 prevViewProjMatrix;
    public float3 prevCameraPosition;
    public ulong prevSkyHash;

    public uint ViewId
    {
        get;
    }

    public bool IsActive
    {
        get; private set;
    }

    public uint2 RenderSize
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

    public THistory GetOrCreateHistory<THistory>(Func<IResourceAllocator, IResourceDatabase, THistory> factory)
        where THistory : class, IViewHistory
    {
        var type = typeof(THistory);
        if (!_histories.TryGetValue(type, out var history))
        {
            history = factory(_allocator, _database);
            _histories[type] = history;
        }

        return (THistory)history;
    }

    public bool TryGetHistory<THistory>([NotNullWhen(true)] out THistory? history)
        where THistory : class, IViewHistory
    {
        if (_histories.TryGetValue(typeof(THistory), out var h))
        {
            history = (THistory)h;
            return true;
        }

        history = null;
        return false;
    }

    public void EnsureSize(uint2 renderSize)
    {
        if (renderSize.x == 0 || renderSize.y == 0)
        {
            return;
        }

        _renderGraph ??= new RenderGraph(_database, _allocator, _pipelineLibrary, _resourceManager, _shaderLibrary);

        if (RenderSize.x != renderSize.x || RenderSize.y != renderSize.y)
        {
            foreach (var history in _histories.Values)
            {
                history.OnResize(renderSize);
            }

            prevViewProjMatrix = default;
            prevCameraPosition = default;
            RenderSize = renderSize;
        }
    }

    public void Dispose()
    {
        foreach (var history in _histories.Values)
        {
            history.Dispose();
        }
        _histories.Clear();

        _renderGraph?.Dispose();
        _renderGraph = null;

        IsActive = false;
        RenderSize = default;
        prevViewProjMatrix = default;
        prevCameraPosition = default;
        prevSkyHash = 0;
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
