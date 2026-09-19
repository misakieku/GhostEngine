using Ghost.Core;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Engine.RenderPipeline;

internal sealed class GPUViewContext : IDisposable
{
    private readonly IResourceAllocator _allocator;
    private readonly IResourceDatabase _database;
    private readonly IPipelineLibrary _pipelineLibrary;
    private readonly ResourceManager _resourceManager;
    private readonly ShaderLibrary _shaderLibrary;

    private RenderGraph? _renderGraph;

    public RenderGraph RenderGraph
    {
        get
        {
            return _renderGraph ?? throw new InvalidOperationException("RenderGraph is not initialized. Call EnsureResources() first.");
        }
    }

    public uint ViewId
    {
        get;
    }

    public bool IsActive
    {
        get; private set;
    }

    public Handle<GPUTexture> HzbTexture
    {
        get; private set;
    }

    public uint HzbMipCount
    {
        get; private set;
    }

    public uint BaseWidth
    {
        get; private set;
    }

    public uint BaseHeight
    {
        get; private set;
    }

    public uint RenderWidth
    {
        get; private set;
    }

    public uint RenderHeight
    {
        get; private set;
    }

    public uint2 BaseSize => new uint2(BaseWidth, BaseHeight);


    public float4x4 prevViewProjMatrix;

    public GPUViewContext(IResourceAllocator allocator, IResourceDatabase database, IPipelineLibrary pipelineLibrary, ResourceManager resourceManager, ShaderLibrary shaderLibrary, uint viewId)
    {
        _allocator = allocator;
        _database = database;
        _pipelineLibrary = pipelineLibrary;
        _resourceManager = resourceManager;
        _shaderLibrary = shaderLibrary;

        ViewId = viewId;
    }

    internal void Active()
    {
        IsActive = true;
    }

    internal static void ComputeHZBDimensions(uint renderWidth, uint renderHeight, out uint baseW, out uint baseH, out uint hzbMipCount)
    {
        baseW = Math.Max(1u, (renderWidth + 1) / 2);
        baseH = Math.Max(1u, (renderHeight + 1) / 2);

        var calculatedMipCount = (uint)Math.Floor(Math.Log2(Math.Max(baseW, baseH))) + 1;
        hzbMipCount = Math.Clamp(calculatedMipCount, 1u, 16u);
    }

    public void EnsureResources(uint renderWidth, uint renderHeight)
    {
        if (renderWidth == 0 || renderHeight == 0)
        {
            return;
        }

        if (!HzbTexture.IsValid || this.RenderWidth != renderWidth || this.RenderHeight != renderHeight || RenderGraph == null)
        {
            if (HzbTexture.IsValid)
            {
                _database.ReleaseResource(HzbTexture.AsResource());
                HzbTexture = Handle<GPUTexture>.Invalid;
            }

            uint baseWidth, baseHeight, hzbMipCount;
            ComputeHZBDimensions(
                renderWidth,
                renderHeight,
                out baseWidth,
                out baseHeight,
                out hzbMipCount);

            BaseWidth = baseWidth;
            BaseHeight = baseHeight;
            HzbMipCount = hzbMipCount;

            var desc = new TextureDesc
            {
                Width = BaseWidth,
                Height = BaseHeight,
                Format = TextureFormat.R32_Float,
                Dimension = TextureDimension.Texture2D,
                MipLevels = (ushort)HzbMipCount,
                Slice = 1,
                Usage = TextureUsage.UnorderedAccess | TextureUsage.ShaderResource
            };

            HzbTexture = _allocator.CreateTexture(in desc, $"View_{ViewId}_HZB");
            RenderWidth = renderWidth;
            RenderHeight = renderHeight;
            prevViewProjMatrix = default; // Zero out so first frame or resize is not static

            _renderGraph = new RenderGraph(_database, _allocator, _pipelineLibrary, _resourceManager, _shaderLibrary);
        }
    }

    public void ReleaseResources(IResourceDatabase db)
    {
        if (HzbTexture.IsValid)
        {
            db.ReleaseResource(HzbTexture.AsResource());
            HzbTexture = Handle<GPUTexture>.Invalid;
        }

        _renderGraph?.Dispose();
        _renderGraph = null;

        RenderWidth = 0;
        RenderHeight = 0;
        BaseWidth = 0;
        BaseHeight = 0;
        prevViewProjMatrix = default;
        IsActive = false;
    }

    public void Dispose()
    {
    }
}

internal sealed class GPUViewManager : IDisposable
{
    public const int MAX_VIEWS = 32;
    private readonly GPUViewContext[] _views = new GPUViewContext[MAX_VIEWS];
    private readonly Stack<uint> _freeIndices = new(MAX_VIEWS);
    private readonly IResourceDatabase _database;

    private readonly Lock _lock = new();

    public GPUViewManager(IResourceAllocator allocator, IResourceDatabase database, IPipelineLibrary pipelineLibrary, ResourceManager resourceManager, ShaderLibrary shaderLibrary)
    {
        _database = database;

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
                _views[viewId].ReleaseResources(_database);
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
            for (var i = 0; i < MAX_VIEWS; i++)
            {
                _views[i].ReleaseResources(_database);
            }
        }
    }
}
