using Ghost.Core;

namespace Ghost.Graphics.RHI;

public readonly struct GPUResource;
public readonly struct GPUTexture;
public readonly struct GPUBuffer;

public readonly struct Sampler;

public static class ResourceHandleExtensions
{
    public static Handle<GPUResource> AsResource(this Handle<GPUTexture> texture)
    {
        return new Handle<GPUResource>(texture.ID, texture.Generation);
    }

    public static Handle<GPUResource> AsResource(this Handle<GPUBuffer> buffer)
    {
        return new Handle<GPUResource>(buffer.ID, buffer.Generation);
    }

    public static Handle<GPUTexture> AsTexture(this Handle<GPUResource> resource)
    {
        return new Handle<GPUTexture>(resource.ID, resource.Generation);
    }

    public static Handle<GPUBuffer> AsGraphicsBuffer(this Handle<GPUResource> resource)
    {
        return new Handle<GPUBuffer>(resource.ID, resource.Generation);
    }
}
