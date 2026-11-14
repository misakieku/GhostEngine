using Ghost.Core;
using Ghost.Engine.Components;
using Ghost.Engine.RenderPipeline;
using Ghost.Engine.Systems;
using Ghost.Engine.Volumes;
using Ghost.Entities;
using Ghost.Graphics;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Ghost.Studio.Rendering;

namespace Ghost.Studio.Systems;

/// <summary>
/// Resizes the offscreen Scene View target when requested and submits the <see cref="EditorCamera"/> singleton view to <see cref="GhostRenderPayload"/>.
/// </summary>
[UpdateAfter<AddGPUInstanceSystem>]
[UpdateAfter<AddGPUViewSystem>]
[UpdateAfter<TransformSystem>]
[UpdateBefore<LightGatherSystem>]
internal sealed class EditorCameraRenderSystem : SystemBase
{
    private readonly SceneViewportTarget _viewportTarget;
    private RenderEngine _renderEngine = null!;
    private Identifier<EntityQuery> _volumeQueryID;

    public EditorCameraRenderSystem(SceneViewportTarget viewportTarget)
    {
        _viewportTarget = viewportTarget;
    }

    protected override void OnInitialize(scoped in SystemAPI systemAPI)
    {
        _renderEngine = systemAPI.World.GetService<RenderEngine>();

        _volumeQueryID = QueryBuilder.New()
            .WithAll<RenderVolume, RenderVolumeDataContainer>()
            .Build(systemAPI.World);
    }

    protected override void OnUpdate(scoped in SystemUpdateAPI systemAPI)
    {
        if (!_viewportTarget.EnsureTexture(_renderEngine))
        {
            return;
        }

        var em = systemAPI.World.EntityManager;
        ref var editorCam = ref em.GetOrCreateSingleton(EditorCamera.Default);

        var payload = (GhostRenderPayload)_renderEngine.GetCurrentFramePayload(systemAPI.Time.FrameIndex);
        payload.DeltaTime = systemAPI.Time.DeltaTime;

        if (editorCam.viewId == uint.MaxValue)
        {
            editorCam.viewId = payload.AllocateView();
        }

        editorCam.camera.swapChainIndex = -1;
        editorCam.camera.colorTarget = _viewportTarget.TextureHandle;
        editorCam.camera.depthTarget = Handle<GPUTexture>.Invalid;

        ref var volumeQuery = ref systemAPI.World.ComponentManager.GetEntityQueryReference(_volumeQueryID);
        var camPos = editorCam.localToWorld.c3.xyz;
        RenderVolumeSystem.EvaluateVolumesForView(em, ref volumeQuery, in camPos, editorCam.camera.renderingLayerMask, editorCam.viewId, payload);

        var renderView = new RenderView
        {
            localToWorld = editorCam.localToWorld,
            nearClipPlane = editorCam.camera.nearClipPlane,
            farClipPlane = editorCam.camera.farClipPlane,
            sensorSize = editorCam.camera.sensorSize,
            gateFit = editorCam.camera.gateFit,
            iso = editorCam.camera.iso,
            shutterSpeed = editorCam.camera.shutterSpeed,
            aperture = editorCam.camera.aperture,
            focalLength = editorCam.camera.focalLength,
            focusDistance = editorCam.camera.focusDistance,
            renderingLayerMask = editorCam.camera.renderingLayerMask,
        };

        var request = new RenderRequest
        {
            view = renderView,
            swapChainIndex = -1,
            colorTarget = _viewportTarget.TextureHandle,
            depthTarget = Handle<GPUTexture>.Invalid,
            viewId = editorCam.viewId,
        };

        payload.AddRenderRequest(in request);
    }
}
