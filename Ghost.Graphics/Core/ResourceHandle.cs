using Ghost.Core;

namespace Ghost.Graphics.Core;

public readonly struct GPUResource : IHandleType;
public readonly struct Texture : IHandleType;
public readonly struct GraphicsBuffer : IHandleType;

public readonly struct Sampler : IIdentifierType;

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
