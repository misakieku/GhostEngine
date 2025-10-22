using Ghost.Core.Graphics;

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

public interface IPipelineLibrary
{
    void CompilePass(IPassDescriptor descriptor);
    void CompileShader(ShaderDescriptor descriptor);
    void PreCookPipelineState();
    void SaveLibraryToDisk(string filePath);
}