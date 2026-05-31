namespace Ghost.Core;

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

    protected GhostObject()
    {
        InstanceID = Guid.NewGuid();
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
