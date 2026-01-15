using Ghost.Core;

namespace Ghost.RenderGraph.Concept;

// ===== Pass Data Structures =====
// These are user-defined data structures that get passed to render functions

public sealed class GBufferData : IPassData
{
    public Identifier<RGTexture> Albedo;
    public Identifier<RGTexture> Normal;
    public Identifier<RGTexture> Depth;
}

public sealed class LightingPassData : IPassData
{
    public Identifier<RGTexture> GBufferAlbedo;
    public Identifier<RGTexture> GBufferNormal;
    public Identifier<RGTexture> GBufferDepth;
    public Identifier<RGTexture> OutputLighting;
}

public sealed class SSAOPassData : IPassData
{
    public Identifier<RGTexture> GBufferDepth;
    public Identifier<RGTexture> GBufferNormal;
    public Identifier<RGTexture> OutputSSAO;
    public Identifier<RGBuffer> OutputSSAOBuffer;
}

public sealed class BloomDownsampleData : IPassData
{
    public Identifier<RGTexture> Input;
    public Identifier<RGTexture> Output;
}

public sealed class TAAPassData : IPassData
{
    public Identifier<RGTexture> InputLighting;
    public Identifier<RGTexture> OutputTAA;
}

public sealed class PostProcessingPassDataV1 : IPassData
{
    public Identifier<RGTexture> InputLighting;
    public Identifier<RGTexture> OutputBackbuffer;
}

public sealed class PostProcessingPassDataV2 : IPassData
{
    public Identifier<RGTexture> InputTAA;
    public Identifier<RGTexture> InputSSAO;
    public Identifier<RGTexture> InputBloom;
    public Identifier<RGTexture> OutputBackbuffer;
}

public sealed class ProfilerMarkerData : IPassData
{
}

public sealed class DebugPassData : IPassData
{
    public Identifier<RGTexture> DebugTexture;
}
