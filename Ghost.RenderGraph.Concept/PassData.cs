namespace Ghost.RenderGraph.Concept;

// Pass data structure for GBuffer outputs
public class GBufferData
{
    public RenderGraphTextureHandle Albedo;
    public RenderGraphTextureHandle Normal;
    public RenderGraphTextureHandle Depth;
}

public class LightingPassData
{
    public RenderGraphTextureHandle GBufferAlbedo;
    public RenderGraphTextureHandle GBufferNormal;
    public RenderGraphTextureHandle GBufferDepth;
    public RenderGraphTextureHandle OutputLighting;
}

public class SSAOPassData
{
    public RenderGraphTextureHandle GBufferDepth;
    public RenderGraphTextureHandle GBufferNormal;
    public RenderGraphTextureHandle OutputSSAO;
}

public class TAAPassData
{
    public RenderGraphTextureHandle InputLighting;
    public RenderGraphTextureHandle OutputTAA;
}

public class PostProcessingPassData
{
    public RenderGraphTextureHandle InputTAA;
    public RenderGraphTextureHandle InputSSAO;
    public RenderGraphTextureHandle OutputBackbuffer;
}

public class DebugPassData
{
    public RenderGraphTextureHandle DebugTexture;
}

public class ProfilerMarkerData { }

public class BloomDownsampleData
{
    public RenderGraphTextureHandle Input;
    public RenderGraphTextureHandle Output;
}

public class PostProcessingPassDataV2
{
    public RenderGraphTextureHandle InputTAA;
    public RenderGraphTextureHandle InputSSAO;
    public RenderGraphTextureHandle InputBloom;
    public RenderGraphTextureHandle OutputBackbuffer;
}
