using Ghost.Core;
using Ghost.Engine.Components;
using Ghost.Graphics;
using Ghost.Graphics.Core;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.Mathematics;
using System.Collections.Concurrent;

namespace Ghost.Engine.RenderPipeline;

internal sealed class GhostRenderPayload : IRenderPayload
{
    public struct AddInstanceRequest
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

    private readonly ConcurrentQueue<AddInstanceRequest> _addRequest;
    private readonly ConcurrentQueue<RemoveInstanceRequest> _removeRequest;

    private uint _instanceCountBefore;
    private uint _instanceCount;

    public ReadOnlySpan<RenderRequest> RenderRequests => _renderRequests;

    public ConcurrentQueue<AddInstanceRequest> AddRequest => _addRequest;
    public ConcurrentQueue<RemoveInstanceRequest> RemoveRequest => _removeRequest;
    public uint InstanceCountBefore => _instanceCountBefore;
    public uint InstanceCount => _instanceCount;

    public GhostRenderPayload(GhostRenderPipeline renderPipeline)
    {
        _renderPipeline = renderPipeline;

        _renderRequests = new UnsafeList<RenderRequest>(4, Misaki.HighPerformance.LowLevel.Buffer.AllocationHandle.Persistent);
        _addRequest = new ConcurrentQueue<AddInstanceRequest>();
        _removeRequest = new ConcurrentQueue<RemoveInstanceRequest>();
    }

    // NOTE: This is not thread safe.
    public void AddRenderRequest(ref readonly RenderRequest renderRequest)
    {
        _renderRequests.Add(renderRequest);
    }

    public uint AddInstance(float4x4 ltw, ref readonly MeshInstance meshInstance)
    {
        var index = _renderPipeline.GPUScene.AddInstance();

        _addRequest.Enqueue(new AddInstanceRequest { instanceId = index, localToWorld = ltw, meshInstance = meshInstance });
        return index;
    }

    public void RemoveInstance(uint instanceId)
    {
        var swapWithInstanceId = _renderPipeline.GPUScene.RemoveInstance(instanceId);
        if (swapWithInstanceId != uint.MaxValue)
        {
            _removeRequest.Enqueue(new RemoveInstanceRequest { instanceId = instanceId, swapWithInstanceId = swapWithInstanceId });
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
        Logger.DebugAssert(_instanceCount == _instanceCountBefore + (uint)_addRequest.Count - (uint)_removeRequest.Count);
    }

    public void Reset()
    {
        _renderRequests.Clear();
        _addRequest.Clear();
        _removeRequest.Clear();
    }

    public void Dispose()
    {
        _renderRequests.Dispose();
    }
}

internal class GhostRenderPipelineSettings : IRenderPipelineSettings
{
    public IRenderPipeline CreatePipeline(RenderSystem renderSystem)
    {
        return new GhostRenderPipeline(renderSystem);
    }

    public IRenderPayload CreatePayload(RenderSystem renderSystem, IRenderPipeline _renderPipeline)
    {
        return new GhostRenderPayload((GhostRenderPipeline)_renderPipeline);
    }
}
