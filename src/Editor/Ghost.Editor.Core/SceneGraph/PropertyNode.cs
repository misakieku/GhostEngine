using Ghost.Editor.Core.Inspector;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Ghost.Editor.Core.SceneGraph;

/// <summary>
/// Represents a single property/field within a ComponentNode.
/// Handles ECS reading/writing as well as serialization overrides (like Guid metadata).
/// </summary>
public abstract class PropertyNode
{
    public PropertyDescriptor Descriptor { get; }
    public ComponentNode Parent { get; }
    public PropertyNode[]? Children { get; protected set; }

    protected PropertyNode(PropertyDescriptor descriptor, ComponentNode parent)
    {
        Descriptor = descriptor;
        Parent = parent;
    }

    /// <summary>
    /// Synchronize the cached value from the ECS backend.
    /// </summary>
    public abstract void SyncFromECS();

    /// <summary>
    /// Flush any dirty UI changes back to the ECS backend.
    /// </summary>
    public abstract void FlushToECS();

    // --- Serialization Hooks ---

    public virtual void SerializeOverride(JsonObject jsonRoot, object boxedComponent)
    {
    }

    public virtual void DeserializeOverride(JsonElement jsonRoot, object boxedComponent)
    {
    }

    public virtual void Validate(object boxedComponent)
    {
    }
}
