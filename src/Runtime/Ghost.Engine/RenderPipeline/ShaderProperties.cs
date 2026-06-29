using Ghost.Core.Graphics;

namespace Ghost.Engine.RenderPipeline;

[GenerateShaderProperty("Hidden/Blit")]
public partial struct BlitShaderProperties
{
    public uint mainTex;
    public uint sampler_mainTex;
}
