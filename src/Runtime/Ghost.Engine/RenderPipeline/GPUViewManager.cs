using Ghost.Core;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Engine.RenderPipeline;

internal sealed class GPUViewContext : IDisposable
{
    public RenderGraph? renderGraph;

    public uint viewId;
    public bool isActive;

    public Handle<GPUTexture> hzbTexture;
    public uint hzbMipCount;
    public uint baseWidth;
    public uint baseHeight;
    public uint renderWidth;
    public uint renderHeight;

    public float4x4 prevViewProjMatrix;

    internal static void ComputeHZBDimensions(
        uint renderWidth,
        uint renderHeight,
        out uint baseW,
        out uint baseH,
        out uint hzbMipCount)
    {
        baseW = Math.Max(1u, (renderWidth + 1) / 2);
        baseH = Math.Max(1u, (renderHeight + 1) / 2);

        var calculatedMipCount = (uint)Math.Floor(Math.Log2(Math.Max(baseW, baseH))) + 1;
        hzbMipCount = Math.Clamp(calculatedMipCount, 1u, 16u);
    }

    public void EnsureResources(
        IResourceAllocator allocator,
        IResourceDatabase database,
        IPipelineLibrary pipelineLibrary,
        ResourceManager resourceManager,
        ShaderLibrary shaderLibrary,
        uint renderWidth,
        uint renderHeight)
    {
        if (renderWidth == 0 || renderHeight == 0)
        {
            return;
        }

        if (!hzbTexture.IsValid || this.renderWidth != renderWidth || this.renderHeight != renderHeight || renderGraph == null)
        {
            if (hzbTexture.IsValid)
            {
                database.ReleaseResource(hzbTexture.AsResource());
                hzbTexture = Handle<GPUTexture>.Invalid;
            }

            ComputeHZBDimensions(
                renderWidth,
                renderHeight,
                out baseWidth,
                out baseHeight,
                out hzbMipCount);

            var desc = new TextureDesc
            {
                Width = baseWidth,
                Height = baseHeight,
                Format = TextureFormat.R32_Float,
                Dimension = TextureDimension.Texture2D,
                MipLevels = (ushort)hzbMipCount,
                Slice = 1,
                Usage = TextureUsage.UnorderedAccess | TextureUsage.ShaderResource
            };

            hzbTexture = allocator.CreateTexture(in desc, $"View_{viewId}_HZB");
            this.renderWidth = renderWidth;
            this.renderHeight = renderHeight;
            prevViewProjMatrix = default; // Zero out so first frame or resize is not static

            renderGraph = new RenderGraph(database, allocator, pipelineLibrary, resourceManager, shaderLibrary);
        }
    }

    public void ReleaseResources(IResourceDatabase db)
    {
        if (hzbTexture.IsValid)
        {
            db.ReleaseResource(hzbTexture.AsResource());
            hzbTexture = Handle<GPUTexture>.Invalid;
        }

        renderGraph?.Dispose();
        renderGraph = null;

        renderWidth = 0;
        renderHeight = 0;
        baseWidth = 0;
        baseHeight = 0;
        prevViewProjMatrix = default;
        isActive = false;
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

    public GPUViewManager(IResourceDatabase database)
    {
        _database = database;
        for (uint i = 0; i < MAX_VIEWS; i++)
        {
            _views[i] = new GPUViewContext { viewId = i };
            _freeIndices.Push(MAX_VIEWS - 1 - i);
        }
    }

    public uint AllocateView()
    {
        lock (_lock)
        {
            if (_freeIndices.TryPop(out var id))
            {
                _views[id].isActive = true;
                return id;
            }

            throw new InvalidOperationException($"Exceeded maximum concurrent GPU views ({MAX_VIEWS}).");
        }
    }

    public void ReleaseView(uint viewId)
    {
        lock (_lock)
        {
            if (viewId < MAX_VIEWS && _views[viewId].isActive)
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
