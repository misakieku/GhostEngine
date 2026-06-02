using Ghost.Editor.Core.Contracts;

namespace Ghost.Editor.Core;

/// <summary>
/// The base class for all objects that can be tracked and recorded by the Undo system.
/// </summary>
public abstract class GhostObject : IDisposable
{
    /// <summary>
    /// A persistent unique identifier used to track this object across Undo/Redo operations,
    /// even if the underlying object is destroyed and resurrected.
    /// </summary>
    public Guid InstanceID { get; protected set; }

    // Use WeakReference so we don't prevent Garbage Collection of dead objects
    private static readonly Dictionary<Guid, WeakReference<GhostObject>> s_objectRegistry = new();

    public static event Action<GhostObject>? OnObjectModified;

    protected GhostObject()
    {
        InstanceID = Guid.NewGuid();
        s_objectRegistry[InstanceID] = new WeakReference<GhostObject>(this);
    }

    protected GhostObject(Guid instanceID)
    {
        InstanceID = instanceID;
        s_objectRegistry[InstanceID] = new WeakReference<GhostObject>(this);
    }

    /// <summary>
    /// Resolves a GhostObject by its InstanceID in O(1) time.
    /// </summary>
    public static GhostObject? Find(Guid id)
    {
        if (s_objectRegistry.TryGetValue(id, out var weakRef))
        {
            if (weakRef.TryGetTarget(out var obj))
            {
                return obj;
            }
            else
            {
                // Dead object, GC has collected it
                s_objectRegistry.Remove(id);
            }
        }
        return null;
    }

    /// <summary>
    /// Called before mutating state.
    /// Hooks into the Undo and Dirty Tracking systems.
    /// </summary>
    public virtual void Modify()
    {
        OnObjectModified?.Invoke(this);

        // TODO: Unify RecordObject in future sessions. For now, we skip IUndoService.RecordObject here
        // since specialized methods are still required in UndoService.

        // Mark dirty for persistence directly
        EditorApplication.GetService<IDirtyTrackerService>().MarkDirty(this);
    }

    /// <summary>
    /// Serializes the state of this object into a binary format.
    /// </summary>
    public virtual void SerializeState(BinaryWriter writer)
    {
    }

    /// <summary>
    /// Deserializes the state of this object from a binary format.
    /// </summary>
    public virtual void DeserializeState(BinaryReader reader)
    {
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            s_objectRegistry.Remove(InstanceID);
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~GhostObject()
    {
        Dispose(false);
    }
}
