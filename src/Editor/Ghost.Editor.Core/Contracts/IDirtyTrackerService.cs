using Ghost.Core;

namespace Ghost.Editor.Core.Contracts;

public interface IDirtyTrackerService
{
    /// <summary>
    /// Marks the specified object as dirty.
    /// </summary>
    void MarkDirty(GhostObject obj);

    /// <summary>
    /// Checks if the specified object is dirty compared to its clean state.
    /// </summary>
    bool IsDirty(GhostObject obj);

    /// <summary>
    /// Marks the specified object as clean (e.g., after a successful save).
    /// </summary>
    void MarkClean(GhostObject obj);

    /// <summary>
    /// Returns a list of all currently dirty objects.
    /// </summary>
    IReadOnlyList<GhostObject> GetDirtyObjects();
}
