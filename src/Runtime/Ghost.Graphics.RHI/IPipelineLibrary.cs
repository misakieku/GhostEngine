using Ghost.Core;

namespace Ghost.Graphics.RHI;

public interface IPipelineLibrary : IDisposable
{
    void SaveLibraryToDisk(string filePath);
    bool HasPipelineStateObject(UInt128 key);
    Result<Key128<GraphicsPipeline>> CreateGraphicsPipeline(ref readonly GraphicsPSODescriptor descriptor, ReadOnlySpan<byte> asByteCode, ReadOnlySpan<byte> msByteCode, ReadOnlySpan<byte> psByteCode);
    Result<Key128<ComputePipeline>> CreateComputePipeline(ref readonly ComputePSODescriptor descriptor, ReadOnlySpan<byte> csByteCode);
}
