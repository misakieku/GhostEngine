using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.CompilerServices;

namespace Ghost.Engine.RenderPipeline;

public sealed class GPUViewContext : IDisposable
{
    private readonly IResourceAllocator _allocator;
    private readonly IResourceDatabase _database;
    private readonly IPipelineLibrary _pipelineLibrary;
    private readonly ResourceManager _resourceManager;
    private readonly ShaderLibrary _shaderLibrary;

    private RenderGraph? _renderGraph;

    public float4x4 prevViewProjMatrix;

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

    public uint2 HzbSize
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
            Logger.DebugAssert(_renderGraph != null, "RenderGraph is not initialized. Call EnsureResources() first.");
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
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ComputeHZBDimensions(uint2 renderSize, out uint2 hzbSize, out uint hzbMipCount)
    {
        hzbSize = new uint2(Math.Max(1u, (renderSize.x + 1) / 2), Math.Max(1u, (renderSize.y + 1) / 2));

        var calculatedMipCount = (uint)Math.Floor(Math.Log2(Math.Max(hzbSize.x, hzbSize.y))) + 1;
        hzbMipCount = Math.Clamp(calculatedMipCount, 1u, 16u);
    }

    public void EnsureResources(uint2 renderSize)
    {
        if (renderSize.x == 0 || renderSize.y == 0)
        {
            return;
        }

        _renderGraph ??= new RenderGraph(_database, _allocator, _pipelineLibrary, _resourceManager, _shaderLibrary);

        if (!HzbTexture.IsValid || RenderSize.x != renderSize.x || RenderSize.y != renderSize.y)
        {
            if (HzbTexture.IsValid)
            {
                _database.ReleaseResource(HzbTexture.AsResource());
            }

            ComputeHZBDimensions(renderSize, out var baseSize, out var hzbMipCount);

            HzbSize = baseSize;
            HzbMipCount = hzbMipCount;

            var desc = new TextureDesc
            {
                Width = HzbSize.x,
                Height = HzbSize.y,
                Format = TextureFormat.R32_Float,
                Dimension = TextureDimension.Texture2D,
                MipLevels = (ushort)HzbMipCount,
                Slice = 1,
                Usage = TextureUsage.UnorderedAccess | TextureUsage.ShaderResource
            };

            HzbTexture = _allocator.CreateTexture(in desc, StringUtility.DebugFormat("View_{0}_HZB", ViewId));
            RenderSize = renderSize;
            prevViewProjMatrix = default; // Zero out so first frame or resize is not static
        }
    }

    public void Dispose()
    {
        if (HzbTexture.IsValid)
        {
            _database.ReleaseResource(HzbTexture.AsResource());
            HzbTexture = Handle<GPUTexture>.Invalid;
        }

        _renderGraph?.Dispose();
        _renderGraph = null;

        IsActive = false;
        RenderSize = default;
        HzbSize = default;
        prevViewProjMatrix = default;
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
            for (var i = 0; i < MAX_VIEWS; i++)
            {
                _views[i].Dispose();
            }
        }
    }
}
