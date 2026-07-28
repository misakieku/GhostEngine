using Ghost.Core;
using Ghost.Core.Graphics;

namespace Ghost.Graphics.RHI;

public interface IPipelineLibrary : IDisposable
{
    void SaveLibraryToDisk(string filePath);
    bool HasPipelineStateObject(UInt128 key);
    Result<Key128<PipelineState>> CreateGraphicsPipeline(scoped in GraphicsPSODesc desc);
    Result<Key128<PipelineState>> CreateComputePipeline(scoped in ComputePSODesc desc);

    void BeginFrame(ulong cpuFrame);
    void EndFrame(ulong gpuFrame);
    void EvictStalePipelines(ulong oldContentHash);
}
