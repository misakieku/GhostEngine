using Ghost.Core;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Engine.RenderPipeline;

public sealed class RisViewResource : IViewResource<RisViewResource, RisViewResource.Desc>
{
    public struct Desc
    {
        public uint2 renderSize;
    }

    private readonly IResourceAllocator _allocator;
    private readonly IResourceDatabase _database;

    public Handle<GPUTexture> DiffuseHistory { get; private set; }
    public Handle<GPUTexture> SpecularHistory { get; private set; }
    public uint2 RenderSize { get; private set; }
    public bool HasHistory { get; private set; }

    public static RisViewResource Create(IResourceAllocator allocator, IResourceDatabase database)
    {
        return new RisViewResource(allocator, database);
    }

    private RisViewResource(IResourceAllocator allocator, IResourceDatabase database)
    {
        _allocator = allocator;
        _database = database;
    }

    private void ReleaseResource()
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

        RenderSize = default;
        HasHistory = false;
    }

    public void EnsureResource(in Desc desc)
    {
        if (RenderSize.x == desc.renderSize.x && RenderSize.y == desc.renderSize.y && DiffuseHistory.IsValid && SpecularHistory.IsValid)
        {
            HasHistory = true;
            return;
        }

        ReleaseResource();

        var texDesc = new TextureDesc
        {
            Dimension = TextureDimension.Texture2D,
            Width = desc.renderSize.x,
            Height = desc.renderSize.y,
            MipLevels = 1,
            Format = TextureFormat.R16G16B16A16_Float,
            Usage = TextureUsage.ShaderResource | TextureUsage.UnorderedAccess,
            Slice = 1,
        };

        DiffuseHistory = _allocator.CreateTexture(in texDesc, "RIS History Diffuse");
        SpecularHistory = _allocator.CreateTexture(in texDesc, "RIS History Specular");
        RenderSize = desc.renderSize;
    }

    public void Dispose()
    {
        ReleaseResource();
    }
}
