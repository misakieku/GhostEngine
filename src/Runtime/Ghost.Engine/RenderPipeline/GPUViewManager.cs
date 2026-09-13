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

    public Handle<GPUTexture> hzbAtlas;
    public uint hzbMipCount;
    public uint atlasWidth;
    public uint atlasHeight;
    public uint baseWidth;
    public uint baseHeight;
    public uint renderWidth;
    public uint renderHeight;
    public float hzbMaxMegapixels;

    public uint4 hzbOffsets0;
    public uint4 hzbOffsets1;
    public uint4 hzbOffsets2;
    public uint4 hzbOffsets3;

    public float4x4 prevViewProjMatrix;

    internal static unsafe void ComputeHZBMipOffsets(
        uint renderWidth,
        uint renderHeight,
        float hzbMaxMegapixels,
        out uint baseW,
        out uint baseH,
        out uint hzbMipCount,
        out uint atlasWidth,
        out uint atlasHeight,
        out uint4 o0,
        out uint4 o1,
        out uint4 o2,
        out uint4 o3)
    {
        var rawBaseW = Math.Max(1u, renderWidth / 2);
        var rawBaseH = Math.Max(1u, renderHeight / 2);

        baseW = rawBaseW;
        baseH = rawBaseH;

        if (hzbMaxMegapixels > 0f && !float.IsPositiveInfinity(hzbMaxMegapixels))
        {
            var budgetPixels = (double)hzbMaxMegapixels * 1_000_000.0;
            var currentPixels = (double)rawBaseW * rawBaseH;
            if (currentPixels > budgetPixels)
            {
                var scale = Math.Sqrt(budgetPixels / currentPixels);
                baseW = Math.Max(1u, (uint)Math.Floor(rawBaseW * scale));
                baseH = Math.Max(1u, (uint)Math.Floor(rawBaseH * scale));
            }
        }

        var calculatedMipCount = (uint)Math.Floor(Math.Log2(Math.Max(baseW, baseH))) + 1;
        hzbMipCount = Math.Clamp(calculatedMipCount, 1u, 16u);

        atlasWidth = baseW + Math.Max(1u, (baseW + 1) / 2);

        var packed = stackalloc uint[16];
        // Mip 0 offset is (0,0); packed[0] stores base resolution (width in low 16 bits, height in high 16 bits)
        packed[0] = (baseW & 0xFFFFu) | ((baseH & 0xFFFFu) << 16);

        uint rightY = 0;
        var prevW = baseW;
        var prevH = baseH;
        for (uint i = 1; i < hzbMipCount; ++i)
        {
            var curW = Math.Max(1u, prevW / 2);
            var curH = Math.Max(1u, prevH / 2);
            packed[i] = (baseW & 0xFFFFu) | ((rightY & 0xFFFFu) << 16);
            rightY += curH;
            prevW = curW;
            prevH = curH;
        }

        for (var i = (int)hzbMipCount; i < 16; i++)
        {
            packed[i] = 0;
        }

        atlasHeight = Math.Max(baseH, rightY);

        o0 = new uint4(packed[0], packed[1], packed[2], packed[3]);
        o1 = new uint4(packed[4], packed[5], packed[6], packed[7]);
        o2 = new uint4(packed[8], packed[9], packed[10], packed[11]);
        o3 = new uint4(packed[12], packed[13], packed[14], packed[15]);
    }

    public void EnsureResources(
        IResourceAllocator allocator,
        IResourceDatabase database,
        IPipelineLibrary pipelineLibrary,
        ResourceManager resourceManager,
        ShaderLibrary shaderLibrary,
        uint renderWidth,
        uint renderHeight,
        float hzbMaxMegapixels = 0f)
    {
        if (renderWidth == 0 || renderHeight == 0)
        {
            return;
        }

        if (!hzbAtlas.IsValid || this.renderWidth != renderWidth || this.renderHeight != renderHeight || this.hzbMaxMegapixels != hzbMaxMegapixels || renderGraph == null)
        {
            if (hzbAtlas.IsValid)
            {
                database.ReleaseResource(hzbAtlas.AsResource());
                hzbAtlas = Handle<GPUTexture>.Invalid;
            }

            ComputeHZBMipOffsets(
                renderWidth,
                renderHeight,
                hzbMaxMegapixels,
                out baseWidth,
                out baseHeight,
                out hzbMipCount,
                out atlasWidth,
                out atlasHeight,
                out hzbOffsets0,
                out hzbOffsets1,
                out hzbOffsets2,
                out hzbOffsets3);

            var desc = new TextureDesc
            {
                Width = atlasWidth,
                Height = atlasHeight,
                Format = TextureFormat.R32_Float,
                Dimension = TextureDimension.Texture2D,
                MipLevels = 1,
                Slice = 1,
                Usage = TextureUsage.UnorderedAccess | TextureUsage.ShaderResource
            };

            hzbAtlas = allocator.CreateTexture(in desc, $"View_{viewId}_HZBAtlas");
            this.renderWidth = renderWidth;
            this.renderHeight = renderHeight;
            this.hzbMaxMegapixels = hzbMaxMegapixels;
            prevViewProjMatrix = default; // Zero out so first frame or resize is not static

            renderGraph = new RenderGraph(database, allocator, pipelineLibrary, resourceManager, shaderLibrary);
        }
    }

    public void ReleaseResources(IResourceDatabase db)
    {
        if (hzbAtlas.IsValid)
        {
            db.ReleaseResource(hzbAtlas.AsResource());
            hzbAtlas = Handle<GPUTexture>.Invalid;
        }

        renderGraph?.Dispose();
        renderGraph = null;

        renderWidth = 0;
        renderHeight = 0;
        baseWidth = 0;
        baseHeight = 0;
        hzbMaxMegapixels = 0f;
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
