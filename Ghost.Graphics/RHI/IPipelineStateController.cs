using Ghost.Graphics.Data;

namespace Ghost.Graphics.RHI;

public interface IShaderPipeline
{
    /// <summary>
    /// Pipeline type
    /// </summary>
    PipelineType Type
    {
        get;
    }
}

public interface IPipelineStateController
{
    public void ColectionShader(ReadOnlySpan<Shader> shaders);

    public void CompileCollected();

    public void PreCookPipelineState();

    public IShaderPipeline GetShaderPipeline(Shader shader);
}