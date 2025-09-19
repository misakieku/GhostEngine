using Ghost.Core;
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
    public void CompileShader(Identifier<Shader> id, string shaderPath);

    public void PreCookPipelineState();

    public IShaderPipeline GetShaderPipeline(Identifier<Shader> id);
}