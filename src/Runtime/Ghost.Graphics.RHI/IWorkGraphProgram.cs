using Ghost.Core;

namespace Ghost.Graphics.RHI;

public interface IWorkGraphProgram : IRHIObject
{
    ProgramIdentifier ProgramIdentifier { get; }
    Handle<GPUBuffer> BackingMemoryBuffer { get; }
    ulong BackingMemoryAddress { get; }
    ulong BackingMemorySize { get; }
    bool IsInitialized { get; }

    uint GetEntrypointIndex(string nodeName);
    void Bind(ICommandBuffer cmdBuffer);
    void Dispatch(ICommandBuffer cmdBuffer, scoped in DispatchGraphDesc desc);
}
