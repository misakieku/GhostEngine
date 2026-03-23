using Ghost.Core;

namespace Ghost.Graphics.RHI;

public interface IPipelineLibrary : IDisposable
{
    void SaveLibraryToDisk(string filePath);
    bool HasPipeline(Key128<GraphicsPipeline> key);
    Result<Key128<GraphicsPipeline>> CompilePSO(ref readonly GraphicsPSODescriptor descriptor, ref readonly GraphicsCompiledResult compiled);
}
