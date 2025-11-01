using Ghost.Core;

namespace Ghost.Graphics.Core;

public readonly struct GPUResource : IHandleType;
public readonly struct Texture : IHandleType;
public readonly struct GraphicsBuffer : IHandleType;

public static class ResourceHandleExtensions
{
    public static Handle<GPUResource> AsResource(this Handle<Texture> texture)
    {
        return new Handle<GPUResource>(texture.id, texture.generation);
    }

    public static Handle<GPUResource> AsResource(this Handle<GraphicsBuffer> buffer)
    {
        return new Handle<GPUResource>(buffer.id, buffer.generation);
    }

    internal static Handle<Texture> AsTexture(this Handle<GPUResource> resource)
    {
        return new Handle<Texture>(resource.id, resource.generation);
    }

    internal static Handle<GraphicsBuffer> AsGraphicsBuffer(this Handle<GPUResource> resource)
    {
        return new Handle<GraphicsBuffer>(resource.id, resource.generation);
    }
}
