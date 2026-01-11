namespace Ghost.RenderGraph.Concept;

/// <summary>
/// Opaque handle to a render graph texture resource.
/// </summary>
public readonly struct RenderGraphTextureHandle : IEquatable<RenderGraphTextureHandle>
{
    public readonly int Index;
    public readonly int Version;
    internal readonly string InternalName;

    public RenderGraphTextureHandle(int index, int version, string name = "")
    {
        Index = index;
        Version = version;
        InternalName = name;
    }

    public string Name => InternalName;

    public bool IsValid() => Index >= 0;

    public readonly bool Equals(RenderGraphTextureHandle other) => Index == other.Index && Version == other.Version;
    public override readonly bool Equals(object? obj) => obj is RenderGraphTextureHandle other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(Index, Version);
    public static bool operator ==(RenderGraphTextureHandle left, RenderGraphTextureHandle right) => left.Equals(right);
    public static bool operator !=(RenderGraphTextureHandle left, RenderGraphTextureHandle right) => !left.Equals(right);
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
