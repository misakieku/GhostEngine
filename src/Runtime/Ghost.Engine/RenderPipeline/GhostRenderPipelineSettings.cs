using Ghost.Core;
using Ghost.Engine.Components;
using Ghost.Graphics;
using Ghost.Graphics.Core;
using Misaki.HighPerformance.LowLevel;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
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

    private readonly UnsafeParallelQueue<UpdateInstanceRequest>* _pUpdateRequest;
    private readonly UnsafeParallelQueue<RemoveInstanceRequest>* _pRemoveRequest;

    private readonly UnsafeParallelQueue<UpdateInstanceRequest>.ParallelProducer _updateRequestProducer;
    private readonly UnsafeParallelQueue<RemoveInstanceRequest>.ParallelProducer _removeRequestProducer;

    private uint _instanceCountBefore;
    private uint _instanceCount;

    public ReadOnlySpan<RenderRequest> RenderRequests => _renderRequests;

    public UnsafeParallelQueue<UpdateInstanceRequest>.ParallelConsumer UpdateRequest => _pUpdateRequest->AsParallelConsumer();
    public UnsafeParallelQueue<RemoveInstanceRequest>.ParallelConsumer RemoveRequest => _pRemoveRequest->AsParallelConsumer();
    public uint InstanceCountBefore => _instanceCountBefore;
    public uint InstanceCount => _instanceCount;

    internal GhostRenderPayload(GhostRenderPipeline renderPipeline)
    {
        _renderPipeline = renderPipeline;

        _renderRequests = new UnsafeList<RenderRequest>(4, AllocationHandle.Persistent);

        _pUpdateRequest = (UnsafeParallelQueue<UpdateInstanceRequest>*)MemoryUtility.Malloc(MemoryUtility.SizeOf<UnsafeParallelQueue<UpdateInstanceRequest>>());
        _pRemoveRequest = (UnsafeParallelQueue<RemoveInstanceRequest>*)MemoryUtility.Malloc(MemoryUtility.SizeOf<UnsafeParallelQueue<RemoveInstanceRequest>>());
        *_pUpdateRequest = new UnsafeParallelQueue<UpdateInstanceRequest>(16, AllocationHandle.Persistent);
        *_pRemoveRequest = new UnsafeParallelQueue<RemoveInstanceRequest>(16, AllocationHandle.Persistent);

        _updateRequestProducer = _pUpdateRequest->AsParallelProducer();
        _removeRequestProducer = _pRemoveRequest->AsParallelProducer();
    }

    public void AddRenderRequest(scoped in RenderRequest renderRequest)
    {
        _renderRequests.Add(renderRequest);
    }

    /// <summary>
    /// Adds a new instance to the persistent GPU scene and enqueues an update request for it.
    /// </summary>
    /// <remarks>
    /// This method is thread-safe and can be called from multiple threads.
    /// </remarks>
    /// <param name="ltw">The local to world transformation matrix for the instance.</param>
    /// <param name="meshInstance">The mesh instance data for the new instance.</param>
    /// <returns></returns>
    public uint AddInstance(float4x4 ltw, scoped in MeshInstance meshInstance)
    {
        var index = _renderPipeline.GPUScene.AddInstance();

        _updateRequestProducer.Enqueue(new UpdateInstanceRequest { instanceId = index, localToWorld = ltw, meshInstance = meshInstance });
        return index;
    }

    /// <summary>
    /// Updates an existing instance in the persistent GPU scene and enqueues an update request for it.
    /// </summary>
    /// <remarks>
    /// This method is thread-safe and can be called from multiple threads.
    /// </remarks>
    /// <param name="instanceId">The ID of the instance to be updated.</param>
    /// <param name="ltw">The new local to world transformation matrix for the instance.</param>
    /// <param name="meshInstance">The new mesh instance data for the instance.</param>
    public void UpdateInstance(uint instanceId, float4x4 ltw, scoped in MeshInstance meshInstance)
    {
        _updateRequestProducer.Enqueue(new UpdateInstanceRequest { instanceId = instanceId, localToWorld = ltw, meshInstance = meshInstance });
    }

    /// <summary>
    /// Removes an existing instance from the persistent GPU scene and enqueues a remove request for it.
    /// </summary>
    /// <remarks>
    /// This method is thread-safe and can be called from multiple threads.
    /// </remarks>
    /// <param name="instanceId">The ID of the instance to be removed.</param>
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
        Logger.DebugAssert(_instanceCount == _instanceCountBefore + (uint)_pUpdateRequest->Count - (uint)_pRemoveRequest->Count);
    }

    public void Reset()
    {
        _renderRequests.Clear();
        _pUpdateRequest->Clear();
        _pRemoveRequest->Clear();
    }

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _renderRequests.Dispose();
        _pUpdateRequest->Dispose();
        _pRemoveRequest->Dispose();

        MemoryUtility.Free(_pUpdateRequest);
        MemoryUtility.Free(_pRemoveRequest);
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
