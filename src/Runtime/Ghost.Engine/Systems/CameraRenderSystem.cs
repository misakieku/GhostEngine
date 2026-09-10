using Ghost.Core;
using Ghost.Engine.Components;
using Ghost.Engine.RenderPipeline;
using Ghost.Entities;
using Ghost.Graphics;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;

namespace Ghost.Engine.Systems;

[UpdateAfter<AddGPUInstanceSystem>]
[UpdateAfter<AddGPUViewBufferSystem>]
internal class CameraRenderSystem : SystemBase
{
    private RenderEngine _renderEngine = null!;
    private Identifier<EntityQuery> _cameraQueryID;

    protected override void OnInitialize(scoped in SystemAPI systemAPI)
    {
        _renderEngine = systemAPI.World.GetService<RenderEngine>();

        _cameraQueryID = QueryBuilder.New()
            .WithAll<Camera, LocalToWorld, GPUViewBufferContainer>()
            .Build(systemAPI.World, true);

        RequireQueryForUpdate(_cameraQueryID);
    }

    protected override void OnUpdate(scoped in SystemAPI systemAPI)
    {
        var payload = (GhostRenderPayload)_renderEngine.GetCurrentFramePayload(systemAPI.Time.FrameIndex);

        ref var cameraQuery = ref systemAPI.World.ComponentManager.GetEntityQueryReference(_cameraQueryID);

        foreach (var chunk in cameraQuery.GetChunkIterator())
        {
            var cameras = chunk.GetComponentData<Camera>();
            var localToWorlds = chunk.GetComponentData<LocalToWorld>();
            var viewContainers = chunk.GetComponentDataRW<GPUViewBufferContainer>();

            for (var i = 0; i < chunk.EntityCount; i++)
            {
                ref readonly var camera = ref cameras[i];
                ref readonly var localToWorld = ref localToWorlds[i];
                ref var container = ref viewContainers[i];

                uint currentWidth = 0;
                uint currentHeight = 0;
                if (camera.swapChainIndex >= 0 && _renderEngine.SwapChainManager.TryGetSwapChain(camera.swapChainIndex, out var sc))
                {
                    currentWidth = sc.Width;
                    currentHeight = sc.Height;
                    _renderEngine.SwapChainManager.ReleaseSwapChain(camera.swapChainIndex);
                }
                else if (camera.colorTarget.IsValid)
                {
                    var descRes = _renderEngine.GraphicsEngine.ResourceDatabase.GetResourceDescription(camera.colorTarget.AsResource());
                    if (descRes.IsSuccess)
                    {
                        currentWidth = descRes.Value.TextureDescriptor.Width;
                        currentHeight = descRes.Value.TextureDescriptor.Height;
                    }
                }

                var hasValidHistory = container.isHistoryValid &&
                                      container.historyWidth == currentWidth &&
                                      container.historyHeight == currentHeight &&
                                      currentWidth > 0 && currentHeight > 0;

                var renderView = new RenderView
                {
                    localToWorld = localToWorld.matrix,
                    nearClipPlane = camera.nearClipPlane,
                    farClipPlane = camera.farClipPlane,
                    sensorSize = camera.sensorSize,
                    gateFit = camera.gateFit,
                    iso = camera.iso,
                    shutterSpeed = camera.shutterSpeed,
                    aperture = camera.aperture,
                    focalLength = camera.focalLength,
                    focusDistance = camera.focusDistance,
                    renderingLayerMask = camera.renderingLayerMask
                };

                var request = new RenderRequest
                {
                    view = renderView,
                    swapChainIndex = camera.swapChainIndex,
                    colorTarget = camera.colorTarget,
                    depthTarget = container.depthTarget,
                    hzbHistory = container.hzbHistory,
                    hzbMipCount = container.hzbMipCount,
                    historyWidth = container.historyWidth,
                    historyHeight = container.historyHeight,
                    hasValidHistory = hasValidHistory
                };

                container.historyWidth = currentWidth;
                container.historyHeight = currentHeight;
                container.isHistoryValid = true;

                payload.AddRenderRequest(in request);
            }
        }
    }
    
    protected override void OnCleanup(scoped in SystemAPI systemAPI)
    {
        ref var cameraQuery = ref systemAPI.World.ComponentManager.GetEntityQueryReference(_cameraQueryID);
        foreach (var chunk in cameraQuery.GetChunkIterator())
        {
            var viewContainers = chunk.GetComponentDataRW<GPUViewBufferContainer>();
            for (var i = 0; i < chunk.EntityCount; i++)
            {
                ref var container = ref viewContainers[i];

                for (var m = 0; m < 16; m++)
                {
                    if (container.hzbHistory[m].IsValid)
                    {
                        _renderEngine.GraphicsEngine.ResourceDatabase.ReleaseResource(container.hzbHistory[m].AsResource());
                        container.hzbHistory[m] = default;
                    }
                }

                if (container.depthTarget.IsValid)
                {
                    _renderEngine.GraphicsEngine.ResourceDatabase.ReleaseResource(container.depthTarget.AsResource());
                    container.depthTarget = default;
                }
            }
        }
    }
}
