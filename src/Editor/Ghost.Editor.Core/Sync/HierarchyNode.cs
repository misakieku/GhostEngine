using Ghost.Entities;
using System.Reflection;

namespace Ghost.Editor.Core.Sync;

public sealed class EntityNode
{
    public int Id { get; }
    public Guid EditorGuid { get; set; } = Guid.NewGuid();
    public string Name { get; set; }
    public int? ParentId { get; set; }
    public Entity EcsEntity { get; set; } = Entity.Invalid;
    public IReadOnlyList<ComponentNode> Components { get; set; }

    public EntityNode(int id, string name, IReadOnlyList<ComponentNode> components, int? parentId = null)
    {
        Id = id;
        Name = name;
        Components = components;
        ParentId = parentId;
    }
}

public sealed class ComponentNode
{
    public string TypeName { get; }
    public Type ComponentType { get; }
    public EntityNode EntityNode { get; set; }
    public IReadOnlyList<PropertyNode> Properties { get; set; }
    public object? LiveValue { get; set; }

    public ComponentNode(string typeName, Type componentType, EntityNode entityNode, IReadOnlyList<PropertyNode> properties, object? liveValue = null)
    {
        TypeName = typeName;
        ComponentType = componentType;
        EntityNode = entityNode;
        Properties = properties;
        LiveValue = liveValue;
    }
}

public abstract class PropertyNode
{
    public string Name { get; }
    public string DisplayName { get; }
    public Type PropertyType { get; }
    public ComponentNode ComponentNode { get; set; }
    public object? Value { get; set; }
    public bool IsReadOnly { get; }
    public string? GroupName { get; }
    public Action<object?> OnChange { get; }

    protected PropertyNode(string name, Type propertyType, ComponentNode componentNode, object? value, Action<object?> onChange, bool isReadOnly = false, string? displayName = null, string? groupName = null)
    {
        Name = name;
        DisplayName = displayName ?? name;
        PropertyType = propertyType;
        ComponentNode = componentNode;
        Value = value;
        IsReadOnly = isReadOnly;
        GroupName = groupName;
        OnChange = onChange;
    }

    /// <summary>
    /// Applies an authoring or command value update to this node and propagates the runtime mutation to ECS.
    /// </summary>
    public abstract void ApplyValue(object? newValue);

    /// <summary>
    /// Refreshes this node from live ECS component data without clobbering authoring-only metadata (such as asset GUIDs).
    /// </summary>
    public virtual void RefreshFromEcs(object boxedComponent, FieldInfo? field)
    {
        if (field != null)
        {
            Value = field.GetValue(boxedComponent);
        }
    }

    /// <summary>
    /// Returns the authoring value to persist during scene serialization (never ephemeral runtime handles).
    /// </summary>
    public abstract object? GetSerializableValue();

    /// <summary>
    /// Completes any deferred async asset resolution once underlying streaming entries become ready.
    /// </summary>
    public virtual bool TryCompletePendingResolution()
    {
        return false;
    }

    /// <summary>
    /// Releases any runtime or streaming asset references held by this property node.
    /// </summary>
    public virtual void ReleaseResources()
    {
    }

    /// <summary>
    /// Creates a deep copy of this property node bound to <paramref name="targetComponent"/>.
    /// </summary>
    public abstract PropertyNode Clone(ComponentNode targetComponent);
}
