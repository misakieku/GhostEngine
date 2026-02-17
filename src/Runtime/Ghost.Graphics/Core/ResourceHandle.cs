using Ghost.Core;

namespace Ghost.Graphics.Core;

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

    internal static Handle<Texture> AsTexture(this Handle<GPUResource> resource)
    {
        return new Handle<Texture>(resource.ID, resource.Generation);
    }

    internal static Handle<GraphicsBuffer> AsGraphicsBuffer(this Handle<GPUResource> resource)
    {
        return new Handle<GraphicsBuffer>(resource.ID, resource.Generation);
    }
}
