namespace Ghost.Graphics.RenderGraphModule;

internal enum PassResourceUsageClass : byte
{
    None,
    ShaderRead,
    IndirectArgument,
    UnorderedAccess,
    ColorAttachment,
    DepthRead,
    DepthWrite
}
