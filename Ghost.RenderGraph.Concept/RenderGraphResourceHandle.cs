namespace Ghost.RenderGraph.Concept;

public class RenderGraphResourceHandle
{
    internal int Id { get; }
    internal ResourceType Type { get; }
    internal string Name { get; }
    internal bool IsImported { get; }

    internal RenderGraphResourceHandle(int id, ResourceType type, string name, bool isImported)
    {
        Id = id;
        Type = type;
        Name = name;
        IsImported = isImported;
    }

    public override string ToString() => Name;
}

public sealed class RenderGraphTextureHandle : RenderGraphResourceHandle
{
    internal TextureDescriptor Descriptor { get; }

    internal RenderGraphTextureHandle(int id, string name, TextureDescriptor descriptor, bool isImported)
        : base(id, ResourceType.Texture, name, isImported)
    {
        Descriptor = descriptor;
    }
}

public sealed class RenderGraphBufferHandle : RenderGraphResourceHandle
{
    internal BufferDescriptor Descriptor { get; }

    internal RenderGraphBufferHandle(int id, string name, BufferDescriptor descriptor, bool isImported)
        : base(id, ResourceType.Buffer, name, isImported)
    {
        Descriptor = descriptor;
    }
}
