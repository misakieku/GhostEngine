namespace Ghost.RenderGraph.Concept;

[Flags]
public enum ResourceState
{
    Undefined = 0,
    RenderTarget = 1 << 0,
    DepthWrite = 1 << 1,
    DepthRead = 1 << 2,
    ShaderResource = 1 << 3,
    UnorderedAccess = 1 << 4,
    CopySource = 1 << 5,
    CopyDest = 1 << 6,
    Present = 1 << 7
}

public enum BarrierType
{
    Transition,  // Regular state transition
    Aliasing     // Aliasing barrier (resource is being reused)
}
