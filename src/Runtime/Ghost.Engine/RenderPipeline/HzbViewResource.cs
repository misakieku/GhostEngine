using Ghost.Core;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.CompilerServices;

namespace Ghost.Engine.RenderPipeline;

public sealed class HzbViewResource : IViewResource<HzbViewResource, HzbViewResource.Desc>
{
    public struct Desc
    {
        public uint2 renderSize;
    }

    private readonly IResourceAllocator _allocator;
    private readonly IResourceDatabase _database;

    public Handle<GPUTexture> Texture { get; private set; }
    public uint2 HzbSize { get; private set; }
    public uint HzbMipCount { get; private set; }

    public static HzbViewResource Create(IResourceAllocator allocator, IResourceDatabase database)
    {
        return new HzbViewResource(allocator, database);
    }

    private HzbViewResource(IResourceAllocator allocator, IResourceDatabase database)
    {
        _allocator = allocator;
        _database = database;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ComputeHZBDimensions(uint2 renderSize, out uint2 hzbSize, out uint hzbMipCount)
    {
        var potWidth = System.Numerics.BitOperations.RoundUpToPowerOf2(renderSize.x);
        var potHeight = System.Numerics.BitOperations.RoundUpToPowerOf2(renderSize.y);
        hzbSize = new uint2(Math.Max(1u, potWidth >> 1), Math.Max(1u, potHeight >> 1));

        var calculatedMipCount = (uint)Math.Floor(Math.Log2(Math.Max(hzbSize.x, hzbSize.y))) + 1;
        hzbMipCount = Math.Clamp(calculatedMipCount, 1u, 16u);
    }

    public void EnsureResource(in Desc desc)
    {
        ComputeHZBDimensions(desc.renderSize, out var hzbSize, out var hzbMipCount);
        if (Texture.IsValid && HzbSize.x == hzbSize.x && HzbSize.y == hzbSize.y && HzbMipCount == hzbMipCount)
        {
            return;
        }

        Reset();

        HzbSize = hzbSize;
        HzbMipCount = hzbMipCount;

        var hzbDesc = new TextureDesc
        {
            Dimension = TextureDimension.Texture2D,
            Width = HzbSize.x,
            Height = HzbSize.y,
            MipLevels = HzbMipCount,
            Format = TextureFormat.R32G32_Float,
            Usage = TextureUsage.ShaderResource | TextureUsage.UnorderedAccess,
            Slice = 1,
        };

        Texture = _allocator.CreateTexture(in hzbDesc, "HZB Texture");
    }

    public void OnResize(uint2 newSize)
    {
        Reset();
    }

    public void Reset()
    {
        if (Texture.IsValid)
        {
            _database.ReleaseResource(Texture.AsResource());
            Texture = Handle<GPUTexture>.Invalid;
        }

        HzbSize = default;
        HzbMipCount = 0;
    }

    public void Dispose()
    {
        Reset();
    }
}
