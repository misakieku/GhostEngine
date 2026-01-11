namespace Ghost.RenderGraph.Concept;

// ===== Pass Data Structures =====
// These are user-defined data structures that get passed to render functions

public sealed class GBufferData : IPassData
{
    public RenderGraphTextureHandle Albedo;
    public RenderGraphTextureHandle Normal;
    public RenderGraphTextureHandle Depth;
}

public sealed class LightingPassData : IPassData
{
    public RenderGraphTextureHandle GBufferAlbedo;
    public RenderGraphTextureHandle GBufferNormal;
    public RenderGraphTextureHandle GBufferDepth;
    public RenderGraphTextureHandle OutputLighting;
}

public sealed class SSAOPassData : IPassData
{
    public RenderGraphTextureHandle GBufferDepth;
    public RenderGraphTextureHandle GBufferNormal;
    public RenderGraphTextureHandle OutputSSAO;
}

public sealed class BloomDownsampleData : IPassData
{
    public RenderGraphTextureHandle Input;
    public RenderGraphTextureHandle Output;
}

public sealed class TAAPassData : IPassData
{
    public RenderGraphTextureHandle InputLighting;
    public RenderGraphTextureHandle OutputTAA;
}

public sealed class PostProcessingPassDataV2 : IPassData
{
    public RenderGraphTextureHandle InputTAA;
    public RenderGraphTextureHandle InputSSAO;
    public RenderGraphTextureHandle InputBloom;
    public RenderGraphTextureHandle OutputBackbuffer;
}

public sealed class ProfilerMarkerData : IPassData
{
}

public sealed class DebugPassData : IPassData
{
    public RenderGraphTextureHandle DebugTexture;
}
