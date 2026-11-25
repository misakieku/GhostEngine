using Ghost.Entities;

namespace Ghost.Editor.Core.Sync;

/// <summary>
/// Tracks specific entities modified by editor actions (gizmos, Inspector, commands) so edit-mode systems update only those entities rather than dirtying entire chunks.
/// </summary>
public sealed class EditorDirtyTracker
{
    private readonly object _lock = new();
    private readonly List<Entity> _dirtyTransforms = new(16);
    private readonly List<Entity> _dirtyGpuInstances = new(16);

    /// <summary>
    /// Marks <paramref name="entity"/> as needing a world transform recomputation (and GPU instance sync for it and descendants).
    /// </summary>
    public void MarkTransformDirty(Entity entity)
    {
        if (!entity.IsValid)
        {
            return;
        }

        lock (_lock)
        {
            if (!_dirtyTransforms.Contains(entity))
            {
                _dirtyTransforms.Add(entity);
            }
        }
    }

    /// <summary>
    /// Marks <paramref name="entity"/> as needing a GPU scene instance update.
    /// </summary>
    public void MarkGpuInstanceDirty(Entity entity)
    {
        if (!entity.IsValid)
        {
            return;
        }

        lock (_lock)
        {
            if (!_dirtyGpuInstances.Contains(entity))
            {
                _dirtyGpuInstances.Add(entity);
            }
        }
    }

    /// <summary>
    /// Drains all entities pending transform updates into <paramref name="output"/> without shrinking backing capacity.
    /// </summary>
    public void DrainTransforms(List<Entity> output)
    {
        lock (_lock)
        {
            for (var i = 0; i < _dirtyTransforms.Count; i++)
            {
                output.Add(_dirtyTransforms[i]);
            }

            _dirtyTransforms.Clear();
        }
    }

    /// <summary>
    /// Drains all entities pending GPU scene instance updates into <paramref name="output"/> without shrinking backing capacity.
    /// </summary>
    public void DrainGpuInstances(List<Entity> output)
    {
        lock (_lock)
        {
            for (var i = 0; i < _dirtyGpuInstances.Count; i++)
            {
                output.Add(_dirtyGpuInstances[i]);
            }

            _dirtyGpuInstances.Clear();
        }
    }
}
