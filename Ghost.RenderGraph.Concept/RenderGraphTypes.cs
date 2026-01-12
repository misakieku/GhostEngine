using Ghost.Core;
using System.Runtime.CompilerServices;

namespace Ghost.RenderGraph.Concept;

internal enum RenderGraphResourceType
{
    Texture,
    Buffer,
    AccelerationStructure,
    Count
}

public struct RGResource;
public struct RGTexture;
public struct RGBuffer;

public static class RGResourceExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Identifier<RGResource> AsResource(this Identifier<RGTexture> texture)
    {
        return new Identifier<RGResource>(texture.Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Identifier<RGResource> AsResource(this Identifier<RGBuffer> buffer)
    {
        return new Identifier<RGResource>(buffer.Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Identifier<RGTexture> AsTexture(this Identifier<RGResource> resource)
    {
        return new Identifier<RGTexture>(resource.Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Identifier<RGBuffer> AsBuffer(this Identifier<RGResource> resource)
    {
        return new Identifier<RGBuffer>(resource.Value);
    }
}

internal readonly struct TextureAccess
{
    public readonly Identifier<RGTexture> id;
    public readonly AccessFlags accessFlags;

    public TextureAccess(Identifier<RGTexture> id, AccessFlags accessFlags)
    {
        this.id = id;
        this.accessFlags = accessFlags;
    }
}

/// <summary>
/// Texture formats supported by the render graph.
/// </summary>
public enum TextureFormat : int
{
    RGBA8,
    RGBA16F,
    RGBA32F,
    Depth32F,
    Depth24Stencil8
}

/// <summary>
/// Descriptor for creating a texture resource.
/// </summary>
public readonly struct TextureDescriptor : IEquatable<TextureDescriptor>
{
    public readonly int Width;
    public readonly int Height;
    public readonly TextureFormat Format;
    public readonly string Name;

    public TextureDescriptor(int width, int height, TextureFormat format, string name)
    {
        Width = width;
        Height = height;
        Format = format;
        Name = name;
    }

    public readonly bool Equals(TextureDescriptor other) =>
        Width == other.Width &&
        Height == other.Height &&
        Format == other.Format &&
        Name == other.Name;

    public override readonly bool Equals(object? obj) => obj is TextureDescriptor other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(Width, Height, Format, Name);
}

/// <summary>
/// Base interface for pass data that can be stored in the blackboard.
/// </summary>
public interface IPassData
{
}
