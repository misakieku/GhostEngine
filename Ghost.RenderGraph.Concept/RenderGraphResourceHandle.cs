using System.Runtime.InteropServices;

namespace Ghost.RenderGraph.Concept;

public struct RenderGraphResourceHandle
{
    [StructLayout(LayoutKind.Explicit)]
    internal struct descriptor_union
    {
        [FieldOffset(0)]
        public TextureDescriptor texture;
        [FieldOffset(0)]
        public BufferDescriptor buffer;
    }

    internal int Id { get; }
    internal ResourceType Type { get; }
    internal string Name { get; }
    internal bool IsImported { get; }
    internal descriptor_union Descriptor { get; }

    internal RenderGraphResourceHandle(int id, ResourceType type, string name, bool isImported, descriptor_union descriptor)
    {
        Id = id;
        Type = type;
        Name = name;
        IsImported = isImported;
        Descriptor = descriptor;
    }

    public override string ToString() => Name;
}

public struct RenderGraphTextureHandle
{
    internal readonly RenderGraphResourceHandle _handle;

    internal int Id => _handle.Id;
    internal ResourceType Type => _handle.Type;
    internal string Name => _handle.Name;
    internal bool IsImported => _handle.IsImported;

    internal RenderGraphTextureHandle(int id, string name, TextureDescriptor descriptor, bool isImported)
    {
        _handle = new RenderGraphResourceHandle(id, ResourceType.Texture, name, isImported, new RenderGraphResourceHandle.descriptor_union() { texture = descriptor });
    }
}

public struct RenderGraphBufferHandle
{
    internal readonly RenderGraphResourceHandle _handle;

    internal BufferDescriptor Descriptor { get; }
    internal int Id => _handle.Id;
    internal ResourceType Type => _handle.Type;
    internal string Name => _handle.Name;
    internal bool IsImported => _handle.IsImported;

    internal RenderGraphBufferHandle(int id, string name, BufferDescriptor descriptor, bool isImported)
    {
        _handle = new RenderGraphResourceHandle(id, ResourceType.Buffer, name, isImported, new RenderGraphResourceHandle.descriptor_union() { buffer = descriptor });
    }
}
