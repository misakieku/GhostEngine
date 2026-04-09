using Ghost.Core;

namespace Ghost.Graphics.RHI;

public interface IPipelineLibrary : IDisposable
{
    void SaveLibraryToDisk(string filePath);
    bool HasPipelineStateObject(UInt128 key);
    Result<Key128<GraphicsPipeline>> CreateGraphicsPipeline(ref readonly GraphicsPSODescriptor descriptor, ref readonly GraphicsCompiledResult compiled);
    Result<Key128<ComputePipeline>> CreateComputePipeline(ref readonly ComputePSODescriptor descriptor, ref readonly ShaderCompileResult compiled);
}
