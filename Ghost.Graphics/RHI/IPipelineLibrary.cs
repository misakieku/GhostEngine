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
    /// <summary>
    /// Load pipeline library from disk.
    /// </summary>
    /// <param name="filePath">File path. If null, load default library.</param>
    void LoadLibraryFromDisk(string? filePath);
    void SaveLibraryToDisk(string filePath);
    GraphicsPipelineKey CompilePassPSO(IPassDescriptor descriptor, ReadOnlySpan<TextureFormat> rtvs, TextureFormat dsv);
}
