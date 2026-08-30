using Ghost.Core;
using Ghost.Engine.Components;
using Ghost.Graphics;
using Ghost.Graphics.Core;
using Misaki.HighPerformance.LowLevel;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Engine.RenderPipeline;

public sealed unsafe class GhostRenderPayload : IRenderPayload
{
    public struct UpdateInstanceRequest
    {
        public MeshInstance meshInstance;
        public float4x4 localToWorld;
        public uint instanceId;
    }

    public struct RemoveInstanceRequest
    {
        public uint instanceId;
        public uint swapWithInstanceId;
    }

    private readonly GhostRenderPipeline _renderPipeline;

    private UnsafeList<RenderRequest> _renderRequests;

    private DisposablePtr<UnsafeParallelQueue<UpdateInstanceRequest>> _updateRequest;
    private DisposablePtr<UnsafeParallelQueue<RemoveInstanceRequest>> _removeRequest;

    private readonly UnsafeParallelQueue<UpdateInstanceRequest>.ParallelProducer _updateRequestProducer;
    private readonly UnsafeParallelQueue<RemoveInstanceRequest>.ParallelProducer _removeRequestProducer;

    private uint _instanceCountBefore;
    private uint _instanceCount;

    public ReadOnlySpan<RenderRequest> RenderRequests => _renderRequests;

    public UnsafeParallelQueue<UpdateInstanceRequest>.ParallelConsumer UpdateRequest => _updateRequest.Get()->AsParallelConsumer();
    public UnsafeParallelQueue<RemoveInstanceRequest>.ParallelConsumer RemoveRequest => _removeRequest.Get()->AsParallelConsumer();
    public uint InstanceCountBefore => _instanceCountBefore;
    public uint InstanceCount => _instanceCount;

    internal GhostRenderPayload(GhostRenderPipeline renderPipeline)
    {
        _renderPipeline = renderPipeline;

        _renderRequests = new UnsafeList<RenderRequest>(4, AllocationHandle.Persistent);
        _updateRequest = UnsafeParallelQueue<UpdateInstanceRequest>.Allocate(16, AllocationHandle.Persistent);
        _removeRequest = UnsafeParallelQueue<RemoveInstanceRequest>.Allocate(16, AllocationHandle.Persistent);

        _updateRequestProducer = _updateRequest.Get()->AsParallelProducer();
        _removeRequestProducer = _removeRequest.Get()->AsParallelProducer();
    }

    // NOTE: This is not thread safe.
    public void AddRenderRequest(scoped in RenderRequest renderRequest)
    {
        _renderRequests.Add(renderRequest);
    }

    public uint AddInstance(float4x4 ltw, scoped in MeshInstance meshInstance)
    {
        var index = _renderPipeline.GPUScene.AddInstance();

        _updateRequestProducer.Enqueue(new UpdateInstanceRequest { instanceId = index, localToWorld = ltw, meshInstance = meshInstance });
        return index;
    }

    public void UpdateInstance(uint instanceId, float4x4 ltw, scoped in MeshInstance meshInstance)
    {
        _updateRequestProducer.Enqueue(new UpdateInstanceRequest { instanceId = instanceId, localToWorld = ltw, meshInstance = meshInstance });
    }

    public void RemoveInstance(uint instanceId)
    {
        var swapWithInstanceId = _renderPipeline.GPUScene.RemoveInstance(instanceId);
        if (swapWithInstanceId != uint.MaxValue)
        {
            _removeRequestProducer.Enqueue(new RemoveInstanceRequest { instanceId = instanceId, swapWithInstanceId = swapWithInstanceId });
        }
    }

    public void BeginRecord()
    {
        _instanceCountBefore = _renderPipeline.GPUScene.InstanceCount;
    }

    public void EndRecord()
    {
        // We capture the count here to prevent that main thread continues to add more requests for next frame while the render thread is still processing current frame's requests.
        _instanceCount = _renderPipeline.GPUScene.InstanceCount;
        Logger.DebugAssert(_instanceCount == _instanceCountBefore + (uint)_updateRequest.Get()->Count - (uint)_removeRequest.Get()->Count);
    }

    public void Reset()
    {
        _renderRequests.Clear();
        _updateRequest.Get()->Clear();
        _removeRequest.Get()->Clear();
    }

    public void Dispose()
    {
        _renderRequests.Dispose();
        _updateRequest.Dispose();
        _removeRequest.Dispose();
    }
}

public class GhostRenderPipelineSettings : IRenderPipelineSettings
{
    public IRenderPipeline CreatePipeline(RenderEngine renderSystem)
    {
        return new GhostRenderPipeline(renderSystem);
    }

    public IRenderPayload CreatePayload(RenderEngine renderSystem, IRenderPipeline _renderPipeline)
    {
        return new GhostRenderPayload((GhostRenderPipeline)_renderPipeline);
    }
}
