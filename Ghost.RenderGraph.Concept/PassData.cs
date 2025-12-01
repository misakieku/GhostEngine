namespace Ghost.RenderGraph.Concept;

// Pass data structure for GBuffer outputs
public class GBufferData
{
    public RenderGraphTextureHandle Albedo = null!;
    public RenderGraphTextureHandle Normal = null!;
    public RenderGraphTextureHandle Depth = null!;
}

public class LightingPassData
{
    public RenderGraphTextureHandle GBufferAlbedo = null!;
    public RenderGraphTextureHandle GBufferNormal = null!;
    public RenderGraphTextureHandle GBufferDepth = null!;
    public RenderGraphTextureHandle OutputLighting = null!;
}

public class SSAOPassData
{
    public RenderGraphTextureHandle GBufferDepth = null!;
    public RenderGraphTextureHandle GBufferNormal = null!;
    public RenderGraphTextureHandle OutputSSAO = null!;
}

public class TAAPassData
{
    public RenderGraphTextureHandle InputLighting = null!;
    public RenderGraphTextureHandle OutputTAA = null!;
}

public class PostProcessingPassData
{
    public RenderGraphTextureHandle InputTAA = null!;
    public RenderGraphTextureHandle InputSSAO = null!;
    public RenderGraphTextureHandle OutputBackbuffer = null!;
}

public class DebugPassData
{
    public RenderGraphTextureHandle DebugTexture = null!;
}

public class ProfilerMarkerData { }

public class BloomDownsampleData
{
    public RenderGraphTextureHandle Input = null!;
    public RenderGraphTextureHandle Output = null!;
}

public class PostProcessingPassDataV2
{
    public RenderGraphTextureHandle InputTAA = null!;
    public RenderGraphTextureHandle InputSSAO = null!;
    public RenderGraphTextureHandle InputBloom = null!;
    public RenderGraphTextureHandle OutputBackbuffer = null!;
}
