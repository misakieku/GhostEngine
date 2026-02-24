using Ghost.Core;

namespace Ghost.Graphics.RHI;

public readonly struct GPUResource;
public readonly struct Texture;
public readonly struct GraphicsBuffer;

public readonly struct Sampler;

public static class ResourceHandleExtensions
{
    public static Handle<GPUResource> AsResource(this Handle<Texture> texture)
    {
        return new Handle<GPUResource>(texture.ID, texture.Generation);
    }

    public static Handle<GPUResource> AsResource(this Handle<GraphicsBuffer> buffer)
    {
        return new Handle<GPUResource>(buffer.ID, buffer.Generation);
    }

    public static Handle<Texture> AsTexture(this Handle<GPUResource> resource)
    {
        return new Handle<Texture>(resource.ID, resource.Generation);
    }

    public static Handle<GraphicsBuffer> AsGraphicsBuffer(this Handle<GPUResource> resource)
    {
        return new Handle<GraphicsBuffer>(resource.ID, resource.Generation);
    }
}
