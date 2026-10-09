using Ghost.Engine.Components;
using Ghost.Engine.Streaming;
using Ghost.Graphics;
using Ghost.Graphics.Core;
using Misaki.HighPerformance.Jobs;
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
    private UnsafeList<uint> _viewsToRelease;

    private UnsafeList<GPUDirectionalLight> _directionalLights;
    private int _primaryDirectionalLightIndex = -1;

    private readonly UnsafeParallelQueue<PunctualLightRequest>* _pPunctualLights;
    private readonly UnsafeParallelQueue<PunctualLightRequest>.ParallelProducer _punctualLightsProducer;

    private readonly UnsafeParallelQueue<UpdateInstanceRequest>* _pUpdateRequest;
    private readonly UnsafeParallelQueue<RemoveInstanceRequest>* _pRemoveRequest;

    private readonly UnsafeParallelQueue<UpdateInstanceRequest>.ParallelProducer _updateRequestProducer;
    private readonly UnsafeParallelQueue<RemoveInstanceRequest>.ParallelProducer _removeRequestProducer;

    private uint _instanceCountBefore;
    private uint _instanceCount;

    public ReadOnlySpan<RenderRequest> RenderRequests => _renderRequests;
    public UnsafeParallelQueue<PunctualLightRequest>.ParallelConsumer PunctualLights => _pPunctualLights->AsParallelConsumer();

    public ReadOnlyView<GPUDirectionalLight> DirectionalLights => _directionalLights;
    public uint DirectionalLightCount => (uint)_directionalLights.Count;
    public int PrimaryDirectionalLightIndex => _primaryDirectionalLightIndex;

    public bool HasDirectionalLight => _directionalLights.Count > 0;

    public UnsafeParallelQueue<UpdateInstanceRequest>.ParallelConsumer UpdateRequest => _pUpdateRequest->AsParallelConsumer();
    public UnsafeParallelQueue<RemoveInstanceRequest>.ParallelConsumer RemoveRequest => _pRemoveRequest->AsParallelConsumer();
    public uint InstanceCountBefore => _instanceCountBefore;
    public uint InstanceCount => _instanceCount;

    internal GhostRenderPayload(GhostRenderPipeline renderPipeline)
    {
        _renderPipeline = renderPipeline;

        _renderRequests = new UnsafeList<RenderRequest>(4, AllocationHandle.Persistent);
        _viewsToRelease = new UnsafeList<uint>(4, AllocationHandle.Persistent);
        _directionalLights = new UnsafeList<GPUDirectionalLight>(4, AllocationHandle.Persistent);

        _pPunctualLights = (UnsafeParallelQueue<PunctualLightRequest>*)MemoryUtility.Malloc(MemoryUtility.SizeOf<UnsafeParallelQueue<PunctualLightRequest>>());
        *_pPunctualLights = new UnsafeParallelQueue<PunctualLightRequest>(1024, AllocationHandle.Persistent);

        _pUpdateRequest = (UnsafeParallelQueue<UpdateInstanceRequest>*)MemoryUtility.Malloc(MemoryUtility.SizeOf<UnsafeParallelQueue<UpdateInstanceRequest>>());
        _pRemoveRequest = (UnsafeParallelQueue<RemoveInstanceRequest>*)MemoryUtility.Malloc(MemoryUtility.SizeOf<UnsafeParallelQueue<RemoveInstanceRequest>>());
        *_pUpdateRequest = new UnsafeParallelQueue<UpdateInstanceRequest>(1024, AllocationHandle.Persistent);
        *_pRemoveRequest = new UnsafeParallelQueue<RemoveInstanceRequest>(1024, AllocationHandle.Persistent);

        _punctualLightsProducer = _pPunctualLights->AsParallelProducer();
        _updateRequestProducer = _pUpdateRequest->AsParallelProducer();
        _removeRequestProducer = _pRemoveRequest->AsParallelProducer();
    }

    public void AddRenderRequest(scoped in RenderRequest renderRequest)
    {
        _renderRequests.Add(renderRequest);
    }

    public void AddPunctualLight(scoped in PunctualLightRequest request)
    {
        _punctualLightsProducer.Enqueue(request);
    }

    public uint AddDirectionalLight(scoped in GPUDirectionalLight light)
    {
        var index = (uint)_directionalLights.Count;
        _directionalLights.Add(light);
        return index;
    }

    public void SetPrimaryDirectionalLightIndex(int index)
    {
        _primaryDirectionalLightIndex = index;
    }

    public void SetDirectionalLight(scoped in GPUDirectionalLight light)
    {
        _directionalLights.Clear();
        _directionalLights.Add(light);
        _primaryDirectionalLightIndex = 0;
    }

    public uint AllocateView()
    {
        return _renderPipeline.GPUViewManager.AllocateView();
    }

    public void ReleaseView(uint viewId)
    {
        _viewsToRelease.Add(viewId);
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
    }

    public void Reset()
    {
        _renderRequests.Clear();
        _directionalLights.Clear();
        _primaryDirectionalLightIndex = -1;
        _pPunctualLights->Clear();
        _pUpdateRequest->Clear();
        _pRemoveRequest->Clear();

        for (var i = 0; i < _viewsToRelease.Count; i++)
        {
            _renderPipeline.GPUViewManager.ReleaseView(_viewsToRelease[i]);
        }
        _viewsToRelease.Clear();
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
        _viewsToRelease.Dispose();
        _directionalLights.Dispose();
        _pPunctualLights->Dispose();
        _pUpdateRequest->Dispose();
        _pRemoveRequest->Dispose();

        MemoryUtility.Free(_pPunctualLights);
        MemoryUtility.Free(_pUpdateRequest);
        MemoryUtility.Free(_pRemoveRequest);
    }
}

