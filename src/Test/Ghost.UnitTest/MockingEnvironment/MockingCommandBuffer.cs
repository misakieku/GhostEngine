using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Graphics.RHI;

namespace Ghost.UnitTest.MockingEnvironment;

internal class MockingCommandBuffer : ICommandBuffer
{
    private static int s_nextInstanceId;

    private readonly IResourceDatabase _resourceDatabase;

    private CommandBufferState _state;

    // Tracking properties for test assertions
    public int BeginCount { get; private set; }
    public int EndCount { get; private set; }
    public int DrawCallCount { get; private set; }
    public int CopyCallCount { get; private set; }
    public int DispatchCallCount { get; private set; }
    public int UpdateSubResourcesCount { get; private set; }
    public bool FailOnBegin { get; set; }
    public bool FailOnEnd { get; set; }
    public int InstanceId { get; }
    public ICommandAllocator? LastBeginAllocator { get; private set; }
    public List<BarrierDesc> RecordedBarriers { get; } = new();

    public CommandBufferType Type
    {
        get;
    }

    public string Name
    {
        get; set;
    } = "MockingCommandBuffer";

    public CommandBufferState State => _state;

    public MockingCommandBuffer(IResourceDatabase resourceDatabase, CommandBufferType type)
    {
        _resourceDatabase = resourceDatabase;
        Type = type;
        InstanceId = Interlocked.Increment(ref s_nextInstanceId);
    }

    public void Barrier(params scoped ReadOnlySpan<BarrierDesc> barrierDescs)
    {
        _state.CommandCount++;
        lock (this)
        {
            foreach (var desc in barrierDescs)
            {
                RecordedBarriers.Add(desc);
                var data = new ResourceBarrierData
                {
                    access = desc.AccessAfter,
                    layout = desc.LayoutAfter,
                    sync = desc.SyncAfter
                };

                _resourceDatabase.SetResourceBarrierData(desc.Resource, data);
            }
        }
    }

    public void Begin(ICommandAllocator allocator)
    {
        BeginCount++;
        LastBeginAllocator = allocator;
        if (FailOnBegin)
        {
            throw new InvalidOperationException("Injected command-buffer begin failure.");
        }

        _state.CommandCount = 0;
        _state.IsRecording = true;
        _state.Error = Error.None;
        _state.ErrorCommandName = string.Empty;
        DrawCallCount = 0;
        CopyCallCount = 0;
        DispatchCallCount = 0;
        UpdateSubResourcesCount = 0;
        RecordedBarriers.Clear();
    }

    public void BeginRenderPass(ReadOnlySpan<PassRenderTargetDesc> rtDescs, ref readonly PassDepthStencilDesc depthDesc, bool allowUAVWrites = false)
    {
        _state.CommandCount++;
    }

    public void ClearDepthStencilView(Handle<GPUTexture> depthStencil, bool inlcludeDepth, bool includeStencil, float clearDepth = 1, byte clearStencil = 0)
    {
        _state.CommandCount++;
    }

    public void ClearRenderTargetView(Handle<GPUTexture> renderTarget, Color128 clearColor)
    {
        _state.CommandCount++;
    }

    public void CopyBuffer(Handle<GPUBuffer> dest, Handle<GPUBuffer> src, ulong destOffset = 0, ulong srcOffset = 0, ulong numBytes = 0)
    {
        _state.CommandCount++;
        CopyCallCount++;
    }

    public void CopyTexture(Handle<GPUTexture> dst, TextureRegion? dstRegion, Handle<GPUTexture> src, TextureRegion? srcRegion)
    {
        _state.CommandCount++;
        CopyCallCount++;
    }

    public void DispatchCompute(uint threadGroupCountX, uint threadGroupCountY, uint threadGroupCountZ)
    {
        _state.CommandCount++;
        DispatchCallCount++;
    }

    public void DispatchGraph(scoped in DispatchGraphDesc desc)
    {
        _state.CommandCount++;
    }

    public void DispatchMesh(uint threadGroupCountX, uint threadGroupCountY, uint threadGroupCountZ)
    {
        _state.CommandCount++;
    }

    public void DispatchRay()
    {
        _state.CommandCount++;
    }

    public void Dispose()
    {
    }

    public void Draw(uint vertexCount, uint instanceCount = 1, uint startVertex = 0, uint startInstance = 0)
    {
        _state.CommandCount++;
        DrawCallCount++;
    }

    public void DrawIndexed(uint indexCount, uint instanceCount = 1, uint startIndex = 0, int baseVertex = 0, uint startInstance = 0)
    {
        _state.CommandCount++;
        DrawCallCount++;
    }

    public Result End()
    {
        EndCount++;
        if (FailOnEnd)
        {
            return Result.Failure("Injected command-buffer end failure.");
        }

        _state.IsRecording = false;
        return Result.Success();
    }

    public void EndRenderPass()
    {
    }

    public void ExecuteIndirect(ICommandSignature commandSignature, Handle<GPUBuffer> argumentBuffer, ulong argumentOffset, Handle<GPUBuffer> countBuffer, ulong countBufferOffset)
    {
        _state.CommandCount++;
    }

    public void SetConstantBufferView(uint slot, Handle<GPUBuffer> buffer)
    {
        _state.CommandCount++;
    }

    public void SetGraphicsRoot32Constants(uint rootIndex, ReadOnlySpan<uint> constantBuffer, uint offsetIn32Bits = 0)
    {
        _state.CommandCount++;
    }

    public void SetComputeRoot32Constants(uint rootIndex, ReadOnlySpan<uint> constantBuffer, uint offsetIn32Bits = 0)
    {
        _state.CommandCount++;
    }

    public void SetIndexBuffer(Handle<GPUBuffer> buffer, IndexType type, ulong offset = 0)
    {
        _state.CommandCount++;
    }

    public void SetPipelineState(Key128<PipelineState> pipelineKey)
    {
        _state.CommandCount++;
    }

    public void SetPrimitiveTopology(PrimitiveTopology topology)
    {
        _state.CommandCount++;
    }

    public void SetProgram(scoped in SetProgramDesc desc)
    {
        _state.CommandCount++;
    }

    public void SetRenderTargets(ReadOnlySpan<Handle<GPUTexture>> renderTargets, Handle<GPUTexture> depthTarget)
    {
        _state.CommandCount++;
    }

    public void SetScissorRect(ScissorRectDesc rect)
    {
        _state.CommandCount++;
    }

    public void SetVertexBuffer(uint slot, Handle<GPUBuffer> buffer, ulong offset = 0)
    {
        _state.CommandCount++;
    }

    public void SetViewport(ViewportDesc viewport)
    {
        _state.CommandCount++;
    }

    public void UpdateSubResources(Handle<GPUResource> resource, Handle<GPUResource> intermediate, params scoped ReadOnlySpan<SubResourceData> subResources)
    {
        _state.CommandCount++;
        UpdateSubResourcesCount++;
    }
}
