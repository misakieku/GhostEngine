using Ghost.Core;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Engine.RenderPipeline;

public sealed class RisViewHistory : IViewHistory
{
    private readonly IResourceAllocator _allocator;
    private readonly IResourceDatabase _database;

    public Handle<GPUTexture> DiffuseHistory { get; private set; }
    public Handle<GPUTexture> SpecularHistory { get; private set; }
    public uint2 RenderSize { get; private set; }
    public bool IsValid { get; private set; }

    public RisViewHistory(IResourceAllocator allocator, IResourceDatabase database)
    {
        _allocator = allocator;
        _database = database;
    }

    public void EnsureResources(uint2 renderSize)
    {
        if (RenderSize.x == renderSize.x && RenderSize.y == renderSize.y && DiffuseHistory.IsValid && SpecularHistory.IsValid)
        {
            return;
        }

        Reset();

        var desc = new TextureDesc
        {
            Dimension = TextureDimension.Texture2D,
            Width = renderSize.x,
            Height = renderSize.y,
            MipLevels = 1,
            Format = TextureFormat.R16G16B16A16_Float,
            Usage = TextureUsage.ShaderResource | TextureUsage.UnorderedAccess,
            Slice = 1,
        };

        DiffuseHistory = _allocator.CreateTexture(in desc, "RIS History Diffuse");
        SpecularHistory = _allocator.CreateTexture(in desc, "RIS History Specular");
        RenderSize = renderSize;
        IsValid = false;
    }

    public void MarkValid()
    {
        IsValid = true;
    }

    public void Invalidate()
    {
        IsValid = false;
    }

    public void OnResize(uint2 newSize)
    {
        Reset();
    }

    public void Reset()
    {
        if (DiffuseHistory.IsValid)
        {
            _database.ReleaseResource(DiffuseHistory.AsResource());
            DiffuseHistory = Handle<GPUTexture>.Invalid;
        }

        if (SpecularHistory.IsValid)
        {
            _database.ReleaseResource(SpecularHistory.AsResource());
            SpecularHistory = Handle<GPUTexture>.Invalid;
        }

        IsValid = false;
        RenderSize = default;
    }

    public void Dispose()
    {
        Reset();
    }
}
