using Ghost.Editor.Core.Assets;
using Ghost.Editor.Core.Contracts;

namespace Ghost.Editor.Core.Services;

internal class DirtyTrackerService : IDirtyTrackerService
{
    private readonly IUndoService _undoService;
    private readonly Dictionary<Guid, int> _cleanVersions = new();
    private readonly HashSet<GhostObject> _trackedObjects = new();

    public DirtyTrackerService(IUndoService undoService)
    {
        _undoService = undoService;
    }

    public void MarkDirty(GhostObject obj)
    {
        // When marked dirty, we just ensure it is tracked.
        // Its "clean version" remains whatever it was (or 0 if it was never saved).
        // If it was never saved and just got modified, its clean version is assumed to be 0 (or something that won't match GlobalVersion).

        if (!_cleanVersions.ContainsKey(obj.InstanceID))
        {
            // If we've never seen it, and it's being marked dirty, 
            // its "clean state" is whatever state existed BEFORE this edit (which caused GlobalVersion to increment).
            // Actually, if it's a brand new edit, UndoService will push an operation and increment GlobalVersion.
            // If the object was clean at the *current* version before the edit, we should record its clean version as (GlobalVersion - 1), 
            // but since UndoService.RecordObject increments GlobalVersion, the timing matters.
            // Let's just say its clean version is 0. If GlobalVersion is > 0, it will be dirty.
            _cleanVersions[obj.InstanceID] = _undoService.GlobalVersion - 1;
        }

        _trackedObjects.Add(obj);

        if (obj is Asset asset)
        {
            EditorApplication.GetService<IAssetRegistry>().SetAssetDirty(asset.ID);
        }
    }

    public bool IsDirty(GhostObject obj)
    {
        if (_cleanVersions.TryGetValue(obj.InstanceID, out var cleanVersion))
        {
            return cleanVersion != _undoService.GlobalVersion;
        }

        // If it's not tracked, it's clean.
        return false;
    }

    public void MarkClean(GhostObject obj)
    {
        _cleanVersions[obj.InstanceID] = _undoService.GlobalVersion;
        _trackedObjects.Add(obj);
    }

    public IReadOnlyList<GhostObject> GetDirtyObjects()
    {
        var dirtyObjects = new List<GhostObject>();

        // Remove dead references
        _trackedObjects.RemoveWhere(obj => GhostObject.Find(obj.InstanceID) == null);

        foreach (var obj in _trackedObjects)
        {
            if (IsDirty(obj))
            {
                dirtyObjects.Add(obj);
            }
        }

        return dirtyObjects;
    }
}
