namespace Ghost.RenderGraph.Concept;

public enum ResourceType
{
    Texture,
    Buffer
}

public enum TextureFormat
{
    RGBA8,
    RGBA16F,
    RGBA32F,
    Depth32F,
    R32Uint
}

public record TextureDescriptor(
    int Width,
    int Height,
    TextureFormat Format,
    string DebugName = "Unnamed Texture"
);

public record BufferDescriptor(
    int SizeInBytes,
    string DebugName = "Unnamed Buffer"
);