public enum RenderPipelineDebugMode : uint
{
    None = 0,
    Meshlet = 1 << 0,
    TileLightHeatmap = 1 << 1,
}


public class GhostRenderPipelineSettings : IRenderPipelineSettings
{
    /// <summary>
    /// Target screen-space pixel error threshold for meshlet DAG LOD refinement. Default is 2.0f.
    /// </summary>
    /// <remarks>
    /// This value controls the level of detail for meshlet rendering. A lower value results in higher detail (more meshlets), while a higher value results in lower detail (fewer meshlets). Adjust this value based on performance and visual quality requirements.
    /// </remarks>
    public float MeshletLodErrorThreshold { get; set; } = 2.0f;

    /// <summary>
    /// The screen percentage threshold for instance culling. Default is 2.0f.
    /// </summary>
    /// <remarks>
    /// This value controls the threshold for culling instance based on their size in screen-percentage. A lower value results in more aggressive culling (fewer instance rendered), while a higher value results in less aggressive culling (more instance rendered). Adjust this value based on performance and visual quality requirements.
    /// </remarks>
    public float InstanceCullingThreshold { get; set; } = 2.0f;

    /// <summary>
    /// Maximum number of visible meshlets on screen. Default is 2,097,152 (2^21). Maximum is 16,777,216 (2^24).
    /// </summary>
    /// <remarks>
    /// This value controls the maximum number of meshlets that can be visible on screen at any given time. Adjust this value based on performance and visual quality requirements.
    /// If the number of visible meshlets exceeds this limit, some meshlets may be culled or not rendered and cause visual artifacts. It is recommended to set this value based on the expected scene complexity and the capabilities of the target hardware.
    /// </remarks>
    public uint MaxVisibleMeshletsOnScreen { get; set; } = 2_097_152;

    public RenderPipelineDebugMode DebugMode { get; set; } = RenderPipelineDebugMode.None;

    /// <summary>
    /// Resolution of the primary directional shadow map Texture2DArray (per cascade slice). Default is 2048.
    /// </summary>
    public uint DirectionalShadowResolution { get; set; } = 2048;

    /// <summary>
    /// Number of Cascaded Shadow Map (CSM) splits for the primary directional light (1 to 4). Default is 4.
    /// </summary>
    public uint DirectionalShadowCascades { get; set; } = 4;

    /// <summary>
    /// Maximum shadow distance in meters for directional cascaded shadows. Default is 150.0m.
    /// </summary>
    public float DirectionalShadowDistance { get; set; } = 150.0f;

    /// <summary>
    /// Blend factor between logarithmic and linear cascade splits. Default is 0.85f.
    /// </summary>
    public float DirectionalShadowSplitLambda { get; set; } = 0.85f;

    /// <summary>
    /// Resolution of the shadow atlas for punctual lights (point and spot lights). Default is 4096.
    /// </summary>
    public uint ShadowAtlasResolution { get; set; } = 4096;

    public IRenderPipeline CreatePipeline(RenderEngine renderEngine, AssetManager assetManager, JobScheduler jobScheduler)
    {
        return new GhostRenderPipeline(renderEngine, assetManager, jobScheduler, this);
    }
}
