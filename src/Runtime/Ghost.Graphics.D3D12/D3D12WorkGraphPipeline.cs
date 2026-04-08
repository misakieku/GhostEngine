using Ghost.Core;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel;
using TerraFX.Interop.DirectX;

namespace Ghost.Graphics.D3D12;

internal class D3D12WorkGraphPipeline : IWorkGraphPipeline
{
    private UniquePtr<ID3D12StateObject> _stateObject;
    private D3D12_PROGRAM_IDENTIFIER _programIdentifier;
    private D3D12_WORK_GRAPH_MEMORY_REQUIREMENTS _memoryRequirements;

    private Handle<GPUResource> _backingBuffer;
}
