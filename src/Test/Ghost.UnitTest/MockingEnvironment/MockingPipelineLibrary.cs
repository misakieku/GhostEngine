using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Graphics.RHI;

namespace Ghost.UnitTest.MockingEnvironment;

internal class MockingPipelineLibrary : IPipelineLibrary
{
    public Result<Key128<PipelineState>> CreateComputePipeline(scoped in ComputePSODesc desc)
    {
        return default;
    }

    public Result<Key128<PipelineState>> CreateGraphicsPipeline(scoped in GraphicsPSODesc desc)
    {
        return default;
    }

    public void BeginFrame(ulong cpuFrame)
    {
    }

    public void EndFrame(ulong gpuFrame)
    {
    }

    public void EvictStalePipelines(ulong oldContentHash)
    {
    }

    public bool HasPipelineStateObject(UInt128 key)
    {
        return true;
    }

    public void SaveLibraryToDisk(string filePath)
    {
    }

    public void Dispose()
    {
    }
}
