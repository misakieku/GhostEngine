using Ghost.Core;
using Ghost.Engine.Components;
using Ghost.Engine.RenderPipeline;
using Ghost.Entities;
using Ghost.Graphics;
using Ghost.Graphics.Core;

namespace Ghost.Engine.Systems;

[UpdateAfter<AddGPUInstanceSystem>]
internal class CameraRenderSystem : SystemBase
{
    private RenderEngine _renderEngine = null!;
    private Identifier<EntityQuery> _cameraQueryID;

    protected override void OnInitialize(scoped in SystemAPI systemAPI)
    {
        _renderEngine = systemAPI.World.GetService<RenderEngine>();

        _cameraQueryID = QueryBuilder.New()
            .WithAll<Camera, LocalToWorld>()
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

            for (var i = 0; i < chunk.EntityCount; i++)
            {
                ref readonly var camera = ref cameras[i];
                ref readonly var localToWorld = ref localToWorlds[i];

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
                    depthTarget = camera.depthTarget
                };

                payload.AddRenderRequest(in request);
            }
        }
    }
}
