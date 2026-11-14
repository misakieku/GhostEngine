using Ghost.Editor.Core.Sync;
using Ghost.Engine.Components;
using Ghost.Engine.RenderPipeline;
using Ghost.Engine.Systems;
using Ghost.Entities;
using Ghost.Graphics;

namespace Ghost.Studio.Systems;

/// <summary>
/// Pushes GPU scene instance updates only for entities marked dirty by editor transform or mesh edits.
/// </summary>
[UpdateAfter<EditorTransformSystem>]
[UpdateAfter<RemoveGPUInstanceSystem>]
[UpdateBefore<AddGPUInstanceSystem>]
[UpdateBefore<EditorCameraRenderSystem>]
internal sealed class EditorUpdateGPUInstanceSystem : SystemBase
{
    private readonly List<Entity> _pendingGpuInstances = new(32);
    private RenderEngine _renderEngine = null!;
    private EditorDirtyTracker? _dirtyTracker;

    protected override void OnInitialize(scoped in SystemAPI systemAPI)
    {
        _renderEngine = systemAPI.World.GetService<RenderEngine>();
        if (systemAPI.World.TryGetService<EditorDirtyTracker>(out var tracker))
        {
            _dirtyTracker = tracker;
        }
    }

    protected override void OnUpdate(scoped in SystemUpdateAPI systemAPI)
    {
        var tracker = _dirtyTracker ?? EditorSyncBridge.Instance.DirtyTracker;
        _pendingGpuInstances.Clear();
        tracker.DrainGpuInstances(_pendingGpuInstances);
        if (_pendingGpuInstances.Count == 0)
        {
            return;
        }

        var payload = (GhostRenderPayload)_renderEngine.GetCurrentFramePayload(systemAPI.Time.FrameIndex);
        var em = systemAPI.World.EntityManager;

        for (var i = 0; i < _pendingGpuInstances.Count; i++)
        {
            var entity = _pendingGpuInstances[i];
            if (!entity.IsValid || !em.Exists(entity))
            {
                continue;
            }

            if (!em.HasComponent<LocalToWorld>(entity) || !em.HasComponent<MeshInstance>(entity) || !em.HasComponent<GPUInstanceRef>(entity))
            {
                continue;
            }

            ref readonly var ltw = ref em.GetComponent<LocalToWorld>(entity);
            ref readonly var mesh = ref em.GetComponent<MeshInstance>(entity);
            ref readonly var instance = ref em.GetComponent<GPUInstanceRef>(entity);

            payload.UpdateInstance(instance.gpuInstanceIndex, ltw.matrix, in mesh);
        }

        _pendingGpuInstances.Clear();
    }
}
