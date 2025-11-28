using Ghost.Core;
using Ghost.Graphics.Contracts;

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

public interface IPipelineLibrary : IDisposable
{
    /// <summary>
    /// Load pipeline library from disk.
    /// </summary>
    /// <param name="filePath">File path. If null, load default library.</param>
    void InitializeLibrary(string? filePath);
    void SaveLibraryToDisk(string filePath);
    Result<GraphicsPipelineKey> CompilePSO(ref readonly GraphicsPSODescriptor descriptor, ref readonly GraphicsCompiledResult compiled);
}
