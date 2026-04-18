using Ghost.Core;
using System.Runtime.CompilerServices;

namespace Ghost.Graphics.RHI;

public readonly struct GPUResource;
public readonly struct GPUTexture;
public readonly struct GPUBuffer;

public readonly struct Sampler;

public static class ResourceHandleExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Handle<GPUResource> AsResource(this Handle<GPUTexture> texture)
    {
        return new Handle<GPUResource>(texture.ID, texture.Generation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Handle<GPUResource> AsResource(this Handle<GPUBuffer> buffer)
    {
        return new Handle<GPUResource>(buffer.ID, buffer.Generation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Handle<GPUTexture> AsTexture(this Handle<GPUResource> resource)
    {
        return new Handle<GPUTexture>(resource.ID, resource.Generation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Handle<GPUBuffer> AsBuffer(this Handle<GPUResource> resource)
    {
        return new Handle<GPUBuffer>(resource.ID, resource.Generation);
    }
}
