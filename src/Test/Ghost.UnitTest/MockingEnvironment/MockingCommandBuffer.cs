using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Graphics.RHI;

namespace Ghost.UnitTest.MockingEnvironment;

internal class MockingCommandBuffer : ICommandBuffer
{
    private readonly IResourceDatabase _resourceDatabase;

    private bool _isEmpty = true;

    // Tracking properties for test assertions
    public int DrawCallCount { get; private set; }
    public int CopyCallCount { get; private set; }
    public int UpdateSubResourcesCount { get; private set; }

    public bool IsEmpty => _isEmpty;

    public CommandBufferType Type
    {
        get;
    }

    public string Name
    {
        get; set;
    } = "MockingCommandBuffer";

    public MockingCommandBuffer(IResourceDatabase resourceDatabase, CommandBufferType type)
    {
        _resourceDatabase = resourceDatabase;
        Type = type;
    }

    public void Barrier(params scoped ReadOnlySpan<BarrierDesc> barrierDescs)
    {
        _isEmpty = false;
        lock (this)
        {
            foreach (var desc in barrierDescs)
            {
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
        _isEmpty = true;
        DrawCallCount = 0;
        CopyCallCount = 0;
        UpdateSubResourcesCount = 0;
    }

    public void BeginRenderPass(ReadOnlySpan<PassRenderTargetDesc> rtDescs, ref readonly PassDepthStencilDesc depthDesc, bool allowUAVWrites = false)
    {
        _isEmpty = false;
    }

    public void ClearDepthStencilView(Handle<GPUTexture> depthStencil, bool inlcludeDepth, bool includeStencil, float clearDepth = 1, byte clearStencil = 0)
    {
        _isEmpty = false;
    }

    public void ClearRenderTargetView(Handle<GPUTexture> renderTarget, Color128 clearColor)
    {
        _isEmpty = false;
    }

    public void CopyBuffer(Handle<GPUBuffer> dest, Handle<GPUBuffer> src, ulong destOffset = 0, ulong srcOffset = 0, ulong numBytes = 0)
    {
        _isEmpty = false;
        CopyCallCount++;
    }

    public void CopyTexture(Handle<GPUTexture> dst, TextureRegion? dstRegion, Handle<GPUTexture> src, TextureRegion? srcRegion)
    {
        _isEmpty = false;
        CopyCallCount++;
    }

    public void DispatchCompute(uint threadGroupCountX, uint threadGroupCountY, uint threadGroupCountZ)
    {
        _isEmpty = false;
    }

    public void DispatchGraph(ref readonly DispatchGraphDesc desc)
    {
        _isEmpty = false;
    }

    public void DispatchMesh(uint threadGroupCountX, uint threadGroupCountY, uint threadGroupCountZ)
    {
        _isEmpty = false;
    }

    public void DispatchRay()
    {
        _isEmpty = false;
    }

    public void Dispose()
    {
    }

    public void Draw(uint vertexCount, uint instanceCount = 1, uint startVertex = 0, uint startInstance = 0)
    {
        _isEmpty = false;
        DrawCallCount++;
    }

    public void DrawIndexed(uint indexCount, uint instanceCount = 1, uint startIndex = 0, int baseVertex = 0, uint startInstance = 0)
    {
        _isEmpty = false;
        DrawCallCount++;
    }

    public Result End()
    {
        return Result.Success();
    }

    public void EndRenderPass()
    {
    }

    public void ExecuteIndirect(ICommandSignature commandSignature, Handle<GPUBuffer> argumentBuffer, ulong argumentOffset, Handle<GPUBuffer> countBuffer, ulong countBufferOffset)
    {
        _isEmpty = false;
    }

    public void SetConstantBufferView(uint slot, Handle<GPUBuffer> buffer)
    {
        _isEmpty = false;
    }

    public void SetGraphicsRoot32Constants(uint rootIndex, ReadOnlySpan<uint> constantBuffer, uint offsetIn32Bits = 0)
    {
        _isEmpty = false;
    }

    public void SetIndexBuffer(Handle<GPUBuffer> buffer, IndexType type, ulong offset = 0)
    {
        _isEmpty = false;
    }

    public void SetPipelineState(Key128<PipelineState> pipelineKey)
    {
        _isEmpty = false;
    }

    public void SetPrimitiveTopology(PrimitiveTopology topology)
    {
        _isEmpty = false;
    }

    public void SetProgram(ref readonly SetProgramDesc desc)
    {
        _isEmpty = false;
    }

    public void SetRenderTargets(ReadOnlySpan<Handle<GPUTexture>> renderTargets, Handle<GPUTexture> depthTarget)
    {
        _isEmpty = false;
    }

    public void SetScissorRect(ScissorRectDesc rect)
    {
        _isEmpty = false;
    }

    public void SetVertexBuffer(uint slot, Handle<GPUBuffer> buffer, ulong offset = 0)
    {
        _isEmpty = false;
    }

    public void SetViewport(ViewportDesc viewport)
    {
        _isEmpty = false;
    }

    public void UpdateSubResources(Handle<GPUResource> resource, Handle<GPUResource> intermediate, params scoped ReadOnlySpan<SubResourceData> subResources)
    {
        _isEmpty = false;
        UpdateSubResourcesCount++;
    }
}
