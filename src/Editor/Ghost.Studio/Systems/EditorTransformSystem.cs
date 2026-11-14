using Ghost.Editor.Core.Sync;
using Ghost.Engine.Systems;
using Ghost.Engine.Utilities;
using Ghost.Entities;

namespace Ghost.Studio.Systems;

/// <summary>
/// Recomputes <see cref="Ghost.Engine.Components.LocalToWorld"/> only for entities explicitly marked dirty by editor edits (and their descendants).
/// </summary>
[UpdateBefore<EditorCameraRenderSystem>]
[UpdateBefore<EditorUpdateGPUInstanceSystem>]
[UpdateBefore<AddGPUInstanceSystem>]
[UpdateBefore<LightGatherSystem>]
internal sealed class EditorTransformSystem : SystemBase
{
    private readonly List<Entity> _pendingTransforms = new(16);
    private readonly List<Entity> _touchedEntities = new(32);
    private EditorDirtyTracker? _dirtyTracker;

    protected override void OnInitialize(scoped in SystemAPI systemAPI)
    {
        if (systemAPI.World.TryGetService<EditorDirtyTracker>(out var tracker))
        {
            _dirtyTracker = tracker;
        }
    }

    protected override void OnUpdate(scoped in SystemUpdateAPI systemAPI)
    {
        var tracker = _dirtyTracker ?? EditorSyncBridge.Instance.DirtyTracker;
        _pendingTransforms.Clear();
        tracker.DrainTransforms(_pendingTransforms);
        if (_pendingTransforms.Count == 0)
        {
            return;
        }

        var em = systemAPI.World.EntityManager;
        _touchedEntities.Clear();

        for (var i = 0; i < _pendingTransforms.Count; i++)
        {
            TransformUtility.UpdateEntityAndDescendants(em, _pendingTransforms[i], _touchedEntities);
        }

        for (var i = 0; i < _touchedEntities.Count; i++)
        {
            tracker.MarkGpuInstanceDirty(_touchedEntities[i]);
        }

        _pendingTransforms.Clear();
        _touchedEntities.Clear();
    }
}
